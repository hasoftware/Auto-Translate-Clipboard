# Auto Translate Clipboard for Linux (preview)

Avalonia port of Auto Translate Clipboard for Linux. Same soul as the Windows
version: press a global hotkey anywhere, type, press Enter, and the translation
is copied to your clipboard.

**Status: preview.** Builds cleanly, but needs testing across distros and
desktop environments. Feedback and testing reports are very welcome.

## Build

Requires the .NET 8 SDK.

```
dotnet build AutoTranslate.Linux.csproj
```

Self-contained release build:

```
dotnet publish AutoTranslate.Linux.csproj -c Release -r linux-x64 --self-contained true -o dist/linux
```

## Platform notes

- **X11**: the global hotkey works out of the box (via libuiohook/SharpHook).
- **Wayland**: most compositors block global keyboard hooks. Workaround: bind
  a custom shortcut in your desktop settings (GNOME/KDE) that runs the
  `AutoTranslate` executable — the already running instance will pop up its
  window (second-launch signalling is built in).
- Run at startup is implemented via a `.desktop` file in
  `~/.config/autostart/autotranslate.desktop`.
- The tray icon uses the StatusNotifier protocol. On vanilla GNOME you may
  need the AppIndicator extension to see it.

## Not done yet

- AppImage / `.deb` / Flatpak packaging (currently a bare executable)
- Native Wayland global-shortcut support via the desktop portal
- Toast notification after translating (the translation still lands on the
  clipboard; there is just no visual confirmation yet)
