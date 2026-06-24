using System;
using System.Diagnostics;
using Microsoft.Win32;

namespace AutoTranslate.Services;

/// <summary>Bật/tắt chạy cùng Windows qua HKCU\...\Run.</summary>
public static class StartupService
{
    private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "AutoTranslate";

    private static string ExePath =>
        Process.GetCurrentProcess().MainModule?.FileName ?? "";

    public static bool IsEnabled()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKey);
        return key?.GetValue(ValueName) is string;
    }

    public static void Set(bool enabled)
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKey, writable: true)
                            ?? Registry.CurrentUser.CreateSubKey(RunKey);
            if (key == null) return;
            if (enabled)
                key.SetValue(ValueName, $"\"{ExePath}\" --tray");   // khởi động ẩn xuống tray
            else
                key.DeleteValue(ValueName, throwOnMissingValue: false);
        }
        catch { /* bỏ qua lỗi quyền/registry */ }
    }
}
