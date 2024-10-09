import { globby } from "globby";
import path from "node:path";
import fs from "node:fs";

export type VerticalType = "app" | "lib" | "pkg";

export type ImageInfo = {
  readonly name: string;
};

export type Vertical = {
  readonly type: VerticalType;
  readonly name: string;
  readonly shortName: string;
  readonly path: string;
  readonly relPath: string;
  readonly image?: ImageInfo;
  readonly rawConfig: Readonly<Record<string, unknown>>;
};

const vertialDirs = {
  app: "src/apps",
  lib: "src/libs",
  pkg: "src/pkgs",
};

const last = (arr: string[]) => arr[arr.length - 1];

const readVertical = (type: VerticalType, dirPath: string): Vertical => {
  const verticalPath = path.resolve(dirPath);
  const dirName = path.basename(verticalPath);
  const configPath = path.resolve(verticalPath, "conf.json");

  let config: Readonly<Record<string, unknown>> = {};
  try {
    const json = fs.readFileSync(configPath, { encoding: "utf-8" });
    config = JSON.parse(json);
  } catch (e) {}

  let name = dirName;
  if (typeof config.name === "string" && config.name) {
    name = config.name;
  }

  let shortName = last(name.split("."));
  if (typeof config.shortName === "string" && config.shortName) {
    shortName = config.shortName;
  }

  let image: ImageInfo | undefined;
  if ("image" in config) {
    // TODO: validate?
    image = {
      name: (config.image as any).name,
    };
  }

  return {
    type,
    name,
    shortName,
    path: verticalPath,
    relPath: dirPath,
    image,
    rawConfig: config,
  };
};

const apps = await globby(`${vertialDirs.app}/*`, { onlyDirectories: true });
const libs = await globby(`${vertialDirs.lib}/*`, { onlyDirectories: true });
const pkgs = await globby(`${vertialDirs.pkg}/*`, { onlyDirectories: true });
const verticals = [
  ...apps.map((app) => readVertical("app", app)),
  ...libs.map((lib) => readVertical("lib", lib)),
  ...pkgs.map((pkg) => readVertical("pkg", pkg)),
];

export { verticals };
