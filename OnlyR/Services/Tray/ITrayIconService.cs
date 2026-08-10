using System;

namespace OnlyR.Services.Tray;

public interface ITrayIconService : IDisposable
{
    void Initialize();

    void RefreshState();

    void ShowMainWindow();
}