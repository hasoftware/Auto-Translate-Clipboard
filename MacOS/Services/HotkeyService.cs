using System;
using SharpHook;
using SharpHook.Native;

namespace AutoTranslate.Services;

/// <summary>
/// Hotkey toàn cục qua SharpHook (libuiohook).
/// Trên macOS cần cấp quyền Accessibility cho app:
/// System Settings → Privacy &amp; Security → Accessibility.
/// </summary>
public sealed class HotkeyService : IDisposable
{
    private const ModifierMask CtrlMask = ModifierMask.LeftCtrl | ModifierMask.RightCtrl;
    private const ModifierMask ShiftMask = ModifierMask.LeftShift | ModifierMask.RightShift;
    private const ModifierMask AltMask = ModifierMask.LeftAlt | ModifierMask.RightAlt;
    private const ModifierMask MetaMask = ModifierMask.LeftMeta | ModifierMask.RightMeta;

    private TaskPoolGlobalHook? _hook;
    private HotkeySpec _spec = new();
    private KeyCode _keyCode = KeyCode.VcT;

    public event Action? Pressed;

    /// <summary>Đăng ký lại hotkey. Trả về true nếu phím chính hợp lệ.</summary>
    public bool Register(HotkeySpec spec)
    {
        if (!Enum.TryParse("Vc" + spec.Key, ignoreCase: true, out KeyCode code))
            return false;

        _spec = spec;
        _keyCode = code;

        if (_hook == null)
        {
            _hook = new TaskPoolGlobalHook();
            _hook.KeyPressed += OnKeyPressed;
            _hook.RunAsync();
        }
        return true;
    }

    private void OnKeyPressed(object? sender, KeyboardHookEventArgs e)
    {
        if (e.Data.KeyCode != _keyCode) return;

        var m = e.RawEvent.Mask;
        if (((m & CtrlMask) != 0) == _spec.Ctrl &&
            ((m & ShiftMask) != 0) == _spec.Shift &&
            ((m & AltMask) != 0) == _spec.Alt &&
            ((m & MetaMask) != 0) == _spec.Meta)
        {
            Pressed?.Invoke();
        }
    }

    public void Dispose()
    {
        _hook?.Dispose();
        _hook = null;
    }
}
