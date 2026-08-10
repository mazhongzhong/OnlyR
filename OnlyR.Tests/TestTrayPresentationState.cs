using OnlyR.Core.Enums;
using OnlyR.Services.Tray;
using System.Threading.Tasks;

namespace OnlyR.Tests;

public sealed class TestTrayPresentationState
{
    [Test]
    public async Task NotRecordingWithCanRecordEnablesOnlyStart()
    {
        var state = TrayPresentationState.Create(
            RecordingStatus.NotRecording,
            canRecord: true,
            canPause: false);

        await Assert.That(state.IconState).IsEqualTo(TrayIconState.Idle);
        await Assert.That(state.ToolTipText).IsEqualTo("Not recording");
        await Assert.That(state.PauseResumeText).IsEqualTo("Pause recording");
        await Assert.That(state.IsStartEnabled).IsTrue();
        await Assert.That(state.IsStopEnabled).IsFalse();
        await Assert.That(state.IsPauseResumeEnabled).IsFalse();
    }

    [Test]
    public async Task NotRecordingHonorsCanRecordFalse()
    {
        var state = TrayPresentationState.Create(
            RecordingStatus.NotRecording,
            canRecord: false,
            canPause: true);

        await Assert.That(state.IsStartEnabled).IsFalse();
        await Assert.That(state.IsStopEnabled).IsFalse();
        await Assert.That(state.IsPauseResumeEnabled).IsFalse();
    }

    [Test]
    public async Task RecordingWithCanPauseEnablesStopAndPause()
    {
        var state = TrayPresentationState.Create(
            RecordingStatus.Recording,
            canRecord: true,
            canPause: true);

        await Assert.That(state.IconState).IsEqualTo(TrayIconState.Recording);
        await Assert.That(state.ToolTipText).IsEqualTo("Recording");
        await Assert.That(state.PauseResumeText).IsEqualTo("Pause recording");
        await Assert.That(state.IsStartEnabled).IsFalse();
        await Assert.That(state.IsStopEnabled).IsTrue();
        await Assert.That(state.IsPauseResumeEnabled).IsTrue();
    }

    [Test]
    public async Task RecordingHonorsCanPauseFalse()
    {
        var state = TrayPresentationState.Create(
            RecordingStatus.Recording,
            canRecord: false,
            canPause: false);

        await Assert.That(state.IsStartEnabled).IsFalse();
        await Assert.That(state.IsStopEnabled).IsTrue();
        await Assert.That(state.IsPauseResumeEnabled).IsFalse();
    }

    [Test]
    public async Task PausedEnablesStopAndResume()
    {
        var state = TrayPresentationState.Create(
            RecordingStatus.Paused,
            canRecord: true,
            canPause: false);

        await Assert.That(state.IconState).IsEqualTo(TrayIconState.Paused);
        await Assert.That(state.ToolTipText).IsEqualTo("Paused");
        await Assert.That(state.PauseResumeText).IsEqualTo("Resume recording");
        await Assert.That(state.IsStartEnabled).IsFalse();
        await Assert.That(state.IsStopEnabled).IsTrue();
        await Assert.That(state.IsPauseResumeEnabled).IsTrue();
    }

    [Test]
    public async Task StopRequestedDisablesAllCommands()
    {
        var state = TrayPresentationState.Create(
            RecordingStatus.StopRequested,
            canRecord: true,
            canPause: true);

        await Assert.That(state.IconState).IsEqualTo(TrayIconState.Recording);
        await Assert.That(state.ToolTipText).IsEqualTo("Stopping");
        await Assert.That(state.PauseResumeText).IsEqualTo("Pause recording");
        await Assert.That(state.IsStartEnabled).IsFalse();
        await Assert.That(state.IsStopEnabled).IsFalse();
        await Assert.That(state.IsPauseResumeEnabled).IsFalse();
    }
}