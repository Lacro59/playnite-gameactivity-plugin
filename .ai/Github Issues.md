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
- **Footer**: references an issue with the full URL on a single line, for example `Fixes https://github.com/Lacro59/playnite-gameactivity-plugin/issues/123`.
- **Ticket reference (required)**: always include the full GitHub issue URL in the `Fixes` footer line.
- **Trailer policy**: do not add tool-generated trailers (for example `Made-with: Cursor`) in commit messages.

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

Fixes https://github.com/Lacro59/playnite-gameactivity-plugin/issues/456
```

## 3. Cheat sheet GitHub

| Action          | Keyword                       | Syntax                                                                                                                                               |
| --------------- | ----------------------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------- |
| Close an issue  | `Fixes`, `Closes`, `Resolves` | `Fixes https://github.com/Lacro59/playnite-gameactivity-plugin/issues/123`                                                                           |
| Link to issue   | `Ref`, `See`                  | `Ref https://github.com/Lacro59/playnite-gameactivity-plugin/issues/123`                                                                             |
| Multiple issues | `Fixes`                       | `Fixes https://github.com/Lacro59/playnite-gameactivity-plugin/issues/123, Fixes https://github.com/Lacro59/playnite-gameactivity-plugin/issues/124` |

---

**Last Updated:** 2026-04-20
**Version:** 1.3
