import { Chalk, type ChalkInstance } from "chalk";
import { globby } from "globby";
import { $, within, echo, cd } from "zx";
import path from "node:path";
import { verticals } from "./_meta.mts";
import fs from "node:fs";

const c = new Chalk({ level: 3 });

type SlnSpec = {
  readonly path: string;
  readonly name: string;
  readonly searchRoot: string;
};

const p = (p: string, fmt: ChalkInstance = c.yellow) =>
  fmt(path.relative(process.cwd(), p));

const globSlnFile = path.resolve("Altinn.Authorization.sln");
const srcDir = path.resolve("src");

const slnSpecs: SlnSpec[] = verticals.map((v) => ({
  path: path.resolve(v.path, `${v.name}.sln`),
  name: v.name,
  searchRoot: v.path,
}));
slnSpecs.unshift({
  path: globSlnFile,
  searchRoot: srcDir,
  name: "Altinn.Authorization",
});

for (const spec of slnSpecs) {
  const stat = fs.statSync(spec.path, { throwIfNoEntry: false });
  if (stat == null || !stat.isFile()) {
    echo(`Creating ${p(spec.path)}`);
    await within(async () => {
      $.cwd = path.dirname(spec.path);
      await $`dotnet new sln -n ${spec.name}`;
    });
  }
}

for (const spec of slnSpecs) {
  echo(`Updating ${p(spec.path)}`);
  await within(async () => {
    $.cwd = path.dirname(spec.path);
    const projects = (await globby(`**/*.*proj`, { cwd: spec.searchRoot })).map(
      (p) => path.resolve(spec.searchRoot, p).replaceAll("\\", "/")
    );

    await $`dotnet sln add ${projects}`;
  });
}
