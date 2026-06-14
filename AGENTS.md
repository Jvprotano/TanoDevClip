# AGENTS.md

Guidance for coding agents working in this repository.

## Project Summary

TanoDev Clip is a Windows desktop clipboard manager for developers. It uses a WPF/WebView2 native shell with a React/Vite UI. Clipboard capture, hotkeys, native paste behavior, persistence and DevTools execution are host-side responsibilities.

## Important Boundaries

- Keep DevTools business logic in `src/TanoDevClip.DevTools`.
- Keep React focused on UI state, rendering and bridge messages.
- Keep Win32, clipboard, hotkey, tray and paste behavior in `src/TanoDevClip.App`.
- Keep persistence in `src/TanoDevClip.Infrastructure`.
- Keep shared clipboard entities, interfaces and classification in `src/TanoDevClip.Core`.

Do not implement a second copy of CPF/CNPJ, JSON, JWT, Base64, URL or regex logic in React unless the product explicitly needs browser-specific semantics.

## DevTools Rules

- `DevToolRunner` is the host-facing dispatcher for tool execution.
- Keep `DevToolRunner` thin. It should route requests, not implement tool algorithms.
- Prefer one C# class per DevTool, matching the existing style of `GuidGenerator`, `StringGenerator` and `LoremGenerator`.
- Host-facing tool methods should return `DevToolResult` so success/error handling is consistent.
- React sends `devtools:run` and displays `devtools:run-result`.
- Tools that generate without required input should auto-run when opened:
  - GUID
  - CPF
  - CNPJ
  - Lorem Ipsum
  - Random string
- Tools that require input should run only from explicit buttons:
  - JWT
  - JSON
  - Base64
  - URL
  - Regex
- Copying generated output should go through `devtools:copy-generated` so the host can save it to history and write the system clipboard.

## Clipboard And Paste Rules

- Single-click in the history selects a clip.
- Double-click in the history pastes into the window that was active before TanoDev Clip opened.
- Keep `clips:copy` and `clips:paste` separate.
- Programmatic clipboard writes must set the ignore hash to avoid immediately recapturing the app's own output.
- When pasting into a previous window, call `ShowWindow(SW_RESTORE)` only if the target is minimized. Do not restore an already maximized target window.

## Validation

Run these after relevant changes:

```powershell
dotnet test TanoDevClip.sln
cd src/TanoDevClip.UI
npm run build
npm run lint
```

For changes touching WebView2, hotkeys, clipboard capture, tray behavior or paste behavior, also run the WPF app and manually verify the Windows behavior.

## Development Commands

Start both Vite and the WPF app:

```powershell
.\scripts\dev-start.ps1
```

Stop local development processes:

```powershell
.\scripts\dev-stop.ps1
```

Run only the WPF app:

```powershell
dotnet run --project src/TanoDevClip.App/TanoDevClip.App.csproj
```

Run only the UI:

```powershell
cd src/TanoDevClip.UI
npm run dev
```

## Documentation To Read First

- `README.md`
- `docs/ARCHITECTURE.md`
- `docs/DEVTOOLS.md`
- `src/TanoDevClip.UI/README.md` for UI-specific work

## Notes For Edits

- Prefer small, scoped changes that preserve the existing bridge contract.
- Add or update xUnit tests for C# behavior changes.
- Add UI validation through `npm run build` and `npm run lint` for TypeScript/React changes.
- Avoid unrelated restyles unless the task is explicitly about UI polish.
- Do not edit generated outputs under `bin`, `obj`, `dist` or `node_modules`.
