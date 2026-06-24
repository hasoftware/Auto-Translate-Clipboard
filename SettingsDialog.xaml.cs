using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using AutoTranslate.Services;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Windows.System;
using Windows.UI.Core;

namespace AutoTranslate;

/// <summary>Một ngôn ngữ trong danh sách chọn hiển thị (có checkbox).</summary>
public sealed class LangItem : INotifyPropertyChanged
{
    public string Name { get; init; } = "";
    private bool _checked;
    public bool Checked
    {
        get => _checked;
        set { if (_checked != value) { _checked = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Checked))); } }
    }
    public event PropertyChangedEventHandler? PropertyChanged;
}

public sealed partial class SettingsDialog : ContentDialog
{
    private bool _capturing;
    private readonly List<LangItem> _items;
    private readonly int _themeIndex;

    public string SelectedTheme =>
        (ThemeCombo.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "System";
    public uint HotkeyModifiers { get; private set; }
    public uint HotkeyVk { get; private set; }
    public string HotkeyDisplay { get; private set; } = "";
    public bool RunAtStartup => StartupSwitch.IsOn;
    public List<string> VisibleLanguages => _items.Where(i => i.Checked).Select(i => i.Name).ToList();

    public SettingsDialog(Settings current)
    {
        this.InitializeComponent();

        // Theme (set lại lúc Opened cho chắc hiển thị)
        _themeIndex = current.Theme switch { "Light" => 1, "Dark" => 2, _ => 0 };
        ThemeCombo.SelectedIndex = _themeIndex;

        // Hotkey hiện tại
        HotkeyModifiers = current.HotkeyModifiers;
        HotkeyVk = current.HotkeyVk;
        HotkeyDisplay = current.HotkeyDisplay;
        HotkeyBtn.Content = HotkeyDisplay;

        StartupSwitch.IsOn = StartupService.IsEnabled();

        // Danh sách ngôn ngữ (rỗng = tích tất cả)
        bool all = current.VisibleLanguages == null || current.VisibleLanguages.Count == 0;
        _items = Languages.Names
            .Select(n => new LangItem { Name = n, Checked = all || current.VisibleLanguages!.Contains(n) })
            .ToList();
        LangList.ItemsSource = _items;

        this.KeyDown += OnKeyDown;
        this.Opened += OnDialogOpened;
    }

    private void OnDialogOpened(ContentDialog sender, ContentDialogOpenedEventArgs args)
    {
        ThemeCombo.SelectedIndex = _themeIndex;          // đảm bảo hiển thị giá trị Theme
        SettingsScroll.ChangeView(null, 0, null, true);  // cuộn về đầu (hiện Theme)
    }

    private void OnSelectAllLangs(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    { foreach (var i in _items) i.Checked = true; }

    private void OnClearLangs(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    { foreach (var i in _items) i.Checked = false; }

    private void OnCaptureHotkey(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        _capturing = true;
        HotkeyBtn.Content = "Press a key combination…";
    }

    private void OnKeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (!_capturing) return;

        var key = e.Key;

        if (key == VirtualKey.Escape)
        {
            _capturing = false;
            HotkeyBtn.Content = HotkeyDisplay;
            e.Handled = true;
            return;
        }

        if (key is VirtualKey.Control or VirtualKey.Shift or VirtualKey.Menu
            or VirtualKey.LeftWindows or VirtualKey.RightWindows
            or VirtualKey.LeftControl or VirtualKey.RightControl
            or VirtualKey.LeftShift or VirtualKey.RightShift)
            return;

        uint mods = 0;
        if (IsDown(VirtualKey.Control)) mods |= 0x0002;
        if (IsDown(VirtualKey.Shift)) mods |= 0x0004;
        if (IsDown(VirtualKey.Menu)) mods |= 0x0001;   // Alt
        if (IsDown(VirtualKey.LeftWindows) || IsDown(VirtualKey.RightWindows)) mods |= 0x0008;

        HotkeyModifiers = mods;
        HotkeyVk = (uint)key;
        HotkeyDisplay = BuildDisplay(mods, key);
        HotkeyBtn.Content = HotkeyDisplay;
        _capturing = false;
        e.Handled = true;
    }

    private static bool IsDown(VirtualKey vk) =>
        InputKeyboardSource.GetKeyStateForCurrentThread(vk).HasFlag(CoreVirtualKeyStates.Down);

    private static string BuildDisplay(uint mods, VirtualKey key)
    {
        var parts = new List<string>();
        if ((mods & 0x0002) != 0) parts.Add("Ctrl");
        if ((mods & 0x0004) != 0) parts.Add("Shift");
        if ((mods & 0x0001) != 0) parts.Add("Alt");
        if ((mods & 0x0008) != 0) parts.Add("Win");
        parts.Add(KeyName(key));
        return string.Join(" + ", parts);
    }

    private static string KeyName(VirtualKey key)
    {
        uint vk = (uint)key;
        if (vk >= 0x30 && vk <= 0x5A) return ((char)vk).ToString();
        return key.ToString();
    }
}
