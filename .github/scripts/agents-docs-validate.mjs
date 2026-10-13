// Validates the agent-guidance contract from docs/adr/0002-tool-neutral-agent-contract.md.
//
//   1. Pairing    every AGENTS.md has a sibling CLAUDE.md that imports it with "@AGENTS.md",
//                 and no directory has a CLAUDE.md without an AGENTS.md beside it.
//   2. Pointers   a CLAUDE.md is the import line plus at most five further lines of content.
//   3. Links      every relative markdown link resolves, in AGENTS.md, CLAUDE.md,
//                 .github/copilot-instructions.md and docs/adr/**.
//   4. Ownership  every AGENTS.md and CLAUDE.md is named in .github/CODEOWNERS.
//   5. Coverage   every vertical that has an AGENTS.md is linked from the root AGENTS.md.
//   6. Presence   every app vertical has an AGENTS.md, except those still listed in
//                 PRESENCE_EXEMPT below while #4078 is outstanding.
//   7. Budget     a warning, not an error, when a file is over the word budget. Words, not
//                 lines: a wide table row and a rule line cost the same line and very
//                 different context. See the ADR's "Alternatives considered".
//
// No dependencies, so it runs with plain `node` and needs no install:
//   node .github/scripts/agents-docs-validate.mjs

import { execFileSync } from "node:child_process";
import fs from "node:fs";
import path from "node:path";

const ROOT_DOC = "AGENTS.md";
const CODEOWNERS = ".github/CODEOWNERS";
const VERTICAL_DIRS = ["src/apps", "src/libs", "src/pkgs", "src/tools"];
const POINTER_MAX_CONTENT_LINES = 5;
const WORD_BUDGET = 1000;

// App verticals that do not have an AGENTS.md yet. Remove a name when its file lands;
// the list must be empty when #4078 is closed.
const PRESENCE_EXEMPT = new Set([
  "src/apps/Altinn.Register", // placeholder until #4056 brings the real Register
]);

const errors = [];
const warnings = [];

// git repeats a path when several patterns match it, and once per stage while a merge
// conflict is unresolved, so the list is deduplicated before anything counts it.
const tracked = (...patterns) => [
  ...new Set(
    execFileSync("git", ["ls-files", "--cached", "--others", "--exclude-standard", "--", ...patterns], {
      encoding: "utf8",
      maxBuffer: 16 * 1024 * 1024,
    })
      .split("\n")
      .filter(Boolean),
  ),
];

const read = (file) => fs.readFileSync(file, "utf8");
const countWords = (text) => text.split(/\s+/u).filter(Boolean).length;

// git lists tracked paths even when the working tree no longer has them, which happens
// mid-rename and in a partial checkout. Read only what is actually on disk.
const agentsFiles = tracked("AGENTS.md", "**/AGENTS.md").filter((f) => fs.existsSync(f));
const claudeFiles = tracked("CLAUDE.md", "**/CLAUDE.md").filter((f) => fs.existsSync(f));
const agentsDirs = new Set(agentsFiles.map((f) => path.dirname(f)));
const claudeDirs = new Set(claudeFiles.map((f) => path.dirname(f)));

// --- 1 and 2: pairing and pointer shape ---

for (const dir of agentsDirs) {
  if (!claudeDirs.has(dir)) {
    errors.push(`${path.join(dir, "AGENTS.md")}: no sibling CLAUDE.md. Add one containing "@AGENTS.md".`);
  }
}

for (const dir of claudeDirs) {
  const pointer = path.join(dir, "CLAUDE.md");
  if (!agentsDirs.has(dir)) {
    errors.push(`${pointer}: no sibling AGENTS.md. Guidance lives in AGENTS.md; CLAUDE.md only points at it.`);
    continue;
  }
  const lines = read(pointer).split("\n");
  if (!lines.some((line) => /(^|\s)@AGENTS\.md(\s|$)/u.test(line))) {
    errors.push(`${pointer}: must import the sibling file with "@AGENTS.md".`);
  }
  // Headings count. The ADR allows five further lines in total, not five plus as many
  // headings as someone cares to add.
  const content = lines.filter((line) => line.trim() && !/(^|\s)@AGENTS\.md(\s|$)/u.test(line));
  if (content.length > POINTER_MAX_CONTENT_LINES) {
    errors.push(
      `${pointer}: ${content.length} lines of content beside the import, at most ${POINTER_MAX_CONTENT_LINES} allowed. Move the rest into AGENTS.md.`,
    );
  }
}

// --- 3: relative links ---

// Inline links, with an optional title: [text](path "title") and [text](<path>).
const INLINE_LINK = /\[[^\]]*\]\(\s*<?([^)<>\s]+)>?(?:\s+["'(][^)]*)?\)/gu;
// Reference definitions, which is where a reference-style link [text][label] resolves to.
const LINK_DEFINITION = /^\s{0,3}\[[^\]]+\]:\s*<?([^\s<>]+)>?/gmu;

const linkedFiles = [
  ...agentsFiles,
  ...claudeFiles,
  ...tracked(".github/copilot-instructions.md", "docs/adr/*.md", "**/docs/adr/*.md").filter((f) => fs.existsSync(f)),
];

for (const file of linkedFiles) {
  const text = read(file);
  for (const pattern of [INLINE_LINK, LINK_DEFINITION]) {
    for (const match of text.matchAll(pattern)) {
      const target = match[1];
      if (/^(https?:|mailto:|tel:|#)/u.test(target)) continue;
      const withoutAnchor = decodeURIComponent(target.split("#")[0]);
      if (!withoutAnchor) continue;
      const resolved = withoutAnchor.startsWith("/")
        ? withoutAnchor.slice(1)
        : path.join(path.dirname(file), withoutAnchor);
      if (!fs.existsSync(resolved)) {
        errors.push(`${file}: broken link "${target}" (looked for ${resolved}).`);
      }
    }
  }
}

// --- 4: ownership ---

if (!fs.existsSync(CODEOWNERS)) {
  errors.push(`${CODEOWNERS}: missing.`);
} else {
  // A line with a pattern and no owners deliberately leaves the path unowned, so it is not
  // coverage. Only a line that names at least one owner counts.
  const owned = new Set(
    read(CODEOWNERS)
      .split("\n")
      .map((line) => line.replace(/#.*$/u, "").trim())
      .filter(Boolean)
      .map((line) => line.split(/\s+/u))
      .filter((fields) => fields.length > 1 && fields.slice(1).some((owner) => owner.startsWith("@") || owner.includes("@")))
      .map((fields) => fields[0].replace(/^\//u, "")),
  );
  for (const file of [...agentsFiles, ...claudeFiles]) {
    if (!owned.has(file)) {
      errors.push(`${file}: not covered by ${CODEOWNERS}. Add a line naming its owning team.`);
    }
  }
}

// --- 5 and 6: root map coverage and presence ---

if (!agentsFiles.includes(ROOT_DOC)) {
  errors.push(`${ROOT_DOC}: missing. It is the top of the hierarchy.`);
} else {
  const rootDoc = read(ROOT_DOC);

  for (const file of agentsFiles) {
    if (file === ROOT_DOC) continue;
    if (!rootDoc.includes(file)) {
      errors.push(`${ROOT_DOC}: does not link ${file}. Every vertical with guidance belongs in the root map.`);
    }
  }

  for (const dir of VERTICAL_DIRS) {
    if (!fs.existsSync(dir)) continue;

    // The map names each deployable app, because each one has its own guidance and its own
    // landmines. Libraries, packages and tools are covered by their parent row, which is how
    // the root file is written: naming all twelve would cost more than it tells anyone.
    if (!rootDoc.includes(dir)) {
      errors.push(`${ROOT_DOC}: "${dir}" is not mentioned in the repository map.`);
    }

    for (const entry of fs.readdirSync(dir, { withFileTypes: true })) {
      if (!entry.isDirectory()) continue;
      const vertical = `${dir}/${entry.name}`;
      if (dir === "src/apps") {
        if (!rootDoc.includes(entry.name)) {
          errors.push(`${ROOT_DOC}: app vertical "${vertical}" is not named in the repository map.`);
        }
        if (!agentsDirs.has(vertical) && !PRESENCE_EXEMPT.has(vertical)) {
          errors.push(`${vertical}: app vertical without an AGENTS.md.`);
        }
      }
    }
  }

  for (const exempt of PRESENCE_EXEMPT) {
    if (agentsDirs.has(exempt)) {
      errors.push(`${exempt}: now has an AGENTS.md. Remove it from PRESENCE_EXEMPT in this script.`);
    }
  }
}

// --- 7: budget ---

for (const file of agentsFiles) {
  const words = countWords(read(file));
  if (words > WORD_BUDGET) {
    warnings.push(
      `${file}: ${words} words, over the ${WORD_BUDGET}-word budget. Usually a sign that something conditional or task-specific belongs in a skill, a nested file or a linked doc.`,
    );
  }
}

// --- report ---

for (const warning of warnings) console.warn(`  warning: ${warning}`);

if (errors.length > 0) {
  console.error(`\nAgent-guidance validation failed with ${errors.length} problem(s):\n`);
  for (const error of errors) console.error(`  - ${error}`);
  console.error("\nThe contract is docs/adr/0002-tool-neutral-agent-contract.md.");
  process.exit(1);
}

console.log(
  `Agent-guidance validation passed: ${agentsFiles.length} AGENTS.md, ${claudeFiles.length} CLAUDE.md, ${warnings.length} warning(s).`,
);
