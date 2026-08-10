# OnlyR system tray integration plan

## 1. Goal

Add reliable Windows system tray operation to OnlyR while preserving its existing recording engine, settings, output paths, error handling, and portable packaging.

The first release shall support:

1. Minimize the main window to the notification area.
2. Treat the window close button as “hide to tray”.
3. Show the current recording state in the tray icon and menu.
4. Start, stop, pause, and resume recording from the tray menu.
5. Restore the main window by double-clicking the tray icon.
6. Open the recordings folder from the tray menu.
7. Exit the application only through the tray menu’s Exit command.

## 2. Baseline

- Upstream repository: `AntonyCorbett/OnlyR`
- Upstream default branch: `master`
- Planning baseline commit: `481fb62cf9d23fca4f81dea2cf54dd46245f8cbe`
- Application: WPF, .NET 10, Windows x86
- Tray API: `System.Windows.Forms.NotifyIcon`
- External tray package: none

The implementation branch shall be `feature/tray-integration`, created from the fork’s `master` branch.

## 3. Scope boundaries

The following code is out of scope and must not be redesigned or replaced:

- `OnlyR.Core/Recorder/AudioRecorder.cs` and the rest of the recording pipeline
- NAudio/WASAPI capture, resampling, channel matching, buffering, mixing, and codecs
- Recording file naming, destination selection, disk-space checks, and error handling
- Existing WPF pages and visual layout
- Existing portable and installer delivery model

The tray menu must execute the existing recording commands. It must not call `AudioRecorder` directly or duplicate the logic in `RecordingPageViewModel`.

This phase also excludes global hotkeys, transcription, autostart, notifications, and a new settings page.

## 4. Required behavior

### 4.1 Window lifecycle

- Normal launch continues to show the main window.
- Minimizing hides the window and removes it from the taskbar.
- Clicking the window close button also hides the window and removes it from the taskbar.
- Hiding the window must not call `MainViewModel.Closing()` and must not stop or dispose an active recording.
- Double-clicking the tray icon restores, activates, and focuses the main window.
- Tray **Exit** sets an explicit exit intent and then invokes the existing close path.
- A real exit must continue to honor OnlyR’s current `AllowCloseWhenRecording` behavior and cleanup path.
- The existing “start minimized” option must result in a hidden main window with a visible tray icon.

### 4.2 Tray states

| Recording state | Tray state | Start | Stop | Pause / Resume |
| --- | --- | --- | --- | --- |
| Not recording | Idle | Enabled when `CanRecord` | Disabled | Disabled |
| Recording | Recording | Disabled | Enabled | Pause, enabled when `CanPause` |
| Paused | Paused | Disabled | Enabled | Resume, enabled |
| Stop requested | Recording/stopping | Disabled | Disabled | Disabled |

The icon and tooltip must update when `RecordingPageViewModel.RecordingStatus` changes.

### 4.3 Tray menu

The first version contains only:

- Start recording
- Stop recording
- Pause / Resume
- Show window
- Open recordings folder
- Exit

The recording items shall invoke:

- `StartRecordingCommand`
- `StopRecordingCommand`
- `PauseResumeRecordingCommand`
- `ShowRecordingsCommand`

Command availability shall follow the existing view-model state; the tray must not maintain a second recording state machine.

## 5. Design and file changes

### 5.1 New files

- `OnlyR/Services/Tray/ITrayIconService.cs`
  - Defines initialization, state refresh, show-window, and disposal responsibilities.
- `OnlyR/Services/Tray/TrayIconService.cs`
  - Owns `NotifyIcon` and `ContextMenuStrip`.
  - Executes the existing view-model commands.
  - Subscribes to view-model property changes.
  - Marshals UI actions onto the WPF dispatcher.
- `OnlyR/Services/Tray/TrayPresentationState.cs`
  - Pure mapping from OnlyR recording properties to icon/menu state, so it can be unit-tested without a live notification area.
- Tray icon resources for idle, recording, and paused states.
- `OnlyR.Tests/TestTrayPresentationState.cs`
  - Covers state-to-icon/menu mapping.

### 5.2 Existing files to modify

- `OnlyR/App.xaml.cs`
  - Register and initialize the tray service after dependency injection is configured.
  - Dispose the tray service during a real application exit.
- `OnlyR/MainWindow.xaml` and `OnlyR/MainWindow.xaml.cs`
  - Handle minimize and close-to-tray.
  - Track explicit exit intent separately from a window hide request.
  - Restore the existing window instead of creating a second one.
- `OnlyR/ViewModel/MainViewModel.cs`
  - Expose a narrow, read-only route to the existing `RecordingPageViewModel` commands and status for the tray service.
  - Keep page ownership and the existing recording workflow unchanged.
- `OnlyR.Tests/TestMainViewModel.cs` or a new window-lifecycle test helper
  - Cover the distinction between hide requests and real exit requests where practical without depending on Windows Explorer.

No changes are planned under `OnlyR.Core` or `OnlyR/Services/Audio`.

## 6. Implementation sequence

### Stage 1: establish a clean baseline

1. Fork the upstream repository.
2. Record the upstream commit used by this plan.
3. Build the unchanged solution in Release configuration.
4. Run the unchanged test suite and record the exact pass/fail counts.
5. Create `feature/tray-integration` from `master`.

Completion criterion: baseline build and tests pass before tray code is added.

### Stage 2: add testable tray state mapping

1. Add `TrayPresentationState`.
2. Map NotRecording, Recording, Paused, and StopRequested to icon, tooltip, menu text, and enabled states.
3. Add unit tests for every state and the `CanRecord` / `CanPause` edge cases.

Completion criterion: all tray state decisions are covered without constructing `NotifyIcon`.

### Stage 3: add `NotifyIcon` and reuse existing commands

1. Implement `TrayIconService` using `System.Windows.Forms.NotifyIcon`.
2. Build the fixed first-version menu.
3. Bind menu actions to the existing `RelayCommand` instances.
4. Subscribe to recording property changes and update tray presentation.
5. Restore the WPF window on tray double-click.

Completion criterion: tray commands drive the same recording flow as the main UI, with no calls into `AudioRecorder`.

### Stage 4: separate hide from real exit

1. Intercept minimize and hide the main window.
2. Intercept the close button and hide the main window.
3. Add explicit exit intent for Tray → Exit.
4. Route only the explicit exit through the existing `MainViewModel.Closing()` path.
5. Preserve asynchronous stop-and-close behavior when `AllowCloseWhenRecording` is enabled.
6. Verify the existing start-minimized option.

Completion criterion: minimize and close keep recording alive; Tray → Exit performs existing cleanup.

### Stage 5: verification and packaging

1. Run formatting and static analysis.
2. Build and run the complete automated test suite.
3. Perform manual Windows audio tests.
4. Build the portable package and verify tray icons are included.
5. Record concrete test results in a short test report before merging.

Completion criterion: automated tests pass, manual acceptance checks pass, and the portable build behaves the same as the development build.

## 7. Verification

### 7.1 Automated commands

```powershell
dotnet build OnlyR.slnx --configuration Release
dotnet test --project OnlyR.Tests/OnlyR.Tests.csproj --configuration Release
```

The final report must state the exact number of tests executed, passed, failed, and skipped. “All tests passed” alone is not sufficient.

### 7.2 Manual acceptance checks on Windows

1. Launch normally: window and idle tray icon are both present.
2. Minimize while idle: window and taskbar button disappear; tray icon remains.
3. Close with X while idle: same result as minimize; process remains running.
4. Start microphone recording from the tray; verify file output and red/recording state.
5. Start loopback recording from the tray; verify system audio output.
6. Start microphone plus loopback recording; verify the mixed file.
7. Minimize and close with X during each recording mode; verify recording continues.
8. Pause and resume from the tray; verify icon, menu text, elapsed-time behavior, and file continuity.
9. Stop from the tray; verify final file move/naming and idle state.
10. Open the recordings folder from the tray.
11. Double-click the tray icon; verify the same main window is restored and focused.
12. Launch with “start minimized”; verify no taskbar button and a usable tray icon.
13. Exit from the tray while idle; verify the process exits and the icon disappears.
14. Exit from the tray while recording with both values of `AllowCloseWhenRecording`; verify the existing policy is preserved.
15. Relaunch after exit; verify no stale icon, locked file, or orphaned process remains.

## 8. Definition of done

The feature is complete only when:

- all seven requested tray behaviors work in the portable Windows build;
- minimize and X never enter the real shutdown path;
- only Tray → Exit requests a real shutdown;
- tray actions reuse the existing recording commands;
- no recording-engine or codec code is changed;
- the complete existing test suite plus new tray tests passes;
- concrete automated and manual test results are committed to the repository.
