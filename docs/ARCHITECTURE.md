# Architecture

TanoDev Clip is a Windows desktop app with a native host and a web UI.

## Runtime Shape

```text
WPF app
  WebView2
    React UI
  Win32 hotkey and clipboard hooks
  SQLite repository
  DevTools runner
```

The React app is not a standalone product surface. It is the UI layer running inside WebView2. Native behavior stays in the WPF host.

## Projects

### `TanoDevClip.App`

Owns the desktop runtime:

- WPF window and tray lifecycle.
- WebView2 startup and fallback to static UI build.
- WebView message dispatch.
- Global hotkey registration.
- Clipboard listener.
- Programmatic clipboard writes.
- Return-window tracking and paste into the previously active app.
- Settings bridge and hotkey re-registration.
- Drag and resize handling for the borderless window.

### `TanoDevClip.UI`

Owns the interface:

- Clipboard list rendering.
- Search and filters.
- Clip detail controls.
- DevTools panel controls.
- Settings modal controls.
- Keyboard shortcuts inside the app.
- Bridge message send/receive.

The UI should not duplicate business rules that already exist in C#.

### `TanoDevClip.DevTools`

Owns developer utility logic:

- GUID generation.
- CPF/CNPJ generation and validation.
- Lorem Ipsum generation.
- Random string generation.
- JWT decoding.
- JSON formatting and validation.
- Base64 encode/decode.
- URL encode/decode.
- Regex helpers.

Use `DevToolRunner` as the host-facing entry point. Add tests in `TanoDevClip.Tests` when adding or changing tools.

### `TanoDevClip.Core`

Owns shared domain abstractions:

- Clipboard item model.
- Clip type enum.
- Repository interface.
- Clipboard classifier interface and default implementation.

### `TanoDevClip.Infrastructure`

Owns persistence and local paths:

- SQLite connection factory.
- Schema bootstrap.
- Repository implementation.
- JSON settings store.
- Local app data paths.

## Message Bridge

React sends messages through:

```ts
window.chrome.webview.postMessage({ type, payload })
```

The host receives them in `MainWindow.CoreWebView2_WebMessageReceived`.

Host responses are sent with:

```csharp
AppWebView.CoreWebView2.PostWebMessageAsJson(json);
```

Keep message payloads small, serializable and version-tolerant. Prefer adding optional fields over changing existing meanings.

Settings messages:

- `settings:save`: persist enabled tools and hotkey, then re-register the global hotkey.
- `settings:reset`: restore default settings.
- `settings:updated`: host response with the saved settings.

`app:info` also includes the current settings payload so the UI can initialize the modal.

## Clipboard Flow

1. Windows emits `WM_CLIPBOARDUPDATE`.
2. `MainWindow` reads text from the system clipboard.
3. The content is hashed with SHA256.
4. Programmatic writes are ignored once via `_ignoreNextClipboardHash`.
5. The classifier determines the clip type.
6. The clip is saved to SQLite.
7. The host notifies React with `clips:updated` and sends a fresh list.

## Copy vs Paste

There are two distinct clip actions:

- `clips:copy`: copy the selected clip to the system clipboard.
- `clips:paste`: copy the selected clip, hide TanoDev Clip, focus the window that was active before opening TanoDev Clip, then send `Ctrl+V`.

Do not merge these commands. The UI uses copy buttons for copy and double-click on the list for paste.

## Return Window Tracking

The host stores the foreground window handle before showing TanoDev Clip from the global hotkey or tray. Paste uses that stored handle.

Important rule:

- Only call `ShowWindow(SW_RESTORE)` when the target is minimized (`IsIconic(target)`).
- Do not restore an already maximized target; that converts maximized apps into smaller windows.

## Static UI Fallback

In development, the app loads:

```text
http://localhost:5173
```

If unavailable, it searches upward from the app base directory for:

```text
src/TanoDevClip.UI/dist/index.html
```

Run `npm run build` before relying on the static fallback.

## Settings Storage

Clip history lives in SQLite. Application settings are separate JSON state:

```text
%LocalAppData%/TanoDevClip/settings.json
```

Settings include:

- enabled DevTools
- global hotkey

The host validates and applies the hotkey before saving settings. If registration fails, it restores the previous hotkey.

## Validation Expectations

For backend/native changes:

```powershell
dotnet test TanoDevClip.sln
```

For UI changes:

```powershell
cd src/TanoDevClip.UI
npm run build
npm run lint
```

For clipboard, hotkey, WebView or paste changes, also run the actual WPF app and verify the behavior in Windows.
