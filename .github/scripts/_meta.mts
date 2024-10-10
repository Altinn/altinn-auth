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

const configSchema = z.object({
  name: z.string().optional(),
  shortName: z.string().optional(),
  image: z
    .object({
      name: z.string().optional(),
      type: z.enum(["dotnet", "docker"]).default("dotnet"),
      source: z.string().optional(),
    })
    .refine((v) => {
      if (v.type === "docker" && !v.name) {
        return { message: "Image name is required for docker images" };
      }
    })
    .optional(),
});

export type VerticalType = "app" | "lib" | "pkg";

export type ImageInfo = {
  readonly name: string;
  readonly type: "dotnet" | "docker";
  readonly source: string;
};

export type Vertical = {
  readonly type: VerticalType;
  readonly name: string;
  readonly shortName: string;
  readonly path: string;
  readonly relPath: string;
  readonly image?: ImageInfo;
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
    const parsed = JSON.parse(json);
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

  let image: ImageInfo | undefined;
  if (type === "app") {
    $.cwd = verticalPath;
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
        throw new Error(`Unsupported image type: ${confImage.type}`);
      }
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
