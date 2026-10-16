import { spawnSync, type SpawnSyncOptionsWithStringEncoding } from "node:child_process";
import { statSync } from "node:fs";
import { join } from "node:path";

function run(command: string, args: string[], options: Partial<SpawnSyncOptionsWithStringEncoding> = {}): string {
  const result = spawnSync(command, args, {
    stdio: "inherit",
    encoding: "utf8",
    ...options,
  });
  if (result.error) throw result.error;
  if (result.status !== 0) {
    throw new Error(`${command} failed (${result.signal ?? result.status})`);
  }
  return result.stdout ?? "";
}

function hasManifests(path: string): boolean {
  try {
    return statSync(join(path, "manifests")).isDirectory();
  } catch (error) {
    if (error && typeof error === "object" && "code" in error
      && (error.code === "ENOENT" || error.code === "ENOTDIR")) return false;
    throw error;
  }
}

interface Vertical {
  id: string;
  name: string;
  shortslug: string;
}

function main(): void {
  const environments = (process.env.ENVIRONMENTS ?? "").trim().split(/\s+/);
  if (environments.some((name) => !/^[a-z0-9-]+$/.test(name))) {
    throw new Error("ENVIRONMENTS must contain space or newline separated environment names using lowercase letters, digits and hyphens.");
  }

  const ids = (process.env.VERTICALS ?? "").trim().split(/\s+/);
  if (ids.some((id) => !/^app:[A-Za-z0-9_.-]+$/.test(id))) {
    throw new Error("VERTICALS must contain space or newline separated app vertical IDs.");
  }
  const apps: Vertical[] = JSON.parse(run("repoctl", [
    "verticals", "list", "--kind", "app", "--format", "json",
  ], {
    stdio: ["ignore", "pipe", "inherit"],
    env: { ...process.env, NO_COLOR: "1" },
  }));
  const verticals = ids.map((id) => {
    const app = apps.find((app) => app.id === id);
    if (!app) throw new Error(`Unknown app vertical: ${id}`);
    return app;
  });
  const sha = process.env.SOURCE_SHA;
  if (!sha) throw new Error("SOURCE_SHA is required.");

  for (const { name, shortslug: shortSlug } of verticals) {
    const path = join("src", "apps", name);
    console.log(`Processing vertical: ${name}`);
    if (!hasManifests(path)) {
      console.log("No manifests directory found, skipping promotion.");
      continue;
    }

    for (const environment of environments) {
      const deploymentEnvironment = `${shortSlug}/${environment}`;
      const id = run("gh", [
        "api", "--method", "POST", "repos/{owner}/{repo}/deployments",
        "--input", "-", "--jq", ".id",
      ], {
        stdio: ["pipe", "pipe", "inherit"],
        input: JSON.stringify({
          ref: sha,
          environment: deploymentEnvironment,
          auto_merge: false,
          required_contexts: [],
          production_environment: environment === "prod",
          description: "OCI artifact promotion requested; awaiting promotion and Flux reconciliation",
        }),
      }).trim();
      console.log(`Created pending deployment ${id} for ${deploymentEnvironment}`);
    }

    const image = `ghcr.io/altinn/altinn-auth/${shortSlug}-manifests`;
    run("docker", [
      "buildx", "imagetools", "create",
      ...environments.flatMap((environment) => ["-t", `${image}:${environment}`]),
      `${image}:${sha}`,
    ]);
  }
}

try {
  main();
} catch (error) {
  console.error(error);
  process.exitCode = 1;
}
