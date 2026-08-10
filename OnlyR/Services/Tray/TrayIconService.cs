using OnlyR.Utils;
using OnlyR.ViewModel;
using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows;
using Forms = System.Windows.Forms;

namespace OnlyR.Services.Tray;

/// <summary>
/// Owns the Windows notification-area icon and connects its menu to existing commands.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class TrayIconService : ITrayIconService
{
    private const string ApplicationName = "OnlyR";

    private readonly RecordingPageViewModel _recordingPage;
    private Forms.NotifyIcon? _notifyIcon;
    private Forms.ContextMenuStrip? _contextMenu;
    private Forms.ToolStripMenuItem? _startMenuItem;
    private Forms.ToolStripMenuItem? _stopMenuItem;
    private Forms.ToolStripMenuItem? _pauseResumeMenuItem;
    private Forms.ToolStripMenuItem? _openRecordingsMenuItem;
    private Icon? _idleIcon;
    private Icon? _recordingIcon;
    private Icon? _pausedIcon;
    private bool _initialized;

    public TrayIconService(MainViewModel mainViewModel)
    {
        _recordingPage = mainViewModel.RecordingPage;
    }

    public void Initialize()
    {
        RunOnUiThread(InitializeCore);
    }

    public void RefreshState()
    {
        RunOnUiThread(RefreshStateCore);
    }

    public void ShowMainWindow()
    {
        RunOnUiThread(ShowMainWindowCore);
    }

    public void Dispose()
    {
        RunOnUiThread(DisposeCore);
        GC.SuppressFinalize(this);
    }

    private void InitializeCore()
    {
        if (_initialized)
        {
            return;
        }

        using var applicationIcon = LoadApplicationIcon();
        _idleIcon = CreateStatusIcon(applicationIcon, Color.DimGray);
        _recordingIcon = CreateStatusIcon(applicationIcon, Color.FromArgb(220, 32, 32));
        _pausedIcon = CreateStatusIcon(applicationIcon, Color.Gold);

        _contextMenu = CreateContextMenu();
        _contextMenu.Opening += ContextMenuOpening;

        _notifyIcon = new Forms.NotifyIcon
        {
            ContextMenuStrip = _contextMenu,
            Icon = _idleIcon,
            Text = ApplicationName,
            Visible = false
        };

        _notifyIcon.DoubleClick += NotifyIconDoubleClick;
        _recordingPage.PropertyChanged += RecordingPagePropertyChanged;

        RefreshStateCore();
        _notifyIcon.Visible = true;
        _initialized = true;
    }

    private Forms.ContextMenuStrip CreateContextMenu()
    {
        var menu = new Forms.ContextMenuStrip();

        menu.Items.Add(new Forms.ToolStripMenuItem(ApplicationName) { Enabled = false });
        menu.Items.Add(new Forms.ToolStripSeparator());

        _startMenuItem = new Forms.ToolStripMenuItem(Properties.Resources.START_RECORDING_TOOLTIP);
        _startMenuItem.Click += StartMenuItemClick;
        menu.Items.Add(_startMenuItem);

        _stopMenuItem = new Forms.ToolStripMenuItem(Properties.Resources.STOP_RECORDING_TOOLTIP);
        _stopMenuItem.Click += StopMenuItemClick;
        menu.Items.Add(_stopMenuItem);

        _pauseResumeMenuItem = new Forms.ToolStripMenuItem(Properties.Resources.PAUSE_RECORDING_TOOLTIP);
        _pauseResumeMenuItem.Click += PauseResumeMenuItemClick;
        menu.Items.Add(_pauseResumeMenuItem);

        menu.Items.Add(new Forms.ToolStripSeparator());

        var showWindowMenuItem = new Forms.ToolStripMenuItem("Show window");
        showWindowMenuItem.Click += ShowWindowMenuItemClick;
        menu.Items.Add(showWindowMenuItem);

        _openRecordingsMenuItem = new Forms.ToolStripMenuItem(Properties.Resources.RECORDINGS_FOLDER_TOOLTIP);
        _openRecordingsMenuItem.Click += OpenRecordingsMenuItemClick;
        menu.Items.Add(_openRecordingsMenuItem);

        menu.Items.Add(new Forms.ToolStripSeparator());

        var exitMenuItem = new Forms.ToolStripMenuItem("Exit");
        exitMenuItem.Click += ExitMenuItemClick;
        menu.Items.Add(exitMenuItem);

        return menu;
    }

    private void RecordingPagePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(RecordingPageViewModel.RecordingStatus)
            or nameof(RecordingPageViewModel.CanRecord)
            or nameof(RecordingPageViewModel.CanPause))
        {
            RefreshState();
        }
    }

    private void ContextMenuOpening(object? sender, CancelEventArgs e)
    {
        RefreshStateCore();
    }

    private void RefreshStateCore()
    {
        if (_notifyIcon == null
            || _startMenuItem == null
            || _stopMenuItem == null
            || _pauseResumeMenuItem == null
            || _openRecordingsMenuItem == null)
        {
            return;
        }

        var state = TrayPresentationState.Create(
            _recordingPage.RecordingStatus,
            _recordingPage.CanRecord,
            _recordingPage.CanPause);

        _notifyIcon.Icon = state.IconState switch
        {
            TrayIconState.Idle => _idleIcon,
            TrayIconState.Recording => _recordingIcon,
            TrayIconState.Paused => _pausedIcon,
            _ => _idleIcon
        };

        _notifyIcon.Text = $"{ApplicationName} - {state.ToolTipText}";
        _startMenuItem.Enabled = state.IsStartEnabled;
        _stopMenuItem.Enabled = state.IsStopEnabled;
        _pauseResumeMenuItem.Text = state.PauseResumeText;
        _pauseResumeMenuItem.Enabled = state.IsPauseResumeEnabled;
        _openRecordingsMenuItem.Enabled = !_recordingPage.NoFolder;
    }

    private void NotifyIconDoubleClick(object? sender, EventArgs e)
    {
        ShowMainWindow();
    }

    private void StartMenuItemClick(object? sender, EventArgs e)
    {
        RunOnUiThread(() => _recordingPage.StartRecordingCommand.Execute(null));
    }

    private void StopMenuItemClick(object? sender, EventArgs e)
    {
        RunOnUiThread(() => _recordingPage.StopRecordingCommand.Execute(null));
    }

    private void PauseResumeMenuItemClick(object? sender, EventArgs e)
    {
        RunOnUiThread(() => _recordingPage.PauseResumeRecordingCommand.Execute(null));
    }

    private void OpenRecordingsMenuItemClick(object? sender, EventArgs e)
    {
        RunOnUiThread(() => _recordingPage.ShowRecordingsCommand.Execute(null));
    }

    private void ShowWindowMenuItemClick(object? sender, EventArgs e)
    {
        ShowMainWindow();
    }

    private static void ExitMenuItemClick(object? sender, EventArgs e)
    {
        RunOnUiThread(() =>
        {
            var window = Application.Current?.MainWindow;
            if (window is OnlyR.MainWindow mainWindow)
            {
                mainWindow.RequestExit();
            }
            else
            {
                window?.Close();
            }
        });
    }

    private static void ShowMainWindowCore()
    {
        var window = Application.Current?.MainWindow;
        if (window == null)
        {
            return;
        }

        window.ShowInTaskbar = true;
        if (!window.IsVisible)
        {
            window.Show();
        }

        if (window.WindowState == WindowState.Minimized)
        {
            window.WindowState = WindowState.Normal;
        }

        _ = window.Activate();
        _ = window.Focus();
    }

    private static Icon LoadApplicationIcon()
    {
        var uri = new Uri("pack://application:,,,/iconmic.ico", UriKind.Absolute);
        var streamInfo = Application.GetResourceStream(uri)
            ?? throw new InvalidOperationException("Could not load the OnlyR application icon");

        using var stream = streamInfo.Stream;
        using var icon = new Icon(stream);
        return (Icon)icon.Clone();
    }

    private static Icon CreateStatusIcon(Icon applicationIcon, Color statusColor)
    {
        using var bitmap = applicationIcon.ToBitmap();
        using var graphics = Graphics.FromImage(bitmap);
        graphics.SmoothingMode = SmoothingMode.AntiAlias;

        var markerDiameter = Math.Max(6, bitmap.Width / 3);
        var x = bitmap.Width - markerDiameter - 1;
        var y = bitmap.Height - markerDiameter - 1;

        using var outlineBrush = new SolidBrush(Color.White);
        using var statusBrush = new SolidBrush(statusColor);
        graphics.FillEllipse(outlineBrush, x - 1, y - 1, markerDiameter + 2, markerDiameter + 2);
        graphics.FillEllipse(statusBrush, x, y, markerDiameter, markerDiameter);

        var iconHandle = bitmap.GetHicon();
        try
        {
            using var icon = Icon.FromHandle(iconHandle);
            return (Icon)icon.Clone();
        }
        finally
        {
            _ = NativeMethods.DestroyIcon(iconHandle);
        }
    }

    private static void RunOnUiThread(Action action)
    {
        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher == null || dispatcher.HasShutdownStarted || dispatcher.HasShutdownFinished)
        {
            return;
        }

        if (dispatcher.CheckAccess())
        {
            action();
        }
        else
        {
            _ = dispatcher.BeginInvoke(action);
        }
    }

    private void DisposeCore()
    {
        _recordingPage.PropertyChanged -= RecordingPagePropertyChanged;

        if (_contextMenu != null)
        {
            _contextMenu.Opening -= ContextMenuOpening;
        }

        if (_notifyIcon != null)
        {
            _notifyIcon.DoubleClick -= NotifyIconDoubleClick;
            _notifyIcon.Visible = false;
            _notifyIcon.Dispose();
            _notifyIcon = null;
        }

        _contextMenu?.Dispose();
        _contextMenu = null;
        _startMenuItem = null;
        _stopMenuItem = null;
        _pauseResumeMenuItem = null;
        _openRecordingsMenuItem = null;

        _idleIcon?.Dispose();
        _recordingIcon?.Dispose();
        _pausedIcon?.Dispose();
        _idleIcon = null;
        _recordingIcon = null;
        _pausedIcon = null;
        _initialized = false;
    }
}