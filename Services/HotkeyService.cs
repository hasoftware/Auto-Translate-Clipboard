using System;
using System.Runtime.InteropServices;

namespace AutoTranslate.Services;

/// <summary>Đăng ký hotkey toàn cục và bắt WM_HOTKEY qua subclass window proc.</summary>
public sealed class HotkeyService : IDisposable
{
    public const uint MOD_ALT = 0x0001;
    public const uint MOD_CONTROL = 0x0002;
    public const uint MOD_SHIFT = 0x0004;
    public const uint MOD_WIN = 0x0008;
    private const uint MOD_NOREPEAT = 0x4000;

    private const int WM_HOTKEY = 0x0312;
    private const int GWLP_WNDPROC = -4;
    private const int HOTKEY_ID = 0xA71;

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);
    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);
    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);
    [DllImport("user32.dll")]
    private static extern IntPtr CallWindowProc(IntPtr lpPrevWndFunc, IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    private delegate IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    private WndProc? _proc;       // giữ delegate sống, tránh bị GC
    private IntPtr _oldProc;
    private IntPtr _hwnd;
    private bool _registered;

    public event Action? Pressed;

    public void Attach(IntPtr hwnd)
    {
        _hwnd = hwnd;
        _proc = HookProc;
        _oldProc = SetWindowLongPtr(hwnd, GWLP_WNDPROC, Marshal.GetFunctionPointerForDelegate(_proc));
    }

    private IntPtr HookProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
    {
        if (msg == WM_HOTKEY && wParam.ToInt32() == HOTKEY_ID)
            Pressed?.Invoke();
        return CallWindowProc(_oldProc, hWnd, msg, wParam, lParam);
    }

    /// <summary>Đăng ký lại hotkey. Trả về true nếu thành công.</summary>
    public bool Register(uint modifiers, uint vk)
    {
        if (_hwnd == IntPtr.Zero) return false;
        if (_registered) UnregisterHotKey(_hwnd, HOTKEY_ID);
        _registered = RegisterHotKey(_hwnd, HOTKEY_ID, modifiers | MOD_NOREPEAT, vk);
        return _registered;
    }

    public void Dispose()
    {
        if (_registered && _hwnd != IntPtr.Zero)
            UnregisterHotKey(_hwnd, HOTKEY_ID);
        _registered = false;
    }
}
