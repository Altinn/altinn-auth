import { verticals } from "./_meta.mts";
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
  argv.type = argv.type.flatMap((t) => t.split(",")).filter((t) => t);
  if (argv.type.includes("all")) {
    argv.type = [];
  }
  if (argv.type.length > 0) {
    output = verticals.filter((v) => argv.type.includes(v.type));
  }
}

const result = output;
export { result as verticals };
