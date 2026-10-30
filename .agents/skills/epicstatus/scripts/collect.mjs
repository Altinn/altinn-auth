// Collects the status of the team's epics and features, for the epicstatus skill: progress,
// recent completions, what is blocked, risks and tempo.
//
//   node .agents/skills/epicstatus/scripts/collect.mjs [--out file]
//
// Progress is counted on leaves, two levels deep: a sub-issue that has sub-issues of its own is
// replaced by them, so an epic made of features is measured by the features' tasks. Activity is
// measured by recently completed leaves and board status, not by updatedAt, which bulk
// relabelling resets.
//
// The data comes from a few flat queries joined on parent links, rather than one nested query:
// nested sub-issue trees cost thousands of GraphQL points per page.
//
// Issues, never people: assignees are not read, and @mentions are stripped from titles. It only
// reads; it never changes anything.
//
// Needs Node 20+ and an authenticated GitHub CLI with the `project` scope
// (`gh auth refresh -s project`) for board status.

import { execFileSync } from "node:child_process";
import fs from "node:fs";
import os from "node:os";
import path from "node:path";
import { fileURLToPath } from "node:url";

const here = path.dirname(fileURLToPath(import.meta.url));
const args = parseArgs(process.argv.slice(2));
const config = JSON.parse(fs.readFileSync(args.config ?? path.join(here, "..", "config.json"), "utf8"));
const now = new Date();
const DAY = 86400000;
const daysSince = (iso) => (iso ? Math.floor((now - new Date(iso)) / DAY) : null);
const knownAreas = new Set(config.areaOrder);
const lookbackDays = Math.max(config.windowDays, config.activeDays, config.tempoWeeks * 7);

// --- data ---

const PARENT_LINK = `parent { url issueType { name } parent { url } }`;

const boardStatus = fetchBoardStatus();
const parents = config.repos.flatMap((repo) =>
  config.types.flatMap((type) =>
    search(
      `repo:${repo} is:issue is:open type:"${type}"`,
      `number title url state repository { nameWithOwner } issueType { name } labels(first: 15) { nodes { name } } ${PARENT_LINK}
       subIssuesSummary { total completed }
       subIssues(first: 100) { totalCount nodes { url state stateReason subIssuesSummary { total completed } } }`,
      50,
    ),
  ),
);
const openIssues = config.repos.flatMap(openIssuesIn);
const recentlyCompleted = config.repos.flatMap((repo) =>
  search(
    `repo:${repo} is:issue is:closed reason:completed closed:>=${isoDate(now - lookbackDays * DAY)}`,
    `number title url closedAt repository { nameWithOwner } issueType { name } ${PARENT_LINK}`,
    100,
  ),
);
const completedParents = config.repos.flatMap((repo) =>
  config.types.flatMap((type) =>
    search(
      `repo:${repo} is:issue is:closed reason:completed type:"${type}" closed:>=${isoDate(now - config.windowDays * DAY)}`,
      `number title url closedAt repository { nameWithOwner } issueType { name } labels(first: 15) { nodes { name } } subIssuesSummary { total completed }`,
      50,
    ),
  ),
);

// An issue belongs to an epic when the epic is its parent or its grandparent.
const under = (issue, url) => issue.parent?.url === url || issue.parent?.parent?.url === url;

// --- per epic or feature ---

const summaries = parents.filter((p) => p.subIssues.totalCount > 0).map(summarise);
const byUrl = new Map(summaries.map((s) => [s.url, s]));

// A feature under an epic in the list is shown under that epic, not on its own.
for (const s of summaries) {
  const parent = s.parentUrl && byUrl.get(s.parentUrl);
  if (parent) {
    parent.features.push({ number: s.number, url: s.url, title: s.title, total: s.leaves.total, completed: s.leaves.completed });
    s.nestedUnder = parent.number;
  }
}

const top = summaries.filter((s) => !s.nestedUnder);
const active = top
  .filter((s) => s.active)
  .sort((a, b) => Number(b.horizon === "Now") - Number(a.horizon === "Now") || b.completedInWindow.length - a.completedInWindow.length || b.leaves.inWork - a.leaves.inWork);
const quiet = top.filter((s) => !s.active).sort((a, b) => b.leaves.total - b.leaves.completed - (a.leaves.total - a.leaves.completed));

const output = {
  generatedAt: now.toISOString(),
  config: { windowDays: config.windowDays, activeDays: config.activeDays, tempoWeeks: config.tempoWeeks, areaOrder: config.areaOrder, areaNames: config.areaNames, repos: config.repos },
  completed: completedParents.map((p) => ({ ...brief(p), closedAt: p.closedAt, subIssues: p.subIssuesSummary })),
  active,
  quiet: quiet.map((s) => ({ ...brief(s), leaves: s.leaves, horizon: s.horizon })),
  withoutSubIssues: parents.filter((p) => !p.subIssues.totalCount).map(brief),
  stats: {
    openParents: parents.length,
    withSubIssues: summaries.length,
    active: active.length,
    quiet: quiet.length,
    nested: summaries.length - top.length,
    completedInWindow: completedParents.length,
  },
};

const out = args.out ?? path.join(os.tmpdir(), `epicstatus-${now.toISOString().slice(0, 10)}.json`);
fs.writeFileSync(out, JSON.stringify(output, null, 2));

console.log(`Scanned:    ${parents.length} open ${config.types.join("/")} issues, ${summaries.length} with sub-issues (${output.stats.nested} shown under their epic)`);
console.log(`Active:     ${active.length}, quiet: ${quiet.length}, completed in the last ${config.windowDays} days: ${completedParents.length}`);
console.log(`Blocked:    ${active.reduce((s, a) => s + a.blocked.length, 0)} blocked sub-issues in active epics`);
console.log(`Written to: ${out}`);

function summarise(p) {
  // Leaf counts from the sub-issue summaries, two levels deep.
  let total = 0;
  let completed = 0;
  for (const child of p.subIssues.nodes) {
    if (child.subIssuesSummary.total) {
      total += child.subIssuesSummary.total;
      completed += child.subIssuesSummary.completed;
    } else if (child.state === "OPEN" || child.stateReason === "COMPLETED") {
      total += 1;
      completed += child.stateReason === "COMPLETED" ? 1 : 0;
    }
  }

  const childHasChildren = new Set(p.subIssues.nodes.filter((c) => c.subIssuesSummary.total).map((c) => c.url));
  const isLeafOf = (i) => under(i, p.url) && !childHasChildren.has(i.url);
  const openLeaves = openIssues.filter(isLeafOf);
  const doneLeaves = recentlyCompleted.filter(isLeafOf);

  const groups = openLeaves.map((l) => statusGroup(boardStatus.get(l.url)?.status));
  const inProgress = groups.filter((g) => g === "inProgress").length;
  const inReview = groups.filter((g) => g === "inReview").length;

  // Completed leaves per week, oldest first, the current week last.
  const weekly = Array.from({ length: config.tempoWeeks }, (_, i) => doneLeaves.filter((l) => Math.floor(daysSince(l.closedAt) / 7) === config.tempoWeeks - 1 - i).length);
  const recent = weekly.reduce((a, b) => a + b, 0);
  const perWeek = recent / config.tempoWeeks;
  const remaining = total - completed;
  const forecastWeeks = recent >= config.minCompletedForForecast && remaining > 0 ? Math.ceil(remaining / perWeek) : null;
  const lastCompleted = doneLeaves.map((l) => l.closedAt).sort().at(-1) ?? null;

  const blocked = openLeaves
    .filter((l) => l.issueDependenciesSummary?.blockedBy > 0 || labelNames(l).includes("status/blocked"))
    .map((l) => ({
      ...brief(l),
      label: labelNames(l).includes("status/blocked"),
      blockers: (l.blockedBy?.nodes ?? [])
        .filter((b) => b.state === "OPEN")
        .map((b) => ({ repo: b.repository.nameWithOwner, number: b.number, url: b.url, title: clean(b.title), otherRepo: !config.repos.includes(b.repository.nameWithOwner) })),
    }));

  const status = boardStatus.get(p.url) ?? {};
  const active =
    status.horizon === "Now" ||
    ["inProgress", "inReview"].includes(statusGroup(status.status)) ||
    inProgress + inReview > 0 ||
    (lastCompleted !== null && daysSince(lastCompleted) < config.activeDays);

  return {
    ...brief(p),
    parentUrl: p.parent?.issueType?.name === "Epic" ? p.parent.url : null,
    horizon: status.horizon ?? null,
    boardStatus: status.status ?? null,
    active,
    leaves: { total, completed, remaining, inProgress, inReview, inWork: inProgress + inReview },
    percent: total ? Math.round((completed / total) * 100) : 0,
    completedInWindow: doneLeaves.filter((l) => daysSince(l.closedAt) < config.windowDays).map(brief),
    weekly,
    perWeek: Math.round(perWeek * 10) / 10,
    forecastWeeks,
    lastCompletedDays: daysSince(lastCompleted),
    blocked,
    openBugs: openLeaves.filter((l) => l.issueType?.name === "Bug").map(brief),
    openWithoutTypeOrArea: openLeaves.filter((l) => !l.issueType || !areasOf(l).length).length,
    truncated: p.subIssues.totalCount > p.subIssues.nodes.length,
    features: [],
  };
}

// --- GitHub ---

function search(query, fields, pageSize) {
  const out = [];
  let after = null;
  for (;;) {
    const data = gql(
      `query($q: String!, $after: String) { search(type: ISSUE, query: $q, first: ${pageSize}, after: $after) {
        pageInfo { hasNextPage endCursor }
        nodes { ... on Issue { ${fields} } } } }`,
      { q: query, after },
    ).search;
    out.push(...data.nodes.filter((n) => n?.number));
    if (!data.pageInfo.hasNextPage) return out;
    after = data.pageInfo.endCursor;
  }
}

function openIssuesIn(repo) {
  const [owner, name] = repo.split("/");
  const out = [];
  let after = null;
  for (;;) {
    const page = gql(
      `query($owner: String!, $name: String!, $after: String) { repository(owner: $owner, name: $name) {
        issues(states: OPEN, first: 100, after: $after) { pageInfo { hasNextPage endCursor } nodes {
          number title url repository { nameWithOwner } issueType { name } labels(first: 15) { nodes { name } } ${PARENT_LINK}
          issueDependenciesSummary { blockedBy }
          blockedBy(first: 3) { nodes { number title url state repository { nameWithOwner } } }
        } } } }`,
      { owner, name, after },
    ).repository.issues;
    out.push(...page.nodes);
    if (!page.pageInfo.hasNextPage) return out;
    after = page.pageInfo.endCursor;
  }
}

function fetchBoardStatus() {
  const map = new Map();
  let after = null;
  for (;;) {
    const project = gql(
      `query($owner: String!, $number: Int!, $after: String) { organization(login: $owner) { projectV2(number: $number) {
        items(first: 100, after: $after) { pageInfo { hasNextPage endCursor } nodes {
          content { ... on Issue { url } }
          status: fieldValueByName(name: "Status") { ... on ProjectV2ItemFieldSingleSelectValue { name } }
          horizon: fieldValueByName(name: "Horizon") { ... on ProjectV2ItemFieldSingleSelectValue { name } }
        } } } } }`,
      { owner: config.project.owner, number: config.project.number, after },
    ).organization.projectV2;
    if (!project) fail(`Project ${config.project.owner}/${config.project.number} not found, or the token lacks the project scope`);
    for (const n of project.items.nodes) {
      if (n.content?.url) map.set(n.content.url, { status: n.status?.name ?? null, horizon: n.horizon?.name ?? null });
    }
    if (!project.items.pageInfo.hasNextPage) return map;
    after = project.items.pageInfo.endCursor;
  }
}

function gql(query, variables) {
  const argv = ["api", "graphql", "-f", `query=${query}`];
  for (const [k, v] of Object.entries(variables)) {
    if (v === null || v === undefined) continue;
    argv.push(typeof v === "number" ? "-F" : "-f", `${k}=${v}`);
  }
  let raw;
  try {
    raw = execFileSync("gh", argv, { encoding: "utf8", maxBuffer: 64 * 1024 * 1024, stdio: ["ignore", "pipe", "pipe"] });
  } catch (e) {
    raw = e.stdout || "";
    if (!raw) fail(`gh api graphql failed: ${String(e.stderr || e.message).trim()}`);
  }
  const res = JSON.parse(raw);
  if (res.errors?.length) fail(`GraphQL: ${res.errors.map((e) => e.message).join("; ")}`);
  return res.data;
}

// --- helpers ---

function statusGroup(name) {
  return Object.entries(config.statuses).find(([, names]) => names.includes(name))?.[0] ?? null;
}

function labelNames(issue) {
  return issue.labels?.nodes.map((l) => l.name) ?? [];
}

function areasOf(issue) {
  return labelNames(issue).filter((l) => l.startsWith("area/") && knownAreas.has(l.slice(5))).map((l) => l.slice(5));
}

function brief(i) {
  return { repo: i.repository?.nameWithOwner ?? null, number: i.number, url: i.url, title: clean(i.title), type: i.issueType?.name ?? null, areas: areasOf(i) };
}

function isoDate(ms) {
  return new Date(ms).toISOString().slice(0, 10);
}

// No person ends up in the report: drop @mentions, including @org/team.
function clean(text) {
  return text.replace(/(^|[^\w`])@[\w-]+(\/[\w-]+)?/g, "$1").replace(/\s{2,}/g, " ").trim();
}

function parseArgs(argv) {
  const out = {};
  for (let i = 0; i < argv.length; i++) {
    const m = /^--(out|config)$/.exec(argv[i]);
    if (!m) fail(`Unknown argument: ${argv[i]}`);
    out[m[1]] = argv[++i];
  }
  return out;
}

function fail(message) {
  console.error(`collect.mjs: ${message}`);
  process.exit(1);
}
