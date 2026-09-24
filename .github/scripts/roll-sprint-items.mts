import { Chalk } from "chalk";
import { Octokit } from "@octokit/action";

const c = new Chalk({ level: 3 });

const ghToken = process.env.GITHUB_TOKEN;
const projectOwner = process.env.GITHUB_PROJECT_OWNER;
const projectNumberString = process.env.GITHUB_PROJECT_NUMBER;
const sprintFieldName = process.env.GITHUB_SPRINT_FIELD_NAME ?? "Sprint";
const statusFieldName = process.env.GITHUB_STATUS_FIELD_NAME ?? "Status";
const statusesToRollRaw = process.env.GITHUB_STATUSES_TO_ROLL;
const dryRun = process.env.DRY_RUN === "true";

if (!ghToken || !projectOwner || !projectNumberString || !statusesToRollRaw) {
    console.error("Missing required environment variables");
    process.exit(1);
}

const projectNumber = Number.parseInt(projectNumberString, 10);

// Status names are compared with punctuation, emoji and casing removed. The board
// spells them "Sprint backlog", "In Progress" with a worker emoji and "Review" with
// a magnifier, and those decorations get edited without anyone thinking of this script.
const normalize = (value: string) =>
    value.toLowerCase().replace(/[^a-z0-9]/g, "");

const statusesToRoll = new Set(
    statusesToRollRaw
        .split(",")
        .map((s) => s.trim())
        .filter((s) => s.length > 0)
        .map(normalize)
);

if (statusesToRoll.size === 0) {
    console.error("GITHUB_STATUSES_TO_ROLL listed no statuses");
    process.exit(1);
}

const github = new Octokit({ auth: ghToken });

type Iteration = {
    readonly id: string;
    readonly title: string;
    readonly startDate: string;
    readonly duration: number;
};

type ProjectResponse = {
    readonly organization: {
        readonly projectV2: {
            readonly id: string;
            readonly title: string;
            readonly field: {
                readonly id: string;
                readonly name: string;
                readonly configuration: {
                    readonly iterations: readonly Iteration[];
                    readonly completedIterations: readonly Iteration[];
                };
            } | null;
        } | null;
    } | null;
};

const project = await github.graphql<ProjectResponse>(
    `
query($org: String!, $project: Int!, $sprintField: String!) {
  organization(login: $org) {
    projectV2(number: $project) {
      id
      title
      field(name: $sprintField) {
        ... on ProjectV2IterationField {
          id
          name
          configuration {
            iterations { id title startDate duration }
            completedIterations { id title startDate duration }
          }
        }
      }
    }
  }
}
  `.trim(),
    { org: projectOwner, project: projectNumber, sprintField: sprintFieldName }
);

const projectV2 = project.organization?.projectV2;

if (!projectV2) {
    console.error(`Project ${projectNumber} not found for ${projectOwner}`);
    process.exit(1);
}

const sprintField = projectV2.field;

if (!sprintField) {
    console.error(
        `Project ${projectV2.title} has no iteration field named ${sprintFieldName}`
    );
    process.exit(1);
}

const currentIteration = findCurrentIteration(
    sprintField.configuration.iterations
);

if (!currentIteration) {
    console.log(
        `No current or upcoming ${c.green(
            sprintFieldName
        )} iteration. Nothing to roll into.`
    );
    process.exit(0);
}

// Only iterations GitHub itself reports as completed are rolled forward. Deriving
// that from the dates instead would also catch the current iteration on its last day.
const completedIterations = new Map(
    sprintField.configuration.completedIterations.map((i) => [i.id, i.title])
);

console.log(
    `Rolling unfinished work into ${c.yellow(
        currentIteration.title
    )} on ${c.cyan(projectV2.title)}.`
);

type FieldValueNode = {
    readonly name?: string;
    readonly iterationId?: string;
    readonly title?: string;
    readonly field?: { readonly name: string };
};

type Item = {
    readonly id: string;
    readonly content: {
        readonly number?: number;
        readonly title?: string;
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
            ... on Issue { number title }
            ... on PullRequest { number title }
          }
          fieldValues(first: 30) {
            nodes {
              ... on ProjectV2ItemFieldSingleSelectValue {
                name
                field { ... on ProjectV2FieldCommon { name } }
              }
              ... on ProjectV2ItemFieldIterationValue {
                iterationId
                title
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
mutation($projectId: ID!, $itemId: ID!, $fieldId: ID!, $iterationId: String!) {
  updateProjectV2ItemFieldValue(input: {
    projectId: $projectId,
    itemId: $itemId,
    fieldId: $fieldId,
    value: { iterationId: $iterationId }
  }) {
    projectV2Item { id }
  }
}
`.trim();

let after: string | null = null;
let scanned = 0;
let rolled = 0;
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

        const status = item.fieldValues.nodes.find(
            (v) => v.field?.name === statusFieldName && v.name
        );

        if (!status?.name || !statusesToRoll.has(normalize(status.name))) {
            continue;
        }

        const sprint = item.fieldValues.nodes.find(
            (v) => v.field?.name === sprintFieldName && v.iterationId
        );

        // An item with no sprint has never been pulled into one. Setting it here
        // would add work to the sprint rather than carry unfinished work over.
        if (!sprint?.iterationId) {
            continue;
        }

        const completedTitle = completedIterations.get(sprint.iterationId);

        // Already in the current or a future iteration, so there is nothing to carry.
        if (!completedTitle) {
            continue;
        }

        const label = item.content?.number
            ? `#${item.content.number} ${item.content.title ?? ""}`.trim()
            : item.id;

        if (dryRun) {
            console.log(
                `${c.gray("[dry run]")} ${c.green(label)}: ${c.magenta(
                    completedTitle
                )} to ${c.magenta(currentIteration.title)} (${status.name})`
            );
            rolled++;
            continue;
        }

        try {
            await github.graphql(updateMutation, {
                projectId: projectV2.id,
                itemId: item.id,
                fieldId: sprintField.id,
                iterationId: currentIteration.id,
            });

            rolled++;
            console.log(
                `${c.green(label)}: ${c.magenta(completedTitle)} to ${c.magenta(
                    currentIteration.title
                )} (${status.name})`
            );
        } catch (error) {
            // One failed item must not strand the rest of the sprint in the old
            // iteration, so the run carries on and reports a failure at the end.
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

console.log(`Scanned ${scanned} items, moved ${rolled}, failed ${failed}.`);

if (failed > 0) {
    process.exit(1);
}

function findCurrentIteration(
    iterations: readonly Iteration[]
): Iteration | undefined {
    // Compared as plain UTC dates. startDate is a YYYY-MM-DD string, and an
    // iteration covers startDate through startDate + duration days, end exclusive.
    const today = Date.parse(new Date().toISOString().slice(0, 10));

    const containing = iterations.find((i) => {
        const start = Date.parse(i.startDate);
        return today >= start && today < start + i.duration * 86_400_000;
    });

    if (containing) {
        return containing;
    }

    // A gap between iterations, or a field configured only with future ones. The
    // nearest upcoming iteration is then the sprint the work belongs in.
    return iterations
        .filter((i) => Date.parse(i.startDate) > today)
        .sort((a, b) => Date.parse(a.startDate) - Date.parse(b.startDate))[0];
}
