using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using AutoTranslate.Services;

namespace AutoTranslate;

public sealed class LangItem : INotifyPropertyChanged
{
    private bool _checked;
    public string Name { get; init; } = "";
    public bool Checked
    {
        get => _checked;
        set { if (_checked != value) { _checked = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Checked))); } }
    }
    public event PropertyChangedEventHandler? PropertyChanged;
}

public partial class SettingsWindow : Window
{
    private readonly List<LangItem> _langs;
    private bool _capturing;
    private HotkeySpec _spec;

    public string SelectedTheme { get; private set; } = "System";
    public HotkeySpec SelectedHotkey => _spec;
    public bool RunAtStartup => StartupSwitch.IsChecked == true;

    /// <summary>Rỗng = hiện tất cả ngôn ngữ.</summary>
    public List<string> VisibleLanguages =>
        _langs.All(l => l.Checked) ? new List<string>()
                                   : _langs.Where(l => l.Checked).Select(l => l.Name).ToList();

    public SettingsWindow() : this(new Settings()) { }

    public SettingsWindow(Settings settings)
    {
        InitializeComponent();

        _spec = settings.Hotkey;
        HotkeyBtn.Content = _spec.Display;

        ThemeCombo.SelectedIndex = settings.Theme switch
        {
            "Light" => 1,
            "Dark" => 2,
            _ => 0,
        };

        StartupSwitch.IsChecked = StartupService.IsEnabled();

        bool showAll = settings.VisibleLanguages is not { Count: > 0 };
        _langs = Languages.Names
            .Select(n => new LangItem { Name = n, Checked = showAll || settings.VisibleLanguages.Contains(n) })
            .ToList();
        LangList.ItemsSource = _langs;
    }

    // ---------- Hotkey capture ----------
    private void OnCaptureHotkey(object? sender, RoutedEventArgs e)
    {
        _capturing = true;
        HotkeyBtn.Content = "Press a key combination…";
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (_capturing)
        {
            var key = KeyToName(e.Key);
            if (key != null)
            {
                _spec = new HotkeySpec
                {
                    Ctrl = e.KeyModifiers.HasFlag(KeyModifiers.Control),
                    Shift = e.KeyModifiers.HasFlag(KeyModifiers.Shift),
                    Alt = e.KeyModifiers.HasFlag(KeyModifiers.Alt),
                    Meta = e.KeyModifiers.HasFlag(KeyModifiers.Meta),
                    Key = key,
                };
                HotkeyBtn.Content = _spec.Display;
                _capturing = false;
            }
            e.Handled = true;
            return;
        }
        base.OnKeyDown(e);
    }

    /// <summary>Chỉ nhận chữ cái, chữ số và F1–F12 làm phím chính.</summary>
    private static string? KeyToName(Key k) => k switch
    {
        >= Key.A and <= Key.Z => k.ToString(),
        >= Key.D0 and <= Key.D9 => k.ToString()[1..],
        >= Key.F1 and <= Key.F12 => k.ToString(),
        _ => null,
    };

    // ---------- Ngôn ngữ ----------
    private void OnSelectAllLangs(object? sender, RoutedEventArgs e)
    {
        foreach (var l in _langs) l.Checked = true;
    }

    private void OnClearLangs(object? sender, RoutedEventArgs e)
    {
        foreach (var l in _langs) l.Checked = false;
    }

    // ---------- Save / Cancel ----------
    private void OnSave(object? sender, RoutedEventArgs e)
    {
        SelectedTheme = ThemeCombo.SelectedIndex switch
        {
            1 => "Light",
            2 => "Dark",
            _ => "System",
        };
        Close(true);
    }

    private void OnCancel(object? sender, RoutedEventArgs e) => Close(false);
}
