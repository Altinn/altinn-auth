// Finds open issues that look done, for the lukkekandidater skill.
//
//   node .agents/skills/lukkekandidater/scripts/collect.mjs [--repo owner/name] [--out file]
//
// Reads every open issue in the configured repositories and scores it on signals that the work
// is finished: a merged PR that closes or references it, all sub-issues completed, a "Done"
// status in a GitHub Project, a fully ticked checklist, and recent comments that say it is done.
// Counter-signals (an open linked PR, reopened after the last merge) pull the score down.
// It only reads; it never comments on or closes anything.
//
// Issues, never people: authors are read only as bot-or-human for comments and are not written
// to the output; @mentions are stripped from titles and comment excerpts.
//
// No dependencies beyond Node 20+ and an authenticated GitHub CLI with the `project` scope
// (`gh auth refresh -s project`); without it the project signal is skipped and reported.

import { execFileSync } from "node:child_process";
import fs from "node:fs";
import os from "node:os";
import path from "node:path";
import { fileURLToPath } from "node:url";

const here = path.dirname(fileURLToPath(import.meta.url));
const args = parseArgs(process.argv.slice(2));
const config = JSON.parse(fs.readFileSync(args.config ?? path.join(here, "..", "config.json"), "utf8"));
const repos = args.repo ? config.repos.filter((r) => r.name === args.repo) : config.repos;
if (!repos.length) fail(`Repository ${args.repo} is not in config.json`);

const STRONG = 3;
const MEDIUM = 2;
const WEAK = 1;
const COUNTER = -2;

const doneStatuses = new Set(config.doneStatuses.map(normaliseStatus));
const wordList = (words) => new RegExp(`(^|[^\\p{L}])(${words.map(escapeRegex).join("|")})(?=$|[^\\p{L}])`, "iu");
const doneWords = wordList(config.doneWords);
const notDoneWords = wordList(config.notDoneWords);
// Words just before the match that turn it into "not done": negations, conditions ("when this is
// fixed") and intentions ("should be done").
const negation = /(ikke|ikkje|not|n't|never|aldri|still|when|once|until|before|if|når|før|hvis|til|be|bli|blir|skal|should|must|will|needs?)\s+(\S+\s+){0,3}$/i;

let projectsReadable = true;
const now = new Date();

const results = repos.map(scanRepo);
const candidates = results.flatMap((r) => r.candidates).sort((a, b) => b.score - a.score || a.repo.localeCompare(b.repo) || a.number - b.number);

const output = {
  generatedAt: now.toISOString(),
  config: { repos: repos.map((r) => r.name), areaOrder: config.areaOrder, areaNames: config.areaNames },
  projectsReadable,
  candidates,
  stats: {
    openIssuesScanned: results.reduce((s, r) => s + r.scanned, 0),
    perRepo: Object.fromEntries(results.map((r) => [r.name, r.scanned])),
    strong: candidates.filter((c) => c.strength === "strong").length,
    possible: candidates.filter((c) => c.strength === "possible").length,
    weakOnly: results.reduce((s, r) => s + r.weakOnly, 0),
  },
};

const out = args.out ?? path.join(os.tmpdir(), `lukkekandidater-${now.toISOString().slice(0, 10)}.json`);
fs.writeFileSync(out, JSON.stringify(output, null, 2));

console.log(`Scanned:    ${output.stats.openIssuesScanned} open issues in ${repos.length} repositories`);
console.log(`Candidates: ${output.stats.strong} strong, ${output.stats.possible} possible (${output.stats.weakOnly} with a weak signal only, left out)`);
if (!projectsReadable) console.log(`Projects:   not readable with this token; run "gh auth refresh -s project" for the status signal`);
console.log(`Written to: ${out}`);

// --- per repository ---

function scanRepo(repo) {
  const [owner, name] = repo.name.split("/");
  const issues = openIssues(owner, name);
  const candidates = [];
  let weakOnly = 0;
  for (const issue of issues) {
    const scored = score(repo, issue);
    if (scored.strength === "none") continue;
    if (scored.strength === "weak") {
      weakOnly++;
      continue;
    }
    candidates.push(scored);
  }
  return { name: repo.name, scanned: issues.length, candidates, weakOnly };
}

function score(repo, issue) {
  const signals = [];
  const counters = [];

  // Pull requests, from the closing references and from the timeline, keyed by URL.
  const prs = new Map();
  const addPr = (pr, closes) => {
    if (!pr?.url) return;
    const known = prs.get(pr.url);
    prs.set(pr.url, { ...pr, closes: closes || known?.closes || false });
  };
  issue.closedByPullRequestsReferences.nodes.forEach((pr) => addPr(pr, true));
  for (const event of issue.timelineItems.nodes) {
    if (event.__typename === "CrossReferencedEvent" && event.source?.__typename === "PullRequest") addPr(event.source, event.willCloseTarget);
    if (event.__typename === "ConnectedEvent" && event.subject?.__typename === "PullRequest") addPr(event.subject, true);
  }

  // A PR that closes the issue is strong evidence. One that only mentions it is weak: epics and
  // features are mentioned by many PRs long before they are done, so it counts once at most.
  const merged = [...prs.values()].filter((pr) => pr.merged);
  for (const pr of merged.filter((pr) => pr.closes)) {
    const branch = pr.baseRefName && !["main", "master"].includes(pr.baseRefName) ? ` til ${pr.baseRefName}` : "";
    signals.push({ kind: "merged-closing-pr", weight: STRONG, detail: `PR som skal lukke saken er merget${branch}`, ref: prRef(pr) });
  }
  const referencing = merged.filter((pr) => !pr.closes).sort((a, b) => Date.parse(b.mergedAt) - Date.parse(a.mergedAt));
  if (referencing.length) {
    signals.push({
      kind: "merged-referencing-pr",
      weight: WEAK,
      detail: referencing.length === 1 ? "En merget PR nevner saken" : `${referencing.length} mergede PR-er nevner saken`,
      ref: prRef(referencing[0]),
    });
  }

  const openPrs = [...prs.values()].filter((pr) => pr.state === "OPEN");
  for (const pr of openPrs) counters.push({ kind: "open-pr", weight: COUNTER, detail: "Koblet PR er fortsatt åpen", ref: prRef(pr) });

  const lastMerge = Math.max(0, ...merged.map((pr) => Date.parse(pr.mergedAt)));
  const reopened = issue.timelineItems.nodes.filter((e) => e.__typename === "ReopenedEvent").map((e) => Date.parse(e.createdAt));
  if (lastMerge && reopened.some((t) => t > lastMerge)) counters.push({ kind: "reopened", weight: COUNTER, detail: "Gjenåpnet etter siste merge" });

  // Sub-issues.
  const subs = issue.subIssuesSummary;
  if (subs.total > 0 && subs.completed === subs.total) {
    signals.push({ kind: "subissues-complete", weight: STRONG, detail: `Alle ${subs.total} sub-issues er ferdige` });
  }

  // Project status.
  for (const item of issue.projectItems?.nodes ?? []) {
    const status = item.fieldValueByName?.name;
    if (item.project?.closed || !status) continue;
    if (doneStatuses.has(normaliseStatus(status))) {
      signals.push({ kind: "project-done", weight: STRONG, detail: `Status «${status.trim()}» i ${item.project.title}` });
    }
  }

  // Checklist in the description, outside code blocks.
  const tasks = checklist(issue.body);
  if (tasks.total >= 2 && tasks.checked === tasks.total) {
    signals.push({ kind: "checklist-complete", weight: MEDIUM, detail: `Alle ${tasks.total} punkter i sjekklisten er krysset av` });
  }

  // Recent human comments that say it is done. Only the latest one, stated rather than asked,
  // counts for more than a hint; earlier ones count once at most.
  const comments = issue.comments.nodes.filter((c) => c.author?.__typename !== "Bot");
  const latest = comments.at(-1);
  const latestExcerpt = latest && doneExcerpt(latest.body);
  if (latestExcerpt) {
    const question = latestExcerpt.endsWith("?");
    signals.push({
      kind: "comment-says-done",
      weight: question ? WEAK : MEDIUM,
      detail: question ? "Siste kommentar spør om saken er løst eller kan lukkes" : "Siste kommentar sier at saken er ferdig",
      excerpt: latestExcerpt,
      at: latest.createdAt,
    });
  } else {
    const earlier = comments
      .slice(0, -1)
      .map((c) => ({ c, excerpt: doneExcerpt(c.body) }))
      .filter((x) => x.excerpt)
      .at(-1);
    if (earlier) {
      signals.push({ kind: "comment-says-done", weight: WEAK, detail: "En nylig kommentar sier at noe er ferdig", excerpt: earlier.excerpt, at: earlier.c.createdAt });
    }
  }

  // The latest comment saying something is still wrong or left outweighs older signals.
  if (latest && notDoneWords.test(stripMarkup(latest.body))) {
    counters.push({ kind: "comment-says-not-done", weight: COUNTER, detail: "Siste kommentar sier at noe gjenstår eller fortsatt feiler", at: latest.createdAt });
  }

  // Two hints are still only hints: "possible" needs at least one medium or strong signal.
  const total = [...signals, ...counters].reduce((s, x) => s + x.weight, 0);
  const best = Math.max(0, ...signals.map((s) => s.weight));
  const strength = !signals.length
    ? "none"
    : best === STRONG && !counters.length
      ? "strong"
      : best >= MEDIUM && total >= MEDIUM
        ? "possible"
        : "weak";

  return {
    repo: repo.name,
    number: issue.number,
    url: issue.url,
    title: clean(issue.title),
    type: issue.issueType?.name ?? null,
    areas: areaOf(repo, issue),
    createdAt: issue.createdAt,
    updatedAt: issue.updatedAt,
    daysSinceUpdate: Math.floor((now - new Date(issue.updatedAt)) / 86400000),
    score: total,
    strength,
    signals,
    counters,
  };
}

// --- GitHub ---

function openIssues(owner, name) {
  const fields = (withProjects) => `
    number title url body createdAt updatedAt
    issueType { name }
    labels(first: 30) { nodes { name } }
    subIssuesSummary { total completed }
    ${withProjects ? `projectItems(first: 10) { nodes { project { title closed } fieldValueByName(name: "Status") { ... on ProjectV2ItemFieldSingleSelectValue { name } } } }` : ""}
    closedByPullRequestsReferences(first: 10, includeClosedPrs: true) { nodes { ...Pr } }
    timelineItems(last: 40, itemTypes: [CROSS_REFERENCED_EVENT, CONNECTED_EVENT, REOPENED_EVENT]) {
      nodes {
        __typename
        ... on CrossReferencedEvent { willCloseTarget source { __typename ... on PullRequest { ...Pr } } }
        ... on ConnectedEvent { subject { __typename ... on PullRequest { ...Pr } } }
        ... on ReopenedEvent { createdAt }
      }
    }
    comments(last: 5) { nodes { body createdAt author { __typename } } }`;
  const fragment = `fragment Pr on PullRequest { number url state merged mergedAt baseRefName repository { nameWithOwner } }`;

  const issues = [];
  let after = null;
  for (;;) {
    const query = `query($owner: String!, $name: String!, $after: String) { repository(owner: $owner, name: $name) {
      issues(states: OPEN, first: 40, after: $after, orderBy: { field: CREATED_AT, direction: ASC }) {
        pageInfo { hasNextPage endCursor }
        nodes { ${fields(projectsReadable)} }
      } } } ${fragment}`;
    let data;
    try {
      data = gql(query, { owner, name, after });
    } catch (e) {
      if (projectsReadable && /project/i.test(e.message)) {
        projectsReadable = false;
        continue; // the same page again, without project fields
      }
      throw e;
    }
    const page = data.repository.issues;
    issues.push(...page.nodes);
    if (!page.pageInfo.hasNextPage) return issues;
    after = page.pageInfo.endCursor;
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
    // gh exits non-zero on GraphQL errors but still prints the response.
    raw = e.stdout || "";
    if (!raw) throw new Error(String(e.stderr || e.message));
  }
  const res = JSON.parse(raw);
  if (res.errors?.length) throw new Error(res.errors.map((e) => e.message).join("; "));
  return res.data;
}

// --- helpers ---

function areaOf(repo, issue) {
  const known = new Set(config.areaOrder);
  const labels = issue.labels.nodes.map((l) => l.name).filter((l) => l.startsWith("area/")).map((l) => l.slice(5)).filter((a) => known.has(a));
  if (labels.length) return [...new Set(labels)];
  return repo.defaultArea ? [repo.defaultArea] : [];
}

function prRef(pr) {
  return { repo: pr.repository?.nameWithOwner ?? null, number: pr.number, url: pr.url, mergedAt: pr.mergedAt ?? null };
}

function checklist(body) {
  let checked = 0;
  let total = 0;
  let inCode = false;
  for (const line of (body ?? "").split("\n")) {
    if (/^\s*(```|~~~)/.test(line)) inCode = !inCode;
    if (inCode) continue;
    const m = /^\s*[-*+] \[( |x|X)\] /.exec(line);
    if (!m) continue;
    total++;
    if (m[1] !== " ") checked++;
  }
  return { checked, total };
}

// The sentence in a comment that says it is done, unless it is negated ("ikke løst").
function doneExcerpt(body) {
  for (const sentence of stripMarkup(body).split(/(?<=[.!?])\s+|\n+/)) {
    const m = doneWords.exec(sentence);
    if (!m) continue;
    if (negation.test(sentence.slice(0, m.index + m[1].length))) continue;
    const excerpt = clean(sentence);
    return excerpt.length > 160 ? `${excerpt.slice(0, 157)}...` : excerpt;
  }
  return null;
}

// Drop code, quotes, headings and emphasis markers, which would hide a "**not** fixed".
function stripMarkup(body) {
  return (body ?? "")
    .replace(/```[\s\S]*?```/g, " ")
    .replace(/^\s*(>|#).*$/gm, " ")
    .replace(/[*_~]+/g, "");
}

function normaliseStatus(status) {
  return status.toLowerCase().replace(/[^\p{L}\s]/gu, "").trim();
}

function escapeRegex(text) {
  return text.replace(/[.*+?^${}()|[\]\\]/g, "\\$&");
}

// No person ends up in the report: drop @mentions, including @org/team.
function clean(text) {
  return text.replace(/(^|[^\w`])@[\w-]+(\/[\w-]+)?/g, "$1").replace(/\s{2,}/g, " ").trim();
}

function parseArgs(argv) {
  const out = {};
  for (let i = 0; i < argv.length; i++) {
    const m = /^--(repo|out|config)$/.exec(argv[i]);
    if (!m) fail(`Unknown argument: ${argv[i]}`);
    out[m[1]] = argv[++i];
  }
  return out;
}

function fail(message) {
  console.error(`collect.mjs: ${message}`);
  process.exit(1);
}
