# Wiki: Revert to Legacy Database Format

**Version:** 1.1  
**Date:** 2026-04-01  
**Plugin:** GameActivity

---

## Purpose

This page explains how to restore the **legacy database format** by running an **older plugin version**.

Use this procedure only if you need to return to a plugin build that reads/writes the previous model.

---

## Scope

This rollback applies to the plugin data folder:

- `ExtensionsData/afbb1a0d-04a1-4d0c-9afa-c6e42ca855b4/GameActivity`

---

## Downgrade Procedure

1. **Close Playnite** completely.
2. **Install the older plugin version** (replace current plugin files).
3. In `ExtensionsData/afbb1a0d-04a1-4d0c-9afa-c6e42ca855b4/GameActivity`, delete all files/folders **except** the migration ZIP archive.
4. Extract the ZIP archive into `ExtensionsData/afbb1a0d-04a1-4d0c-9afa-c6e42ca855b4/GameActivity`.
5. Start Playnite.
6. Open GameActivity and verify sessions and details are visible.
