import { globby } from "globby";
import path from "node:path";
import fs from "node:fs/promises";
import { z } from "zod";
import { fromError } from "zod-validation-error";
import { $, within, usePwsh } from "zx";

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
    return { type: "lib", name: lib } as Dep;
  }

  if (val.startsWith("pkg:")) {
    const pkg = val.substring(4);
    return { type: "pkg", name: pkg } as Dep;
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

const configSchema = z.object({
  name: z.string().optional(),
  shortName: z.string().optional(),
  image: imageSchema.optional(),
  infra: infraSchema.optional(),
  database: databaseSchema.optional(),
  deps: z.array(depSchema).default([]),
});

export type DepType = "lib" | "pkg";

export type Dep = {
  readonly type: DepType;
  readonly name: string;
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

export type Vertical = {
  readonly type: VerticalType;
  readonly name: string;
  readonly shortName: string;
  readonly path: string;
  readonly relPath: string;
  readonly image?: ImageInfo;
  readonly infra?: InfraInfo;
  readonly database?: DatabaseInfo;
  readonly projects: VerticalProjects;
  readonly deps: readonly Dep[];
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
): Promise<Vertical> => {
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

  return {
    type,
    name,
    shortName,
    path: verticalPath,
    relPath: dirPath,
    image,
    infra,
    database,
    projects,
    deps: config.deps,
  };
};

const validateDeps = (verticals: readonly Vertical[]) => {
  for (const vertical of verticals) {
    for (const dep of vertical.deps) {
      const found = verticals.find(
        (v) => v.name === dep.name && v.type === dep.type
      );
      if (!found) {
        throw new Error(
          `Dependency ${dep.type}:${dep.name} of ${vertical.name} not found`
        );
      }
    }
  }
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
const verticals = await Promise.all(promises);
validateDeps(verticals);

export const getApp = (name: string) => {
  const app = verticals.find((v) => v.name === name);
  if (!app) {
    throw new Error(`App not found: ${name}`);
  }

  return app;
};

export { verticals };
