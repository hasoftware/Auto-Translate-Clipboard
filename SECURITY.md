# Security Policy

## Supported Versions

Only the latest release of Auto Translate Clipboard receives security fixes.

| Version        | Supported          |
| -------------- | ------------------ |
| Latest release | :white_check_mark: |
| Older releases | :x:                |

## Reporting a Vulnerability

Please do **not** report security vulnerabilities through public GitHub
issues.

Instead, report them privately via one of these channels:

- **GitHub**: use [private vulnerability reporting](https://github.com/hasoftware/Auto-Translate-Clipboard/security/advisories/new)
  (Security tab → "Report a vulnerability"), or
- **Email**: admin@hasoftware.vn

Please include as much of the following as you can:

- A description of the issue and its potential impact
- Steps to reproduce, or a proof of concept
- The app version and your Windows version

You can expect an initial response within 7 days. Please give us a reasonable
amount of time to investigate and release a fix before disclosing the issue
publicly.

## Scope notes

- The app sends the text you translate to Google Translate's public endpoint
  over HTTPS. Do not use it for confidential content if that is a concern.
- The app stores its settings locally under your user profile; it does not
  collect telemetry or send data anywhere else.
