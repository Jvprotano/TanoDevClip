# TanoDevClip.UI

React + Vite + TypeScript UI for TanoDev Clip.

This project is the WebView2 front-end. It should stay focused on presentation, local UI state, keyboard handling and bridge messages to the native host.

## Responsibilities

- Render clipboard history, search, filters and clip details.
- Render the DevTools panel and collect user input.
- Render the settings modal and collect enabled-tool/hotkey preferences.
- Send bridge messages through `window.chrome.webview.postMessage`.
- Receive host messages and update UI state.
- Provide a preview mode when the WebView bridge is unavailable.

## Non-Responsibilities

- Do not implement clipboard capture, paste behavior or Win32 focus logic here.
- Do not duplicate DevTools business rules here. Generators, validators and text transforms belong in `src/TanoDevClip.DevTools`.
- Do not persist clipboard data directly from the UI.

## Bridge

The bridge wrapper is:

```text
src/bridge/tanoDevBridge.ts
```

Important outbound messages:

- `app:get-info`
- `app:hide`
- `app:drag-window`
- `clips:list`
- `clips:copy`
- `clips:paste`
- `clips:toggle-pin`
- `devtools:run`
- `devtools:copy-generated`
- `settings:save`
- `settings:reset`

Important inbound messages:

- `app:info`
- `app:focus-search`
- `app:error`
- `clips:list-result`
- `clips:updated`
- `devtools:run-result`
- `settings:updated`

See the host implementation in `src/TanoDevClip.App/MainWindow.xaml.cs`.

## DevTools UI

`src/components/DevToolsView.tsx` owns the UI state for tool inputs and sends `devtools:run` payloads to the host. It intentionally does not compute CPF, CNPJ, JSON, JWT, Base64, URL or regex results itself.

Tools that generate output without required input should auto-run when opened or when their generation options change:

- GUID
- CPF
- CNPJ
- Lorem Ipsum
- Random string

Tools that require user input should run only when the user clicks the relevant action:

- JWT decode
- JSON format/minify
- Base64 encode/decode
- URL encode/decode
- Regex helper

The DevTools panel must filter tabs using `settings.enabledTools`. Disabled tools should not be visible in the drawer.

## Settings UI

`src/components/SettingsModal.tsx` owns temporary settings form state. Saving sends `settings:save`; reset sends `settings:reset`.

The modal controls:

- enabled DevTools
- global app hotkey
- reset to default

The actual hotkey registration happens in the WPF host, not in React.

## Run

```powershell
npm install
npm run dev
```

The WPF app expects Vite at:

```text
http://localhost:5173
```

## Validate

```powershell
npm run build
npm run lint
```

For visual or interaction changes, run the desktop app as well. The browser preview is useful for layout, but it does not exercise WebView2, clipboard capture or native paste behavior.
