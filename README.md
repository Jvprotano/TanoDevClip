# TanoDev Clip

TanoDev Clip is a local-first clipboard manager for Windows, built for developers. The MVP captures copied text, classifies common developer content, persists history in SQLite, and includes a small Dev Tools area starting with a GUID Generator.

## Stack

- C# / .NET 10
- WPF desktop shell
- WebView2 host
- React + Vite + TypeScript UI
- SQLite with `Microsoft.Data.Sqlite`
- xUnit tests

## Implemented MVP

- WPF window hosted by WebView2 with a dark, VS Code-inspired UI.
- React UI loaded from `http://localhost:5173` during development.
- Simple future fallback for `src/TanoDevClip.UI/dist/index.html` when the Vite server is not available.
- Global hotkey: `Ctrl+Alt+Space` to show/hide the main window.
- Win32 clipboard listener using `AddClipboardFormatListener` and `WM_CLIPBOARDUPDATE`.
- Automatic text capture with SHA256 content hash.
- SQLite database at `%LocalAppData%/TanoDevClip/tanodevclip.db`.
- Search by content, title, type, source app and source window title.
- Basic classification: JSON, SQL, URL, JWT, GUID, email and text fallback.
- Copy old clips back to the clipboard.
- Pin/unpin clips, with pinned clips listed first.
- GUID Generator with default, no-hyphens and uppercase formats.
- Generated GUIDs are saved to history when copied.

## Run

Install dependencies and build:

```powershell
dotnet restore
dotnet build
dotnet test
```

Run the React UI:

```powershell
cd src/TanoDevClip.UI
npm install
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

For local debugging, start both the Vite UI and the WPF app from the repository root with one command:

```powershell
.\scripts\dev-start.ps1
```

The script waits for Vite before opening the desktop app. React changes use Vite HMR, and C# changes run through `dotnet watch` Hot Reload. If a C# edit cannot be hot-reloaded, `dotnet watch` restarts the desktop app automatically.

Press `Ctrl+C` in that terminal to stop both processes. If you started it with `-NoWait` or need to clean up a previous run, stop both manually with:

```powershell
.\scripts\dev-stop.ps1
```

To run without C# Hot Reload:

```powershell
.\scripts\dev-start.ps1 -NoHotReload
```

You can also create a static UI build:

```powershell
cd src/TanoDevClip.UI
npm run build
```

## Database

The local database is created automatically on startup:

```text
%LocalAppData%/TanoDevClip/tanodevclip.db
```

The MVP uses a simple `LIKE` search. The repository layer is intentionally small so FTS5 can be added later without changing the UI contract.

## Hotkey

- `Ctrl+Alt+Space`: show the window and focus search.
- If the window is visible and focused, the same hotkey hides it.

If another app already owns the hotkey, TanoDev Clip continues running without crashing.

## Project Layout

```text
src/
  TanoDevClip.App/             WPF shell, WebView2 host, bridge, hotkey, clipboard listener
  TanoDevClip.Core/            Entities, enums, interfaces, clipboard classification
  TanoDevClip.Infrastructure/  SQLite bootstrap, repository, local paths
  TanoDevClip.DevTools/        Developer tools
  TanoDevClip.UI/              React + Vite + TypeScript UI
tests/
  TanoDevClip.Tests/           Classifier and dev tool tests
```

## Next Steps

- Add tray icon and startup/minimize behavior.
- Add FTS5 search.
- Add JSON formatter, JWT decoder and SQL formatter.
- Add import/export for local history.
- Add packaging/installer.
