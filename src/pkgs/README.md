# Publishing NuGet Packages with Release Please

## Overview
This guide outlines the release strategy for Altinn Authorization C# packages using Release Please. We adhere to [Semantic Versioning (SemVer)](https://semver.org/) to manage versions consistently.

## Release Process
Releases are automatically generated based on commit messages that follow the [Conventional Commits](https://www.conventionalcommits.org/en/v1.0.0/) specification. When a pull request (PR) is merged into the main branch, Release Please will:

1. Detect changes in the repository.
2. Update or generate the `CHANGELOG.md`.
3. Update the version in `*.csproj` or `version.json`.
4. Create a GitHub Release if necessary.

## Commit Message Conventions
Each commit message must follow a structured format to ensure correct versioning:

| Change Type | Prefix       |
| ----------- | ------------ |
| Patch       | `fix:`       |
| Minor       | `feat:`      |
| Major       | `fix!:`      |
| Major       | `feat!:`     |
| Major       | `refactor!:` |

### Example
Removing support for .NET 8 in the PEP package:
```bash
git add .
git commit -m "feat!: remove support for .NET 8"
```

Then, create a PR with the following title format:
```
release(<scope>): <component> <version>
```
For example:
```
release(main): Altinn.Authorization.PEP 5.0.0
```
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
1. A commit **MUST** be prefixed with a type (`feat`, `fix`, etc.), followed by an optional scope, an optional `!` for breaking changes, and a required colon and space.
2. `feat` **MUST** be used for new features.
3. `fix` **MUST** be used for bug fixes.
4. An optional scope **MAY** be provided in parentheses, e.g., `fix(parser):`.
5. The description **MUST** immediately follow the colon and space.
6. A longer commit body **MAY** be included, starting one blank line after the description.
7. A commit body **MAY** include multiple paragraphs.
8. Footers **MAY** be included after one blank line.
9. Footers **MUST** follow the format `<token>: <value>`, e.g., `BREAKING CHANGE: environment variables now take precedence over config files`.
10. Breaking changes **MUST** be indicated in either the type prefix (`!`) or the footer.
11. `BREAKING CHANGE` in the footer **MAY** be omitted if `!` is used in the prefix.
12. Other commit types **MAY** be used, such as `docs: update API documentation`.
13. Commit messages **MUST NOT** be case-sensitive, except `BREAKING CHANGE` which **MUST** be uppercase.

## Automation
- **GitHub Actions** runs Release Please when PRs are merged into `main`.
- The version bump is determined automatically based on commit history.
- A new release is created only when a version change is required.

## Publishing
When a release is created, `.github/workflows/cd-pkgs.yml` runs three jobs for each released package:

1. **publish** builds, tests and packs the package, then attaches the `.nupkg` files to the GitHub release.
2. **publish-github-packages** pushes it to the Altinn GitHub Packages feed.
3. **publish-nuget** pushes it to nuget.org with [Trusted Publishing](https://learn.microsoft.com/nuget/nuget-org/trusted-publishing). No API key is stored. `NuGet/login` exchanges the job's GitHub OIDC token for a short-lived key.

Trusted Publishing depends on settings outside this repository:

- A nuget.org Trusted Publishing policy owned by `altinn` (the owner of the packages), with repository owner `Altinn`, repository `altinn-auth`, workflow file `cd-pkgs.yml` and environment `nuget`.
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

