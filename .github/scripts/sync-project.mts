import { Chalk } from "chalk";
import { Octokit } from "@octokit/action";
import fs from "node:fs";

const c = new Chalk({ level: 3 });

const ghToken = process.env.GITHUB_TOKEN;
const projectNumberString = process.env.GITHUB_PROJECT_NUMBER;
const dryRun = process.env.DRY_RUN === "true";
const summaryFile = process.env.GITHUB_STEP_SUMMARY;

if (!ghToken || !projectNumberString) {
    console.error("Missing required environment variables");
    process.exit(1);
}

const owner = "Altinn";
const repo = "altinn-auth";
const projectNumber = Number.parseInt(projectNumberString, 10);

const github = new Octokit({
    auth: ghToken,
});

type IssuesResponse = {
    readonly organization: {
        readonly projectV2: {
            readonly id: string;
            readonly title: string;
        } | null;
        readonly repository: {
            readonly issues: {
                readonly pageInfo: {
                    readonly hasNextPage: boolean;
                    readonly endCursor: string | null;
                };
                readonly nodes: readonly {
                    readonly id: string;
                    readonly number: number;
                    readonly title: string;
                    readonly projectItems: {
                        readonly nodes: readonly {
                            readonly project: { readonly number: number };
                        }[];
                    };
                }[];
            };
        };
    };
};

type MissingIssue = {
    readonly id: string;
    readonly number: number;
    readonly title: string;
};

// Archived items count as being on the project, so an item someone archived
// on purpose is not added back.
const missing: MissingIssue[] = [];
let projectId: string | undefined;
let projectTitle: string | undefined;
let openCount = 0;
let cursor: string | null = null;

do {
    const page: IssuesResponse = await github.graphql<IssuesResponse>(
        `
query($org: String!, $repo: String!, $project: Int!, $cursor: String) {
  organization(login: $org) {
    projectV2(number: $project) {
      id
      title
    }
    repository(name: $repo) {
      issues(first: 100, after: $cursor, states: OPEN) {
        pageInfo {
          hasNextPage
          endCursor
        }
        nodes {
          id
          number
          title
          projectItems(first: 50, includeArchived: true) {
            nodes {
              project {
                number
              }
            }
          }
        }
      }
    }
  }
}
    `.trim(),
        { org: owner, repo, project: projectNumber, cursor }
    );

    if (!page.organization.projectV2) {
        console.error("Project not found");
        process.exit(1);
    }

    projectId = page.organization.projectV2.id;
    projectTitle = page.organization.projectV2.title;

    const issues = page.organization.repository.issues;
    openCount += issues.nodes.length;
    for (const issue of issues.nodes) {
        if (!issue.projectItems.nodes.some((i) => i.project.number === projectNumber)) {
            missing.push({ id: issue.id, number: issue.number, title: issue.title });
        }
    }

    cursor = issues.pageInfo.hasNextPage ? issues.pageInfo.endCursor : null;
} while (cursor);

console.log(
    `${openCount} open issues, ${c.yellow(missing.length)} missing from project ${c.yellow(projectTitle)}.`
);

const added: MissingIssue[] = [];
const failed: MissingIssue[] = [];

// One mutation per issue: batching several adds in one request has given
// 502 responses from the GitHub API.
for (const issue of missing) {
    if (dryRun) {
        console.log(`Would add #${issue.number} ${c.cyan(issue.title)}`);
        continue;
    }

    try {
        await github.graphql(
            `
mutation($projectId: ID!, $issueId: ID!) {
  addProjectV2ItemById(input: {
    projectId: $projectId,
    contentId: $issueId
  }) {
    item {
      id
    }
  }
}
      `,
            { projectId, issueId: issue.id }
        );
        added.push(issue);
        console.log(`Added #${issue.number} ${c.cyan(issue.title)}`);
    } catch (error) {
        failed.push(issue);
        console.error(`Failed to add #${issue.number}: ${error}`);
    }
}

if (summaryFile) {
    const lines = [
        `## Sync to project ${projectTitle}`,
        "",
        `${openCount} open issues, ${missing.length} missing from the project${dryRun ? " (dry run, nothing added)" : ""}.`,
        "",
        ...missing.map((i) => {
            const status = dryRun ? "would add" : failed.includes(i) ? "failed" : "added";
            return `- #${i.number} ${i.title}: ${status}`;
        }),
    ];
    fs.appendFileSync(summaryFile, lines.join("\n") + "\n");
}

if (failed.length > 0) {
    process.exit(1);
}
