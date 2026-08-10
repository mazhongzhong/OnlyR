using OnlyR.Core.Enums;
using System;

namespace OnlyR.Services.Tray;

/// <summary>
/// Identifies which tray icon variant should be displayed.
/// </summary>
public enum TrayIconState
{
    Idle,
    Recording,
    Paused
}

/// <summary>
/// Contains the tray presentation values derived from the current recording state.
/// </summary>
public sealed record TrayPresentationState(
    TrayIconState IconState,
    string ToolTipText,
    string PauseResumeText,
    bool IsStartEnabled,
    bool IsStopEnabled,
    bool IsPauseResumeEnabled)
{
    /// <summary>
    /// Creates tray presentation values without depending on a real tray icon or window.
    /// </summary>
    public static TrayPresentationState Create(
        RecordingStatus recordingStatus,
        bool canRecord,
        bool canPause) =>
        recordingStatus switch
        {
            RecordingStatus.NotRecording => new TrayPresentationState(
                TrayIconState.Idle,
                Properties.Resources.NOT_RECORDING,
                Properties.Resources.PAUSE_RECORDING_TOOLTIP,
                canRecord,
                false,
                false),

            RecordingStatus.Recording => new TrayPresentationState(
                TrayIconState.Recording,
                Properties.Resources.RECORDING,
                Properties.Resources.PAUSE_RECORDING_TOOLTIP,
                false,
                true,
                canPause),

            RecordingStatus.Paused => new TrayPresentationState(
                TrayIconState.Paused,
                Properties.Resources.PAUSED,
                Properties.Resources.RESUME_RECORDING_TOOLTIP,
                false,
                true,
                true),

            RecordingStatus.StopRequested => new TrayPresentationState(
                TrayIconState.Recording,
                Properties.Resources.STOPPING,
                Properties.Resources.PAUSE_RECORDING_TOOLTIP,
                false,
                false,
                false),

            _ => throw new ArgumentOutOfRangeException(
                nameof(recordingStatus),
                recordingStatus,
                "Unsupported recording status")
        };
}