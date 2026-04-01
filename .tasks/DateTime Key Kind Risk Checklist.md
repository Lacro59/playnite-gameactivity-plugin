# DateTime Key Kind Risk Checklist

## Goal

- [ ] Ensure every session/date lookup uses a `DateTime` key resolution strategy that is robust to `Kind` differences (`Utc`, `Local`, `Unspecified`).

## Priority A — Direct session deletion and key removal

- [ ] Review `source/Models/GameActivities.cs` in `DeleteActivity(DateTime dateSelected)`.
- [ ] Verify that activity lookup does not rely on strict `DateTime ==` with mixed local/UTC input.
- [ ] Verify that associated `ItemsDetails` entry is removed using a resolved key (not a raw input date).
- [ ] Add diagnostic logs for unresolved session/detail keys (include value, kind, ticks, and available keys).

## Priority A — Central detail-key resolver behavior

- [ ] Review `source/Models/ActivityDetails.cs` in `Get(DateTime dateSession)`.
- [ ] Validate string-based key matching when `dateSession` and dictionary keys have different `Kind`.
- [ ] Confirm whether key normalization should be explicit (`ToUniversalTime`) on both sides.
- [ ] Ensure fallback behavior is deterministic when multiple keys map to the same second.

## Priority B — Callers that depend on date-based detail retrieval

- [ ] Review `source/Services/GameActivityMonitoring.cs` (`OnTimedEvent`, `OnTimedBackupEvent`) where `ItemsDetails.Get(runningActivity.ActivityBackup.DateSession)` is used repeatedly.
- [ ] Review `source/Services/GameActivityDatabase.cs` (`BuildFullExportRows`) where `ItemsDetails.Get((DateTime)session.DateSession)` is used for export.
- [ ] Review `source/Controls/PluginChartLog.xaml.cs` where `GetSessionActivityDetails(dateSelected, titleChart)` feeds charts.
- [ ] Review `source/Services/GameActivityExport.cs` where `GetSessionActivityDetails(session.DateSession)` is used for CSV export rows.

## Priority B — Selection-to-session mapping

- [ ] Review `source/Models/GameActivities.cs` in `GetDateSelectedSession(DateTime? dateSelected, string title)`.
- [ ] Validate local/UTC round-trip (`ToLocalTime` + `ToUniversalTime`) and second-level string comparison.
- [ ] Check behavior for duplicate sessions in the same displayed second (indicator/title disambiguation).

## Priority C — UI deletion path before model deletion

- [ ] Review `source/ViewModels/GameActivityViewSingleViewModel.cs` where deletion passes `activity.GameLastActivity` to `_gameActivities.DeleteActivity(...)`.
- [ ] Validate that UI-local session date values are converted/matched consistently with stored UTC session keys.
- [ ] Ensure fallback matching (`FindActivityIndex`) remains consistent with any future key-resolution strategy.

## Priority C — Data merge/restore paths writing dictionary keys

- [ ] Review `source/GameActivity.cs` (`OnGameStarted`) where session key is created and inserted in both `Items` and `ItemsDetails.Items`.
- [ ] Review `source/Views/GameActivityBackup.xaml.cs` (`InitializeChart`, `ExecuteAdd`) where backup data is inserted with `TryAdd(activityBackup.DateSession, ...)`.
- [ ] Review `source/Services/GameActivityDatabase.cs` (`MergeData`) where `ItemsDetails.Items` keys are copied with `TryAdd`.
- [ ] Validate that imported/merged/restored keys keep a canonical `Kind` and do not create near-duplicate entries.

## Cross-check tests to run after fixes

- [ ] Delete a session selected from UI when stored key is UTC and input date is local.
- [ ] Delete/merge sessions where two keys differ only by `Kind`.
- [ ] Open chart/log view for sessions created from backup restore.
- [ ] Export CSV/all-data after merge and backup restore flows.
- [ ] Confirm no regression in average metrics (`AvgCPU`, `AvgGPU`, `AvgRAM`, etc.) for old and new records.
