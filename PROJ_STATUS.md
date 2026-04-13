# Jira StopWatch — Comprehensive Project Documentation

## 1. Project Overview

**Jira StopWatch** is a Windows desktop application for recording time spent on different Jira tasks. It provides multiple stopwatch timers that users can associate with Jira issues, then submit recorded time as worklogs directly to Jira. The project is licensed under **Apache License 2.0** and was originally authored by **Carsten Gehling**.

- **Product Homepage**: [jirastopwatch.com](http://jirastopwatch.com)
- **Repository**: [github.com/tulleuchen/jirastopwatch](https://github.com/tulleuchen/jirastopwatch)
- **Latest Version**: 2.2.0 (2017-10-31)
- **Status**: Looking for a new maintainer (noted in [`README.md`](README.md:4))

---

## 2. Solution & Build Structure

### Solution Files
| Solution | File                                         | Purpose                                              |
| -------- | -------------------------------------------- | ---------------------------------------------------- |
| Main     | [`StopWatch.sln`](StopWatch.sln:1)           | Contains `StopWatch` (app) + `StopWatchTest` (tests) |
| Setup    | [`StopWatchSetup.sln`](StopWatchSetup.sln:1) | MSI installer project (`StopWatchSetup.vdproj`)      |

### Project Configuration
- **Target Framework**: .NET Framework 4.5 ([`StopWatch.csproj`](source/StopWatch/StopWatch.csproj:12))
- **Output Type**: `WinExe` (Windows Forms application) ([`StopWatch.csproj`](source/StopWatch/StopWatch.csproj:8))
- **Namespace**: `StopWatch`
- **Visual Studio**: Version 14 (VS 2015)
- **Debug Config**: Warnings treated as errors ([`StopWatch.csproj`](source/StopWatch/StopWatch.csproj:24))

### External Dependencies
| Package                                             | Version       | Purpose                                                      |
| --------------------------------------------------- | ------------- | ------------------------------------------------------------ |
| [RestSharp](https://github.com/restsharp/RestSharp) | 105.2.3       | HTTP client for Jira REST API communication ([`packages.config`](source/StopWatch/packages.config:3)) |
| NUnit                                               | 2.6.4         | Unit testing framework (test project only)                   |
| Moq                                                 | 4.2.1510.2205 | Mocking framework (test project only)                        |
| NUnitTestAdapter                                    | 2.0.0         | VS test runner adapter (test project only)                   |

---

## 3. Architecture Overview

```
┌─────────────────────────────────────────────────────────┐
│                    UI Layer (WinForms)                    │
│  MainForm ─── IssueControl ─── WorklogForm               │
│  SettingsForm ─── EditTimeForm ─── AboutForm              │
├─────────────────────────────────────────────────────────┤
│               Application Core                            │
│  Settings (Singleton) ─── WatchTimer (Model)              │
├─────────────────────────────────────────────────────────┤
│               Jira Integration Layer                      │
│  JiraClient ─── JiraApiRequestFactory ─── JiraApiRequester│
├─────────────────────────────────────────────────────────┤
│               Infrastructure / Helpers                    │
│  RestSharp Factories ─── DPAPI ─── Logger ─── Helpers     │
└─────────────────────────────────────────────────────────┘
```

---

## 4. Entry Point & Application Lifecycle

### [`Program.cs`](source/StopWatch/Program.cs:24)

The static [`Program`](source/StopWatch/Program.cs:25) class serves as the entry point:

- **Single Instance Enforcement** (line 28): Uses a named `Mutex` (`{D5597999-20FE-430F-8E5D-8893EBED2599}`) to ensure only one instance runs. If a second instance is launched, it sends a `WM_SHOWME` broadcast message to bring the existing instance to the foreground.
- **TLS Security** (line 37): Forces `SecurityProtocol` to TLS 1.1/1.2 (disabling older insecure protocols).
- **Session Lock Handling** (line 45): Subscribes to `SystemEvents.SessionSwitch` to detect desktop lock/unlock events, delegating to [`MainForm.HandleSessionLock()`](source/StopWatch/UI/MainForm.cs:69) and [`MainForm.HandleSessionUnlock()`](source/StopWatch/UI/MainForm.cs:88).
- **Global Error Handling** (lines 42-43): Catches unhandled thread and domain exceptions, logs them to `jirastopwatch.log` in `UserAppDataPath`.

---

## 5. Jira Integration Layer

### 5.1 [`JiraClient`](source/StopWatch/Jira/JiraClient.cs:21) — High-Level API Facade

The central class for all Jira operations. Uses constructor injection of [`IJiraApiRequestFactory`](source/StopWatch/Jira/IJiraApiRequestFactory.cs:21) and [`IJiraApiRequester`](source/StopWatch/Jira/IJiraApiRequester.cs:20).

| Method                                                       | Line | Purpose                                                      |
| ------------------------------------------------------------ | ---- | ------------------------------------------------------------ |
| [`Authenticate()`](source/StopWatch/Jira/JiraClient.cs:38)   | 38   | Sets Basic Auth credentials and loads Jira time-tracking configuration |
| [`ValidateSession()`](source/StopWatch/Jira/JiraClient.cs:47) | 47   | Verifies current session via `/rest/auth/1/session`          |
| [`GetFavoriteFilters()`](source/StopWatch/Jira/JiraClient.cs:66) | 66   | Retrieves user's favorite JQL filters                        |
| [`GetIssuesByJQL()`](source/StopWatch/Jira/JiraClient.cs:80) | 80   | Searches issues by JQL (max 200 results)                     |
| [`GetIssueSummary()`](source/StopWatch/Jira/JiraClient.cs:94) | 94   | Fetches issue summary (optionally with project name)         |
| [`GetIssueTimetracking()`](source/StopWatch/Jira/JiraClient.cs:101) | 101  | Gets remaining estimate for an issue                         |
| [`GetTimeTrackingConfiguration()`](source/StopWatch/Jira/JiraClient.cs:115) | 115  | Fetches Jira's time tracking config (hours/day, etc.)        |
| [`PostWorklog()`](source/StopWatch/Jira/JiraClient.cs:128)   | 128  | Posts work log with time, start time, comment, and estimate adjustment |
| [`PostComment()`](source/StopWatch/Jira/JiraClient.cs:143)   | 143  | Posts a standalone comment to an issue                       |
| [`GetAvailableTransitions()`](source/StopWatch/Jira/JiraClient.cs:158) | 158  | Gets workflow transitions for an issue                       |
| [`DoTransition()`](source/StopWatch/Jira/JiraClient.cs:172)  | 172  | Executes a workflow transition (e.g., "In Progress")         |

### 5.2 [`JiraApiRequestFactory`](source/StopWatch/Jira/JiraApiRequestFactory.cs:21) — Request Builder

Implements [`IJiraApiRequestFactory`](source/StopWatch/Jira/IJiraApiRequestFactory.cs:21). Creates `IRestRequest` objects for each Jira REST API v2 endpoint:

| Endpoint                                      | Method   | Used For           |
| --------------------------------------------- | -------- | ------------------ |
| `/rest/auth/1/session`                        | GET      | Session validation |
| `/rest/api/2/filter/favourite`                | GET      | Favorite filters   |
| `/rest/api/2/search?jql={jql}&maxResults=200` | GET      | Issue search       |
| `/rest/api/2/issue/{key}`                     | GET      | Issue summary      |
| `/rest/api/2/issue/{key}?fields=timetracking` | GET      | Time tracking      |
| `/rest/api/2/issue/{key}/worklog`             | POST     | Submit worklog     |
| `/rest/api/2/issue/{key}/comment`             | POST     | Post comment       |
| `/rest/api/2/issue/{key}/transitions`         | GET/POST | Transitions        |
| `/rest/api/2/configuration`                   | GET      | Jira configuration |

Worklog posting supports 4 estimate adjustment methods defined in [`EstimateUpdateMethods`](source/StopWatch/Helpers/NativeMethods.cs:36): `Auto`, `Leave`, `SetTo`, `ManualDecrease`.

### 5.3 [`JiraApiRequester`](source/StopWatch/Jira/JiraApiRequester.cs:23) — HTTP Executor

Implements [`IJiraApiRequester`](source/StopWatch/Jira/IJiraApiRequester.cs:20). Responsible for:

- **Basic Auth** (line 67-74): Adds `Authorization: Basic <base64(username:apiToken)>` header to every request.
- **Request Execution** (line 34): Uses [`RestClientFactory`](source/StopWatch/RestSharp/RestClientFactory.cs:21) to create RestSharp clients and execute requests.
- **Error Handling**: Throws [`RequestDeniedException`](source/StopWatch/Jira/JiraApiRequester.cs:84) on HTTP 401, 400, or any non-200/201 status.
- **Logging**: Logs requests/responses via the singleton [`Logger`](source/StopWatch/Logging/Logger.cs:22).

### 5.4 Jira DTOs

| DTO                                                          | File                           | Properties                                                   |
| ------------------------------------------------------------ | ------------------------------ | ------------------------------------------------------------ |
| [`Issue`](source/StopWatch/Jira/DTO/Issue.cs:18)             | DTO/Issue.cs:18                | `Key`, `Fields`                                              |
| [`IssueFields`](source/StopWatch/Jira/DTO/IssueFields.cs:18) | DTO/IssueFields.cs:18          | `Summary`, `Timetracking`, `Project`                         |
| [`TimetrackingFields`](source/StopWatch/Jira/DTO/IssueFields.cs:25) | DTO/IssueFields.cs:25          | `RemainingEstimate`, `RemainingEstimateSeconds`              |
| [`ProjectFields`](source/StopWatch/Jira/DTO/IssueFields.cs:31) | DTO/IssueFields.cs:31          | `Name`                                                       |
| [`Filter`](source/StopWatch/Jira/DTO/Filter.cs:18)           | DTO/Filter.cs:18               | `Id`, `Name`, `Jql`                                          |
| [`SearchResult`](source/StopWatch/Jira/DTO/SearchResult.cs:20) | DTO/SearchResult.cs:20         | `Issues` (List)                                              |
| [`AvailableTransitions`](source/StopWatch/Jira/DTO/AvailableTransitions.cs:20) | DTO/AvailableTransitions.cs:20 | `Expand`, `Transitions` (List)                               |
| [`Transition`](source/StopWatch/Jira/DTO/Transition.cs:18)   | DTO/Transition.cs:18           | `Id`, `Name`                                                 |
| [`JiraConfiguration`](source/StopWatch/Jira/DTO/JiraConfiguration.cs:18) | DTO/JiraConfiguration.cs:18    | `timeTrackingConfiguration`                                  |
| [`TimeTrackingConfiguration`](source/StopWatch/Jira/DTO/JiraConfiguration.cs:23) | DTO/JiraConfiguration.cs:23    | `workingHoursPerDay`, `workingHoursPerWeek`, `timeFormat`, `defaultUnit` |

---

## 6. Model Layer

### [`WatchTimer`](source/StopWatch/Model/WatchTimer.cs:31)

The core timer model with start/pause/reset behavior:

| Member                                                       | Line | Description                                                  |
| ------------------------------------------------------------ | ---- | ------------------------------------------------------------ |
| [`TimeElapsed`](source/StopWatch/Model/WatchTimer.cs:34)     | 34   | Returns `totalTime + (now - sessionStartTime)` when running  |
| [`TimeElapsedNearestMinute`](source/StopWatch/Model/WatchTimer.cs:54) | 54   | Rounds up to nearest minute (for worklog posting)            |
| [`Running`](source/StopWatch/Model/WatchTimer.cs:62)         | 62   | Boolean indicating if timer is active                        |
| [`Start()`](source/StopWatch/Model/WatchTimer.cs:73)         | 73   | Starts timer, records `initialStartTime` on first start      |
| [`Pause()`](source/StopWatch/Model/WatchTimer.cs:86)         | 86   | Pauses timer, accumulates elapsed into `totalTime`           |
| [`Reset()`](source/StopWatch/Model/WatchTimer.cs:96)         | 96   | Zeros everything, clears `initialStartTime`                  |
| [`GetState()`](source/StopWatch/Model/WatchTimer.cs:104)     | 104  | Snapshots current state as [`TimerState`](source/StopWatch/Model/WatchTimer.cs:20) |
| [`SetState()`](source/StopWatch/Model/WatchTimer.cs:128)     | 128  | Restores from a `TimerState` snapshot                        |
| [`GetInitialStartTime()`](source/StopWatch/Model/WatchTimer.cs:144) | 144  | Returns the earlier of recorded vs. estimated start time (for worklog accuracy) |

### [`TimerState`](source/StopWatch/Model/WatchTimer.cs:20)

Serializable snapshot: `Running`, `TotalTime`, `SessionStartTime`, `InitialStartTime`.

---

## 7. Settings & Persistence

### [`Settings`](source/StopWatch/Settings/Settings.cs:45) — Singleton

Thread-safe singleton using .NET `Properties.Settings` for user-scoped configuration with a lock-guarded [`Save()`](source/StopWatch/Settings/Settings.cs:145).

| Setting               | Type                                                         | Default                      | Description                         |
| --------------------- | ------------------------------------------------------------ | ---------------------------- | ----------------------------------- |
| `JiraBaseUrl`         | string                                                       | `http://myjiraserver.local/` | Jira server URL                     |
| `Username`            | string                                                       | empty                        | Jira username                       |
| `ApiToken`            | string                                                       | empty                        | DPAPI-encrypted API token           |
| `AlwaysOnTop`         | bool                                                         | false                        | Window stays on top                 |
| `MinimizeToTray`      | bool                                                         | false                        | Minimize to system tray             |
| `IssueCount`          | int                                                          | 6                            | Number of issue timer rows          |
| `AllowMultipleTimers` | bool                                                         | false                        | Allow concurrent timers             |
| `IncludeProjectName`  | bool                                                         | false                        | Show project name with summary      |
| `SaveTimerState`      | [`SaveTimerSetting`](source/StopWatch/Settings/Settings.cs:24) | NoSave                       | Timer persistence on exit           |
| `PauseOnSessionLock`  | [`PauseAndResumeSetting`](source/StopWatch/Settings/Settings.cs:31) | NoPause                      | Behavior on desktop lock            |
| `PostWorklogComment`  | [`WorklogCommentSetting`](source/StopWatch/Settings/Settings.cs:38) | WorklogOnly                  | Where to post comments              |
| `StartTransitions`    | string                                                       | empty                        | Transition names to trigger on play |
| `LoggingEnabled`      | bool                                                         | false                        | Enable API logging                  |
| `CheckForUpdate`      | bool                                                         | true                         | Auto-check for updates on GitHub    |
| `CurrentFilter`       | int                                                          | 0                            | Currently selected JQL filter ID    |

**Issue Persistence**: [`PersistedIssue`](source/StopWatch/Settings/PersistedIssue.cs:21) objects are serialized via `BinaryFormatter` → Base64 string. Each stores: `Key`, `TimerRunning`, `InitialStartTime`, `SessionStartTime`, `TotalTime`, `Comment`, `EstimateUpdateMethod`, `EstimateUpdateValue`.

**Settings Upgrade**: On version change, [`Properties.Settings.Default.Upgrade()`](source/StopWatch/Settings/Settings.cs:103) is called to migrate from previous versions.

---

## 8. UI Layer (Windows Forms)

### 8.1 [`MainForm`](source/StopWatch/UI/MainForm.cs:28) — Primary Window

The main application form orchestrating everything:

- **Initialization** (constructor, line 32): Creates the full dependency chain — `RestRequestFactory` → `JiraApiRequestFactory` → `RestClientFactory` → `JiraApiRequester` → `JiraClient`.
- **Timer Tick** (line 125): A `System.Windows.Forms.Timer` with 500ms initial delay then 30s recurring interval. Updates connection status, issue summaries, and persists settings.
- **Issue Controls Management** (line 308): Dynamically creates/removes [`IssueControl`](source/StopWatch/UI/IssueControl.cs:24) instances in a scrollable panel. Maximum 20 issues ([`maxIssues`](source/StopWatch/UI/MainForm.cs:724)).
- **Filter Loading** (line 573): Loads user's Jira favorite filters plus hardcoded "My open issues" default filter.
- **State Transitions** (line 501): When a timer starts, optionally triggers Jira workflow transitions matching configured names.
- **Update Check** (line 648): On first tick, checks GitHub API for newer releases.
- **Single Instance** (line 622): Overrides `WndProc` to handle `WM_SHOWME` messages.

**Keyboard Shortcuts** (line 730):

| Key          | Action                       |
| ------------ | ---------------------------- |
| CTRL+UP/DOWN | Navigate timer rows          |
| CTRL+P       | Toggle play/pause            |
| CTRL+L       | Submit worklog               |
| CTRL+E       | Edit time                    |
| CTRL+R       | Reset timer                  |
| CTRL+DEL     | Delete timer row             |
| CTRL+N       | Add new timer                |
| CTRL+I       | Focus issue combobox         |
| CTRL+C/V     | Copy/paste issue key         |
| CTRL+O       | Open issue in browser        |
| ALT+DOWN     | Open issue combobox dropdown |

### 8.2 [`IssueControl`](source/StopWatch/UI/IssueControl.cs:24) — Issue Timer Row (UserControl)

Each row contains: issue combobox (`cbJira`), open-in-browser button, play/pause button, time display, post worklog button, reset button, and remove button. Key behaviors:

- **Issue Selection** (line 485): On dropdown, loads issues from selected JQL filter via async `Task.Factory.StartNew()`.
- **Timer Controls** (line 542): `StartStop()` toggles the [`WatchTimer`](source/StopWatch/Model/WatchTimer.cs:31) and fires `TimerStarted` event.
- **Worklog Submission** (line 577): Opens [`WorklogForm`](source/StopWatch/UI/WorklogForm.cs:22), posts via `JiraClient`, then resets on success.
- **Paste/URL parsing** (line 521): Accepts pasted Jira URLs and extracts issue keys via [`JiraKeyHelpers.ParseUrlToKey()`](source/StopWatch/Helpers/JiraKeyHelpers.cs:22).
- **Custom ComboBox Drawing** (line 448): Owner-drawn dropdown with two columns (key + summary).

### 8.3 [`WorklogForm`](source/StopWatch/UI/WorklogForm.cs:22) — Worklog Submission Dialog

- Comment text box with CTRL+Enter to submit
- Start date/time pickers (pre-populated from timer's initial start time)
- Four estimate update options: Auto adjust, Leave unchanged, Set to, Manual decrease
- Validates time inputs using [`JiraTimeHelpers.JiraTimeToTimeSpan()`](source/StopWatch/Helpers/JiraTimeHelpers.cs:66)
- Supports "Save comment only" mode (DialogResult.Yes) without posting

### 8.4 [`SettingsForm`](source/StopWatch/UI/SettingsForm.cs:25) — Configuration Dialog

Exposes all user settings. Includes a link to Atlassian's API token management page ([`lblOpenAPITokensPage`](source/StopWatch/UI/SettingsForm.cs:132), line 132).

### 8.5 [`EditTimeForm`](source/StopWatch/UI/EditTimeForm.cs:22) — Manual Time Entry

Allows editing timer value in Jira time format (e.g., `2h 30m`). Validates input and highlights invalid entries.

### 8.6 [`AboutForm`](source/StopWatch/UI/AboutForm.cs:20) — About Dialog

Displays product name, version, and links to license and homepage.

### 8.7 [`ComboTextBoxEvents`](source/StopWatch/UI/ComboTextBoxEvents.cs:22) — Win32 Subclassing

Subclasses the edit control within a ComboBox using `NativeWindow` to intercept `WM_PASTE` (for URL-to-key extraction) and `WM_LBUTTONDOWN` (for selection tracking).

---

## 9. Helpers & Utilities

### [`JiraTimeHelpers`](source/StopWatch/Helpers/JiraTimeHelpers.cs:22) (static)
- [`DateTimeToJiraDateTime()`](source/StopWatch/Helpers/JiraTimeHelpers.cs:26) — Formats `DateTimeOffset` to Jira's datetime format (culture-invariant)
- [`TimeSpanToJiraTime()`](source/StopWatch/Helpers/JiraTimeHelpers.cs:32) — Converts `TimeSpan` to Jira format (e.g., `2d 3h 15m`), respects Jira's `workingHoursPerDay` configuration
- [`JiraTimeToTimeSpan()`](source/StopWatch/Helpers/JiraTimeHelpers.cs:66) — Parses Jira time strings (`2h 5m`, `1.5h`, `1d2h5m`) to `TimeSpan`

### [`JiraKeyHelpers`](source/StopWatch/Helpers/JiraKeyHelpers.cs:20) (static)
- [`ParseUrlToKey()`](source/StopWatch/Helpers/JiraKeyHelpers.cs:22) — Extracts Jira issue key (pattern `[A-Z0-9_]+-\d+`) from URLs or text

### [`DPAPI`](source/StopWatch/Helpers/DPApi.cs:25) (static)
- Windows Data Protection API wrapper for encrypting/decrypting the API token stored in user settings
- Uses user-scoped key by default ([`KeyType.UserKey`](source/StopWatch/Helpers/DPApi.cs:128))

### [`InvokeExtensions`](source/StopWatch/Helpers/InvokeExtensions.cs:21) (extension)
- [`InvokeIfRequired()`](source/StopWatch/Helpers/InvokeExtensions.cs:28) — Thread-safe UI control invocation

### [`NativeMethods`](source/StopWatch/Helpers/NativeMethods.cs:21)
- Win32 interop for `PostMessage` and `RegisterWindowMessage` (single instance support)
- Defines [`EstimateUpdateMethods`](source/StopWatch/Helpers/NativeMethods.cs:36) enum

### [`CrossPlatformHelpers`](source/StopWatch/Helpers/CrossPlatformHelpers.cs:20)
- [`IsWindowsEnvironment()`](source/StopWatch/Helpers/CrossPlatformHelpers.cs:22) — Platform detection for conditional features (tray icon, session lock)

### [`StringHelpers`](source/StopWatch/Helpers/StringHelpers.cs:20)
- [`Truncate()`](source/StopWatch/Helpers/StringHelpers.cs:22) — Safe string truncation for logging

### [`Logger`](source/StopWatch/Logging/Logger.cs:22) — Singleton
- File-based logger with rotation (max 1MB per file, 5 rotated files)
- Thread-safe via `Monitor.TryEnter()` with 1-second timeout
- Controlled by `Settings.LoggingEnabled`

---

## 10. Update Check

### [`ReleaseHelper`](source/StopWatch/UpdateCheck/ReleaseHelper.cs:21)
- [`GetLatestVersion()`](source/StopWatch/UpdateCheck/ReleaseHelper.cs:23) — Queries GitHub API at `/repos/carstengehling/jirastopwatch/releases/latest`
- Returns [`GithubRelease`](source/StopWatch/UpdateCheck/DTO/GithubRelease.cs:20) DTO with `TagName`, `Name`, `Draft`, `PreRelease`, `PublishedAt`

---

## 11. RestSharp Abstraction Layer

Factory pattern for testability:

| Interface                                                    | Implementation                                               | Purpose                                                      |
| ------------------------------------------------------------ | ------------------------------------------------------------ | ------------------------------------------------------------ |
| [`IRestClientFactory`](source/StopWatch/RestSharp/IRestClientFactory.cs:20) | [`RestClientFactory`](source/StopWatch/RestSharp/RestClientFactory.cs:21) | Creates `RestClient` with shared `CookieContainer` and configurable `BaseUrl` |
| [`IRestRequestFactory`](source/StopWatch/RestSharp/IRestRequestFactory.cs:20) | [`RestRequestFactory`](source/StopWatch/RestSharp/RestRequestFactory.cs:20) | Creates `RestRequest` with URL and method                    |

---

## 12. Test Project

[`StopWatchTest`](source/StopWatchTest/StopWatchTest.csproj:1) — NUnit 2.6.4 + Moq 4.2 test suite.

### Test Files & Coverage

| Test Class                                                   | File                         | Tests | Focus                                                        |
| ------------------------------------------------------------ | ---------------------------- | ----- | ------------------------------------------------------------ |
| [`JiraClientTest`](source/StopWatchTest/JiraClientTest.cs:11) | JiraClientTest.cs            | 18    | All `JiraClient` methods — success/failure paths for Authenticate, ValidateSession, GetFavoriteFilters, GetIssuesByJQL, GetIssueSummary, GetIssueTimetracking, PostWorklog, PostComment, GetAvailableTransitions, DoTransition |
| [`JiraApiRequesterTest`](source/StopWatchTest/JiraApiRequesterTest.cs:17) | JiraApiRequesterTest.cs      | 2     | Basic auth header construction, valid/invalid credential handling |
| [`JiraApiRequestFactoryTest`](source/StopWatchTest/JiraApiRequestFactoryTest.cs:10) | JiraApiRequestFactoryTest.cs | 10    | All request factory methods — URL construction, parameter validation, whitespace trimming |
| [`JiraKeyHelpersTest`](source/StopWatchTest/JiraKeyHelpersTest.cs:8) | JiraKeyHelpersTest.cs        | 2     | URL-to-key parsing — match/no-match scenarios                |
| [`JiraTimeHelpersTest`](source/StopWatchTest/JiraTimeHelpersTest.cs:10) | JiraTimeHelpersTest.cs       | 12    | DateTime formatting, timezone handling, regional settings, Jira time parsing (days/hours/minutes, decimals, whitespace) |

Testing approach uses **Moq** to mock all interfaces (`IJiraApiRequestFactory`, `IJiraApiRequester`, `IRestClient`, `IRestClientFactory`, `IRestRequestFactory`), enabling isolated unit testing of each layer.

---

## 13. Key Design Patterns

| Pattern                   | Usage                                                        |
| ------------------------- | ------------------------------------------------------------ |
| **Singleton**             | [`Settings.Instance`](source/StopWatch/Settings/Settings.cs:47), [`Logger.Instance`](source/StopWatch/Logging/Logger.cs:28) |
| **Factory**               | `RestClientFactory`, `RestRequestFactory`, `JiraApiRequestFactory` |
| **Interface Segregation** | `IJiraApiRequester`, `IJiraApiRequestFactory`, `IRestClientFactory`, `IRestRequestFactory` |
| **Dependency Injection**  | Constructor injection throughout (manual, no DI container)   |
| **Observer/Events**       | `TimerStarted`, `TimerReset`, `Selected`, `RemoveMeTriggered`, `TimeEdited` events |
| **DTO**                   | Separate DTO classes for all Jira API responses              |

---

## 14. Configuration File ([`App.config`](source/StopWatch/App.config:1))

User-scoped settings stored in the standard .NET `userSettings` section with `PerUserRoamingAndLocal` scope. Default values defined in [`App.config`](source/StopWatch/App.config:28) and [`Properties/Settings.settings`](source/StopWatch/Properties/Settings.settings).

---

## 15. Documentation Site

The [`docs/`](docs/) folder contains a Bootstrap-based static website (previously hosted at jirastopwatch.com, now redirects to `jirastopwatch.github.io`). The [`docs/doc/index.html`](docs/doc/index.html:1) page provides end-user documentation covering installation, basic setup, usage workflow (choosing issues, using timers, submitting worklogs), keyboard shortcuts, and advanced settings.