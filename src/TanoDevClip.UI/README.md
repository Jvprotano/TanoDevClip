# TanoDev Clip

<div align="center">

**A fast, local-first clipboard manager for Windows, built for developers.**

[Download for Windows](https://github.com/Jvprotano/TanoDevClip/releases/latest/download/ProtanoSoftware.TanoDevClip-win-Setup.exe)
·
[Releases](https://github.com/Jvprotano/TanoDevClip/releases)
·
[Report an issue](https://github.com/Jvprotano/TanoDevClip/issues)

<br />

![Windows](https://img.shields.io/badge/Windows-10%20%7C%2011-0078D4?logo=windows)
![.NET](https://img.shields.io/badge/.NET-10-512BD4?logo=dotnet)
![License](https://img.shields.io/badge/license-MIT-green)

</div>

<!-- Add a real product screenshot at docs/assets/app-preview.png before the public launch. -->

<p align="center">
  <img
    src="docs/assets/app-preview.png"
    alt="TanoDev Clip showing clipboard history and developer tools"
    width="900"
  />
</p>

## What is TanoDev Clip?

TanoDev Clip keeps a searchable history of the text and images you copy and lets you quickly paste them back into your previous application.

Press `Ctrl + Alt + Space`, find the content you need, and double-click it to paste.

Everything is stored locally on your computer.

## Features

- Searchable clipboard history for text and images.
- Image previews in history.
- Configurable global hotkey.
- Paste directly into the previously active application.
- Pin frequently used clips.
- Search by content, title, type, source application, and window title.
- Automatic classification of JSON, SQL, URLs, JWTs, GUIDs, emails, and text.
- Windows tray integration.
- Local SQLite storage.
- No account or backend service required.

### Built-in DevTools

TanoDev Clip also includes small utilities for common development tasks:

- GUID generation.
- CPF and CNPJ generation and validation.
- Lorem Ipsum and random string generation.
- JSON formatting, minification, and validation.
- JWT decoding.
- Base64 encoding and decoding.
- URL encoding and decoding.
- .NET regular expression helpers.

Generated results can be copied directly and are added to clipboard history.

## Download

Download the latest installer:

**[Download TanoDev Clip for Windows](https://github.com/Jvprotano/TanoDevClip/releases/latest/download/ProtanoSoftware.TanoDevClip-win-Setup.exe)**

### Requirements

- Windows 10 or Windows 11.
- 64-bit system.
- No separate .NET installation required.
- WebView2 is installed by the installer when necessary.

> [!NOTE]
> Early releases may trigger a Microsoft Defender SmartScreen warning because the installer is not yet code-signed. Download builds only from this repository's official Releases page.

## Basic usage

1. Copy text normally from any application.
2. Press `Ctrl + Alt + Space`.
3. Search or select a previous clip.
4. Double-click it to paste into the application you were using.

Additional shortcuts:

- `Ctrl + Alt + Space` — show or hide TanoDev Clip.
- `Ctrl + D` — open or close the DevTools panel.

The global shortcut can be changed in the application settings.

## Privacy

Clipboard history and settings are stored locally:

```text
%LocalAppData%/TanoDevClip/tanodevclip.db
%LocalAppData%/TanoDevClip/settings.json
```

TanoDev Clip does not require an account, cloud synchronization, or a remote clipboard database.

## Development

### Requirements

- Windows.
- .NET 10 SDK.
- Node.js and npm.

Clone the repository:

```powershell
git clone https://github.com/Jvprotano/TanoDevClip.git
cd TanoDevClip
```

Install the UI dependencies:

```powershell
cd src/TanoDevClip.UI
npm ci
cd ../..
```

Start the React UI and WPF application together:

```powershell
.\scripts\dev-start.ps1
```

The script starts:

- Vite with Hot Module Replacement for the React UI.
- `dotnet watch` for the WPF application.

Press `Ctrl+C` to stop both processes.

### Validation

Run the automated tests:

```powershell
dotnet test TanoDevClip.sln
```

Validate the UI:

```powershell
cd src/TanoDevClip.UI
npm run lint
npm run build
```

Clipboard, hotkey, tray, WebView2, and paste behavior should also be tested manually on Windows.

## Architecture

TanoDev Clip combines a native Windows host with a web-based interface:

```text
WPF host
├── WebView2
│   └── React + Vite + TypeScript UI
├── Win32 clipboard and hotkey integration
├── SQLite persistence
└── C# DevTools
```

The main projects are:

- `TanoDevClip.App` — WPF shell, WebView2, tray, hotkeys, and native clipboard behavior.
- `TanoDevClip.Core` — clipboard models, interfaces, and classification.
- `TanoDevClip.Infrastructure` — SQLite, repositories, settings, and local paths.
- `TanoDevClip.DevTools` — generators, validators, and transformations.
- `TanoDevClip.UI` — React user interface.
- `TanoDevClip.Tests` — automated tests.

More information:

- [Architecture](docs/ARCHITECTURE.md)
- [DevTools](docs/DEVTOOLS.md)
- [Agent guide](AGENTS.md)

## Contributing

Bug reports, feature suggestions, and pull requests are welcome.

Before changing the native host, WebView2 bridge, or DevTools behavior, read the relevant documentation under [`docs/`](docs/).

Please keep new functionality:

- Local-first.
- Testable.
- Consistent with the existing native/UI boundary.
- Focused on developer workflows.

## License

TanoDev Clip is available under the [MIT License](LICENSE).
