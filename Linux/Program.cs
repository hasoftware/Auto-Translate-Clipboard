using System;
using System.IO;
using Avalonia;

namespace AutoTranslate;

internal static class Program
{
    // Giữ file lock suốt vòng đời app để chặn instance thứ hai
    private static FileStream? _instanceLock;

    internal static string DataDir { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "AutoTranslate");

    /// <summary>Instance thứ hai chạm vào file này để ra hiệu cho bản đang chạy hiện cửa sổ.</summary>
    internal static string ShowSignalPath => Path.Combine(DataDir, ".show-signal");

    [STAThread]
    public static void Main(string[] args)
    {
        if (!TryLockSingleInstance())
        {
            // Đã có bản đang chạy → ra hiệu cho nó hiện cửa sổ rồi thoát bản này
            try { File.WriteAllText(ShowSignalPath, DateTime.UtcNow.Ticks.ToString()); } catch { }
            return;
        }

        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    private static bool TryLockSingleInstance()
    {
        try
        {
            Directory.CreateDirectory(DataDir);
            _instanceLock = new FileStream(
                Path.Combine(DataDir, ".instance.lock"),
                FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
            return true;
        }
        catch (IOException)
        {
            return false;
        }
    }

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}
