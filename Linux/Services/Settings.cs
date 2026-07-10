using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace AutoTranslate.Services;

/// <summary>Hotkey: các modifier + phím chính (tên phím kiểu "T", "1", "F5").</summary>
public sealed class HotkeySpec
{
    public bool Ctrl { get; set; } = true;
    public bool Shift { get; set; } = true;
    public bool Alt { get; set; }
    public bool Meta { get; set; }
    public string Key { get; set; } = "T";

    public string Display
    {
        get
        {
            var parts = new List<string>();
            if (Ctrl) parts.Add("Ctrl");
            if (Shift) parts.Add("Shift");
            if (Alt) parts.Add("Alt");
            if (Meta) parts.Add(OperatingSystem.IsMacOS() ? "Cmd" : "Meta");
            parts.Add(Key);
            return string.Join(" + ", parts);
        }
    }
}

/// <summary>Cài đặt người dùng, lưu JSON trong thư mục dữ liệu ứng dụng của HĐH.</summary>
public sealed class Settings
{
    public string SourceLanguage { get; set; } = "Vietnamese";
    public string TargetLanguage { get; set; } = "English";
    public bool RunAtStartup { get; set; }
    // "System" | "Light" | "Dark"
    public string Theme { get; set; } = "System";
    public HotkeySpec Hotkey { get; set; } = new();

    // Ngôn ngữ hiển thị trong dropdown. Rỗng = hiện tất cả.
    public List<string> VisibleLanguages { get; set; } = new();

    private static readonly string Dir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "AutoTranslate");
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
