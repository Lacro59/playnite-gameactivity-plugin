# Renaming Task — Plugin Database Classes

## Renames

| Old                               | New                                            |
| --------------------------------- | ---------------------------------------------- |
| `PluginDataBaseGameBase`          | `PluginGameEntry`                              |
| `PluginDataBaseGame<T>`           | `PluginGameCollection<T>`                      |
| `PluginDataBaseGameDetails<T, Y>` | `PluginGameCollectionWithDetails<T, TDetails>` |
| Paramètre de type`Y`              | `TDetails`                                     |

---

## Search & Replace

```text
Find    : PluginDataBaseGameBase
Replace : PluginGameEntry

Find    : PluginDataBaseGameDetails<
Replace : PluginGameCollectionWithDetails<

Find    : PluginDataBaseGame<
Replace : PluginGameCollection<
```

---

## Checklist

- [x] `PluginGameEntry.cs`
- [x] `PluginGameCollection.cs`
- [x] `PluginGameCollectionWithDetails.cs`
- [x] Build solution
