// Collects the state of the team board for the tavlestatus skill: flow (what is in progress,
// for how long, and which PRs wait for review) and hygiene (where the board does not match
// reality).
//
//   node .agents/skills/tavlestatus/scripts/collect.mjs [--out file]
//
// Writes JSON to --out (default: the OS temp directory) and prints a short summary. It only
// reads; it never changes the board or any issue.
//
// Issues, never people: assignees are counted (has one or not), never named, and PR authors are
// read only to drop bot PRs. @mentions are stripped from titles.
//
// Needs Node 20+ and an authenticated GitHub CLI with the `project` scope
// (`gh auth refresh -s project`).

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
const t = config.thresholds;

const statusOf = (name) => Object.entries(config.statuses).find(([, names]) => names.includes(name))?.[0] ?? (name ? "other" : "none");
const daysSince = (iso) => (iso ? Math.floor((now - new Date(iso)) / DAY) : null);

// --- data ---

const board = fetchBoard();
const items = board.items.map(toItem).filter(Boolean);
const open = items.filter((i) => i.state === "OPEN");
const onBoard = new Set(items.map((i) => i.url));
const repoIssues = config.repos.flatMap(openIssuesIn);
const prs = config.repos.flatMap(openPrsIn).filter((pr) => !isExcluded(pr));

// --- flow ---

const wip = open.filter((i) => i.statusGroup === "inProgress" || i.statusGroup === "inReview");
const horizonInUse = open.some((i) => i.fields.Horizon);

const inWork = wip
  .map((i) => {
    const limit = i.statusGroup === "inReview" ? t.inReviewDays : t.inProgressDays;
    const openPrs = i.prs.filter((p) => p.state === "OPEN");
    const mergedClosing = i.prs.filter((p) => p.merged && p.closes);
    const flags = [];
    if (i.daysInStatus !== null && i.daysInStatus > limit) flags.push("over-threshold");
    if (i.statusGroup === "inReview" && !openPrs.length) flags.push("in-review-without-open-pr");
    if (mergedClosing.length) flags.push("closing-pr-merged");
    if (horizonInUse && i.fields.Horizon !== "Now") flags.push("not-horizon-now");
    if (!i.hasAssignee) flags.push("no-assignee");
    return { ...brief(i), status: i.status, daysInStatus: i.daysInStatus, thresholdDays: limit, openPrs: openPrs.map(prBrief), mergedPrs: mergedClosing.map(prBrief), horizon: i.fields.Horizon ?? null, flags };
  })
  .sort((a, b) => (b.daysInStatus ?? 0) - (a.daysInStatus ?? 0));

const prsWaiting = prs
  .map((pr) => ({
    repo: pr.repository.nameWithOwner,
    number: pr.number,
    url: pr.url,
    title: prTitle(pr.title),
    ageDays: daysSince(pr.createdAt),
    daysSinceUpdate: daysSince(pr.updatedAt),
    reviewDecision: pr.reviewDecision,
    reviews: pr.reviews.totalCount,
    issues: pr.closingIssuesReferences.nodes.map((n) => n.number),
  }))
  .filter((pr) => pr.reviewDecision !== "APPROVED" && pr.ageDays >= t.prWaitingDays && pr.ageDays < t.stalePrDays)
  .sort((a, b) => b.ageDays - a.ageDays);

// PRs open this long are no longer waiting for review; they are abandoned or parked.
const stalePrs = prs
  .filter((pr) => daysSince(pr.createdAt) >= t.stalePrDays)
  .map((pr) => ({ repo: pr.repository.nameWithOwner, number: pr.number, url: pr.url, title: prTitle(pr.title), ageDays: daysSince(pr.createdAt), daysSinceUpdate: daysSince(pr.updatedAt) }))
  .sort((a, b) => b.ageDays - a.ageDays);

const prsApprovedNotMerged = prs
  .filter((pr) => pr.reviewDecision === "APPROVED" && daysSince(pr.updatedAt) >= t.prWaitingDays && daysSince(pr.createdAt) < t.stalePrDays)
  .map((pr) => ({ repo: pr.repository.nameWithOwner, number: pr.number, url: pr.url, title: prTitle(pr.title), daysSinceUpdate: daysSince(pr.updatedAt) }));

const doneInWindow = items.filter((i) => i.state === "CLOSED" && i.stateReason === "COMPLETED" && daysSince(i.closedAt) < t.doneWindowDays);

const counts = Object.fromEntries(["none", "backlog", "ready", "inProgress", "inReview", "done", "other"].map((g) => [g, open.filter((i) => i.statusGroup === g).length]));

// --- hygiene ---

const check = (id, list, detail = () => null) => ({ id, count: list.length, items: list.map((i) => ({ ...brief(i), detail: detail(i) })) });
const hygiene = [
  check("closed-not-done", items.filter((i) => i.state === "CLOSED" && i.statusGroup !== "done"), (i) => `Status «${i.status ?? "ingen"}»${i.stateReason === "NOT_PLANNED" ? ", lukket som not planned" : ""}`),
  check("open-but-done", open.filter((i) => i.statusGroup === "done")),
  check("open-without-status", open.filter((i) => i.statusGroup === "none")),
  check("missing-type", open.filter((i) => i.kind === "Issue" && !i.type)),
  check("missing-area", open.filter((i) => i.kind === "Issue" && !i.areas.length)),
  check("old-triage", open.filter((i) => i.labels.includes("status/triage") && daysSince(i.createdAt) > t.triageDays), (i) => `${daysSince(i.createdAt)} dager gammel`),
  check("open-under-closed-parent", open.filter((i) => i.parent?.state === "CLOSED"), (i) => `Forelder ${i.parent.repository.nameWithOwner === i.repo ? "" : i.parent.repository.nameWithOwner}#${i.parent.number} er lukket`),
  {
    id: "not-on-board",
    count: repoIssues.filter((i) => !onBoard.has(i.url)).length,
    items: repoIssues.filter((i) => !onBoard.has(i.url)).map((i) => ({ repo: i.repo, number: i.number, url: i.url, title: i.title, detail: null })),
  },
  { id: "stale-prs", count: stalePrs.length, items: stalePrs.map((pr) => ({ ...pr, detail: `Åpen i ${pr.ageDays} dager, sist endret for ${pr.daysSinceUpdate} dager siden` })) },
];

// How far back status changes go. On a young board, days in status say little yet.
const statusHistoryDays = Math.max(0, ...items.map((i) => i.daysInStatus ?? 0));

const fieldReport = config.fieldsToCheck.map((name) => {
  const field = board.fields.find((f) => f.name === name);
  if (!field) return { name, exists: false };
  const used = open.filter((i) => i.fields[name] !== undefined).length;
  return {
    name,
    exists: true,
    options: field.options ? field.options.length : null,
    usedOnOpen: used,
    percent: open.length ? Math.round((used / open.length) * 100) : 0,
    flag: field.options && !field.options.length ? "no-options" : used / Math.max(open.length, 1) * 100 < t.fieldUsagePercent ? "rarely-used" : null,
  };
});

// --- output ---

const output = {
  generatedAt: now.toISOString(),
  project: { title: board.title, url: config.project.url, workflows: board.workflows, statusHistoryDays },
  config: { thresholds: t, wipLimit: config.wipLimit, areaNames: config.areaNames, repos: config.repos },
  flow: {
    counts,
    wip: { total: wip.length, limit: config.wipLimit, overLimit: config.wipLimit ? wip.length > config.wipLimit : null },
    inWork,
    prsWaiting,
    prsApprovedNotMerged,
    doneInWindow: { days: t.doneWindowDays, count: doneInWindow.length, items: doneInWindow.map(brief) },
  },
  hygiene,
  fields: fieldReport,
  stats: { boardItems: items.length, openOnBoard: open.length, openInRepos: repoIssues.length, openPrs: prs.length },
};

const out = args.out ?? path.join(os.tmpdir(), `tavlestatus-${now.toISOString().slice(0, 10)}.json`);
fs.writeFileSync(out, JSON.stringify(output, null, 2));

console.log(`Board:      ${board.title}: ${items.length} items, ${open.length} open`);
console.log(`Flow:       ${wip.length} in work (${counts.inProgress} in progress, ${counts.inReview} in review), ${inWork.filter((i) => i.flags.includes("over-threshold")).length} over threshold, ${prsWaiting.length} PRs waiting for review`);
console.log(`Hygiene:    ${hygiene.filter((h) => h.count).map((h) => `${h.id} ${h.count}`).join(", ") || "nothing to fix"}`);
console.log(`Fields:     ${fieldReport.filter((f) => f.flag).map((f) => `${f.name} ${f.flag}`).join(", ") || "ok"}`);
console.log(`Written to: ${out}`);

// --- GitHub ---

function fetchBoard() {
  const value = (type, extra) => `... on ${type} { ${extra} field { ... on ProjectV2FieldCommon { name } } }`;
  const query = `query($owner: String!, $number: Int!, $after: String) { organization(login: $owner) { projectV2(number: $number) {
    title
    workflows(first: 20) { nodes { name enabled } }
    fields(first: 50) { nodes { ... on ProjectV2FieldCommon { name dataType } ... on ProjectV2SingleSelectField { options { name } } } }
    items(first: 100, after: $after) {
      pageInfo { hasNextPage endCursor }
      nodes {
        fieldValues(first: 30) { nodes {
          ${value("ProjectV2ItemFieldSingleSelectValue", "name updatedAt")}
          ${value("ProjectV2ItemFieldIterationValue", "title")}
          ${value("ProjectV2ItemFieldDateValue", "date")}
          ${value("ProjectV2ItemFieldNumberValue", "number")}
          ${value("ProjectV2ItemFieldTextValue", "text")}
        } }
        content {
          __typename
          ... on DraftIssue { title }
          ... on Issue {
            number title url state stateReason createdAt closedAt
            repository { nameWithOwner }
            issueType { name }
            labels(first: 30) { nodes { name } }
            assignees { totalCount }
            parent { number state repository { nameWithOwner } }
            closedByPullRequestsReferences(first: 10, includeClosedPrs: true) { nodes { number url state isDraft merged createdAt repository { nameWithOwner } } }
          }
          ... on PullRequest { number title url state createdAt closedAt merged repository { nameWithOwner } labels(first: 30) { nodes { name } } assignees { totalCount } }
        }
      }
    }
  } } }`;
  const nodes = [];
  let meta = null;
  let after = null;
  for (;;) {
    const project = gql(query, { owner: config.project.owner, number: config.project.number, after }).organization.projectV2;
    if (!project) fail(`Project ${config.project.owner}/${config.project.number} not found, or the token lacks the project scope`);
    meta ??= { title: project.title, workflows: project.workflows.nodes, fields: project.fields.nodes.filter((f) => f.name) };
    nodes.push(...project.items.nodes);
    if (!project.items.pageInfo.hasNextPage) return { ...meta, items: nodes };
    after = project.items.pageInfo.endCursor;
  }
}

function toItem(node) {
  const c = node.content;
  if (!c || c.__typename === "DraftIssue") return null;
  const fields = {};
  let statusUpdatedAt = null;
  for (const v of node.fieldValues.nodes) {
    const name = v.field?.name;
    if (!name) continue;
    fields[name] = v.name ?? v.title ?? v.date ?? v.number ?? v.text;
    if (name === "Status") statusUpdatedAt = v.updatedAt;
  }
  const labels = c.labels?.nodes.map((l) => l.name) ?? [];
  const prs = (c.closedByPullRequestsReferences?.nodes ?? []).map((p) => ({ ...p, closes: true }));
  return {
    kind: c.__typename,
    repo: c.repository.nameWithOwner,
    number: c.number,
    url: c.url,
    title: clean(c.title),
    state: c.state === "MERGED" ? "CLOSED" : c.state,
    stateReason: c.stateReason ?? (c.merged ? "COMPLETED" : null),
    createdAt: c.createdAt,
    closedAt: c.closedAt,
    type: c.issueType?.name ?? null,
    labels,
    areas: labels.filter((l) => l.startsWith("area/") && l !== "area/x").map((l) => l.slice(5)),
    hasAssignee: (c.assignees?.totalCount ?? 0) > 0,
    parent: c.parent ?? null,
    prs,
    fields,
    status: fields.Status ?? null,
    statusGroup: statusOf(fields.Status),
    daysInStatus: daysSince(statusUpdatedAt),
  };
}

function openIssuesIn(repo) {
  const [owner, name] = repo.split("/");
  const out = [];
  let after = null;
  for (;;) {
    const page = gql(
      `query($owner: String!, $name: String!, $after: String) { repository(owner: $owner, name: $name) {
        issues(states: OPEN, first: 100, after: $after) { pageInfo { hasNextPage endCursor } nodes { number title url } } } }`,
      { owner, name, after },
    ).repository.issues;
    out.push(...page.nodes.map((n) => ({ repo, number: n.number, url: n.url, title: clean(n.title) })));
    if (!page.pageInfo.hasNextPage) return out;
    after = page.pageInfo.endCursor;
  }
}

function openPrsIn(repo) {
  const out = [];
  let after = null;
  for (;;) {
    const data = gql(
      `query($q: String!, $after: String) { search(type: ISSUE, query: $q, first: 50, after: $after) {
        pageInfo { hasNextPage endCursor }
        nodes { ... on PullRequest {
          number title url createdAt updatedAt reviewDecision
          author { __typename login }
          repository { nameWithOwner }
          reviews(first: 1) { totalCount }
          closingIssuesReferences(first: 5) { nodes { number } }
        } } } }`,
      { q: `repo:${repo} is:pr is:open draft:false`, after },
    ).search;
    out.push(...data.nodes.filter((n) => n?.number));
    if (!data.pageInfo.hasNextPage) return out;
    after = data.pageInfo.endCursor;
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

function brief(i) {
  return { repo: i.repo, number: i.number, url: i.url, title: i.title, type: i.type, areas: i.areas };
}

function prBrief(p) {
  return { repo: p.repository?.nameWithOwner ?? null, number: p.number, url: p.url, isDraft: p.isDraft ?? false, ageDays: daysSince(p.createdAt) };
}

function isExcluded(pr) {
  const login = (pr.author?.login ?? "").toLowerCase();
  if (pr.author?.__typename === "Bot" || login.endsWith("[bot]")) return true;
  if (config.exclude.authorLogins.some((l) => login === l.toLowerCase())) return true;
  return config.exclude.titlePatterns.some((p) => new RegExp(p, "i").test(pr.title));
}

// A PR titled after its branch, like "Feature/<user>/26/09/thing", carries a user name in its
// second segment. Drop that segment; the skill rewrites such titles anyway.
function prTitle(title) {
  return clean(title.replace(/^([A-Za-z]+)\/[A-Za-z0-9._-]+\/(?=\S)/, "$1/"));
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
