// Turns a ukesrapport written in markdown into rich text that pastes correctly into Slack and
// Teams. Neither renders markdown pasted as plain text, but both keep bold, italics, links and
// lists from HTML on the clipboard, as when copying from a web page.
//
//   node .agents/skills/ukesrapport/scripts/render.mjs <report.md> [--no-copy] [--no-open]
//
// Writes <report>.html next to the input. On Windows it also puts the HTML on the clipboard,
// so Ctrl+V in Slack or Teams pastes the formatted report. Elsewhere, or with --no-copy, it
// opens the HTML file in the browser to copy from (Ctrl+A, Ctrl+C).
//
// Handles the subset the skill's template uses: paragraphs, "- " lists, **bold**, _italic_,
// `code` and [text](url).

import { execFileSync, spawn } from "node:child_process";
import fs from "node:fs";
import os from "node:os";
import path from "node:path";

const argv = process.argv.slice(2);
const input = argv.find((a) => !a.startsWith("--"));
if (!input) fail("Usage: render.mjs <report.md> [--no-copy] [--no-open]");
const copy = !argv.includes("--no-copy") && process.platform === "win32";
const open = !argv.includes("--no-open") && !copy;

const markdown = fs.readFileSync(input, "utf8").replace(/\r\n/g, "\n");
const body = toHtml(markdown);
const output = input.replace(/\.(md|txt)$/i, "") + ".html";
fs.writeFileSync(
  output,
  `<!doctype html><html lang="nb"><head><meta charset="utf-8"><title>Ukesrapport</title></head>` +
    `<body style="font-family: sans-serif; max-width: 48rem; margin: 2rem auto; line-height: 1.5">${body}</body></html>\n`,
);

if (copy && copyHtmlOnWindows(body, markdown)) {
  console.log(`Copied to the clipboard as rich text. Paste with Ctrl+V in Slack or Teams.`);
}
if (open) openFile(output);
console.log(`HTML: ${output}`);

function toHtml(text) {
  const blocks = [];
  let list = null;
  let paragraph = [];
  const flush = () => {
    if (paragraph.length) blocks.push(`<p>${paragraph.map(inline).join("<br>")}</p>`);
    paragraph = [];
    if (list) blocks.push(`<ul>${list.map((li) => `<li>${inline(li)}</li>`).join("")}</ul>`);
    list = null;
  };
  for (const line of text.split("\n")) {
    const item = /^\s*[-*] (.*)$/.exec(line);
    if (item) {
      if (paragraph.length) {
        blocks.push(`<p>${paragraph.map(inline).join("<br>")}</p>`);
        paragraph = [];
      }
      (list ??= []).push(item[1]);
    } else if (!line.trim()) {
      flush();
    } else {
      if (list) flush();
      paragraph.push(line.trim());
    }
  }
  flush();
  return blocks.join("\n");
}

function inline(text) {
  return escape(text)
    .replace(/`([^`]+)`/g, "<code>$1</code>")
    // Only web links: report text comes partly from issue titles, so no javascript: and the like.
    .replace(/\[([^\]]+)\]\((https?:\/\/[^)\s]+)\)/g, '<a href="$2">$1</a>')
    .replace(/\*\*([^*]+)\*\*/g, "<b>$1</b>")
    .replace(/(^|[\s(])_([^_]+)_(?=$|[\s).,:;!?])/g, "$1<i>$2</i>");
}

function escape(text) {
  return text.replace(/&/g, "&amp;").replace(/</g, "&lt;").replace(/>/g, "&gt;").replace(/"/g, "&quot;");
}

// Windows PowerShell 5.1 (powershell.exe, present on every Windows machine) can put HTML on
// the clipboard; PowerShell 7 dropped -AsHtml. The plain-text markdown is set first so editors
// that do not take HTML still get something readable.
function copyHtmlOnWindows(html, plain) {
  const dir = fs.mkdtempSync(path.join(os.tmpdir(), "ukesrapport-"));
  const htmlFile = path.join(dir, "report.html");
  const textFile = path.join(dir, "report.txt");
  fs.writeFileSync(htmlFile, html);
  fs.writeFileSync(textFile, plain);
  const script =
    `$ErrorActionPreference = 'Stop'; Add-Type -AssemblyName System.Windows.Forms; ` +
    `$html = [IO.File]::ReadAllText('${htmlFile}', [Text.Encoding]::UTF8); ` +
    `$text = [IO.File]::ReadAllText('${textFile}', [Text.Encoding]::UTF8); ` +
    `$data = New-Object System.Windows.Forms.DataObject; ` +
    `$data.SetData([System.Windows.Forms.DataFormats]::Html, (CfHtml $html)); ` +
    `$data.SetData([System.Windows.Forms.DataFormats]::UnicodeText, $text); ` +
    `[System.Windows.Forms.Clipboard]::SetDataObject($data, $true)`;
  try {
    execFileSync("powershell.exe", ["-NoProfile", "-STA", "-Command", cfHtmlFunction() + script], { stdio: ["ignore", "ignore", "pipe"] });
    return true;
  } catch (e) {
    console.error(`Could not copy to the clipboard (${String(e.stderr || e.message).trim()}). Opening the HTML file instead.`);
    openFile(output);
    return false;
  } finally {
    fs.rmSync(dir, { recursive: true, force: true });
  }
}

// The Windows clipboard's HTML format needs a header with byte offsets into the UTF-8 payload.
function cfHtmlFunction() {
  return `
function CfHtml([string]$fragment) {
  $enc = [Text.Encoding]::UTF8
  $pre = '<html><body><!--StartFragment-->'
  $post = '<!--EndFragment--></body></html>'
  $header = "Version:0.9\`r\`nStartHTML:{0:D10}\`r\`nEndHTML:{1:D10}\`r\`nStartFragment:{2:D10}\`r\`nEndFragment:{3:D10}\`r\`n"
  $headerLength = $enc.GetByteCount(($header -f 0, 0, 0, 0))
  $startFragment = $headerLength + $enc.GetByteCount($pre)
  $endFragment = $startFragment + $enc.GetByteCount($fragment)
  $endHtml = $endFragment + $enc.GetByteCount($post)
  return ($header -f $headerLength, $endHtml, $startFragment, $endFragment) + $pre + $fragment + $post
}
`;
}

function openFile(file) {
  const [cmd, args] =
    process.platform === "win32" ? ["cmd", ["/c", "start", "", file]] : process.platform === "darwin" ? ["open", [file]] : ["xdg-open", [file]];
  spawn(cmd, args, { detached: true, stdio: "ignore" }).unref();
}

function fail(message) {
  console.error(`render.mjs: ${message}`);
  process.exit(1);
}
