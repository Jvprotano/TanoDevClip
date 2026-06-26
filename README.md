# TanoDev Clip

TanoDev Clip is a local-first clipboard manager for Windows, built for developers. It captures copied text and images, classifies common developer text content, persists history in SQLite, supports quick paste back into the previously active app, and includes a DevTools panel for small developer utilities.

## Stack

- C# / .NET 10
- WPF desktop shell
- WebView2 host
- React + Vite + TypeScript UI
- SQLite with `Microsoft.Data.Sqlite`
- xUnit tests

## Current Features

- Borderless WPF window hosted by WebView2.
- React UI loaded from `http://localhost:5173` during development.
- Static UI fallback from `src/TanoDevClip.UI/dist/index.html` when the Vite server is unavailable.
- Global hotkey: `Ctrl+Alt+Space` to show/hide the main window and focus search.
- Win32 clipboard listener using `AddClipboardFormatListener` and `WM_CLIPBOARDUPDATE`.
- Automatic text and image capture with SHA256 content hash.
- Image history with PNG persistence and UI previews.
- SQLite database at `%LocalAppData%/TanoDevClip/tanodevclip.db`.
- Search by content/title metadata, type, source app and source window title.
- Classification for JSON, SQL, URL, JWT, GUID, email and text fallback.
- Pin/unpin clips, with pinned clips listed first.
- Copy a clip back to the clipboard.
- Double-click a clip to paste it into the app/window that was active before TanoDev Clip opened.
- DevTools panel with GUID, CPF, CNPJ, Lorem Ipsum, random strings, JWT decode, JSON format/validate, Base64, URL encode/decode and regex helpers.
- Settings modal to enable/disable DevTools, configure the global hotkey and reset defaults.
- Generated/copied DevTools output is saved to clipboard history.

## Architecture

The app has a native host and a web UI. Keep the boundary clear:

- `TanoDevClip.App`: WPF shell, WebView2, tray, hotkeys, Win32 clipboard listener, native paste behavior and UI bridge messages.
- `TanoDevClip.Core`: clipboard entities, enums, interfaces and classification.
- `TanoDevClip.Infrastructure`: SQLite bootstrap, repositories, settings storage and local path helpers.
- `TanoDevClip.DevTools`: pure DevTools logic. This is the source of truth for generators, validators and text transforms.
- `TanoDevClip.UI`: React UI. It renders controls, manages UI state and sends bridge messages to the host. It should not duplicate DevTools business rules.
- `TanoDevClip.Tests`: unit tests for classifier, generators and DevTools behavior.

More detail:

- [Architecture](docs/ARCHITECTURE.md)
- [DevTools](docs/DEVTOOLS.md)
- [Agent guide](AGENTS.md)

## Run

Install dependencies and build:

```powershell
dotnet restore
dotnet build
dotnet test
```

Install UI dependencies:

```powershell
cd src/TanoDevClip.UI
npm install
```

Run the React UI:

```powershell
cd src/TanoDevClip.UI
npm run dev
```

Run the desktop app from the repository root:

```powershell
dotnet run --project src/TanoDevClip.App/TanoDevClip.App.csproj
```

The app expects the Vite dev server at:

```text
http://localhost:5173
```

For local debugging, start both the Vite UI and the WPF app from the repository root:

```powershell
.\scripts\dev-start.ps1
```

The script waits for Vite before opening the desktop app. React changes use Vite HMR, and C# changes run through `dotnet watch` Hot Reload. If a C# edit cannot be hot-reloaded, `dotnet watch` restarts the desktop app automatically.

Press `Ctrl+C` in that terminal to stop both processes. If you started it with `-NoWait` or need to clean up a previous run, stop both manually:

```powershell
.\scripts\dev-stop.ps1
```

To run without C# Hot Reload:

```powershell
.\scripts\dev-start.ps1 -NoHotReload
```

Create a static UI build:

```powershell
cd src/TanoDevClip.UI
npm run build
```

## Validation

Run these before handing off changes:

```powershell
dotnet test TanoDevClip.sln
cd src/TanoDevClip.UI
npm run build
npm run lint
```

For UI behavior changes, also run the app and verify the relevant flow in WebView2. For paste behavior, test against a real Windows target app such as VS Code, Notepad or Chrome.

## Clipboard And Paste Behavior

- Clipboard capture happens in the native WPF app.
- Programmatic copies set an ignore hash so the app does not immediately recapture its own output as a new external clip.
- Double-click paste is intentionally separate from copy:
  - `clips:copy` copies the clip and hides TanoDev Clip.
  - `clips:paste` copies the clip, hides TanoDev Clip, restores focus to the previous app/window and sends `Ctrl+V`.
- The previous window handle is recorded before showing TanoDev Clip from the hotkey or tray.
- Do not call `ShowWindow(SW_RESTORE)` on an already maximized target window; it converts maximized apps such as VS Code or Chrome into smaller windows. Restore only if `IsIconic(target)` is true.

## Database

The local database is created automatically on startup:

```text
%LocalAppData%/TanoDevClip/tanodevclip.db
```

The repository layer is intentionally small. If FTS5 is added later, keep the UI contract stable and adapt the repository implementation.

## Settings

Application settings are stored separately from clip history:

```text
%LocalAppData%/TanoDevClip/settings.json
```

Settings currently include:

- enabled DevTools
- global hotkey

The default hotkey is `Ctrl+Alt+Space`. Settings can be reset to defaults from the settings modal.

## Hotkeys

- `Ctrl+Alt+Space`: show the window and focus search by default.
- If the window is visible and focused, the same hotkey hides it.
- `Ctrl+D`: toggle the DevTools panel inside the app.

The global hotkey can be changed in settings. The host re-registers the Win32 hotkey immediately after saving.

If another app already owns the global hotkey, TanoDev Clip continues running without crashing.

## Project Layout

```text
src/
  TanoDevClip.App/             WPF shell, WebView2 host, bridge, hotkey, clipboard listener
  TanoDevClip.Core/            Entities, enums, interfaces, clipboard classification
  TanoDevClip.Infrastructure/  SQLite bootstrap, repository, local paths
  TanoDevClip.DevTools/        Developer tools logic and runner
  TanoDevClip.UI/              React + Vite + TypeScript UI
tests/
  TanoDevClip.Tests/           Classifier and dev tool tests
docs/
  ARCHITECTURE.md              Runtime boundaries and bridge flow
  DEVTOOLS.md                  DevTools contract and implementation rules
```

## Near-Term Ideas

- Add packaging/installer.
- Add startup-at-login settings.
- Add import/export for local history.
- Add FTS5 search if simple `LIKE` search becomes insufficient.
