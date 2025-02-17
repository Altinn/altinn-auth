import { Chalk, type ChalkInstance } from "chalk";
import { globby } from "globby";
import { $, within, echo, cd } from "zx";
import path from "node:path";
import { verticals, type VerticalType, type Vertical } from "./_meta.mts";
import fs from "node:fs";
import yargs from "yargs";
import { hideBin } from "yargs/helpers";

const argv = yargs(hideBin(process.argv))
  .option("purge", {
    type: "boolean",
  })
  .option("debug", {
    type: "boolean",
  })
  .parse();

const c = new Chalk({ level: 3 });

type SlnSpec = {
  readonly dir: string;
  readonly name: string;
  readonly displayName: string;
  readonly type?: VerticalType;
  readonly tree: ProjectTree;
  readonly vertical?: Vertical;
};

type ProjectTree = ProjectTreeNode[];
type ProjectTreeNode = ProjectTreeDir | ProjectTreeItem;
type ProjectTreeDir = {
  readonly type: "dir";
  readonly name: string;
  readonly children: ProjectTree;
};
type ProjectTreeItem = {
  readonly type: "item";
  readonly name: string;
  readonly diskPath: string;
};

const depTypeDirName = {
  lib: "libs",
  pkg: "pkgs",
};

const rootSln: SlnSpec = {
  dir: path.resolve("."),
  name: "Altinn.Authorization",
  displayName: "Altinn.Authorization",
  tree: [],
};

const verticalSlns: SlnSpec[] = [];
const deps = new Map<string, ProjectTreeDir>();

for (const vertical of verticals) {
  let typeDir = rootSln.tree.find(
    (node) => node.type === "dir" && node.name === vertical.type
  ) as ProjectTreeDir;
  if (!typeDir) {
    typeDir = { type: "dir", name: vertical.type, children: [] };
    rootSln.tree.push(typeDir);
  }

  const verticalDir: ProjectTreeDir = {
    type: "dir",
    name: vertical.name,
    children: [],
  };
  typeDir.children.push(verticalDir);

  verticalSlns.push({
    dir: vertical.path,
    name: vertical.name,
    displayName: vertical.shortName,
    type: vertical.type,
    tree: verticalDir.children,
    vertical,
  });

  const srcDir: ProjectTreeDir = { type: "dir", name: "src", children: [] };
  verticalDir.children.push(srcDir);

  const testDir: ProjectTreeDir = { type: "dir", name: "test", children: [] };
  verticalDir.children.push(testDir);

  for (const srcProjects of vertical.projects.src) {
    srcDir.children.push({
      type: "item",
      name: srcProjects.name,
      diskPath: srcProjects.path,
    });
  }

  for (const testProjects of vertical.projects.test) {
    testDir.children.push({
      type: "item",
      name: testProjects.name,
      diskPath: testProjects.path,
    });
  }

  if (vertical.type === "lib" || vertical.type === "pkg") {
    deps.set(`${vertical.type}:${vertical.name}`, {
      type: "dir",
      name: vertical.name,
      children: srcDir.children,
    });
  }
}

for (const sln of verticalSlns) {
  for (const dep of sln.vertical!.deps) {
    const typeDirName = depTypeDirName[dep.type];
    let typeDir = sln.tree.find(
      (node) => node.type === "dir" && node.name === typeDirName
    ) as ProjectTreeDir;
    if (!typeDir) {
      typeDir = { type: "dir", name: typeDirName, children: [] };
      sln.tree.push(typeDir);
    }

    const depDir = deps.get(`${dep.type}:${dep.name}`);
    if (!depDir) {
      throw new Error(
        `Dependency ${dep.type}:${dep.name} of ${sln.name} not found`
      );
    }

    typeDir.children.push(depDir);
  }
}

const fileExists = (file: string) => {
  const stat = fs.statSync(file, { throwIfNoEntry: false });
  return stat?.isFile() ?? false;
};

const getSlnProjects = async (slnFile: string) => {
  const textOutput = await $`dotnet sln ${slnFile} list`.text();
  const items = textOutput
    .split("\n")
    .map((p) => p.trim().replaceAll("\\", "/"))
    .filter((p) => p.length > 0 && p.endsWith("proj"));

  return new Set(items);
};

type FlattenedTreeItem = {
  readonly name: string;
  readonly diskPath: string;
  readonly treeDir: string;
};

function* flattenTree(
  tree: ProjectTree,
  path: readonly string[] = []
): Generator<FlattenedTreeItem> {
  for (const item of tree) {
    if (item.type === "dir") {
      yield* flattenTree(item.children, [...path, item.name]);
    } else {
      yield {
        name: item.name,
        diskPath: item.diskPath.replaceAll("\\", "/"),
        treeDir: path.join("/"),
      };
    }
  }
}

for (const sln of [rootSln, ...verticalSlns]) {
  await within(async () => {
    const dir = sln.dir;
    $.cwd = dir;
    $.verbose = argv.debug;

    const p = (p: string, fmt: ChalkInstance = c.yellow) =>
      fmt(path.relative(dir, p).replaceAll("\\", "/"));

    const slnFile = path.resolve(dir, `${sln.name}.sln`).replaceAll("\\", "/");
    const display = sln.type
      ? `${c.cyan(sln.type)}: ${c.yellow(sln.displayName)}`
      : c.yellow(sln.displayName);

    echo(``);
    echo(`#############################################`);
    echo(`# ${display}`);

    if (argv.purge) {
      echo(`${c.red("~")} ${p(slnFile)}`);
      if (fileExists(slnFile)) {
        fs.unlinkSync(slnFile);
      }

      await $`dotnet new sln --name ${sln.name}`;
    } else if (!fileExists(slnFile)) {
      echo(`${c.magenta("!")} ${p(slnFile)}`);
      await $`dotnet new sln --name ${sln.name}`;
    }

    const existingProjects = await getSlnProjects(slnFile);
    for (const proj of existingProjects) {
      const projPath = path.resolve(dir, proj).replaceAll("\\", "/");
      echo(`${c.cyan("-")} ${p(projPath)}`);
    }

    let added = 0;
    for (const item of flattenTree(sln.tree)) {
      const relPath = path.relative(dir, item.diskPath).replaceAll("\\", "/");
      if (existingProjects.has(relPath)) {
        continue;
      }

      echo(`${c.green("+")} ${p(item.diskPath)}`);
      await $`dotnet sln ${slnFile} add ${item.diskPath} --solution-folder ${item.treeDir}`;
      added++;
    }
  });
}

// const slnSpecs: SlnSpec[] = verticals.map((v) => ({
//   path: path.resolve(v.path, `${v.name}.sln`),
//   name: v.name,
//   searchRoot: v.path,
//   isRoot: false,
// }));
// slnSpecs.unshift({
//   path: globSlnFile,
//   searchRoot: srcDir,
//   name: "Altinn.Authorization",
//   isRoot: true,
// });

// for (const spec of slnSpecs) {
//   const stat = fs.statSync(spec.path, { throwIfNoEntry: false });
//   if (stat == null || !stat.isFile()) {
//     echo(`Creating ${p(spec.path)}`);
//     await within(async () => {
//       $.cwd = path.dirname(spec.path);
//       await $`dotnet new sln -n ${spec.name}`;
//     });
//   }
// }

// for (const spec of slnSpecs) {
//   echo(`Updating ${p(spec.path)}`);
//   await within(async () => {
//     $.cwd = path.dirname(spec.path);
//     const projects = (await globby(`**/*.*proj`, { cwd: spec.searchRoot })).map(
//       (p) => path.resolve(spec.searchRoot, p).replaceAll("\\", "/")
//     );

//     await $`dotnet sln add ${projects}`;
//   });
// }
