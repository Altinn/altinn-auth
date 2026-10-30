// Collects incoming issues, response times and bugs, for the innkommende skill.
//
//   node .agents/skills/innkommende/scripts/collect.mjs [--out file]
//
// "The team" is the members of the configured GitHub teams. Everyone else is either another
// Altinn team (an organisation member) or external (service owners, system vendors, the public).
// Logins are used only for that split and for "did the team answer"; no login, name or author
// is written to the output, only the category.
//
// It only reads; it never comments on or changes an issue.
//
// Needs Node 20+ and an authenticated GitHub CLI with `read:org` (team membership) and
// `project` (board status) scopes.

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

// --- data ---

const team = new Set(config.teams.flatMap((slug) => JSON.parse(gh(["api", "--paginate", "--slurp", `orgs/${config.org}/teams/${slug}/members?per_page=100`])).flat().map((m) => m.login.toLowerCase())));
if (!team.size) fail("No team members found; check the team slugs and the read:org scope");

const boardStatus = fetchBoardStatus();
const ISSUE = `number title url state stateReason createdAt closedAt
  repository { nameWithOwner }
  author { __typename login } authorAssociation
  issueType { name }
  labels(first: 15) { nodes { name } }
  closedByPullRequestsReferences(first: 5, includeClosedPrs: false) { nodes { state } }
  comments(first: 30) { totalCount nodes { createdAt author { __typename login } } }
  lastComments: comments(last: 3) { nodes { createdAt authorAssociation author { __typename login } } }`;

const trendStart = isoDate(now - config.trendWeeks * 7 * DAY);
const created = config.repos.flatMap((repo) => search(`repo:${repo} is:issue created:>=${trendStart}`, ISSUE)).filter((i) => i.author?.__typename !== "Bot");
const openAll = config.repos.flatMap(openIssuesIn);
const bugsOpen = config.repos.flatMap((repo) => [...search(`repo:${repo} is:issue is:open type:Bug`, ISSUE), ...config.bugLabels.flatMap((l) => search(`repo:${repo} is:issue is:open label:"${l}"`, ISSUE))]);
const bugsClosed = config.repos.flatMap((repo) => [
  ...search(`repo:${repo} is:issue is:closed type:Bug closed:>=${isoDate(now - config.windowDays * DAY)}`, ISSUE),
  ...config.bugLabels.flatMap((l) => search(`repo:${repo} is:issue is:closed label:"${l}" closed:>=${isoDate(now - config.windowDays * DAY)}`, ISSUE)),
]);

// --- classification ---

function origin(issue) {
  const login = issue.author?.login?.toLowerCase();
  if (login && team.has(login)) return "team";
  if (["MEMBER", "OWNER"].includes(issue.authorAssociation)) return "altinn";
  return "external";
}

// The first sign the team picked it up: a comment from a team member, or the issue being closed.
function firstResponse(issue) {
  const comment = issue.comments.nodes.find((c) => c.author?.__typename !== "Bot" && team.has(c.author?.login?.toLowerCase()));
  const times = [comment?.createdAt, issue.closedAt].filter(Boolean).sort();
  return times[0] ?? null;
}

// Waiting on the team, on any open issue:
// - "comment-waiting": the latest human comment is from someone outside the team, and the team
//   has not answered since. Issues are mostly written by the team, so this is where questions
//   from other teams, service owners and vendors show up.
// - "no-response": opened by someone outside the team, and no team member has commented yet.
// Both only within waitingMaxAgeDays; older silence is counted, not listed.
const isTeam = (c) => team.has(c.author?.login?.toLowerCase());
const human = (list) => list.filter((c) => c.author?.__typename !== "Bot");
const category = (association) => (["MEMBER", "OWNER"].includes(association) ? "altinn" : "external");

function waiting(issue) {
  if (issue.state !== "OPEN") return null;
  const last = human(issue.lastComments.nodes).at(-1);
  if (last && !isTeam(last)) {
    const days = daysSince(last.createdAt);
    if (days < config.waitingDays) return null;
    return { reason: "comment-waiting", days, from: category(last.authorAssociation), old: days > config.waitingMaxAgeDays };
  }
  if (origin(issue) !== "team" && !human(issue.comments.nodes).some(isTeam)) {
    const days = daysSince(issue.createdAt);
    if (days < config.waitingDays) return null;
    return { reason: "no-response", days, from: origin(issue), old: days > config.waitingMaxAgeDays };
  }
  return null;
}

const isBug = (i) => i.issueType?.name === "Bug" || labelNames(i).some((l) => config.bugLabels.includes(l));
const unique = (list) => [...new Map(list.map((i) => [i.url, i])).values()];

// --- incoming ---

const inWindow = created.filter((i) => daysSince(i.createdAt) < config.windowDays);
const incoming = {
  days: config.windowDays,
  count: inWindow.length,
  byOrigin: countBy(inWindow, origin),
  byArea: countBy(inWindow, (i) => areasOf(i)[0] ?? "none"),
  byType: countBy(inWindow, (i) => i.issueType?.name ?? (isBug(i) ? "Bug" : "none")),
  stillOpen: inWindow.filter((i) => i.state === "OPEN").length,
  closedAsNotPlanned: inWindow.filter((i) => i.stateReason === "NOT_PLANNED" || i.stateReason === "DUPLICATE").length,
  missingAreaOrType: inWindow.filter((i) => i.state === "OPEN" && (!areasOf(i).length || !i.issueType)).map(brief),
  inTriage: inWindow.filter((i) => i.state === "OPEN" && labelNames(i).includes("status/triage")).map(brief),
  fromOthers: inWindow.filter((i) => origin(i) !== "team").map((i) => ({ ...brief(i), origin: origin(i), state: i.state, respondedAfterDays: responseDays(i) })),
};

// --- waiting on the team ---

const waitingAll = openAll
  .map((i) => ({ i, w: waiting(i) }))
  .filter((x) => x.w);
const waitingOn = waitingAll
  .filter((x) => !x.w.old)
  .map(({ i, w }) => ({ ...brief(i), reason: w.reason, from: w.from, days: w.days, ageDays: daysSince(i.createdAt) }))
  .sort((a, b) => a.days - b.days);
const waitingOld = countBy(waitingAll.filter((x) => x.w.old), (x) => x.w.reason);

// --- response time trend, issues from others in the trend window ---

const fromOthers = created.filter((i) => origin(i) !== "team");
const responded = fromOthers.map(responseDays).filter((d) => d !== null);
const trend = {
  weeks: config.trendWeeks,
  issuesFromOthers: fromOthers.length,
  responded: responded.length,
  unanswered: fromOthers.length - responded.length,
  medianDays: median(responded),
  byOrigin: Object.fromEntries(["altinn", "external"].map((o) => {
    const d = fromOthers.filter((i) => origin(i) === o).map(responseDays).filter((x) => x !== null);
    return [o, { issues: fromOthers.filter((i) => origin(i) === o).length, medianDays: median(d) }];
  })),
};

// --- bugs ---

const openBugs = unique(bugsOpen).filter(isBug);
const bugs = {
  open: openBugs.length,
  newInWindow: inWindow.filter(isBug).map(brief),
  closedInWindow: unique(bugsClosed).filter((i) => i.stateReason === "COMPLETED").length,
  byArea: countBy(openBugs, (i) => areasOf(i)[0] ?? "none"),
  byAge: countBy(openBugs, (i) => { const d = daysSince(i.createdAt); return d < 30 ? "<30" : d < 90 ? "30-90" : d < 365 ? "90-365" : ">365"; }),
  inWork: openBugs.filter((i) => ["inProgress", "inReview"].includes(statusGroup(boardStatus.get(i.url)))).length,
  withOpenPr: openBugs.filter((i) => i.closedByPullRequestsReferences.nodes.some((p) => p.state === "OPEN")).length,
  fromOthers: openBugs.filter((i) => origin(i) !== "team").length,
  // Oldest bugs nobody is on: no open PR and not in progress or review on the board.
  oldestUntouched: openBugs
    .filter((i) => !i.closedByPullRequestsReferences.nodes.some((p) => p.state === "OPEN") && !["inProgress", "inReview"].includes(statusGroup(boardStatus.get(i.url))))
    .sort((a, b) => new Date(a.createdAt) - new Date(b.createdAt))
    .slice(0, 15)
    .map((i) => ({ ...brief(i), ageDays: daysSince(i.createdAt), origin: origin(i) })),
};

const output = {
  generatedAt: now.toISOString(),
  config: { windowDays: config.windowDays, trendWeeks: config.trendWeeks, waitingDays: config.waitingDays, waitingMaxAgeDays: config.waitingMaxAgeDays, areaOrder: config.areaOrder, areaNames: config.areaNames, repos: config.repos },
  incoming,
  waitingOn,
  waitingOld,
  trend,
  bugs,
};

const out = args.out ?? path.join(os.tmpdir(), `innkommende-${now.toISOString().slice(0, 10)}.json`);
fs.writeFileSync(out, JSON.stringify(output, null, 2));

console.log(`Incoming:   ${incoming.count} new in ${config.windowDays} days (team ${incoming.byOrigin.team ?? 0}, other Altinn ${incoming.byOrigin.altinn ?? 0}, external ${incoming.byOrigin.external ?? 0})`);
console.log(`Waiting:    ${waitingOn.length} issues from others waiting on the team for ${config.waitingDays}+ days`);
console.log(`Response:   median ${trend.medianDays ?? "-"} days over ${trend.responded} of ${trend.issuesFromOthers} issues from others (${config.trendWeeks} weeks)`);
console.log(`Bugs:       ${bugs.open} open, ${bugs.newInWindow.length} new and ${bugs.closedInWindow} closed in ${config.windowDays} days`);
console.log(`Written to: ${out}`);

// --- GitHub ---

function openIssuesIn(repo) {
  const [owner, name] = repo.split("/");
  const out = [];
  let after = null;
  for (;;) {
    const page = gql(
      `query($owner: String!, $name: String!, $after: String) { repository(owner: $owner, name: $name) {
        issues(states: OPEN, first: 100, after: $after) { pageInfo { hasNextPage endCursor } nodes { ${ISSUE} } } } }`,
      { owner, name, after },
    ).repository.issues;
    out.push(...page.nodes.filter((i) => i.author?.__typename !== "Bot"));
    if (!page.pageInfo.hasNextPage) return out;
    after = page.pageInfo.endCursor;
  }
}

function search(query, fields) {
  const out = [];
  let after = null;
  for (;;) {
    const data = gql(
      `query($q: String!, $after: String) { search(type: ISSUE, query: $q, first: 50, after: $after) {
        pageInfo { hasNextPage endCursor }
        nodes { ... on Issue { ${fields} } } } }`,
      { q: query, after },
    ).search;
    out.push(...data.nodes.filter((n) => n?.number));
    if (!data.pageInfo.hasNextPage) return out;
    after = data.pageInfo.endCursor;
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
        } } } } }`,
      { owner: config.project.owner, number: config.project.number, after },
    ).organization.projectV2;
    if (!project) fail(`Project ${config.project.owner}/${config.project.number} not found, or the token lacks the project scope`);
    for (const n of project.items.nodes) if (n.content?.url) map.set(n.content.url, n.status?.name ?? null);
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
  const res = JSON.parse(gh(argv));
  if (res.errors?.length) fail(`GraphQL: ${res.errors.map((e) => e.message).join("; ")}`);
  return res.data;
}

function gh(argv) {
  try {
    return execFileSync("gh", argv, { encoding: "utf8", maxBuffer: 64 * 1024 * 1024, stdio: ["ignore", "pipe", "pipe"] });
  } catch (e) {
    if (e.stdout) return e.stdout;
    fail(`gh ${argv.slice(0, 2).join(" ")} failed: ${String(e.stderr || e.message).trim()}`);
  }
}

// --- helpers ---

function responseDays(issue) {
  const first = firstResponse(issue);
  return first ? Math.round(((new Date(first) - new Date(issue.createdAt)) / DAY) * 10) / 10 : null;
}

function median(values) {
  if (!values.length) return null;
  const s = [...values].sort((a, b) => a - b);
  const m = Math.floor(s.length / 2);
  return s.length % 2 ? s[m] : Math.round(((s[m - 1] + s[m]) / 2) * 10) / 10;
}

function countBy(list, key) {
  const out = {};
  for (const item of list) {
    const k = key(item);
    out[k] = (out[k] ?? 0) + 1;
  }
  return out;
}

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
  return { repo: i.repository.nameWithOwner, number: i.number, url: i.url, title: clean(i.title), type: i.issueType?.name ?? null, areas: areasOf(i) };
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
