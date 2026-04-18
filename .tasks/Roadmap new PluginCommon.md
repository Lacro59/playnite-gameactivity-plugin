# Roadmap new PluginCommon

## Core Architecture

- [x] PluginSettingsViewModel
- [x] PluginMenus
- [x] PluginWindows
- [ ] PluginExportCsv
- [x] PluginDatabaseObject<TSettings, TItem, T> where TSettings : PluginSettings
- [x] ListViewExtend
- [x] PluginDatabase.PersistSettingsAction

## Controls

- [ ] ~~PluginViewItem~~

```csharp
/// <summary>
/// Only reacts to <see cref="SystemCheckerSettings.EnableIntegrationViewItem"/>.
/// Ignores theme-bound properties (HasData, IsMinimumOK, etc.) updated by
/// <see cref="SystemCheckerDatabase.SetThemesResources"/> on selection change,
/// avoiding unnecessary refresh of every list item when only the detail view should update.
/// </summary>
protected override void PluginSettings_PropertyChanged(object sender, PropertyChangedEventArgs e)
{
    if (e?.PropertyName == nameof(SystemCheckerSettings.EnableIntegrationViewItem))
    {
        base.PluginSettings_PropertyChanged(sender, e);
    }
}
```

- [x] Games.ItemUpdated intentionally absent — handled by base via OnStaticGamesItemUpdated.

## Migration Plan — ListViewExtend

### Objective

- Replace legacy sorting mechanisms (`Tag` on `GridViewColumnHeader`) with `ListViewColumnOptions` attached properties.
- Remove the "hidden `Width=\"0\"` column used only for sorting" pattern.
- Standardize views to use explicit business columns and keep sorting behavior predictable.

### Migration Steps for Other Plugins

#### Step 1 - Identify impacted views

- List all XAML files that use `controlsShared:ListViewExtend`.
- Mark views using `Tag="NoSort"` and/or hidden `Width="0"` columns for sorting.

#### Step 2 - Replace hidden sort columns

- Remove any technical `GridViewColumn Width="0"` used only for sorting.
- Move sort mapping to the visible column with `controlsShared:ListViewColumnOptions.SortMemberPath`.

#### Step 3 - Replace `NoSort` tags

- Remove `Tag="NoSort"` from headers.
- Set `controlsShared:ListViewColumnOptions.DisableSorting="True"` on non-sortable columns.

#### Step 4 - Review column menu behavior

- Decide per view if user column management is needed.
- Set `ColumnManagementMenuEnable="False"` when column management is not desired.

#### Step 5 - Keep column persistence consistent

- For views with persisted columns, configure:
  - `EnableColumnPersistence`
  - `ColumnConfigurationFilePath`
  - `ColumnConfigurationScope`
  - `ColumnConfigurationKey`
- For views without persistence, keep defaults and avoid partial configuration.

#### Step 6 - Validate each plugin migration

- Run a global search to ensure no remaining `Tag="NoSort"` / `nosort`.
- Verify there are no remaining hidden `Width="0"` sort-only columns.
- Open key views and validate:
  - sortable columns still sort correctly,
  - non-sortable columns ignore header clicks,
  - column menu behavior matches UX expectations.
- Run linter/build checks and fix regressions before closing migration.
