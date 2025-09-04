import { Chalk } from "chalk";
import { globby } from "globby";
import { Octokit } from "@octokit/action";
import { $, retry } from "zx";
import path from "node:path";

const c = new Chalk({ level: 3 });

const ghToken = process.env.GITHUB_TOKEN;
const issueNumber = process.env.GITHUB_ISSUE_NUMBER;
const projectNumberString = process.env.GITHUB_PROJECT_NUMBER;
const fieldsJson = process.env.GITHUB_PROJECT_FIELDS;

if (!ghToken || !issueNumber || !projectNumberString || !fieldsJson) {
    console.error("Missing required environment variables");
    process.exit(1);
}

type FieldValue =
    | { readonly text: string }
    | { readonly number: number }
    | { readonly date: string }
    | { readonly singleSelectOptionId: string }
    | { readonly iterationId: string };

const fieldsSpec = JSON.parse(fieldsJson) as Readonly<
    Record<string, FieldValue>
>;
const projectNumber = Number.parseInt(projectNumberString, 10);
const issueInfo = {
    issue_number: Number.parseInt(issueNumber, 10),
    owner: "Altinn",
    repo: "altinn-authorization-tmp",
} as const;

const github = new Octokit({
    auth: ghToken,
});

type FieldSpec = {
    readonly id: string;
    readonly name: string;
    readonly options?: readonly {
        readonly id: string;
        readonly name: string;
    }[];
};

type ProjectResponse = {
    readonly organization: {
        readonly repository: {
            readonly issue: {
                readonly id: string;
                readonly number: number;
                readonly title: string;
            };
        };
        readonly projectV2: {
            readonly id: string;
            readonly title: string;
            readonly fields: {
                readonly nodes: FieldSpec[];
            };
        };
    };
};

const info = await github.graphql<ProjectResponse>(
    `
query($org: String!, $project: Int!, $repo: String!, $issue: Int!) {
  organization(login: $org) {
    repository(name: $repo) {
      issue(number: $issue) {
        id
        number
        title
      }
    }
    projectV2(number: $project) {
      id
      title
      fields(first: 20) {
        nodes {
          ... on ProjectV2Field {
            id
            name
          }
          ... on ProjectV2SingleSelectField {
            id
            name
            options {
              id
              name
            }
          }
        }
      }
    }
  }
}
  `.trim(),
    {
        org: issueInfo.owner,
        project: projectNumber,
        repo: issueInfo.repo,
        issue: issueInfo.issue_number,
    }
);

if (!info.organization?.repository?.issue) {
    console.error("Issue not found");
    process.exit(1);
}

if (!info.organization.projectV2) {
    console.error("Project not found");
    process.exit(1);
}

type ProjectItemResponse = {
    readonly addProjectV2ItemById: {
        readonly item: {
            readonly id: string;
        };
    };
};

const projectItemInfo = await github.graphql<ProjectItemResponse>(
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
    {
        projectId: info.organization.projectV2.id,
        issueId: info.organization.repository.issue.id,
    }
);

console.log(
    `Issue ${c.cyan(
        info.organization.repository.issue.title
    )} added to project ${c.yellow(info.organization.projectV2.title)}.`
);

const projectItemId = projectItemInfo.addProjectV2ItemById.item.id;

for (const [fieldId, fieldValue] of Object.entries(fieldsSpec)) {
    const fieldSpec = info.organization.projectV2.fields.nodes.find(
        (f) => f.id === fieldId
    );

    const fieldName = fieldSpec?.name ?? `<${fieldId}>`;
    let valueDisplay = getValueDisplay(fieldValue, fieldSpec);

    await github.graphql(
        `
mutation($projectId: ID!, $itemId: ID!, $fieldId: ID!, $value: ProjectV2FieldValue!) {
  updateProjectV2ItemFieldValue(input: {
    projectId: $projectId,
    itemId: $itemId,
    fieldId: $fieldId,
    value: $value
  }) {
    projectV2Item {
      id
    }
  }
}
    `,
        {
            projectId: info.organization.projectV2.id,
            itemId: projectItemId,
            fieldId,
            value: fieldValue,
        }
    );

    console.log(`Set ${c.green(fieldName)} to ${c.magenta(valueDisplay)}`);
}

function getValueDisplay(value: FieldValue, spec?: FieldSpec) {
    if ("text" in value) {
        return value.text;
    } else if ("number" in value) {
        return value.number.toString();
    } else if ("date" in value) {
        return value.date;
    } else if ("singleSelectOptionId" in value) {
        return (
            spec?.options?.find((o) => o.id === value.singleSelectOptionId)?.name ??
            `<${value.singleSelectOptionId}>`
        );
    } else if ("iterationId" in value) {
        return `<${value.iterationId}>`;
    } else {
        return JSON.stringify(value);
    }
}
