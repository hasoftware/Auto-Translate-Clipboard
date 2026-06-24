using System;
using System.Threading;
using Microsoft.UI.Xaml;

namespace AutoTranslate;

public partial class App : Application
{
    private const string MutexName = "AutoTranslate_SingleInstance_Mutex_v1";
    private const string ShowEventName = "AutoTranslate_Show_Event_v1";

    private static Mutex? _mutex;
    private static EventWaitHandle? _showEvent;
    private MainWindow? _mainWindow;

    public App()
    {
        this.InitializeComponent();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        _mutex = new Mutex(initiallyOwned: true, MutexName, out bool createdNew);
        if (!createdNew)
        {
            // Đã có bản đang chạy → ra hiệu cho nó hiện cửa sổ rồi thoát bản này
            try { EventWaitHandle.OpenExisting(ShowEventName).Set(); } catch { }
            Environment.Exit(0);
            return;
        }

        _mainWindow = new MainWindow();

        // Thread nền: khi bản thứ 2 ra hiệu thì hiện cửa sổ bản đang chạy
        _showEvent = new EventWaitHandle(false, EventResetMode.AutoReset, ShowEventName);
        var listener = new Thread(() =>
        {
            while (true)
            {
                _showEvent.WaitOne();
                _mainWindow?.DispatcherQueue.TryEnqueue(() => _mainWindow!.ShowFromTray());
            }
        })
        { IsBackground = true };
        listener.Start();

        // Khởi động cùng Windows (--tray) thì ẩn xuống tray, không bật cửa sổ
        var cmd = Environment.GetCommandLineArgs();
        bool trayStart = Array.Exists(cmd, a => string.Equals(a, "--tray", StringComparison.OrdinalIgnoreCase));
        if (!trayStart)
            _mainWindow.Activate();
    }
}
