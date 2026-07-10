using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Styling;
using Avalonia.Threading;
using AutoTranslate.Services;

namespace AutoTranslate;

public partial class MainWindow : Window
{
    private readonly Settings _settings;
    private readonly HotkeyService _hotkey = new();
    private bool _loaded;
    private bool _allowClose;

    public MainWindow()
    {
        InitializeComponent();
        _settings = Settings.Load();
        ApplyTheme(_settings.Theme);
        PopulateLanguages();

        // Hotkey toàn cục (SharpHook chạy thread riêng → về UI thread)
        _hotkey.Pressed += () => Dispatcher.UIThread.Post(ToggleWindow);
        _hotkey.Register(_settings.Hotkey);

        // Enter dịch, Shift+Enter xuống dòng (tunnel để chặn trước khi TextBox chèn newline)
        InputBox.AddHandler(KeyDownEvent, OnInputKeyDown, RoutingStrategies.Tunnel);

        KeyDown += (_, e) =>
        {
            if (e.Key == Key.Escape) { HideToTray(); e.Handled = true; }
        };

        // Đóng cửa sổ = ẩn xuống tray, không thoát
        Closing += (_, e) =>
        {
            if (!_allowClose) { e.Cancel = true; HideToTray(); }
        };

        Opened += (_, _) => InputBox.Focus();

        _loaded = true;
        _ = TranslationService.WarmUpAsync();
    }

    // ---------- Hiện / ẩn ----------
    internal void ShowFromTray()
    {
        Show();
        Activate();
        InputBox.Focus();
        InputBox.SelectAll();
    }

    private void HideToTray() => Hide();

    private void ToggleWindow()
    {
        if (IsVisible && IsActive)
            HideToTray();
        else
            ShowFromTray();
    }

    internal void PrepareExit()
    {
        _allowClose = true;
        _hotkey.Dispose();
    }

    // ---------- Theme ----------
    private static void ApplyTheme(string theme)
    {
        if (Application.Current is null) return;
        Application.Current.RequestedThemeVariant = theme switch
        {
            "Light" => ThemeVariant.Light,
            "Dark" => ThemeVariant.Dark,
            _ => ThemeVariant.Default,
        };
    }

    // ---------- Ngôn ngữ ----------
    private void PopulateLanguages()
    {
        var visible = (_settings.VisibleLanguages is { Count: > 0 })
            ? Languages.Names.Where(n => _settings.VisibleLanguages.Contains(n)).ToList()
            : new List<string>(Languages.Names);
        if (visible.Count == 0)
            visible = new List<string>(Languages.Names);

        var prevSource = SourceCombo.SelectedItem as string ?? _settings.SourceLanguage;
        var prevTarget = TargetCombo.SelectedItem as string ?? _settings.TargetLanguage;

        SourceCombo.ItemsSource = new List<string>(visible);
        TargetCombo.ItemsSource = new List<string>(visible);

        SourceCombo.SelectedItem = visible.Contains(prevSource) ? prevSource : visible.First();
        TargetCombo.SelectedItem = visible.Contains(prevTarget) ? prevTarget
            : (visible.Count > 1 ? visible[1] : visible.First());
    }

    private void OnSourceChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (!_loaded || SourceCombo.SelectedItem is not string s) return;
        _settings.SourceLanguage = s;
        _settings.Save();
    }

    private void OnTargetChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (!_loaded || TargetCombo.SelectedItem is not string t) return;
        _settings.TargetLanguage = t;
        _settings.Save();
    }

    private void OnSwap(object? sender, RoutedEventArgs e)
    {
        var s = SourceCombo.SelectedItem;
        SourceCombo.SelectedItem = TargetCombo.SelectedItem;
        TargetCombo.SelectedItem = s;
    }

    // ---------- Dịch ----------
    private void OnTranslateClick(object? sender, RoutedEventArgs e) => StartTranslate();

    private void OnInputKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && !e.KeyModifiers.HasFlag(KeyModifiers.Shift))
        {
            e.Handled = true;
            StartTranslate();
        }
    }

    /// <summary>Ẩn cửa sổ ngay rồi dịch ở chế độ nền; xong thì copy vào clipboard.</summary>
    private void StartTranslate()
    {
        var text = InputBox.Text;
        if (string.IsNullOrWhiteSpace(text))
            return;

        var sl = Languages.CodeOf((string)SourceCombo.SelectedItem!);
        var tl = Languages.CodeOf((string)TargetCombo.SelectedItem!);

        InputBox.Text = "";
        HideToTray();

        _ = TranslateToClipboardAsync(sl, tl, text);
    }

    private async Task TranslateToClipboardAsync(string sl, string tl, string text)
    {
        try
        {
            var res = await TranslationService.TranslateAsync(sl, tl, text);
            await Dispatcher.UIThread.InvokeAsync(async () =>
            {
                if (Clipboard is { } cb)
                    await cb.SetTextAsync(res.Text);
            });
        }
        catch
        {
            // Mạng lỗi: bỏ qua, người dùng nhấn hotkey dịch lại
        }
    }

    // ---------- About / Settings ----------
    private async void OnOpenAbout(object? sender, RoutedEventArgs e)
    {
        var dlg = new AboutWindow();
        await dlg.ShowDialog(this);
    }

    private async void OnOpenSettings(object? sender, RoutedEventArgs e)
    {
        var dlg = new SettingsWindow(_settings);
        var saved = await dlg.ShowDialog<bool>(this);
        if (!saved) return;

        _settings.Theme = dlg.SelectedTheme;
        ApplyTheme(_settings.Theme);

        _settings.Hotkey = dlg.SelectedHotkey;
        _hotkey.Register(_settings.Hotkey);

        StartupService.Set(dlg.RunAtStartup);
        _settings.RunAtStartup = dlg.RunAtStartup;

        _settings.VisibleLanguages = dlg.VisibleLanguages;
        PopulateLanguages();

        _settings.Save();
    }
}
