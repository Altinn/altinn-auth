import { globby } from "globby";
import path from "node:path";
import fs from "node:fs/promises";
import { z } from "zod";
import { fromError } from "zod-validation-error";
import { $, within } from "zx";

let _queue: Promise<void> = Promise.resolve();
const enqueue = <T extends unknown>(fn: () => Promise<T>): Promise<T> => {
  var task = _queue.then(() => within(fn));
  _queue = task.then(
    (_) => {},
    (_) => {}
  );
  return task;
};

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
});

export type VerticalType = "app" | "lib" | "pkg";

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
};

const vertialDirs = {
  app: "src/apps",
  lib: "src/libs",
  pkg: "src/pkgs",
};

const last = (arr: string[]) => arr[arr.length - 1];

const readVertical = async (
  type: VerticalType,
  dirPath: string
): Promise<Vertical> => {
  const verticalPath = path.resolve(dirPath);
  const dirName = path.basename(verticalPath);
  const configPath = path.resolve(verticalPath, "conf.json");

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
  };
};

const apps = await globby(`${vertialDirs.app}/*`, { onlyDirectories: true });
const libs = await globby(`${vertialDirs.lib}/*`, { onlyDirectories: true });
const pkgs = await globby(`${vertialDirs.pkg}/*`, { onlyDirectories: true });
const promises = [
  ...apps.map((app) => readVertical("app", app)),
  ...libs.map((lib) => readVertical("lib", lib)),
  ...pkgs.map((pkg) => readVertical("pkg", pkg)),
];
const verticals = await Promise.all(promises);

export const getApp = (name: string) => {
  const app = verticals.find((v) => v.name === name);
  if (!app) {
    throw new Error(`App not found: ${name}`);
  }

  return app;
};

export { verticals };
