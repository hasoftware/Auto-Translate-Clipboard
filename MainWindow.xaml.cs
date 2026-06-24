using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using AutoTranslate.Services;
using H.NotifyIcon;
using Microsoft.UI.Input;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Windows.ApplicationModel.DataTransfer;
using Windows.Graphics;
using Windows.System;
using Windows.UI.Core;

namespace AutoTranslate;

public sealed partial class MainWindow : Window
{
    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);
    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    private readonly Settings _settings;
    private readonly HotkeyService _hotkey = new();
    private TaskbarIcon? _tray;
    private IntPtr _hwnd;
    private bool _loaded;
    private bool _allowClose;

    public MainWindow()
    {
        this.InitializeComponent();
        _settings = Settings.Load();
        _hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);

        // Giao diện Fluent
        this.SystemBackdrop = new MicaBackdrop();
        this.ExtendsContentIntoTitleBar = true;
        this.SetTitleBar(AppTitleBar);
        this.Title = "Auto Translate Clipboard";
        ApplyTheme(_settings.Theme);

        var iconPath = Path.Combine(AppContext.BaseDirectory, "Assets", "hasoftware.ico");
        if (File.Exists(iconPath))
            this.AppWindow.SetIcon(iconPath);

        if (this.AppWindow.Presenter is OverlappedPresenter p)
        {
            p.IsResizable = false;
            p.IsMaximizable = false;
            p.IsMinimizable = true;
        }
        this.AppWindow.Resize(new SizeInt32(480, 360));
        ContentPanel.SizeChanged += (s, e) => SizeToContent();

        // Ngôn ngữ (theo danh sách hiển thị người dùng chọn)
        PopulateLanguages();

        // Hotkey toàn cục
        _hotkey.Attach(_hwnd);
        _hotkey.Pressed += () => DispatcherQueue.TryEnqueue(ToggleWindow);
        _hotkey.Register(_settings.HotkeyModifiers, _settings.HotkeyVk);

        // Tray + ẩn xuống tray khi đóng
        SetupTray(iconPath);
        this.AppWindow.Closing += OnAppWindowClosing;

        _loaded = true;
        InputBox.Focus(FocusState.Programmatic);

        // Làm nóng kết nối (DNS + TLS) để lần dịch đầu nhanh hơn
        _ = TranslationService.WarmUpAsync();
    }

    // ---------- Tray ----------
    private void SetupTray(string iconPath)
    {
        var menu = new MenuFlyout();

        var open = new MenuFlyoutItem { Text = "Open" };
        open.Click += (s, e) => ShowFromTray();

        var startup = new ToggleMenuFlyoutItem { Text = "Run at startup", IsChecked = StartupService.IsEnabled() };
        startup.Click += (s, e) =>
        {
            StartupService.Set(startup.IsChecked);
            _settings.RunAtStartup = startup.IsChecked;
            _settings.Save();
        };

        var exit = new MenuFlyoutItem { Text = "Exit" };
        exit.Click += (s, e) => ExitApp();

        menu.Items.Add(open);
        menu.Items.Add(startup);
        menu.Items.Add(new MenuFlyoutSeparator());
        menu.Items.Add(exit);

        _tray = new TaskbarIcon
        {
            ToolTipText = "Auto Translate Clipboard",
            ContextMenuMode = ContextMenuMode.SecondWindow,
            ContextFlyout = menu,
            LeftClickCommand = new RelayCommand(ShowFromTray),
        };
        if (File.Exists(iconPath))
            _tray.Icon = new System.Drawing.Icon(iconPath);
        _tray.ForceCreate();
    }

    private void OnAppWindowClosing(AppWindow sender, AppWindowClosingEventArgs args)
    {
        if (_allowClose) return;
        args.Cancel = true;   // không thoát, chỉ ẩn xuống tray
        HideToTray();
    }

    private void HideToTray() => this.AppWindow.Hide();

    /// <summary>Hotkey bật/tắt: đang hiện & ở trên cùng → ẩn; ngược lại → hiện.</summary>
    private void ToggleWindow()
    {
        if (this.AppWindow.IsVisible && GetForegroundWindow() == _hwnd)
            HideToTray();
        else
            ShowFromTray();
    }

    private void OnEscape(Microsoft.UI.Xaml.Input.KeyboardAccelerator sender,
                          Microsoft.UI.Xaml.Input.KeyboardAcceleratorInvokedEventArgs args)
    {
        HideToTray();
        args.Handled = true;
    }

    /// <summary>Co cửa sổ vừa khít nội dung (40px title bar + chiều cao nội dung).</summary>
    private void SizeToContent()
    {
        if (ContentPanel.ActualHeight < 50) return;
        var scale = (this.Content as FrameworkElement)?.XamlRoot?.RasterizationScale ?? 1.0;
        int w = (int)Math.Round(480 * scale);
        int h = (int)Math.Round((40 + ContentPanel.ActualHeight + 8) * scale);
        if (this.AppWindow.Size.Width != w || this.AppWindow.Size.Height != h)
            this.AppWindow.Resize(new SizeInt32(w, h));
    }

    internal void ShowFromTray()
    {
        this.AppWindow.Show();
        this.Activate();
        SetForegroundWindow(_hwnd);
        InputBox.Focus(FocusState.Programmatic);
        InputBox.SelectAll();
    }

    // ---------- About ----------
    private async void OnOpenAbout(object sender, RoutedEventArgs e)
    {
        var scale = (this.Content as FrameworkElement)?.XamlRoot?.RasterizationScale ?? 1.0;
        this.AppWindow.Resize(new SizeInt32((int)(480 * scale), (int)(520 * scale)));

        var dialog = new AboutDialog { XamlRoot = this.Content.XamlRoot };
        await dialog.ShowAsync();

        SizeToContent();
    }

    // ---------- Settings ----------
    private async void OnOpenSettings(object sender, RoutedEventArgs e)
    {
        // Phóng to tạm để hộp Settings đủ chỗ (cửa sổ chính rất nhỏ)
        var scale = (this.Content as FrameworkElement)?.XamlRoot?.RasterizationScale ?? 1.0;
        this.AppWindow.Resize(new SizeInt32((int)(480 * scale), (int)(660 * scale)));

        var dialog = new SettingsDialog(_settings) { XamlRoot = this.Content.XamlRoot };
        var result = await dialog.ShowAsync();

        if (result == ContentDialogResult.Primary)
        {
            _settings.Theme = dialog.SelectedTheme;
            ApplyTheme(_settings.Theme);

            _settings.HotkeyModifiers = dialog.HotkeyModifiers;
            _settings.HotkeyVk = dialog.HotkeyVk;
            _settings.HotkeyDisplay = dialog.HotkeyDisplay;
            _hotkey.Register(_settings.HotkeyModifiers, _settings.HotkeyVk);

            StartupService.Set(dialog.RunAtStartup);
            _settings.RunAtStartup = dialog.RunAtStartup;

            _settings.VisibleLanguages = dialog.VisibleLanguages;
            PopulateLanguages();

            _settings.Save();
        }

        SizeToContent();  // co cửa sổ lại khít nội dung
    }

    private void ExitApp()
    {
        _allowClose = true;
        _hotkey.Dispose();
        _tray?.Dispose();
        Application.Current.Exit();
    }

    // ---------- Theme ----------
    private void ApplyTheme(string theme)
    {
        if (this.Content is FrameworkElement root)
        {
            root.RequestedTheme = theme switch
            {
                "Light" => ElementTheme.Light,
                "Dark" => ElementTheme.Dark,
                _ => ElementTheme.Default,
            };
        }
    }

    // ---------- Ngôn ngữ ----------
    private void PopulateLanguages()
    {
        var visible = (_settings.VisibleLanguages != null && _settings.VisibleLanguages.Count > 0)
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

    private void OnSourceChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_loaded || SourceCombo.SelectedItem is not string s) return;
        _settings.SourceLanguage = s;
        _settings.Save();
    }

    private void OnTargetChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_loaded || TargetCombo.SelectedItem is not string t) return;
        _settings.TargetLanguage = t;
        _settings.Save();
    }

    private void OnSwap(object sender, RoutedEventArgs e)
    {
        var s = SourceCombo.SelectedItem;
        SourceCombo.SelectedItem = TargetCombo.SelectedItem;
        TargetCombo.SelectedItem = s;
    }

    // ---------- Dịch ----------
    private void OnTranslateClick(object sender, RoutedEventArgs e) => StartTranslate();

    private void OnInputKeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == VirtualKey.Enter)
        {
            bool shift = InputKeyboardSource
                .GetKeyStateForCurrentThread(VirtualKey.Shift)
                .HasFlag(CoreVirtualKeyStates.Down);

            if (!shift)
            {
                e.Handled = true;
                StartTranslate();
            }
        }
    }

    /// <summary>Ẩn cửa sổ NGAY rồi dịch ở chế độ nền; xong thì copy + thông báo.</summary>
    private void StartTranslate()
    {
        var text = InputBox.Text;
        if (string.IsNullOrWhiteSpace(text))
            return;

        var sl = Languages.CodeOf((string)SourceCombo.SelectedItem);
        var tl = Languages.CodeOf((string)TargetCombo.SelectedItem);

        // Ẩn ngay để không gây khó chịu — việc dịch chạy nền
        InputBox.Text = "";
        HideToTray();

        _ = TranslateAndNotifyAsync(sl, tl, text);
    }

    private async Task TranslateAndNotifyAsync(string sl, string tl, string text)
    {
        try
        {
            var res = await TranslationService.TranslateAsync(sl, tl, text);

            var dp = new DataPackage();
            dp.SetText(res.Text);
            Clipboard.SetContent(dp);

            try { _tray?.ShowNotification(title: "Đã dịch & copy — Ctrl+V để dán", message: res.Text); } catch { }
        }
        catch (Exception)
        {
            try { _tray?.ShowNotification(title: "Dịch thất bại", message: "Kiểm tra kết nối mạng rồi thử lại."); } catch { }
        }
    }
}
