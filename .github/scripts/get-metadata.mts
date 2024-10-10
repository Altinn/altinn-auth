import { verticals } from "./_meta.mts";
import { inspect } from "node:util";

console.log(inspect(verticals, { depth: 10, colors: true }));
