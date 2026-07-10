# Contributing to Auto Translate Clipboard

Thank you for your interest in contributing! This document explains how to
report issues, suggest features, and submit code changes.

## Code of Conduct

This project follows the [Code of Conduct](CODE_OF_CONDUCT.md). By
participating, you are expected to uphold it.

## Reporting bugs

1. Search the [existing issues](https://github.com/hasoftware/Auto-Translate-Clipboard/issues)
   first to avoid duplicates.
2. Open a new issue using the **Bug report** template.
3. Include your Windows version, the app version, clear steps to reproduce,
   what you expected, and what actually happened. Screenshots help a lot.

For security vulnerabilities, please do **not** open a public issue — see
[SECURITY.md](SECURITY.md) instead.

## Suggesting features

Open an issue using the **Feature request** template. Describe the problem you
are trying to solve, not only the solution you have in mind — that makes it
easier to find the best design together.

## Development setup

Requirements:

- Windows 10 or 11, 64-bit
- .NET 8 SDK or newer

The project is unpackaged WinUI 3 and builds with the dotnet CLI; no Visual
Studio workload is required.

Build and run during development:

```
dotnet build AutoTranslateWinUI.csproj -p:Platform=x64 -r win-x64
```

Create a self-contained release build:

```
dotnet publish AutoTranslateWinUI.csproj -c Release -r win-x64 -p:Platform=x64 --self-contained true -o dist\AutoTranslate
```

### Project structure

- `MainWindow` — the translation window, tray icon, global hotkey, and
  clipboard workflow.
- `SettingsDialog` — theme, hotkey capture, run at startup, and the language
  picker.
- `AboutDialog` — app information and links.
- `Services/` — translation, language list, settings persistence, global
  hotkey, and startup registration.

## Submitting changes (pull requests)

1. Fork the repository and create a branch from `main`:
   `git checkout -b feature/my-change`
2. Make your changes. Keep the code style consistent with the surrounding
   code (naming, formatting, XAML conventions).
3. Build the project and test the affected workflow manually (hotkey, tray,
   translate, clipboard) before opening the PR.
4. Keep pull requests focused — one feature or fix per PR is much easier to
   review than a large mixed change.
5. Open the pull request against `main` and fill in the PR template.

By contributing, you agree that your contributions will be licensed under the
[MIT License](LICENSE) that covers this project.

## Questions

If you are unsure about anything, feel free to open an issue and ask, or reach
out via the links in the [README](README.md#author).
