# 🤖 AI Coding Instructions & Rules: Playnite Plugin Development

## 👤 User Context
- **Profile:** Experienced developer.
- **Communication Style:** Direct, technical, and concise. No conversational filler or unnecessary apologies.
- **Objective:** Production-ready, maintainable, and high-performance code for Playnite.

## 🌍 Language & Documentation
- **Code & Docs:** All code elements, comments, and documentation (XML Doc, README) must be in **English**.

## 📏 Naming Conventions (Standard C#)
- **PascalCase:** Classes, Methods, Properties, Public Fields, and Enums.
- **camelCase:** Local variables and Method parameters.
- **_camelCase:** Private fields (must start with an underscore).
- **Interfaces:** Must always start with a capital "I" (e.g., `IGameProvider`).
- **Meaningful Names:** Avoid abbreviations (use `userRepository` instead of `uRepo`).

## ⚙️ Versions & Frameworks Specifications
- **Target Framework:** .NET Framework 4.6.2 (C#).
- **SDK:** `Playnite.SDK` (Latest compatible with 4.6.2).
- **Strict Compliance:** Use **ONLY** features compatible with **C# 7.0** and below.
- **Restrictions:**
    - No range operators (`..`).
    - No `using` declarations without braces.
    - No `records` or `init-only` properties.
    - No `switch` expressions (use `switch` statements).
    - No `nullable reference types` (C# 8.0 features).
    - No pattern matching enhancements beyond C# 7.0.
- **Dependencies:** Ensure all NuGet packages are compatible with .NET Framework 4.6.2.

## 🎮 Playnite Plugin Specifics

### Plugin Architecture
- **Environment:** Playnite Desktop Client Plugin.
- **Base Classes:** Implement `GenericPlugin`, `LibraryPlugin`, or `MetadataPlugin` depending on plugin type.
- **API Access:** Always pass `IPlayniteAPI` via dependency injection to sub-services.
- **Settings:** Implement `ISettings` interface and use `GetSettings<T>()` pattern.

### Plugin Lifecycle
- **Initialization:** `OnApplicationStarted()` - Use for startup tasks.
- **Game Events:** `OnGameInstalled()`, `OnGameUninstalled()`, `OnGameStarting()`, `OnGameStarted()`, `OnGameStopped()`.
- **Library Events:** `OnLibraryUpdated()` - For library plugins.
- **Shutdown:** `OnApplicationStopped()` - Clean up resources.

### Core Features
- **Game Actions:** Override `GetGameActions()` to add custom game actions.
- **Menu Items:** Override `GetGameMenuItems()` and `GetMainMenuItems()` for UI integration.
- **Metadata Providers:** Implement `OnDemandMetadataProvider` for metadata plugins.
- **Database Access:** Use `IPlayniteAPI.Database` for game operations.

### API Usage
- **Logging:** Use `Playnite.SDK.LogManager.GetLogger()` for all logging.
- **Serialization:** Use `Playnite.SDK.Data.Serialization` for JSON operations.
- **Resources:** Access via `ResourceProvider.GetString("LOC_KEY")` for localization.
- **Dialogs:** Use `IPlayniteAPI.Dialogs` for user notifications and input.

### UI Development
- **Framework:** WPF (XAML) for custom views and settings windows.
- **Thread Safety:** Always check `Application.Current.Dispatcher.CheckAccess()` before UI operations.
- **View Models:** Implement `INotifyPropertyChanged` for data binding.
- **Resources:** Define styles in ResourceDictionary, reference Playnite themes when possible.

## 📦 Plugin Manifest (extension.yaml)
- **Required Fields:**
    - `Id` (GUID format): Unique plugin identifier.
    - `Name`: Display name.
    - `Author`: Your name/organization.
    - `Version`: Semantic versioning (e.g., 1.0.0).
    - `Module`: DLL filename (must match assembly name).
    - `Type`: `GenericPlugin`, `LibraryPlugin`, or `MetadataPlugin`.
- **Optional Fields:**
    - `Icon`: Plugin icon filename.
    - `Links`: Project website, support, source repository.

## 🧵 Threading & Asynchrony
- **UI Thread Access:** Use `Application.Current.Dispatcher.Invoke()` or `InvokeAsync()` for UI updates.
- **Async/Await:** Supported in .NET 4.6.2. Use for I/O-bound operations.
- **Background Work:** Use `Task.Run()` for CPU-bound operations.
- **Blocking Calls:** **NEVER** block UI thread with `.Result` or `.Wait()`. Always use `await` or callbacks.
- **Cancellation:** Use `CancellationToken` for long-running operations.

## 🚀 Performance Guidelines
- **Caching:**
    - Cache metadata and images to avoid repeated API calls.
    - Use `System.Runtime.Caching.MemoryCache` for in-memory caching.
    - Invalidate cache appropriately on data updates.
- **Lazy Loading:** Use `Lazy<T>` for expensive initializations.
- **Batch Operations:**
    - Use `IPlayniteAPI.Database.Games.BeginBufferUpdate()` / `EndBufferUpdate()` for bulk updates.
    - Minimize database queries in loops.
- **Resource Management:**
    - Always dispose `HttpClient`, `Stream`, database connections.
    - Use `using` statements with explicit braces for `IDisposable` objects.
- **Image Handling:**
    - Resize images before storing in Playnite database.
    - Use appropriate image formats (WebP for size, PNG for quality).

## 🛠 Coding Standards
- **Design Principles:** Follow SOLID principles and clean architecture.
- **Typing:** Strict typing is mandatory. No `dynamic` unless absolutely necessary.
- **Error Handling:**
    - Use robust try-catch blocks around external API calls.
    - Use guard clauses at method entry points.
    - Log all exceptions with context using `LogManager.GetLogger().Error(ex, "context")`.
- **Null Checks:** Always validate inputs and check for null before dereferencing.
- **Async Methods:** Methods returning `Task` should have `Async` suffix (e.g., `LoadDataAsync()`).

## 📊 Logging Best Practices
- **Levels:**
    - `Debug`: Detailed diagnostic information (disabled in production).
    - `Info`: General informational messages (plugin start, significant events).
    - `Warn`: Potentially harmful situations (deprecated API usage, fallback logic).
    - `Error`: Error events that might still allow the application to continue.
- **Exception Logging:** Always use `logger.Error(ex, "Context message")` to include stack traces.
- **Sensitive Data:** Never log passwords, API keys, or personal user information.

## 🧪 Testing
- **Framework:** NUnit 3.x (compatible with .NET Framework 4.6.2).
- **Mocking:** Use Moq (v4.x) for mocking `IPlayniteAPI` and dependencies.
- **Coverage:** Aim for >70% code coverage on business logic.
- **Test Structure:**
    - Arrange: Set up test data and mocks.
    - Act: Execute the method under test.
    - Assert: Verify expected outcomes.
- **Naming:** Use descriptive test names: `MethodName_Scenario_ExpectedBehavior`.

## 🌐 Localization
- **Resources:** Use XAML files (Playnite standard) for all user-facing strings.
- **Structure:** 
    - `Localization/LocSource.xaml` - Source file with all keys
    - `Localization/{lang_code}.xaml` - Translation files (e.g., `en_US.xaml`, `fr_FR.xaml`)
- **Naming:** Use `LOC_` prefix for localization keys (e.g., `LOC_PluginName`).
- **Access:** Use `ResourceProvider.GetString("LOC_KEY")` via Playnite SDK.
- **Fallback:** Always provide English (`LOCSource.xaml`) as default language.
- **Shared Resources:** Check for generic keys in common files before creating new ones:
    - `source/playnite-plugincommon/CommonPluginsResources/Localization/Common/LocSource.xaml`
    - `source/playnite-plugincommon/CommonPluginsResources/ResourcesPlaynite/*.xaml`
- **Translation Management:** Use Crowdin for community translations (see `crowdin.yml`).

## 🌿 Version Control
- **Commits:** Use Conventional Commits format:
    - `feat:` New feature.
    - `fix:` Bug fix.
    - `docs:` Documentation changes.
    - `refactor:` Code refactoring.
    - `test:` Adding or updating tests.
    - `chore:` Maintenance tasks.
- **.gitignore:** Exclude `bin/`, `obj/`, `*.user`, `.vs/`, build artifacts.
- **Branching:** Use feature branches, merge via pull requests.

## 📁 Project Structure (Recommended)

```
playnite-myplugin/
├── source/
│   ├── MyPlugin.csproj
│   ├── extension.yaml
│   ├── icon.png
│   ├── App.xaml                      # Global WPF resources
│   ├── MyPlugin.cs                   # Main plugin class (inherits PluginExtended)
│   ├── MyPluginSettings.cs           # Settings + ViewModel
│   ├── Clients/                      # External API clients (if needed)
│   │   └── ExternalApiClient.cs
│   ├── Services/                     # Business logic
│   │   ├── MyPluginDatabase.cs
│   │   └── (SubFolder)/              # Group related services in subfolders
│   ├── Models/                       # Data models
│   │   └── PluginData.cs
│   ├── Views/                        # XAML views
│   │   ├── PluginGameView.xaml/.cs
│   │   └── PluginSettingsView.xaml/.cs
│   ├── ViewModels/                   # MVVM ViewModels
│   │   └── PluginGameViewModel.cs
│   ├── Controls/                     # Custom controls (if needed)
│   │   └── CustomControl.xaml/.cs
│   ├── Converters/                   # WPF value converters (if needed)
│   │   └── MyConverter.cs
│   ├── Localization/                 # Translations
│   │   ├── LocSource.xaml
│   │   ├── en_US.xaml
│   │   └── ... (other languages)
│   ├── playnite-plugincommon/        # Git submodule (shared library)
│   │   ├── CommonPluginsControls/
│   │   ├── CommonPluginsResources/
│   │   ├── CommonPluginsShared/
│   │   └── CommonPluginsStores/
│   └── Properties/
│       └── AssemblyInfo.cs
├── build/
│   └── build.ps1
├── manifest/
│   └── PluginName.yaml
└── README.md
```

### 📂 Folder Organization

- **Clients/** : Separate from Services/ for external API interactions (optional)
- **Services/** : Business logic and data access. Use subfolders to group related services
- **Converters/** : All IValueConverter implementations for WPF data binding (optional)
- **Controls/** : Reusable custom UI controls (optional)
- **playnite-plugincommon/** : Shared library between Lacro59 plugins (Git submodule)

## 🏗 Lacro59 Plugin Architecture

### Base Class Pattern
- **Inheritance:** All plugins inherit from `PluginExtended<TSettings, TDatabase>`
- **Settings:** Combine settings and ViewModel in one class (e.g., `MyPluginSettings`)
- **Database:** Custom database class manages plugin-specific data caching

### Shared Library (playnite-plugincommon)

Common projects available via Git submodule:
- **CommonPluginsShared** - Shared utilities (logging, HTTP, helpers)
- **CommonPluginsControls** - Reusable UI controls
- **CommonPluginsResources** - Common resources
- **CommonPluginsStores** - Store integrations (Steam, Epic, etc.)
- **CommonPlayniteShared** - Playnite extensions and helpers

### Custom Elements Integration (Optional)

To expose controls for Playnite theme integration:
```csharp
AddCustomElementSupport(new AddCustomElementSupportArgs
{
    ElementList = new List<string> { "PluginButton", "PluginViewItem" },
    SourceName = "MyPlugin"
});
```

Usage in themes:
```xaml
<ContentControl Content="{Binding ElementName=PART_HtmlDescription, Path=DataContext.PluginButton}" />
```

### Search Integration (Optional)

Register a search provider accessible via prefix in Playnite search bar:
```csharp
Searches = new List<SearchSupport>
{
    new SearchSupport("prefix", "PluginName", new MyPluginSearch())
};
```

## 📝 Response Rules
1. **Code First:** Provide the code solution before any technical explanation.
2. **Modular:** Break down logic into small, testable, and reusable methods.
3. **XML Documentation:** Add XML Doc comments for public APIs.
4. **Comments:** Document only complex logic in English (avoid obvious comments).
5. **Unit Tests:** Suggest unit test structures when relevant.
6. **Complete Solutions:** Provide full, working code snippets (not partial fragments).

## 🚫 Out of Scope
- No deprecated code (unless explicitly required by .NET 4.6.2 limitations).
- No obvious comments (e.g., `i++ // increment`).
- No "fluff" or repetitive greetings.
- No placeholder code (`// TODO: implement`). Provide working implementations.

## ✅ Quality Checklist (Before Delivery)
- [ ] Code compiles without warnings on .NET Framework 4.6.2
- [ ] No C# 8.0+ features used
- [ ] All `IDisposable` objects wrapped in `using` with braces
- [ ] Exception handling implemented for external calls
- [ ] Logging added for significant operations
- [ ] UI operations use Dispatcher when needed
- [ ] XML documentation for public members
- [ ] Naming conventions respected
- [ ] No hardcoded strings (use resources/constants)

---

**Last Updated:** 2026-02-15  
**Version:** 2.0