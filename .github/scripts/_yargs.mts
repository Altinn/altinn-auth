import _yargs from "yargs";
import { hideBin } from "yargs/helpers";

const fixArgv = (argv?: string[]) => {
  argv ??= process.argv;
  argv = hideBin(process.argv);

  if (argv!.length > 0 && argv![0].trim() === "--") {
    argv = argv!.slice(1);
  }

  return argv;
};

export const yargs = (argv?: string[]) => _yargs(fixArgv(argv));
