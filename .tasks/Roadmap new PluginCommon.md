# Roadmap new PluginCommon

## Core Architecture

- [x] PluginSettingsViewModel
- [x] PluginMenus
- [x] PluginWindows
- [ ] PluginExportCsv
- [x] PluginDatabaseObject<TSettings, TItem, T> where TSettings : PluginSettings
- [ ] ListViewExtend
- [ ] PluginDatabase.PersistSettingsAction

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
