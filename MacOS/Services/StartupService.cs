using System;
using System.Diagnostics;
using System.IO;

namespace AutoTranslate.Services;

/// <summary>Bật/tắt khởi động cùng macOS qua LaunchAgents (~/Library/LaunchAgents).</summary>
public static class StartupService
{
    private const string Label = "com.hasoftware.autotranslate";

    private static string PlistPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        "Library", "LaunchAgents", Label + ".plist");

    private static string ExePath =>
        Process.GetCurrentProcess().MainModule?.FileName ?? "";

    public static bool IsEnabled() => File.Exists(PlistPath);

    public static void Set(bool enabled)
    {
        try
        {
            if (enabled)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(PlistPath)!);
                File.WriteAllText(PlistPath, $"""
                    <?xml version="1.0" encoding="UTF-8"?>
                    <!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
                    <plist version="1.0">
                    <dict>
                        <key>Label</key>
                        <string>{Label}</string>
                        <key>ProgramArguments</key>
                        <array>
                            <string>{ExePath}</string>
                            <string>--tray</string>
                        </array>
                        <key>RunAtLoad</key>
                        <true/>
                    </dict>
                    </plist>
                    """);
            }
            else if (File.Exists(PlistPath))
            {
                File.Delete(PlistPath);
            }
        }
        catch { /* bỏ qua lỗi quyền */ }
    }
}
