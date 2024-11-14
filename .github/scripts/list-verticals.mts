import { join } from "path";
import { verticals } from "./_meta.mts";
import * as actions from "@actions/core";
import yargs from "yargs";
import { hideBin } from "yargs/helpers";
import { string } from "zod";

const argv = yargs(hideBin(process.argv))
  .option("type", {
    type: "array",
    required: true,
  })
  .parse();

let output = verticals;
if (argv.type) {
  output = verticals.filter((v) => argv.type.includes(v.type));
}

const paths = output.map((v) => v.relPath);
var matrix = {
  shortName: output.map((v) => v.shortName),
  include: output.map((v) => {
    const ret: Record<string, string> = {
      path: v.relPath,
      name: v.name,
      shortName: v.shortName,
      type: v.type,
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
  }),
};

actions.setOutput("matrix", JSON.stringify(matrix));
actions.setOutput("verticals", JSON.stringify(paths));
