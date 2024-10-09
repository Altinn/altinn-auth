import { verticals } from "./_meta.mts";
import * as actions from "@actions/core";
import yargs from "yargs";
import { hideBin } from "yargs/helpers";

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

    return ret;
  }),
};

actions.setOutput("matrix", JSON.stringify(matrix));
actions.setOutput("verticals", JSON.stringify(paths));
