import { verticals } from "./_ci-verticals.mts";
import * as actions from "@actions/core";

const pathsFilters: Record<string, string[]> = {};
for (const v of verticals) {
  const filters = [`${v.relPath}/**`];
  pathsFilters[v.id] = filters;

  if (v.type === "app") {
    pathsFilters[`${v.id}:infra`] = [`${v.relPath}/infra/**`];
  }
}

for (const v of verticals) {
  const filters = [...pathsFilters[v.id]];
  if (v.deps) {
    for (const dep of v.deps) {
      const depId = dep.id;
      if (pathsFilters[depId]) {
        filters.push(...pathsFilters[depId]);
      }
    }
  }

  pathsFilters[`${v.id}:full`] = filters;
}

pathsFilters["shared"] = [
  ".github/**",
  "src/Directory.Build.props",
  "src/Directory.Build.targets",
  "src/Directory.Packages.props",
  "src/Altinn.ruleset",
  "src/Stylecop.json",
  "src/.gitignore",
  ".editorconfig",
  ".gitignore",
  "global.json",
];

pathsFilters["infra"] = ["infra/**"];

actions.setOutput("pathsFilters", JSON.stringify(pathsFilters));
