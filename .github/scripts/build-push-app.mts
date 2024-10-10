import { $ } from "zx";
import yargs from "yargs";
import { hideBin } from "yargs/helpers";
import { getApp } from "./_meta.mts";
import * as actions from "@actions/core";

const argv = yargs(hideBin(process.argv))
  .positional("name", {
    type: "string",
    required: true,
  })
  .option("tag", {
    type: "string",
  })
  .parse();

const app = getApp(argv.name);
const tag =
  argv.tag || (process.env.GITHUB_SHA ?? "").substring(0, 7) || "latest";

if (!app.image) {
  throw new Error(`No image config found for ${app.name}`);
}

const imgCfg = app.image;
if (imgCfg.type !== "dotnet") {
  throw new Error(`Unsupported image type (not implemented): ${imgCfg.type}`);
}

const source = imgCfg.source.replaceAll("\\", "/");
$.cwd = app.path;
await $`dotnet publish ${source} --os linux-musl -t:PublishContainer -p:ContainerImageTag=${tag} -bl`.verbose(
  true
);

const fullImage = `ghcr.io/altinn/altinn-authorization-tmp/${imgCfg.name}:${tag}`;
actions.setOutput("image", fullImage);
actions.setOutput("tag", tag);
