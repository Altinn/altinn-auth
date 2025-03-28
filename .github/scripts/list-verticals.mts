import { verticals as sourceVerticals } from "./_ci-verticals.mts";
import * as actions from "@actions/core";
import yargs from "yargs";
import { hideBin } from "yargs/helpers";

const argv = yargs(hideBin(process.argv))
  .option("changed", {
    type: "string",
    required: true,
  })
  .parse();

const changedPaths: Record<string, string> & {
  readonly changes: readonly string[];
} = (() => {
  const defaultValue = {
    changes: [],
  };
  try {
    return JSON.parse(process.env.PATHS_CHANGED!) ?? defaultValue;
  } catch (e) {
    return defaultValue;
  }
})();

const changed = (id: string, includeShared: boolean = true) => {
  const hasChanged = Boolean(
    (includeShared && changedPaths["shared"] == "true") ||
      changedPaths[id] == "true"
  );
  return hasChanged ? "true" : "false";
};

const filters: Record<string, (v: Record<string, string>) => boolean> = {
  none: () => true,
  infra: (v) => v.changedInfra === "true",
  self: (v) => v.changed === "true",
  selfOnly: (v) => v.changedOnly === "true",
  full: (v) => v.changedFull === "true",
};

const mapped = sourceVerticals.map((v) => {
  const ret: Record<string, string> = {
    id: v.id,
    slug: v.slug,
    displayName: v.displayName,
    path: v.relPath,
    name: v.name,
    shortName: v.shortName,
    type: v.type,
    sonarcloud: v.sonarcloud.enabled ? v.sonarcloud.projectKey : "false",
    sonarcloudName: v.sonarcloud.enabled ? v.sonarcloud.displayName : "false",
    changed: changed(v.id),
    changedOnly: changed(v.id, false),
    changedInfra: changed(`${v.id}:infra`, false),
    changedFull: changed(`${v.id}:full`),
  };

  if (v.image && v.image.name) {
    ret.imageName = v.image.name;
  }

  if (v.infra) {
    ret.infra = "true";

    if (v.infra.terraform) {
      ret.terraform = "true";
      ret.terraformStateFile = v.infra.terraform.stateFile;
    }
  }

  if (v.database) {
    ret.databaseBootstrap = v.database.bootstrap.toString();
    ret.databaseName = v.database.name;
    ret.databaseRolePrefix = v.database.roleprefix;
    ret.databaseSchema = Object.keys(v.database.schema).join(",");
  } else {
    ret.databaseBootstrap = "false";
  }

  return ret;
});
const filtered = mapped.filter(filters[argv.changed]);

const paths = filtered.map((v) => v.path);
var matrix = {
  displayName: filtered.map((v) => v.displayName),
  include: filtered,
};

actions.setOutput("matrix", JSON.stringify(matrix));
actions.setOutput("verticals", JSON.stringify(paths));
actions.setOutput("all", JSON.stringify(mapped));
