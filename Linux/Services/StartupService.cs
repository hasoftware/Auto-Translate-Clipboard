using System;
using System.Diagnostics;
using System.IO;

namespace AutoTranslate.Services;

/// <summary>Bật/tắt khởi động cùng Linux qua file .desktop trong ~/.config/autostart.</summary>
public static class StartupService
{
    private static string DesktopPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "autostart", "autotranslate.desktop");

    private static string ExePath =>
        Process.GetCurrentProcess().MainModule?.FileName ?? "";

    public static bool IsEnabled() => File.Exists(DesktopPath);

    public static void Set(bool enabled)
    {
        try
        {
            if (enabled)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(DesktopPath)!);
                File.WriteAllText(DesktopPath, $"""
                    [Desktop Entry]
                    Type=Application
                    Name=Auto Translate Clipboard
                    Comment=Hotkey translation tool
                    Exec="{ExePath}" --tray
                    Terminal=false
                    X-GNOME-Autostart-enabled=true
                    """);
            }
            else if (File.Exists(DesktopPath))
            {
                File.Delete(DesktopPath);
            }
        }
        catch { /* bỏ qua lỗi quyền */ }
    }
}
