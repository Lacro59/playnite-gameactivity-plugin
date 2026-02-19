# 🤖 AI Coding Instructions & Rules: Playnite Plugin Development

## 👤 User Context
- **Profile:** Experienced developer.
- **Communication Style:** Direct, technical, and concise. No conversational filler or unnecessary apologies.
- **Objective:** Production-ready, maintainable, and high-performance code for Playnite.

---

## ❓ Clarification & Decision Policy

- **Ambiguity Handling:** If requirements are ambiguous, incomplete, contradictory, or technically unclear, request clarification before implementation.
- **Missing Dependencies:** If a required file, interface, model, API contract, or configuration is missing, explicitly request it.
- **Architectural Choices:** If multiple valid architectural approaches exist and the choice impacts structure, maintainability, or performance, briefly present the alternatives and request direction.
- **No Hidden Assumptions:** Do not silently assume business logic, data structure, or workflow behavior.
- **Explicit Assumptions:** If assumptions are strictly necessary to proceed, clearly state them before implementation.
- **Scope Validation:** If the request exceeds defined constraints (framework version, C# version, plugin architecture), explicitly flag the conflict before proceeding.
- **Partial Context:** If only partial code is provided, do not invent surrounding architecture. Request missing context when required for correctness.

---

## 🌍 Language & Documentation
- **Code & Docs:** All code elements, comments, and documentation (XML Doc, README) must be in **English**.

---

## 📏 Naming Conventions (Standard C#)
- **PascalCase:** Classes, Methods, Properties, Public Fields, and Enums.
- **camelCase:** Local variables and Method parameters.
- **_camelCase:** Private fields (must start with an underscore).
- **Interfaces:** Must always start with a capital "I" (e.g., `IGameProvider`).
- **Meaningful Names:** Avoid abbreviations (use `userRepository` instead of `uRepo`).

---

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

---

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

---

## 🧵 Threading & Asynchrony
- **UI Thread Access:** Use `Application.Current.Dispatcher.Invoke()` or `InvokeAsync()` for UI updates.
- **Async/Await:** Supported in .NET 4.6.2. Use for I/O-bound operations.
- **Background Work:** Use `Task.Run()` for CPU-bound operations.
- **Blocking Calls:** **NEVER** block UI thread with `.Result` or `.Wait()`. Always use `await` or callbacks.
- **Cancellation:** Use `CancellationToken` for long-running operations.

---

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

---

## 🛠 Coding Standards
- **Design Principles:** Follow SOLID principles and clean architecture.
- **Typing:** Strict typing is mandatory. No `dynamic` unless absolutely necessary.
- **Error Handling:**
    - Use robust try-catch blocks around external API calls.
    - Use guard clauses at method entry points.
    - Log all exceptions with context using `LogManager.GetLogger().Error(ex, "context")`.
- **Null Checks:** Always validate inputs and check for null before dereferencing.
- **Async Methods:** Methods returning `Task` should have `Async` suffix (e.g., `LoadDataAsync()`).

---

## 📊 Logging Best Practices
- **Levels:**
    - `Debug`: Detailed diagnostic information (disabled in production).
    - `Info`: General informational messages (plugin start, significant events).
    - `Warn`: Potentially harmful situations (deprecated API usage, fallback logic).
    - `Error`: Error events that might still allow the application to continue.
- **Exception Logging:** Always use `logger.Error(ex, "Context message")` to include stack traces.
- **Sensitive Data:** Never log passwords, API keys, or personal user information.

---

## 🧪 Testing
- **Framework:** NUnit 3.x (compatible with .NET Framework 4.6.2).
- **Mocking:** Use Moq (v4.x) for mocking `IPlayniteAPI` and dependencies.
- **Coverage:** Aim for >70% code coverage on business logic.
- **Test Structure:**
    - Arrange: Set up test data and mocks.
    - Act: Execute the method under test.
    - Assert: Verify expected outcomes.
- **Naming:** Use descriptive test names: `MethodName_Scenario_ExpectedBehavior`.

---

## 📝 Response Rules
1. **Code First:** Provide the code solution before any technical explanation.
2. **Modular:** Break down logic into small, testable, and reusable methods.
3. **XML Documentation:** Add XML Doc comments for public APIs.
4. **Comments:** Document only complex logic in English (avoid obvious comments).
5. **Unit Tests:** Suggest unit test structures when relevant.
6. **Complete Solutions:** Provide full, working code snippets (not partial fragments).
7. **Clarification Compliance:** Follow the Clarification & Decision Policy before implementing.

---

## 🚫 Out of Scope
- No deprecated code (unless explicitly required by .NET 4.6.2 limitations).
- No obvious comments (e.g., `i++ // increment`).
- No "fluff" or repetitive greetings.
- No placeholder code (`// TODO: implement`). Provide working implementations.

---

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
- [ ] Assumptions explicitly stated if required

---

**Last Updated:** 2026-02-19  
**Version:** 2.1
