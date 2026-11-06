# Publishing NuGet Packages with Release Please

**TL;DR**

- Encourage scopes naming components or areas within a vertical; omit them only for changes without a particular component. Release automation reads the final squash commit.
- Reference issues in the body or trailers, never in commit or PR titles. Prefer `Resolves: #<issue>` in the PR description, only when the PR completes the issue.
- Release Please prepares package releases, and GitHub Actions publishes them after merge.

## Overview
This guide outlines the release strategy for Altinn Authorization C# packages using Release Please. We adhere to [Semantic Versioning (SemVer)](https://semver.org/) to manage versions consistently.

## Release Process
Releases are automatically generated based on commit messages that follow the [Conventional Commits](https://www.conventionalcommits.org/en/v1.0.0/) specification. When a pull request (PR) is merged into the main branch, Release Please will:

1. Detect changes in the repository.
2. Update or generate the `CHANGELOG.md`.
3. Update the version in `*.csproj` or `version.json`.
4. Create a GitHub Release if necessary.

## Commit Message Conventions
Each commit message and PR title must follow the [repository convention](../../CONTRIBUTING.md#branches-commits-and-pull-requests). Scopes are optional but encouraged, naming a component or area within the vertical rather than the vertical itself. Use lowercase kebab-case consistently for each area, for example `authorized-parties`, not `AuthorizedParties`. Omit the scope only for changes without a particular component, such as `docs: document commit conventions`. This overrides historical issue-number scopes such as `fix(#4044): ...`.

| Change Type | Prefix       |
| ----------- | ------------ |
| Patch       | `fix(<scope>):`       |
| Minor       | `feat(<scope>):`      |
| Major       | `fix(<scope>)!:`      |
| Major       | `feat(<scope>)!:`     |
| Major       | `refactor(<scope>)!:` |

The `(<scope>)` portion may be omitted under the conditions above; `!` still appears immediately before the colon.

### Example
Removing support for .NET 8 in the PEP package:
```bash
git add .
git commit -m "feat!: remove support for .NET 8"
```

Use the same change description and scope, if present, in the contributor PR title. Removing framework support across the package does not concern a particular component, so this example omits the scope:
```
feat!: remove support for .NET 8
```
`release(main): ...` is a release-automation title, not the title for a contributor's feature or fix PR.

Reference issues in the commit body or trailers after a blank line, and in the PR description. Keep issue numbers out of titles because GitHub appends the PR number on squash merge. Use `Refs: #4044` to reference an issue without closing it. The PR description is the primary closing mechanism: use `Resolves: #4044` there only when the PR completes the issue. Also include and preserve `Resolves: #4044` in the final squash commit so the completed issue remains visible in the commit log, even when the PR description handles closing it. Preserve all commit trailers when squashing, including release instructions such as `Release-As: 2.5.0` and attribution such as `Co-authored-by`; review the final squash message because intermediate trailers do not survive automatically.
Since we introduced a breaking change, we must bump the major version.

When this PR is merged, Release Please will create another PR with the updated changelog, including:
```
feat!: remove support for .NET 8
```
Once the Release Please PR is merged, the package can be safely deployed.

## Specification
This guide follows the key terms defined in [RFC 2119](https://datatracker.ietf.org/doc/html/rfc2119):
- **MUST**: A requirement that must be followed.
- **MAY**: An optional action.
- **SHOULD**: A recommendation.

### Commit Message Structure
1. A commit **MUST** be prefixed with a type (`feat`, `fix`, etc.), followed by an optional scope in parentheses, an optional `!` for breaking changes, and a required colon and space.
2. `feat` **MUST** be used for new features.
3. `fix` **MUST** be used for bug fixes.
4. A scope is encouraged and, when present, **MUST** use lowercase kebab-case and identify a component or area within the vertical, e.g., `fix(authorized-parties):`, rather than the vertical itself or an issue number. It **MAY** be omitted only for changes without a particular component.
5. The description **MUST** immediately follow the colon and space.
6. A longer commit body **MAY** be included, starting one blank line after the description.
7. A commit body **MAY** include multiple paragraphs.
8. Footers **MAY** be included after one blank line.
9. Footers **MUST** follow the format `<token>: <value>`, e.g., `BREAKING CHANGE: environment variables now take precedence over config files`.
10. Breaking changes **MUST** be indicated in either the type prefix (`!`) or the footer.
11. `BREAKING CHANGE` in the footer **MAY** be omitted if `!` is used in the prefix.
12. Other commit types **MAY** be used, such as `docs: document commit conventions`.
13. Although Conventional Commits permits different casing, this repository requires lowercase kebab-case scopes. `BREAKING CHANGE` **MUST** be uppercase.

## Automation
- **GitHub Actions** runs Release Please when PRs are merged into `main`.
- The version bump is determined automatically based on commit history.
- A new release is created only when a version change is required.

## Publishing
When a release is created, `.github/workflows/release.yml` runs three jobs for each released package:

1. **publish** builds, tests and packs the package, then attaches the `.nupkg` files to the GitHub release.
2. **publish-github-packages** pushes it to the Altinn GitHub Packages feed.
3. **publish-nuget** pushes it to nuget.org with [Trusted Publishing](https://learn.microsoft.com/nuget/nuget-org/trusted-publishing). No API key is stored. `NuGet/login` exchanges the job's GitHub OIDC token for a short-lived key.

Trusted Publishing depends on settings outside this repository:

- A nuget.org Trusted Publishing policy owned by `altinn` (the owner of the packages), with repository owner `Altinn`, repository `altinn-auth`, workflow file `release.yml` and environment `nuget`.
- A `nuget` GitHub environment in this repository, limited to `main`, with the variable `NUGET_USER` set to the nuget.org profile name (not email) of the policy's creator.

All pushes use `--skip-duplicate`, so rerunning a job does not fail on versions that are already published.

## Semantic Versioning Rules
| **Condition**                                                     | **Version Change**  | **Example** (`1.2.3 →`) |
| ----------------------------------------------------------------- | ------------------- | ----------------------- |
| 🚀 **Breaking changes** (require code modifications by consumers)  | **Major** (`X.0.0`) | `2.0.0`                 |
| ➖ **Removing or renaming public APIs**                            | **Major** (`X.0.0`) | `2.0.0`                 |
| 🔄 **Changing method signatures**                                  | **Major** (`X.0.0`) | `2.0.0`                 |
| ⚠ **Changing default behavior in a non-backward-compatible way**  | **Major** (`X.0.0`) | `2.0.0`                 |
| 🔄 **Removing support for a .NET version**                         | **Major** (`X.0.0`) | `2.0.0`                 |
| 🔄 **Upgrading to a new .NET version (breaking compatibility)**    | **Major** (`X.0.0`) | `2.0.0`                 |
| 🔄 **Upgrading to a new .NET version (fully backward-compatible)** | **Minor** (`X.Y.0`) | `1.3.0`                 |
| ✨ **Adding new features (backward-compatible)**                   | **Minor** (`X.Y.0`) | `1.3.0`                 |
| ➕ **Adding a new public API**                                     | **Minor** (`X.Y.0`) | `1.3.0`                 |
| 📦 **Internal performance improvements (no API changes)**          | **Minor** (`X.Y.0`) | `1.3.0`                 |
| 🔧 **Deprecating an API (but still available for now)**            | **Minor** (`X.Y.0`) | `1.3.0`                 |
| 🐛 **Bug fixes (no breaking changes)**                             | **Patch** (`X.Y.Z`) | `1.2.4`                 |
| 📌 **Fixing security vulnerabilities (no breaking changes)**       | **Patch** (`X.Y.Z`) | `1.2.4`                 |
| 📈 **Performance optimizations (no API change)**                   | **Patch** (`X.Y.Z`) | `1.2.4`                 |
| 📝 **Documentation updates only (no code changes)**                | **No version bump** | -                       |

---
By following this guide, you ensure that releases are structured, predictable, and automated correctly.

