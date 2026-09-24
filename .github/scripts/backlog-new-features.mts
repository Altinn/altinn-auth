import { Chalk } from "chalk";
import { Octokit } from "@octokit/action";

const c = new Chalk({ level: 3 });

const ghToken = process.env.GITHUB_TOKEN;
const projectOwner = process.env.GITHUB_PROJECT_OWNER;
const projectNumberString = process.env.GITHUB_PROJECT_NUMBER;
const statusFieldName = process.env.GITHUB_STATUS_FIELD_NAME ?? "Status";
const issueTypesRaw = process.env.GITHUB_FEATURE_ISSUE_TYPES ?? "Feature";
const fromStatusRaw = process.env.GITHUB_FROM_STATUS ?? "New";
const backlogStatusRaw = process.env.GITHUB_BACKLOG_STATUS;
const dryRun = process.env.DRY_RUN === "true";

if (!ghToken || !projectOwner || !projectNumberString || !backlogStatusRaw) {
    console.error("Missing required environment variables");
    process.exit(1);
}

const projectNumber = Number.parseInt(projectNumberString, 10);

// Status names are compared with punctuation, emoji and casing removed. The board
// spells the refinement backlog with a chart emoji, and that decoration gets edited
// without anyone thinking of this script.
const normalize = (value: string) =>
    value.toLowerCase().replace(/[^a-z0-9]/g, "");

const featureIssueTypes = new Set(
    issueTypesRaw
        .split(",")
        .map((s) => s.trim())
        .filter((s) => s.length > 0)
        .map(normalize)
);

if (featureIssueTypes.size === 0) {
    console.error("GITHUB_FEATURE_ISSUE_TYPES listed no issue types");
    process.exit(1);
}

const fromStatus = normalize(fromStatusRaw);
const backlogStatus = normalize(backlogStatusRaw);

if (fromStatus === backlogStatus) {
    console.error(
        "GITHUB_FROM_STATUS and GITHUB_BACKLOG_STATUS name the same status"
    );
    process.exit(1);
}

const github = new Octokit({ auth: ghToken });

type StatusOption = {
    readonly id: string;
    readonly name: string;
};

type ProjectResponse = {
    readonly organization: {
        readonly projectV2: {
            readonly id: string;
            readonly title: string;
            readonly field: {
                readonly id: string;
                readonly name: string;
                readonly options: readonly StatusOption[];
            } | null;
        } | null;
    } | null;
};

const project = await github.graphql<ProjectResponse>(
    `
query($org: String!, $project: Int!, $statusField: String!) {
  organization(login: $org) {
    projectV2(number: $project) {
      id
      title
      field(name: $statusField) {
        ... on ProjectV2SingleSelectField {
          id
          name
          options { id name }
        }
      }
    }
  }
}
  `.trim(),
    { org: projectOwner, project: projectNumber, statusField: statusFieldName }
);

const projectV2 = project.organization?.projectV2;

if (!projectV2) {
    console.error(`Project ${projectNumber} not found for ${projectOwner}`);
    process.exit(1);
}

const statusField = projectV2.field;

if (!statusField) {
    console.error(
        `Project ${projectV2.title} has no single select field named ${statusFieldName}`
    );
    process.exit(1);
}

const backlogOption = statusField.options.find(
    (o) => normalize(o.name) === backlogStatus
);

// Without the target column there is nothing safe to do, and guessing a neighbouring
// column would move Features somewhere nobody refines them.
if (!backlogOption) {
    console.error(
        `Field ${statusFieldName} has no option matching ${backlogStatusRaw}`
    );
    process.exit(1);
}

console.log(
    `Moving ${c.yellow(fromStatusRaw)} Features into ${c.magenta(
        backlogOption.name
    )} on ${c.cyan(projectV2.title)}.`
);

type FieldValueNode = {
    readonly name?: string;
    readonly field?: { readonly name: string };
};

type Item = {
    readonly id: string;
    readonly content: {
        readonly number?: number;
        readonly title?: string;
        readonly issueType?: { readonly name: string } | null;
    } | null;
    readonly fieldValues: { readonly nodes: readonly FieldValueNode[] };
};

type ItemsResponse = {
    readonly organization: {
        readonly projectV2: {
            readonly items: {
                readonly pageInfo: {
                    readonly hasNextPage: boolean;
                    readonly endCursor: string | null;
                };
                readonly nodes: readonly Item[];
            };
        };
    };
};

const itemsQuery = `
query($org: String!, $project: Int!, $after: String) {
  organization(login: $org) {
    projectV2(number: $project) {
      items(first: 100, after: $after) {
        pageInfo { hasNextPage endCursor }
        nodes {
          id
          content {
            ... on Issue {
              number
              title
              issueType { name }
            }
          }
          fieldValues(first: 30) {
            nodes {
              ... on ProjectV2ItemFieldSingleSelectValue {
                name
                field { ... on ProjectV2FieldCommon { name } }
              }
            }
          }
        }
      }
    }
  }
}
`.trim();

const updateMutation = `
mutation($projectId: ID!, $itemId: ID!, $fieldId: ID!, $optionId: String!) {
  updateProjectV2ItemFieldValue(input: {
    projectId: $projectId,
    itemId: $itemId,
    fieldId: $fieldId,
    value: { singleSelectOptionId: $optionId }
  }) {
    projectV2Item { id }
  }
}
`.trim();

let after: string | null = null;
let scanned = 0;
let moved = 0;
let failed = 0;

do {
    const page: ItemsResponse = await github.graphql<ItemsResponse>(itemsQuery, {
        org: projectOwner,
        project: projectNumber,
        after,
    });

    const items = page.organization.projectV2.items;

    for (const item of items.nodes) {
        scanned++;

        // Pull requests and draft items carry no issue type, so they fall out here.
        const issueType = item.content?.issueType?.name;

        if (!issueType || !featureIssueTypes.has(normalize(issueType))) {
            continue;
        }

        const status = item.fieldValues.nodes.find(
            (v) => v.field?.name === statusFieldName && v.name
        );

        // Only the default column is swept. A Feature someone has deliberately moved
        // on to refinement, a sprint or Done is left where it is.
        if (!status?.name || normalize(status.name) !== fromStatus) {
            continue;
        }

        const label = item.content?.number
            ? `#${item.content.number} ${item.content.title ?? ""}`.trim()
            : item.id;

        if (dryRun) {
            console.log(
                `${c.gray("[dry run]")} ${c.green(label)}: ${c.magenta(
                    status.name
                )} to ${c.magenta(backlogOption.name)}`
            );
            moved++;
            continue;
        }

        try {
            await github.graphql(updateMutation, {
                projectId: projectV2.id,
                itemId: item.id,
                fieldId: statusField.id,
                optionId: backlogOption.id,
            });

            moved++;
            console.log(
                `${c.green(label)}: ${c.magenta(status.name)} to ${c.magenta(
                    backlogOption.name
                )}`
            );
        } catch (error) {
            // One failed item must not leave the rest of the Features sitting in the
            // default column, so the run carries on and reports a failure at the end.
            failed++;
            console.error(
                `${c.red("Failed")} ${label}: ${
                    error instanceof Error ? error.message : String(error)
                }`
            );
        }
    }

    after = items.pageInfo.hasNextPage ? items.pageInfo.endCursor : null;
} while (after);

console.log(`Scanned ${scanned} items, moved ${moved}, failed ${failed}.`);

if (failed > 0) {
    process.exit(1);
}
