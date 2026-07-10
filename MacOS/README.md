# Auto Translate Clipboard for macOS (preview)

Avalonia port of Auto Translate Clipboard for macOS. Same soul as the Windows
version: press a global hotkey anywhere, type, press Enter, and the translation
is copied to your clipboard.

**Status: preview.** Builds cleanly, but has not yet been tested on real Apple
hardware. Feedback and testing reports are very welcome.

## Build

Requires the .NET 8 SDK.

```
dotnet build AutoTranslate.MacOS.csproj
```

Self-contained release build (Apple Silicon):

```
dotnet publish AutoTranslate.MacOS.csproj -c Release -r osx-arm64 --self-contained true -o dist/macos
```

Use `-r osx-x64` for Intel Macs.

## Platform notes

- **Accessibility permission is required** for the global hotkey. On first
  run, grant it in System Settings → Privacy & Security → Accessibility.
  Without it the hotkey will not fire (the rest of the app still works).
- Run at startup is implemented via a LaunchAgent plist in
  `~/Library/LaunchAgents/com.hasoftware.autotranslate.plist`.
- Running the executable a second time shows the window of the already
  running instance.
- The default hotkey is Ctrl + Shift + T (not Cmd) so it does not clash with
  the macOS "new tab" shortcut. You can change it in Settings.

## Not done yet

- `.app` bundle and `.dmg` packaging (currently a bare executable)
- Code signing / notarization
- Toast notification after translating (the translation still lands on the
  clipboard; there is just no visual confirmation yet)
