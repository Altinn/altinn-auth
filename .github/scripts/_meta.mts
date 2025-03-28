import { globby } from "globby";
import path from "node:path";
import fs from "node:fs/promises";
import { z } from "zod";
import { fromError } from "zod-validation-error";
import { $, within, usePwsh } from "zx";
import slugify from "slugify";

if (process.platform === "win32") {
  usePwsh();
}

let _queue: Promise<void> = Promise.resolve();
const enqueue = <T extends unknown>(fn: () => Promise<T>): Promise<T> => {
  var task = _queue.then(() => within(fn));
  _queue = task.then(
    (_) => {},
    (_) => {}
  );
  return task;
};

const depSchema = z.string().transform((val, ctx) => {
  if (val.startsWith("lib:")) {
    const lib = val.substring(4);
    return { type: "lib", name: lib } as RawDep;
  }

  if (val.startsWith("pkg:")) {
    const pkg = val.substring(4);
    return { type: "pkg", name: pkg } as RawDep;
  }

  ctx.addIssue({
    code: z.ZodIssueCode.custom,
    message: `Invalid dependency: ${val}, must start with 'lib:' or 'pkg:'`,
  });

  return z.NEVER;
});

const dotnetImageSchema = z.object({
  type: z.literal("dotnet"),
  source: z.string().optional(),
  name: z.string().optional(),
});

const dockerImageSchema = z.object({
  type: z.literal("docker"),
  source: z.string().optional(),
  name: z.string().min(5),
});

const imageSchema = z
  .object({
    type: z.enum(["dotnet", "docker"]).default("dotnet"),
  })
  .passthrough()
  .pipe(z.discriminatedUnion("type", [dotnetImageSchema, dockerImageSchema]));

const terraformSchema = z.object({
  stateFile: z.string().min(10),
});

const infraSchema = z.object({
  terraform: terraformSchema.optional(),
});

const databaseSchema = z.object({
  bootstrap: z.boolean().optional().default(false),
  name: z.string().min(3).optional(),
  roleprefix: z.string().min(3).optional(),
  schema: z.any().optional(),
});

const sonarcloudSchema = z
  .boolean()
  .transform((val, ctx) => {
    return { enabled: val } as RawSonarcloud;
  })
  .or(
    z.object({
      enabled: z.boolean().default(true),
      projectKey: z.string().optional(),
    })
  );

const configSchema = z.object({
  name: z.string().optional(),
  shortName: z.string().optional(),
  image: imageSchema.optional(),
  infra: infraSchema.optional(),
  database: databaseSchema.optional(),
  deps: z.array(depSchema).default([]),
  sonarcloud: sonarcloudSchema.default({ enabled: true }),
});

export type DepType = "lib" | "pkg";

type RawDep = {
  readonly type: DepType;
  readonly name: string;
};

type RawSonarcloud = {
  readonly enabled: boolean;
  readonly projectKey?: string;
};

export type VerticalType = "app" | "lib" | "pkg" | "tool";

export type ImageInfo = {
  readonly name: string;
  readonly type: "dotnet" | "docker";
  readonly source: string;
};

export type TerraformInfo = {
  readonly stateFile: string;
};

export type InfraInfo = {
  readonly terraform?: TerraformInfo;
};

export type DatabaseInfo = {
  readonly bootstrap: boolean;
  readonly name: string;
  readonly roleprefix: string;
  readonly schema: object;
};

export type SonarcloudInfo =
  | {
      readonly enabled: false;
      readonly projectKey: undefined;
      readonly displayName: undefined;
    }
  | {
      readonly enabled: true;
      readonly projectKey: string;
      readonly displayName: string;
    };

type RawVertical = {
  readonly id: string;
  readonly slug: string;
  readonly displayName: string;
  readonly type: VerticalType;
  readonly name: string;
  readonly shortName: string;
  readonly path: string;
  readonly relPath: string;
  readonly image?: ImageInfo;
  readonly infra?: InfraInfo;
  readonly database?: DatabaseInfo;
  readonly projects: VerticalProjects;
  readonly sonarcloud: SonarcloudInfo;
  readonly deps: readonly RawDep[];
};

export type Vertical = {
  readonly id: string;
  readonly slug: string;
  readonly displayName: string;
  readonly type: VerticalType;
  readonly name: string;
  readonly shortName: string;
  readonly path: string;
  readonly relPath: string;
  readonly image?: ImageInfo;
  readonly infra?: InfraInfo;
  readonly database?: DatabaseInfo;
  readonly projects: VerticalProjects;
  readonly sonarcloud: SonarcloudInfo;
  readonly deps: readonly Vertical[];
};

export type VerticalProjects = Readonly<
  Record<ProjectType, readonly Project[]>
>;

export type ProjectType = "src" | "test";

export type Project = {
  readonly name: string;
  readonly path: string;
  readonly rootRelPath: string;
  readonly verticalRelPath: string;
  readonly type: ProjectType;
};

const vertialDirs = {
  app: "src/apps",
  lib: "src/libs",
  pkg: "src/pkgs",
  tool: "src/tools",
};

const last = (arr: string[]) => arr[arr.length - 1];

const readProjects = async (
  verticalPath: string,
  verticalRelPath: string,
  type: ProjectType
): Promise<readonly Project[]> => {
  const projectFiles = await globby(`${type}/*/*.*proj`, { cwd: verticalPath });
  return projectFiles.map((file) => {
    const ext = path.extname(file);
    const name = path.basename(file, ext);
    const filePath = path.resolve(verticalPath, file);
    const rootRelPath = path.join(verticalRelPath, file).replaceAll("\\", "/");

    return {
      name,
      path: filePath,
      rootRelPath,
      verticalRelPath: file,
      type,
    };
  });
};

const readVertical = async (
  type: VerticalType,
  dirPath: string
): Promise<RawVertical> => {
  const verticalPath = path.resolve(dirPath);
  const dirName = path.basename(verticalPath);
  const configPath = path.resolve(verticalPath, "conf.json");
  const projects = {
    src: await readProjects(verticalPath, dirPath, "src"),
    test: await readProjects(verticalPath, dirPath, "test"),
  };

  let parsed: any = {};
  try {
    const json = await fs.readFile(configPath, { encoding: "utf-8" });
    parsed = JSON.parse(json);
  } catch (e) {}

  const result = await configSchema.safeParseAsync(parsed);
  if (!result.success) {
    const error = fromError(result.error);
    console.error(`Error parsing ${configPath}: ${error.toString()}`);
    throw error;
  }

  const config = result.data;

  let name = dirName;
  if (config.name) {
    name = config.name;
  }

  let shortName = last(name.split("."));
  if (config.shortName) {
    shortName = config.shortName;
  }

  let image: ImageInfo | undefined = void 0;
  let infra: InfraInfo | undefined = void 0;
  let database: DatabaseInfo | undefined = void 0;
  if (type === "app") {
    const confImage = config.image ?? { type: "dotnet" };

    switch (confImage.type) {
      case "dotnet": {
        if (!confImage.source) {
          confImage.source = `src/${name}`;
        }

        if (!confImage.name) {
          confImage.name = await enqueue(async () => {
            $.cwd = verticalPath;
            const result =
              await $`dotnet msbuild ${confImage.source} -getProperty:ContainerName`;
            return result.stdout.trim();
          });
        }
        break;
      }

      case "docker": {
        if (!confImage.source) {
          confImage.source = "Dockerfile";
        }
        break;
      }

      default: {
        throw new Error(`Unsupported image type: ${(confImage as any).type}`);
      }
    }

    const confInfra = config.infra;
    if (confInfra) {
      infra = confInfra as InfraInfo;
    }

    const confDatabase = config.database;
    if (confDatabase) {
      database = confDatabase as DatabaseInfo;
    }

    image = confImage as ImageInfo;
  }

  const id = `${type}:${name}`;
  const displayName = `${type}: ${shortName}`;
  const slug = slugify(id.replaceAll(/[\.:]/g, "-"), { lower: true });

  const confSonarcloud = config.sonarcloud;
  const sonarcloud = confSonarcloud.enabled
    ? ({
        enabled: true,
        projectKey: confSonarcloud.projectKey ?? `authorization-${slug}`,
        displayName: `Authorization ${name}`,
      } as const)
    : ({ enabled: false } as const);

  return {
    id,
    slug,
    displayName,
    type,
    name,
    shortName,
    path: verticalPath,
    relPath: dirPath,
    image,
    infra,
    database,
    projects,
    sonarcloud,
    deps: config.deps,
  };
};

const apps = await globby(`${vertialDirs.app}/*`, { onlyDirectories: true });
const libs = await globby(`${vertialDirs.lib}/*`, { onlyDirectories: true });
const pkgs = await globby(`${vertialDirs.pkg}/*`, { onlyDirectories: true });
const tools = await globby(`${vertialDirs.tool}/*`, { onlyDirectories: true });
const promises = [
  ...apps.map((app) => readVertical("app", app)),
  ...libs.map((lib) => readVertical("lib", lib)),
  ...pkgs.map((pkg) => readVertical("pkg", pkg)),
  ...tools.map((tool) => readVertical("tool", tool)),
];
const rawVerticals = await Promise.all(promises);
rawVerticals.sort((a, b) => a.deps.length - b.deps.length);
const lookup = new Map<string, Vertical>();

// first pass, populate lookup
for (const raw of rawVerticals) {
  lookup.set(raw.id, { ...raw, deps: [] });
}

// second pass, direct deps
for (const raw of rawVerticals) {
  const vertical = lookup.get(raw.id)!;
  for (const dep of raw.deps) {
    const resolved = lookup.get(`${dep.type}:${dep.name}`);
    if (!resolved) {
      throw new Error(`Dependency not found: ${dep.type}:${dep.name}`);
    }

    (vertical.deps as Vertical[]).push(resolved);
  }
}

// third pass, transitive deps
for (const vertical of lookup.values()) {
  const todo = [vertical];
  const deps = vertical.deps as Vertical[];
  const seen = new Set<Vertical>();

  while (todo.length > 0) {
    const node = todo.pop()!;
    if (seen.has(node)) {
      continue;
    }

    seen.add(node);
    for (const dep of node.deps) {
      if (seen.has(dep)) {
        continue;
      }

      todo.push(dep);
      if (!deps.includes(dep)) {
        deps.push(dep);
      }
    }
  }
}

export const getApp = (name: string) => {
  const app = lookup.get(`app:${name}`);
  if (!app) {
    throw new Error(`App not found: ${name}`);
  }

  return app;
};

const verticals = [...lookup.values()];
verticals.sort((a, b) => (a.type < b.type ? -1 : 1));
export { verticals };
