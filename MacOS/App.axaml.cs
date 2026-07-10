using System;
using System.IO;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;

namespace AutoTranslate;

public partial class App : Application
{
    private MainWindow? _mainWindow;
    private TrayIcon? _tray;
    private FileSystemWatcher? _showSignalWatcher;

    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // App sống ở tray; đóng cửa sổ không thoát app
            desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;

            _mainWindow = new MainWindow();
            SetupTray(desktop);
            WatchShowSignal();

            // Khởi động cùng hệ điều hành (--tray) thì ẩn, không bật cửa sổ
            bool trayStart = desktop.Args?.Any(a =>
                string.Equals(a, "--tray", StringComparison.OrdinalIgnoreCase)) == true;
            if (!trayStart)
                _mainWindow.Show();
        }

        base.OnFrameworkInitializationCompleted();
    }

    /// <summary>Instance thứ hai chạm file signal → bản đang chạy hiện cửa sổ.</summary>
    private void WatchShowSignal()
    {
        try
        {
            _showSignalWatcher = new FileSystemWatcher(Program.DataDir, Path.GetFileName(Program.ShowSignalPath))
            {
                EnableRaisingEvents = true,
            };
            FileSystemEventHandler onSignal = (_, _) =>
                Dispatcher.UIThread.Post(() => _mainWindow?.ShowFromTray());
            _showSignalWatcher.Created += onSignal;
            _showSignalWatcher.Changed += onSignal;
        }
        catch { /* không có watcher thì mất tính năng gọi lại, không chết app */ }
    }

    private void SetupTray(IClassicDesktopStyleApplicationLifetime desktop)
    {
        var menu = new NativeMenu();

        var open = new NativeMenuItem("Open");
        open.Click += (_, _) => _mainWindow?.ShowFromTray();

        var exit = new NativeMenuItem("Exit");
        exit.Click += (_, _) =>
        {
            _mainWindow?.PrepareExit();
            desktop.Shutdown();
        };

        menu.Items.Add(open);
        menu.Items.Add(new NativeMenuItemSeparator());
        menu.Items.Add(exit);

        _tray = new TrayIcon
        {
            ToolTipText = "Auto Translate Clipboard",
            Icon = new WindowIcon(new Bitmap(AssetLoader.Open(
                new Uri("avares://AutoTranslate/Assets/logo.png")))),
            Menu = menu,
        };
        _tray.Clicked += (_, _) => _mainWindow?.ShowFromTray();

        TrayIcon.SetIcons(this, new TrayIcons { _tray });
    }
}
