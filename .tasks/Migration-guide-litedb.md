# Migration Guide: PluginDatabaseObject → LiteDB

**Version:** 1.0
**Date:** 2026-02-24
**Applies to:** All plugins using `PluginDatabaseObject<TSettings, TDatabase, TItem, T>`

---

## Overview

This guide covers the migration from the legacy JSON-file-per-game database backend
to a single LiteDB file, and the removal of `Task.Run()` in `PluginUserControlExtend`
to eliminate UI thread starvation during list scrolling.

### Performance gains

| Metric                     | Before                  | After                        |
| -------------------------- | ----------------------- | ---------------------------- |
| `UpdateDataAsync` per game | 180–1400 ms             | 3–6 ms                       |
| `SetData` per game         | 127–413 ms              | 1–2 ms                       |
| Cache disk write           | Every`CheckConfig` call | Once, 2 s after scroll stops |

---

## Step 1 — NuGet

- [x] Add `LiteDB` **4.x**
- [x] Verify no dependency conflict with existing packages

---

## Step 2 — Shared files (deliver once in `CommonPluginsShared`)

- [x] `Collections/LiteDbItemCollection.cs` *(new)*
- [x] `Collections/PluginDatabaseObject.cs` *(updated — `TDatabase` generic parameter removed)*
- [x] `Controls/PluginUserControlExtend.cs` *(updated — synchronous cache lookup, no `Task.Run`)*

---

## Step 3 — Per-plugin: database class

### 3.1 — Update class signature

- [x] Update class signature

```csharp
// Before
public class MyPluginDatabase
    : PluginDatabaseObject<MySettings, MyCollection, MyItem, MyItemData>

// After
public class MyPluginDatabase
    : PluginDatabaseObject<MySettings, MyItem, MyItemData>
```

### 3.2 — Remove the collection wrapper class

- [x] Remove the collection wrapper class

If the plugin had a dedicated collection class (e.g. `MyCollection`) that only
existed to hold custom properties, delete it and move those properties directly
onto the database class.

```csharp
// Before — separate collection class
public class MyCollection : PluginItemCollection<MyItem>
{
    public MyCustomData CustomProperty { get; set; }
}

// After — property lives on the database class
public class MyPluginDatabase : PluginDatabaseObject<MySettings, MyItem, MyItemData>
{
    public MyCustomData CustomProperty { get; set; }
}
```

### 3.3 — Replace `Database.X` accesses

- [x] Replace `Database.X` accesses

```csharp
// Before
var data = PluginDatabase.Database.CustomProperty;

// After
var data = PluginDatabase.CustomProperty;
```

### 3.4 — Replace collection enumeration

- [x] Replace collection enumeration

```csharp
// Before
IEnumerable<MyItem> items = PluginDatabase.Database.Where(x => x.IsInstalled);

// After
IEnumerable<MyItem> items = PluginDatabase.GetAllItems().Where(x => x.IsInstalled);
```

---

## Step 4 — Per-plugin: UI controls

### 4.1 — Update `AttachStaticEvents()`

- [x] In every control that inherits `PluginUserControlExtend`:

```csharp
// Before
PluginDatabase.Database.ItemUpdated += CreateDatabaseItemUpdatedHandler<MyItem>();
PluginDatabase.Database.ItemCollectionChanged += CreateDatabaseCollectionChangedHandler<MyItem>();

// After
PluginDatabase.DatabaseItemUpdated += CreateDatabaseItemUpdatedHandler<MyItem>();
PluginDatabase.DatabaseItemCollectionChanged += CreateDatabaseCollectionChangedHandler<MyItem>();
```

No other changes required in controls — handler signatures are unchanged.

---

## Step 5 — Notes on `[BsonIgnore]`

- [x] `[BsonIgnore]`

Skipping `[BsonIgnore]` on `[DontSerialize]` properties is acceptable.
Properties such as `IsDeleted`, `IsSaved`, `HasData`, and `Count` will be serialized
to BSON but cause no data corruption — they are recalculated or overwritten on every
`Upsert`. The only cost is a negligible increase in BSON entry size.

---

## Step 6 — Validation checklist

### Build

- [x] Compiles on .NET Framework 4.6.2 without errors
- [x] No C# 8.0+ features introduced

### First launch (JSON migration)

- [x] Migration progress dialog appears
- [x] Log shows: `MigrateJsonToLiteDb — completed: N migrated, 0 failed`
- [x] All `.json` files removed from `PluginDatabasePath` after migration
- [x] Log shows: `PreWarm — N items cached in Xms`

### Runtime performance

- [x] Scrolling the game list: `UpdateDataAsync end [<10ms]` for every game
- [x] No `Task.Run` starvation (no batch of `DB fetch done [>100ms]` entries)

### Functional tests

- [ ] ~~`ClearHardwareCache()` invalidates and reloads correctly~~
- [ ] ~~`ClearDatabase()` removes all LiteDB entries and associated tags~~
- [x] Games added after migration appear correctly (no missing data)
- [x] Games deleted from Playnite are removed from LiteDB on next startup (`DeleteDataWithDeletedGame`)

---

## Troubleshooting

| Symptom                                        | Likely cause                                    | Fix                                                        |
| ---------------------------------------------- | ----------------------------------------------- | ---------------------------------------------------------- |
| `UpdateDataAsync end [>100ms]` after migration | `PreWarm()` not called in `LoadDatabase()`      | Ensure`_database.PreWarm()` is called before `return true` |
| `DB fetch done [>100ms]` in batches            | `Task.Run()` still present in `UpdateDataAsync` | Replace with synchronous`GetOnlyCache()` call              |
| `Cache saved to disk` on every scroll event    | `SaveCacheToDisk()` still called directly       | Replace all call sites with`ScheduleCacheSave()`           |
| Migration dialog never appears                 | `PluginDatabasePath` points to wrong folder     | Verify`Paths.PluginDatabasePath` in constructor            |
| `MigrateJsonToLiteDb — failed on 'file.json'`  | Corrupted or incompatible JSON file             | Check file manually; delete if unrecoverable               |
| LiteDB file locked on second Playnite instance | LiteDB 4.x single-writer limitation             | Expected behavior — only one Playnite instance supported   |
