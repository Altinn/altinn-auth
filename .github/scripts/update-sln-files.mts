import { Chalk, type ChalkInstance } from "chalk";
import { $, within, echo, retry } from "zx";
import path from "node:path";
import { verticals, type VerticalType, type Vertical } from "./_meta.mts";
import fs from "node:fs";
import { yargs } from "./_yargs.mts";

const argv = yargs()
  .option("purge", {
    type: "boolean",
  })
  .option("debug", {
    type: "boolean",
  })
  .option("slnx", {
    type: "boolean",
  })
  .parse();

const format = argv.slnx ? "slnx" : "sln";

const c = new Chalk({ level: 3 });

type SlnSpec = {
  readonly dir: string;
  readonly name: string;
  readonly displayName: string;
  readonly type?: VerticalType;
  readonly tree: ProjectTree;
  readonly vertical?: Vertical;
  hasProjects: boolean;
};

type ProjectTree = ProjectTreeNode[];
type ProjectTreeDirType =
  | VerticalType
  | "src"
  | "test"
  | `${VerticalType}-type`
  | "deps:libs-dir"
  | "deps:pkgs-dir"
  | "deps:lib"
  | "deps:pkg";
type ProjectTreeNode = ProjectTreeDir | ProjectTreeItem;
type ProjectTreeDir = {
  readonly type: "dir";
  readonly dirType: ProjectTreeDirType;
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
} as const;

const rootSln: SlnSpec = {
  dir: path.resolve("."),
  name: "Altinn.Authorization",
  displayName: "Altinn.Authorization",
  tree: [],
  hasProjects: true,
};

const verticalSlns: SlnSpec[] = [];
const deps = new Map<string, ProjectTreeDir>();

for (const vertical of verticals) {
  let typeDir = rootSln.tree.find(
    (node) => node.type === "dir" && node.name === vertical.type
  ) as ProjectTreeDir;
  if (!typeDir) {
    typeDir = {
      type: "dir",
      dirType: `${vertical.type}-type`,
      name: vertical.type,
      children: [],
    };
    rootSln.tree.push(typeDir);
  }

  const verticalDir: ProjectTreeDir = {
    type: "dir",
    dirType: vertical.type,
    name: vertical.name,
    children: [],
  };
  typeDir.children.push(verticalDir);

  var verticalSln = {
    dir: vertical.path,
    name: vertical.name,
    displayName: vertical.shortName,
    type: vertical.type,
    tree: verticalDir.children,
    vertical,
    hasProjects: false,
  } as SlnSpec;
  verticalSlns.push(verticalSln);

  const srcDir: ProjectTreeDir = {
    type: "dir",
    dirType: "src",
    name: "src",
    children: [],
  };
  verticalDir.children.push(srcDir);

  const testDir: ProjectTreeDir = {
    type: "dir",
    dirType: "test",
    name: "test",
    children: [],
  };
  verticalDir.children.push(testDir);

  for (const srcProjects of vertical.projects.src) {
    verticalSln.hasProjects = true;
    srcDir.children.push({
      type: "item",
      name: srcProjects.name,
      diskPath: srcProjects.path,
    });
  }

  for (const testProjects of vertical.projects.test) {
    verticalSln.hasProjects = true;
    testDir.children.push({
      type: "item",
      name: testProjects.name,
      diskPath: testProjects.path,
    });
  }

  if (vertical.type === "lib" || vertical.type === "pkg") {
    deps.set(`${vertical.type}:${vertical.name}`, {
      type: "dir",
      dirType: `deps:${vertical.type}`,
      name: vertical.name,
      children: srcDir.children,
    });
  }
}

for (const sln of verticalSlns) {
  for (const dep of sln.vertical!.deps) {
    const type = dep.type as "lib" | "pkg";
    const typeDirName = depTypeDirName[type];
    let typeDir = sln.tree.find(
      (node) => node.type === "dir" && node.name === typeDirName
    ) as ProjectTreeDir;
    if (!typeDir) {
      typeDir = {
        type: "dir",
        dirType: `deps:${type}s-dir`,
        name: typeDirName,
        children: [],
      };
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

const isDepsDir = (dirType: ProjectTreeDirType): boolean =>
  dirType === "deps:libs-dir" || dirType === "deps:pkgs-dir";

function* flattenTree(
  tree: ProjectTree,
  includeDeps = true,
  path: readonly string[] = []
): Generator<FlattenedTreeItem> {
  for (const item of tree) {
    if (item.type === "dir") {
      if (!includeDeps && isDepsDir(item.dirType)) {
        continue;
      }

      yield* flattenTree(item.children, includeDeps, [...path, item.name]);
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
  if (!sln.hasProjects) {
    continue;
  }

  await within(async () => {
    const dir = sln.dir;
    $.cwd = dir;
    $.verbose = argv.debug;

    const isRoot = sln === rootSln;

    const p = (p: string, fmt: ChalkInstance = c.yellow) =>
      fmt(path.relative(dir, p).replaceAll("\\", "/"));

    const slnFile = path.resolve(dir, `${sln.name}.sln`).replaceAll("\\", "/");
    const slnxFile = slnFile + "x";
    const workFile = argv.slnx ? slnxFile : slnFile;
    const altFile = argv.slnx ? slnFile : slnxFile;

    const display = sln.type
      ? `${c.cyan(sln.type)}: ${c.yellow(sln.displayName)}`
      : c.yellow(sln.displayName);

    echo(``);
    echo(`#############################################`);
    echo(`# ${display}`);

    if (argv.purge) {
      echo(`${c.red("~")} ${p(workFile)}`);
      if (fileExists(slnFile)) {
        fs.unlinkSync(slnFile);
      }

      if (fileExists(slnxFile)) {
        fs.unlinkSync(slnxFile);
      }

      await retry(
        2,
        () => $`dotnet new sln --name ${sln.name} --format ${format}`
      );
    } else if (!fileExists(workFile)) {
      if (fileExists(altFile)) {
        echo(`${c.red("~")} ${p(workFile)}`);
        fs.unlinkSync(altFile);
      } else {
        echo(`${c.magenta("!")} ${p(workFile)}`);
      }

      await retry(
        2,
        () => $`dotnet new sln --name ${sln.name} --format ${format}`
      );
    }

    const existingProjects = await getSlnProjects(workFile);
    for (const proj of existingProjects) {
      const projPath = path.resolve(dir, proj).replaceAll("\\", "/");
      echo(`${c.cyan("-")} ${p(projPath)}`);
    }

    let added = 0;
    for (const item of flattenTree(sln.tree, !isRoot)) {
      const relPath = path.relative(dir, item.diskPath).replaceAll("\\", "/");
      if (existingProjects.has(relPath)) {
        continue;
      }

      echo(`${c.green("+")} ${p(item.diskPath)}`);
      await retry(
        2,
        () =>
          $`dotnet sln ${workFile} add ${item.diskPath} --solution-folder ${item.treeDir}`
      );
      added++;
    }
  });
}
