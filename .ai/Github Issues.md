# Git Commit Message Convention

## 1. Raw template

```text
<type>(<scope>): <description>

<body>

<footer>
```

## 2. Writing guide

### Format

- **Type**: `fix`, `feat`, `docs`, `style`, `refactor`, `perf`, `test`, `chore`.
- **Scope**: optional, indicates the impacted module or file, for example `api` or `middleware`.
- **Subject**: use the imperative mood, with no leading capital letter and no trailing period.
- **Body**: optional, explain the **why** and **how**, not only the **what**.
- **Footer**: references an issue, for example `Fixes #123`.
- **Ticket reference (required)**: always include the full GitHub issue URL in the message (example: `https://github.com/Lacro59/playnite-gameactivity-plugin/issues/252`).

### Examples

#### Simple fix

```text
fix(ui): resolve button misalignment on login page
```

#### Detailed fix

```text
fix(api): handle null values in user profile update

The API was throwing a 500 error when the 'bio' field was sent as null.
Added a null-check validator before database persistence.

Closes #456
```

## 3. Cheat sheet GitHub

| Action          | Keyword                       | Syntax                   |
| --------------- | ----------------------------- | ------------------------ |
| Close an issue  | `Fixes`, `Closes`, `Resolves` | `Fixes #123`             |
| Link to issue   | `Ref`, `See`                  | `Ref #123`               |
| Multiple issues | `Fixes`                       | `Fixes #123, Fixes #124` |

---

**Last Updated:** 2026-04-20
**Version:** 1.2
