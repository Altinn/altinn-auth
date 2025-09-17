import { Chalk } from "chalk";
import { globby } from "globby";
import { $, retry } from "zx";
import path from "node:path";
import { Octokit } from "@octokit/action";

const c = new Chalk({ level: 3 });

const ghToken = process.env.GITHUB_TOKEN;
const filesGlob = process.env.FILES_GLOB;

const github = new Octokit({
  auth: ghToken,
});

if (!ghToken || !filesGlob) {
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
      $`dotnet nuget push --skip-duplicate "${fullPath}" --api-key "${ghToken}" --source "github"`
  );

  console.log(`Published ${c.green(name)}`);
}
