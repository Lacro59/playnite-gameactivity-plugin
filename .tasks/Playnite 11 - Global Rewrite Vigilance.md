# Playnite 11 Rewrite - Global Vigilance

## Objective

Track critical technical vigilance points during the Playnite 11 rewrite to avoid regressions and keep migration decisions explicit.

## 1) DateTime and Timezone Boundaries

### Why this matters

Historical bugs showed day drift (day -1 / day +1) when UTC timestamps were shown or propagated without correct local conversion.

Known references:

- #129: Incorrect tooltip date.
- #253: Local-to-UTC session conversion affecting "Last Played".

### Timezone Rules

- Keep UTC as the single source of truth in persistence and domain models.
- Convert to local time only at integration/display boundaries:
  - UI labels, tooltips, charts, date pickers.
  - Playnite-facing fields with local display semantics (for example `LastActivity`).
  - User-facing exports unless explicitly documented as UTC.
- Avoid mixing UTC/local values inside identity matching and internal indexing.

### Implementation notes

- Centralize conversion helpers in one shared location:
  - `AsUtcSafe(DateTime dt)`
  - `AsLocalSafe(DateTime dt)`
- Handle `DateTimeKind.Unspecified` explicitly and consistently.
- Avoid string-based DateTime identity matching when a stronger key is available.

### Timezone Validation

- Test UTC-X and UTC+X environments.
- Test around UTC day rollover and DST transitions.
- Test add/edit/delete/merge/sync flows.

## 2) Data Model and Backward Compatibility

### Data Model Rules

- Keep migration paths deterministic and idempotent.
- Maintain compatibility with existing user data until migration is complete and validated.
- Version migration steps and document each schema change.

### Data Model Validation

- Run migration tests on representative legacy datasets.
- Verify no silent data truncation or field loss.

## 3) Playnite 11 API Integration Boundaries

### API Integration Rules

- Isolate Playnite API writes through dedicated services/mappers.
- Keep domain logic independent from direct UI/API side effects where possible.
- Ensure all Playnite-facing updates are explicit and reviewed.

### API Integration Validation

- Regression test game fields updated by the plugin (`Playtime`, `PlayCount`, `LastActivity`, etc.).
- Verify behavior consistency between single updates and bulk/sync operations.

## 4) UI/UX Behavior Parity

### UI/UX Rules

- Keep user-visible behavior consistent unless changes are intentional and documented.
- Preserve date/time semantics across all views (list, details, tooltip, charts).
- Keep localization/resource keys aligned with current standards.

### UI/UX Validation

- Run parity checks between old plugin behavior and Playnite 11 rewrite for core screens.
- Validate localized strings and date formatting in non-English locales.

## 5) Performance and Responsiveness

### Performance Rules

- Keep heavy operations off the UI thread.
- Avoid unnecessary full-list recomputations when incremental updates are possible.
- Prefer bounded datasets/pagination for charts and detailed logs.

### Performance Validation

- Benchmark with large libraries and long activity histories.
- Confirm no UI stalls during load, merge, export, and refresh actions.

## 6) Observability, Errors, and Recovery

### Observability Rules

- Log failures with actionable context (operation, game id/name, exception).
- Keep recoverable fallbacks where strict failure would degrade UX.
- Preserve safe backup/restore paths for active session data.

### Observability Validation

- Simulate partial failures (read/write/import/export) and verify graceful recovery.
- Ensure logs are sufficient to diagnose field mismatch and migration issues.

## 7) Test and Release Gate

Minimum gate before merge/release:

- Unit and integration checks for critical flows.
- Manual checklist covering timezone, migration, and Playnite field updates.
- Validation of historical issue scenarios (#129, #253).
- Changelog entry for any behavior change.
