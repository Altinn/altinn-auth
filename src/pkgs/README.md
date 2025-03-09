# Overview
This README defines the release strategy for Altinn Autorization C# packages using Release Please. We follow Semantic Versioning (SemVer) to manage versions consistently.

# Release

Releases are automatically created based on commit messages that follow [Conventional Commits](https://www.conventionalcommits.org/en/v1.0.0/). When a pull request (PR) is merged, Release Please will:

## Specification
The key words “MUST”, “MUST NOT”, “REQUIRED”, “SHALL”, “SHALL NOT”, “SHOULD”, “SHOULD NOT”, “RECOMMENDED”, “MAY”, and “OPTIONAL” in this document are to be interpreted as described in RFC 2119.

1. Commits MUST be prefixed with a type, which consists of a noun, feat, fix, etc., followed by the OPTIONAL scope, OPTIONAL !, and REQUIRED terminal colon and space.
2. The type feat MUST be used when a commit adds a new feature to your application or library.
3. The type fix MUST be used when a commit represents a bug fix for your application.
4. A scope MAY be provided after a type. A scope MUST consist of a noun describing a section of the codebase surrounded by parenthesis, e.g., fix(parser):
5. A description MUST immediately follow the colon and space after the type/scope prefix. The description is a short summary of the code changes, e.g., fix: array parsing issue when multiple spaces were contained in string.
6. A longer commit body MAY be provided after the short description, providing additional contextual information about the code changes. The body MUST begin one blank line after the description.
7. A commit body is free-form and MAY consist of any number of newline separated paragraphs.
8. One or more footers MAY be provided one blank line after the body. Each footer MUST consist of a word token, followed by either a :<space> or <space># separator, followed by a string value (this is inspired by the git trailer convention).
9. A footer’s token MUST use - in place of whitespace characters, e.g., Acked-by (this helps differentiate the footer section from a multi-paragraph body). An exception is made for BREAKING CHANGE, which MAY also be used as a token.
10. A footer’s value MAY contain spaces and newlines, and parsing MUST terminate when the next valid footer token/separator pair is observed.
11. Breaking changes MUST be indicated in the type/scope prefix of a commit, or as an entry in the footer.
12. If included as a footer, a breaking change MUST consist of the uppercase text BREAKING CHANGE, followed by a colon, space, and description, e.g., BREAKING CHANGE: environment variables now take precedence over config files.
13. If included in the type/scope prefix, breaking changes MUST be indicated by a ! immediately before the :. If ! is used, BREAKING CHANGE: MAY be omitted from the footer section, and the commit description SHALL be used to describe the breaking change.
14. Types other than feat and fix MAY be used in your commit messages, e.g., docs: update ref docs.
15. The units of information that make up Conventional Commits MUST NOT be treated as case sensitive by implementors, with the exception of BREAKING CHANGE which MUST be uppercase.
16. BREAKING-CHANGE MUST be synonymous with BREAKING CHANGE, when used as a token in a footer.


Release please

1. Detect changes in the repository.
2. Generate or update the `CHANGELOG.md`.
3. Update the version in `*.csproj` or `version.json`.
4. Create a GitHub Release if required.

## Create Release
| Segment | PR Title Prefix |
| ------- | --------------- |
| Patch   | `fix:`          |
| Minor   | `feat:`         |
| Major   | `fix!:`         |
| Major   | `feat!:`        |
| Major   | `refactor!:`    |

Automation
* GitHub Actions runs Release Please when PRs are merged into main.
* The version is determined automatically based on the commit history.
* A new release is published only when a version bump is required.

## SemVer
| **Condition**                                                                | **Version Change**           | **Example** (Current: `1.2.3`) → **(New)** |
| ---------------------------------------------------------------------------- | ---------------------------- | ------------------------------------------ |
| 🚀 **Breaking changes** (Changes that require consumers to modify their code) | **Major** (`X.0.0`)          | `1.2.3` → `2.0.0`                          |
| ➖ **Removing or renaming a public method/class/interface/enums etc.**        | **Major** (`X.0.0`)          | `1.2.3` → `2.0.0`                          |
| 🔄 **Changing method signatures (parameters, return type, etc.)**             | **Major** (`X.0.0`)          | `1.2.3` → `2.0.0`                          |
| ⚠ **Changing default behavior in a non-backward-compatible way**             | **Major** (`X.0.0`)          | `1.2.3` → `2.0.0`                          |
| 🔄 **Removing support for a .NET version**                                    | **Major** (`X.0.0`)          | `1.2.3` → `2.0.0`                          |
| 🔄 **Upgrading to a new .NET version that breaks compatibility**              | **Major** (`X.0.0`)          | `1.2.3` → `2.0.0`                          |
| 🔄 **Upgrading to a new .NET version (but fully backward-compatible)**        | **Minor** (`X.Y.0`)          | `1.2.3` → `1.3.0`                          |
| ✨ **Adding new features (backward-compatible)**                              | **Minor** (`X.Y.0`)          | `1.2.3` → `1.3.0`                          |
| ➕ **Adding a new public method/class**                                       | **Minor** (`X.Y.0`)          | `1.2.3` → `1.3.0`                          |
| 📦 **Enhancing performance or internal improvements (no API changes)**        | **Minor** (`X.Y.0`)          | `1.2.3` → `1.3.0`                          |
| 🔧 **Deprecating a method (but still keeping it for now)**                    | **Minor** (`X.Y.0`)          | `1.2.3` → `1.3.0`                          |
| 🐛 **Bug fixes (no new features, no breaking changes)**                       | **Patch** (`X.Y.Z`)          | `1.2.3` → `1.2.4`                          |
| 📌 **Fixing a security vulnerability (no breaking changes)**                  | **Patch** (`X.Y.Z`)          | `1.2.3` → `1.2.4`                          |
| 📈 **Performance improvements (no API change)**                               | **Patch** (`X.Y.Z`)          | `1.2.3` → `1.2.4`                          |
| 📝 **Documentation updates only (no code changes)**                           | **No version bump required** | -                                          |
