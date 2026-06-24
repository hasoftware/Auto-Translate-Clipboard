using System;
using System.IO;
using System.Text.Json;

namespace AutoTranslate.Services;

/// <summary>Cài đặt người dùng, lưu JSON trong %LocalAppData%\AutoTranslate\settings.json.</summary>
public sealed class Settings
{
    public string SourceLanguage { get; set; } = "Vietnamese";
    public string TargetLanguage { get; set; } = "English";
    public bool RunAtStartup { get; set; } = false;
    // "System" | "Light" | "Dark"
    public string Theme { get; set; } = "System";

    // Hotkey toàn cục (mặc định Ctrl+Shift+T). 0x2=Ctrl, 0x4=Shift, 0x54='T'
    public uint HotkeyModifiers { get; set; } = 0x0002 | 0x0004;
    public uint HotkeyVk { get; set; } = 0x54;
    public string HotkeyDisplay { get; set; } = "Ctrl + Shift + T";

    // Ngôn ngữ hiển thị trong dropdown. Rỗng = hiện tất cả.
    public System.Collections.Generic.List<string> VisibleLanguages { get; set; } = new();

    private static readonly string Dir =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AutoTranslate");
    private static readonly string FilePath = Path.Combine(Dir, "settings.json");

    public static Settings Load()
    {
        try
        {
            if (File.Exists(FilePath))
            {
                var json = File.ReadAllText(FilePath);
                var s = JsonSerializer.Deserialize<Settings>(json);
                if (s != null) return s;
            }
        }
        catch { /* dùng mặc định nếu lỗi */ }
        return new Settings();
    }

    public void Save()
    {
        try
        {
            Directory.CreateDirectory(Dir);
            var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(FilePath, json);
        }
        catch { /* bỏ qua lỗi ghi */ }
    }
}
