import { Chalk } from "chalk";
import { globby } from "globby";
import { $, retry } from "zx";
import path from "node:path";

const c = new Chalk({ level: 3 });

const apiKey = process.env.NUGET_APIKEY;
const filesGlob = process.env.FILES_GLOB;

if (!apiKey || !filesGlob) {
  console.error("Missing required environment variables");
  process.exit(1);
}

for (const file of await globby(filesGlob)) {
  const name = path.basename(file);
  const fullPath = path.resolve(file);

  console.log(`Publishing ${c.yellow(name)}`);
  await retry(
    5,
    () =>
      $`dotnet nuget push --skip-duplicate "${fullPath}" --api-key "${apiKey}" --source "https://api.nuget.org/v3/index.json"`
  );
}
