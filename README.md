<!-- markdownlint-disable MD033 MD041 -->

[![Crowdin](https://badges.crowdin.net/playnite-extensions/localized.svg)](https://crowdin.com/project/playnite-extensions)
[![GitHub release](https://img.shields.io/github/v/release/Lacro59/playnite-gameactivity-plugin?logo=github&color=8A2BE2)](https://github.com/Lacro59/playnite-gameactivity-plugin/releases/latest)
[![GitHub Release Date](https://img.shields.io/github/release-date/Lacro59/playnite-gameactivity-plugin?logo=github)](https://github.com/Lacro59/playnite-gameactivity-plugin/releases/latest)
[![GitHub downloads](https://img.shields.io/github/downloads/Lacro59/playnite-gameactivity-plugin/total?logo=github)](https://github.com/Lacro59/playnite-gameactivity-plugin/releases)
[![GitHub commit activity](https://img.shields.io/github/commit-activity/m/Lacro59/playnite-gameactivity-plugin/devel?logo=github)](https://github.com/Lacro59/playnite-gameactivity-plugin/graphs/commit-activity)
[![GitHub contributors](https://img.shields.io/github/contributors/Lacro59/playnite-gameactivity-plugin?logo=github)](https://github.com/Lacro59/playnite-gameactivity-plugin/graphs/contributors)
[![GitHub license](https://img.shields.io/github/license/Lacro59/playnite-gameactivity-plugin?logo=github)](https://github.com/Lacro59/playnite-gameactivity-plugin/blob/master/LICENSE)

# GameActivity for Playnite

Track gameplay sessions, visualize playtime trends, and monitor hardware performance directly inside [Playnite](https://playnite.link).

## ✨ Features

- **Advanced activity tracking**: records session date and elapsed time per game, then aggregates trends by day, week, month, source, and genre.
- **Per-game performance insights**: displays average metrics and session-level details such as FPS, CPU/GPU usage, RAM usage, temperatures, and power.
- **Hardware monitoring providers**: supports Windows Performance Counters, WMI, LibreHardware, HWiNFO, MSI Afterburner, and RivaTuner depending on your setup.
- **QuickSearch integration**: lets you search games by activity data using FPS, session duration, or date queries.
- **Built-in data tools**: includes CSV export, data mismatch checks, isolated-data detection, transfer tools, and database maintenance actions.
- **Theme integration points**: exposes controls for custom themes in game details, list views, and dedicated activity views.

## 📸 Screenshots

### Main interface

<a href="https://github.com/Lacro59/playnite-gameactivity-plugin/blob/master/forum/main_01.jpg?raw=true">
  <picture>
    <img alt="Game activity main dashboard with playtime charts and statistics" src="https://github.com/Lacro59/playnite-gameactivity-plugin/blob/master/forum/main_01.jpg?raw=true" height="200px">
  </picture>
</a>

### In-view controls

<a href="https://github.com/Lacro59/playnite-gameactivity-plugin/blob/master/forum/control_01.jpg?raw=true">
  <picture>
    <img alt="Chart controls for filtering and navigating session metrics" src="https://github.com/Lacro59/playnite-gameactivity-plugin/blob/master/forum/control_01.jpg?raw=true" height="200px">
  </picture>
</a>

### Settings panel

<a href="https://github.com/Lacro59/playnite-gameactivity-plugin/blob/master/forum/settings_01.jpg?raw=true">
  <picture>
    <img alt="Plugin settings including monitoring and integration options" src="https://github.com/Lacro59/playnite-gameactivity-plugin/blob/master/forum/settings_01.jpg?raw=true" height="200px">
  </picture>
</a>

## 🔍 Global Search

Not implemented

## 🔍 QuickSearch

GameActivity integrates with Playnite QuickSearch (command key: `ga`) and adds sub-commands to filter games by recorded activity data.

Example queries:

- `ga fps > 60`
- `ga fps 30 <> 60`
- `ga time > 2 h`
- `ga date 2025-01-01 <> 2025-01-31`

| Parameter | Purpose | Syntax | Example |
| --- | --- | --- | --- |
| `fps` | Filter by average FPS | `fps <value`, `fps >value`, `fps <min> <> <max>` | `fps > 75` |
| `time` | Filter by session duration | `time <value> <unit>`, `time >value <unit>`, `time <min> <unit> <> <max> <unit>` | `time 30 min <> 2 h` |
| `date` | Filter by session date | `date <YYYY-MM-DD`, `date >YYYY-MM-DD`, `date <start> <> <end>` | `date > 2026-01-01` |

Notes:

- Filters are not combinable in one query (`fps`, `time`, and `date` are used separately).
- Commands are interpreted in a case-insensitive way for parameter names.
- Date format should remain `YYYY-MM-DD` for reliable parsing.

## ⚙️ Configuration

### General behavior

- Enable or disable integration buttons in header, sidebar, and game details.
- Configure chart visibility, axis display, data density, and displayed metric series.
- Adjust session handling rules (ignore short sessions, cumulative behavior, paused-time subtraction).

### Hardware monitoring

- Enable logging and select automatic or manual provider mode.
- Configure provider-specific options (for example HWiNFO sensor IDs / indexes, LibreHardware remote endpoint, RivaTuner usage).
- Set fallback and cache behavior for stability when a provider fails.

### Warnings and analysis

- Configure in-game warning thresholds for FPS, CPU/GPU temperature, CPU/GPU usage, and RAM usage.
- Tune analysis windows used for recent activity and chart grouping.

> Auto-detection can work in many setups, but manually selecting and configuring your preferred provider often gives more accurate and stable metrics.

## 📥 Installation

### Install from Playnite Add-ons Browser (recommended)

1. Open Playnite.
2. Go to Add-ons > Browse > Generic.
3. Search for `GameActivity` and install it.
4. Restart Playnite if requested.

Official Playnite guide: [Installing Extensions](https://api.playnite.link/docs/manual/features/extensionsSupport/installingExtensions.html)

### Manual installation (`.pext`)

1. Download the latest `.pext` file from [Releases](https://github.com/Lacro59/playnite-gameactivity-plugin/releases/latest).
2. In Playnite, open Add-ons > Install from file.
3. Select the downloaded `.pext`.
4. Restart Playnite.

## 🤝 Contributing & Feedback

- **Bug reports**: [Open an issue](https://github.com/Lacro59/playnite-gameactivity-plugin/issues/new?template=bug_report.md)
- **Feature requests**: [Request an enhancement](https://github.com/Lacro59/playnite-gameactivity-plugin/issues/new?template=feature_request.md)
- **Pull requests**: [Submit a PR](https://github.com/Lacro59/playnite-gameactivity-plugin/pulls) targeting the `devel` branch
- **Translations**: [Contribute on Crowdin](https://crowdin.com/project/playnite-extensions)
- **Wiki & troubleshooting**: [Project wiki](https://github.com/Lacro59/playnite-gameactivity-plugin/wiki)

## 💝 Support

[![Ko-fi](https://img.shields.io/badge/Ko--fi-Support-FF5E5B?logo=ko-fi&logoColor=white)](https://ko-fi.com/lacro59)

If this plugin helps you, you can also support:

- [Playnite](https://www.patreon.com/playnite)
- [Freepik](https://www.flaticon.com/authors/freepik)

## 📄 License

This project is licensed under the [MIT License](https://github.com/Lacro59/playnite-gameactivity-plugin/blob/master/LICENSE).
