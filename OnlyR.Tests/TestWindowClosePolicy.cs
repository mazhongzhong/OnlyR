using OnlyR.Services.Tray;
using System.Threading.Tasks;

namespace OnlyR.Tests;

public sealed class TestWindowClosePolicy
{
    [Test]
    public async Task CloseButtonHidesWhenCloseToTrayIsEnabled()
    {
        var action = WindowClosePolicy.GetAction(closeToTray: true, exitRequested: false);

        await Assert.That(action).IsEqualTo(WindowCloseAction.HideToTray);
    }

    [Test]
    public async Task TrayExitClosesWhenCloseToTrayIsEnabled()
    {
        var action = WindowClosePolicy.GetAction(closeToTray: true, exitRequested: true);

        await Assert.That(action).IsEqualTo(WindowCloseAction.CloseApplication);
    }

    [Test]
    public async Task CloseButtonClosesWhenCloseToTrayIsDisabled()
    {
        var action = WindowClosePolicy.GetAction(closeToTray: false, exitRequested: false);

        await Assert.That(action).IsEqualTo(WindowCloseAction.CloseApplication);
    }
}