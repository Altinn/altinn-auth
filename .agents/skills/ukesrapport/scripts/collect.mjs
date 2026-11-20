// Collects what the authorization team delivered in a period, for the ukesrapport skill.
//
//   node .agents/skills/ukesrapport/scripts/collect.mjs [--from YYYY-MM-DD] [--to YYYY-MM-DD] [--out file]
//
// Default period: Monday 00:00 of the current week until now, in the configured time zone.
// Writes JSON to --out (default: the OS temp directory) and prints a short summary.
//
// Deliveries are tasks, never people. Authors are read only to drop bot pull requests and
// are not written to the output; @mentions are stripped from every title.
//
// No dependencies beyond Node 20+ and an authenticated GitHub CLI (`gh auth status`).

import { execFileSync } from "node:child_process";
import fs from "node:fs";
import os from "node:os";
import path from "node:path";
import { fileURLToPath } from "node:url";

const here = path.dirname(fileURLToPath(import.meta.url));
const args = parseArgs(process.argv.slice(2));
const config = JSON.parse(fs.readFileSync(args.config ?? path.join(here, "..", "config.json"), "utf8"));
const tz = config.timezone;

// --- period ---

const now = new Date();
const from = args.from ? zonedMidnight(args.from) : zonedMidnight(mondayOf(now));
const to = args.to ? new Date(zonedMidnight(addDays(args.to, 1)).getTime() - 1000) : now;
if (!(from < to)) fail(`Empty period: ${from.toISOString()} .. ${to.toISOString()}`);

// --- GitHub ---

const ISSUE_FIELDS = `
  number title url createdAt closedAt stateReason
  issueType { name }
  labels(first: 30) { nodes { name } }
  parent { ...ParentFields parent { ...ParentFields } }
  closedByPullRequestsReferences(first: 10, includeClosedPrs: false) { nodes { number url mergedAt repository { nameWithOwner } } }`;

const PARENT_FIELDS = `
  fragment ParentFields on Issue {
    number title url state stateReason closedAt
    repository { nameWithOwner }
    issueType { name }
    labels(first: 30) { nodes { name } }
    subIssuesSummary { total completed }
  }`;

const PR_FIELDS = `
  number title url mergedAt body changedFiles
  author { __typename login }
  labels(first: 30) { nodes { name } }
  files(first: 100) { nodes { path } }
  closingIssuesReferences(first: 10) { nodes { number repository { nameWithOwner } labels(first: 30) { nodes { name } } } }`;

const range = `${isoInZone(from)}..${isoInZone(to)}`;
const repos = config.repos.map((repo) => collectRepo(repo));

const output = {
  period: {
    from: isoInZone(from),
    to: isoInZone(to),
    timezone: tz,
    isoWeek: isoWeek(from) === isoWeek(to) ? isoWeek(from) : `${isoWeek(from)}..${isoWeek(to)}`,
    partial: !args.to,
  },
  config: {
    repos: config.repos.map((r) => r.name),
    areaOrder: config.areaOrder,
    areaNames: config.areaNames,
    releases: config.releases,
  },
  deliveries: repos.flatMap((r) => r.deliveries),
  parents: collectParents(repos.flatMap((r) => r.deliveries)),
  releases: repos.flatMap((r) => r.releases),
  stats: summarise(repos),
};

const out = args.out ?? path.join(os.tmpdir(), `ukesrapport-${output.period.isoWeek}.json`);
fs.writeFileSync(out, JSON.stringify(output, null, 2));

console.log(`Period:     ${output.period.from} .. ${output.period.to} (${output.period.isoWeek}${output.period.partial ? ", so far" : ""})`);
console.log(`Deliveries: ${output.deliveries.length} (${output.stats.issuesCompleted} issues completed, ${output.stats.prsMerged} PRs merged)`);
console.log(`Excluded:   ${output.stats.prsExcluded} bot/release/dependency PRs, ${output.stats.issuesNotCompleted} issues closed as not planned or duplicate`);
console.log(`Area:       ${output.stats.areaUncertain} deliveries with uncertain area`);
console.log(`Backlog:    ${output.stats.likelyBacklogCleanup} old issues closed without a recent change (likely cleanup)`);
console.log(`Releases:   ${output.releases.length}`);
console.log(`Written to: ${out}`);

// --- per repository ---

function collectRepo(repo) {
  const name = repo.name;
  const issues = search(`repo:${name} is:issue is:closed closed:${range}`, "Issue", ISSUE_FIELDS);
  const prs = search(`repo:${name} is:pr is:merged merged:${range}`, "PullRequest", PR_FIELDS);

  const excludedPrs = prs.filter(isExcluded);
  const keptPrs = prs.filter((pr) => !isExcluded(pr));
  const completed = issues.filter((i) => i.stateReason === "COMPLETED");

  // One delivery per piece of work: a PR and the issues it completes are one delivery, and
  // several PRs for the same issue join that issue's delivery.
  const issueByNumber = new Map(completed.map((i) => [i.number, i]));
  const groupOfIssue = new Map();
  const groups = [];

  for (const pr of keptPrs) {
    const refs = new Set([
      ...pr.closingIssuesReferences.nodes.filter((n) => n.repository.nameWithOwner === name).map((n) => n.number),
      ...issueRefs(pr.title),
      ...issueRefs(trailerLines(pr.body)),
    ]);
    const linked = [...refs].map((n) => issueByNumber.get(n)).filter(Boolean);
    let group = linked.map((i) => groupOfIssue.get(i.number)).find(Boolean);
    if (!group) groups.push((group = { issues: [], prs: [], extraLabels: [] }));
    group.prs.push(pr);
    group.extraLabels.push(...pr.closingIssuesReferences.nodes.flatMap(labelNames));
    for (const issue of linked) {
      if (groupOfIssue.has(issue.number)) continue;
      groupOfIssue.set(issue.number, group);
      group.issues.push(issue);
    }
  }

  for (const issue of completed) {
    if (!groupOfIssue.has(issue.number)) groups.push({ issues: [issue], prs: [], extraLabels: [] });
  }

  const deliveries = groups.map((g) =>
    delivery(repo, {
      title: g.issues[0]?.title ?? g.prs[0].title,
      issues: g.issues,
      prs: g.prs,
      labels: [...g.issues.flatMap(labelNames), ...g.extraLabels],
      prLabels: g.prs.flatMap(labelNames),
      paths: g.prs.flatMap((pr) => pr.files.nodes.map((f) => f.path)),
      filesTruncated: g.prs.some((pr) => pr.changedFiles > pr.files.nodes.length),
    }),
  );

  return {
    name,
    deliveries,
    releases: releases(name),
    counts: {
      issuesCompleted: completed.length,
      issuesNotCompleted: issues.length - completed.length,
      prsMerged: keptPrs.length,
      prsExcluded: excludedPrs.length,
    },
  };
}

function delivery(repo, { title, issues, prs, labels, prLabels, paths, filesTruncated = false }) {
  const issue = issues[0];
  const earlierChanges = prs.length
    ? []
    : issues.flatMap((i) => i.closedByPullRequestsReferences.nodes.map((n) => ({ repo: n.repository.nameWithOwner, number: n.number, url: n.url, mergedAt: n.mergedAt })));
  let area = deriveArea(repo, labels, prLabels, paths, filesTruncated);
  // An unlabelled issue completed by a PR in another configured repository belongs to that
  // repository's area, typically frontend work tracked in altinn-auth.
  if (area.source === "none") {
    const other = earlierChanges.map((c) => config.repos.find((r) => r.name === c.repo && r.defaultArea)).find(Boolean);
    if (other) area = { areas: [other.defaultArea], source: "linked-pr-repo", certain: true, crossCutting: false };
  }
  return {
    repo: repo.name,
    title: clean(title),
    prTitles: prs.map((p) => clean(p.title)),
    type: issue?.issueType?.name ?? null,
    commitType: prs.map((p) => conventionalType(p.title)).find(Boolean) ?? null,
    issues: issues.map((i) => ({ number: i.number, url: i.url, title: clean(i.title), type: i.issueType?.name ?? null })),
    prs: prs.map((p) => ({ number: p.number, url: p.url })),
    areas: area.areas,
    areaSource: area.source,
    areaCertain: area.certain,
    crossCutting: area.crossCutting,
    filesTruncated,
    // For an issue closed without a PR in the period: when it was opened, and any change that
    // completed it earlier. An old issue with neither is usually backlog cleanup, not new work.
    createdAt: issue?.createdAt ?? null,
    earlierChanges,
    likelyBacklogCleanup: likelyBacklogCleanup(issue, prs, earlierChanges),
    parent: issues.map((i) => i.parent).find(Boolean) ? parentRef(issues.map((i) => i.parent).find(Boolean)) : null,
    epic: issues.map((i) => epicOf(i)).find(Boolean) ?? null,
    closedAt: issue?.closedAt ?? null,
    mergedAt: prs[0]?.mergedAt ?? null,
  };
}

// An issue closed with no change in the period, opened more than half a year ago and not
// completed by anything merged in the last month, is almost always backlog cleanup.
function likelyBacklogCleanup(issue, prs, earlierChanges) {
  if (!issue || prs.length) return false;
  const day = 86400000;
  const old = from - new Date(issue.createdAt) > 182 * day;
  const recentChange = earlierChanges.some((c) => c.mergedAt && to - new Date(c.mergedAt) < 30 * day);
  return old && !recentChange;
}

// --- area ---

// Order of trust: area labels on the issues, then on the PR, then the PR's file paths, then
// the repository's default. Labels and the default are certain; paths are when one area dominates.
function deriveArea(repo, labels, prLabels, paths, filesTruncated) {
  const fromIssue = areaLabels(labels);
  if (fromIssue.length) return { areas: fromIssue, source: "issue-label", certain: true, crossCutting: fromIssue.length > 2 };
  const fromPr = areaLabels(prLabels);
  if (fromPr.length) return { areas: fromPr, source: "pr-label", certain: true, crossCutting: fromPr.length > 2 };

  const prefixes = Object.keys(repo.pathAreas ?? {}).sort((a, b) => b.length - a.length);
  if (paths.length && prefixes.length) {
    const counts = new Map();
    for (const p of paths) {
      const prefix = prefixes.find((x) => p.startsWith(x));
      if (prefix === undefined) continue;
      const area = repo.pathAreas[prefix];
      counts.set(area, (counts.get(area) ?? 0) + 1);
    }
    const ranked = [...counts].sort((a, b) => b[1] - a[1]);
    if (ranked.length) {
      const total = ranked.reduce((s, [, c]) => s + c, 0);
      const main = ranked.filter(([, c]) => c / total >= 0.25).map(([a]) => a);
      // A single dominant area from paths is a good guess; a spread is a guess at best, and a
      // change that touches four or more areas is repository-wide (formatting, CI, imports).
      const crossCutting = ranked.length >= 4;
      return {
        areas: main.length ? main : [ranked[0][0]],
        source: "paths",
        // Only the first 100 files of a PR are read, so a larger one may reach further.
        certain: !crossCutting && !filesTruncated && main.length === 1 && ranked[0][1] / total >= 0.6,
        crossCutting,
      };
    }
  }

  if (repo.defaultArea) return { areas: [repo.defaultArea], source: "repo", certain: true, crossCutting: false };
  return { areas: [], source: "none", certain: false, crossCutting: false };
}

function areaLabels(labels) {
  const known = new Set(config.areaOrder);
  return [...new Set(labels.filter((l) => l.startsWith("area/")).map((l) => l.slice(5)))].filter((a) => known.has(a));
}

// --- parents and epics ---

function parentRef(p) {
  return { repo: p.repository.nameWithOwner, number: p.number, type: p.issueType?.name ?? null };
}

function epicOf(issue) {
  for (let p = issue.parent; p; p = p.parent) {
    if (p.issueType?.name === "Epic") return { repo: p.repository.nameWithOwner, number: p.number };
  }
  return null;
}

function collectParents(deliveries) {
  const byKey = new Map();
  const inPeriod = new Map();
  for (const d of deliveries) {
    const keys = new Set([d.parent, d.epic].filter(Boolean).map((ref) => `${ref.repo}#${ref.number}`));
    for (const key of keys) inPeriod.set(key, (inPeriod.get(key) ?? 0) + 1);
  }
  for (const key of inPeriod.keys()) {
    const [repo, number] = key.split("#");
    const [owner, nameOnly] = repo.split("/");
    const data = gql(
      `query($owner: String!, $name: String!, $number: Int!) { repository(owner: $owner, name: $name) { issue(number: $number) { ...ParentFields } } } ${PARENT_FIELDS}`,
      { owner, name: nameOnly, number: Number(number) },
    ).repository.issue;
    byKey.set(key, {
      repo,
      number: data.number,
      url: data.url,
      title: clean(data.title),
      type: data.issueType?.name ?? null,
      state: data.state,
      closedInPeriod: data.state === "CLOSED" && data.stateReason === "COMPLETED" && new Date(data.closedAt) >= from,
      areas: areaLabels(labelNames(data)),
      subIssues: data.subIssuesSummary,
      deliveriesInPeriod: inPeriod.get(key),
    });
  }
  return [...byKey.values()];
}

// --- releases ---

// Releases come newest first, so pages are read until one ends before the period.
function releases(name) {
  const list = [];
  for (let page = 1; ; page++) {
    const batch = JSON.parse(gh(["api", `repos/${name}/releases?per_page=100&page=${page}`]));
    list.push(...batch);
    const oldest = batch.at(-1);
    if (batch.length < 100 || new Date(oldest.published_at ?? oldest.created_at) < from) break;
  }
  return list
    .filter((r) => !r.draft && r.published_at && new Date(r.published_at) >= from && new Date(r.published_at) <= to)
    .map((r) => ({ repo: name, name: clean(r.name || r.tag_name), tag: r.tag_name, url: r.html_url, publishedAt: r.published_at, prerelease: r.prerelease }));
}

// --- exclusion ---

function isExcluded(pr) {
  const login = (pr.author?.login ?? "").toLowerCase();
  if (pr.author?.__typename === "Bot" || login.endsWith("[bot]")) return true;
  if (config.exclude.authorLogins.some((l) => login === l.toLowerCase())) return true;
  return config.exclude.titlePatterns.some((p) => new RegExp(p, "i").test(pr.title));
}

// --- helpers ---

function search(query, type, fields) {
  const results = [];
  let after = null;
  do {
    const data = gql(
      `query($q: String!, $after: String) { search(type: ISSUE, query: $q, first: 50, after: $after) {
         pageInfo { hasNextPage endCursor }
         nodes { ... on ${type} { ${fields} } }
       } } ${fields.includes("...ParentFields") ? PARENT_FIELDS : ""}`,
      { q: query, after },
    ).search;
    results.push(...data.nodes.filter((n) => n?.number));
    after = data.pageInfo.hasNextPage ? data.pageInfo.endCursor : null;
  } while (after);
  return results;
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
    return execFileSync("gh", argv, { encoding: "utf8", maxBuffer: 64 * 1024 * 1024 });
  } catch (e) {
    fail(`gh ${argv.slice(0, 2).join(" ")} failed: ${e.stderr || e.message}`);
  }
}

function labelNames(node) {
  return node.labels?.nodes?.map((l) => l.name) ?? [];
}

function issueRefs(text) {
  return [...(text ?? "").matchAll(/(?<![\w/])#(\d+)\b/g)].map((m) => Number(m[1]));
}

// Only the closing and reference trailers of a PR body, so a stray "#12" in prose does not link.
function trailerLines(body) {
  return (body ?? "")
    .split("\n")
    .filter((l) => /^\s*(resolves|closes|fixes|refs|part of)\b/i.test(l))
    .join("\n");
}

function conventionalType(title) {
  return /^(\w+)(\([^)]*\))?!?:/.exec(title)?.[1]?.toLowerCase() ?? null;
}

// No person ends up in the report: drop @mentions, including @org/team.
function clean(text) {
  return text.replace(/(^|[^\w`])@[\w-]+(\/[\w-]+)?/g, "$1").replace(/\s{2,}/g, " ").trim();
}

function summarise(repos) {
  const all = repos.flatMap((r) => r.deliveries);
  const sum = (k) => repos.reduce((s, r) => s + r.counts[k], 0);
  return {
    issuesCompleted: sum("issuesCompleted"),
    issuesNotCompleted: sum("issuesNotCompleted"),
    prsMerged: sum("prsMerged"),
    prsExcluded: sum("prsExcluded"),
    areaUncertain: all.filter((d) => !d.areaCertain).length,
    likelyBacklogCleanup: all.filter((d) => d.likelyBacklogCleanup).length,
    perRepo: Object.fromEntries(repos.map((r) => [r.name, r.counts])),
  };
}

function parseArgs(argv) {
  const out = {};
  for (let i = 0; i < argv.length; i++) {
    const m = /^--(from|to|out|config)$/.exec(argv[i]);
    if (!m) fail(`Unknown argument: ${argv[i]}`);
    out[m[1]] = argv[++i];
  }
  for (const k of ["from", "to"]) {
    if (out[k] && !/^\d{4}-\d{2}-\d{2}$/.test(out[k])) fail(`--${k} must be YYYY-MM-DD`);
  }
  return out;
}

// --- time zone arithmetic, on calendar dates (YYYY-MM-DD) in the configured zone ---

function zonedDate(date) {
  return new Intl.DateTimeFormat("en-CA", { timeZone: tz, year: "numeric", month: "2-digit", day: "2-digit" }).format(date);
}

function offsetAt(date) {
  const name = new Intl.DateTimeFormat("en-US", { timeZone: tz, timeZoneName: "longOffset" })
    .formatToParts(date)
    .find((p) => p.type === "timeZoneName").value;
  return name === "GMT" ? "+00:00" : name.slice(3);
}

// Midnight local time. The offset is taken at noon the same day, which is right on every
// day DST cannot change at midnight, as in Europe.
function zonedMidnight(ymd) {
  return new Date(`${ymd}T00:00:00${offsetAt(new Date(`${ymd}T12:00:00Z`))}`);
}

function isoInZone(date) {
  const time = new Intl.DateTimeFormat("en-GB", { timeZone: tz, hour: "2-digit", minute: "2-digit", second: "2-digit", hourCycle: "h23" }).format(date);
  return `${zonedDate(date)}T${time}${offsetAt(date)}`;
}

function mondayOf(date) {
  const ymd = zonedDate(date);
  const weekday = new Date(`${ymd}T12:00:00Z`).getUTCDay() || 7;
  return addDays(ymd, 1 - weekday);
}

function addDays(ymd, days) {
  const d = new Date(`${ymd}T12:00:00Z`);
  d.setUTCDate(d.getUTCDate() + days);
  return d.toISOString().slice(0, 10);
}

function isoWeek(date) {
  const d = new Date(`${zonedDate(date)}T12:00:00Z`);
  const day = d.getUTCDay() || 7;
  d.setUTCDate(d.getUTCDate() + 4 - day);
  const year = d.getUTCFullYear();
  const week = Math.ceil(((d - Date.UTC(year, 0, 1)) / 86400000 + 1) / 7);
  return `${year}-W${String(week).padStart(2, "0")}`;
}

function fail(message) {
  console.error(`collect.mjs: ${message}`);
  process.exit(1);
}
