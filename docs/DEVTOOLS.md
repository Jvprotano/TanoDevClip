# DevTools

The DevTools panel provides small developer utilities from inside TanoDev Clip.

## Design Rule

DevTools logic belongs in C#:

```text
src/TanoDevClip.DevTools
```

React owns only the form controls and result display. This keeps behavior testable in xUnit and prevents two implementations of the same rule.

Prefer one C# class per tool. `DevToolRunner` should stay thin: it adapts the WebView payload to the right tool class and returns `DevToolResult`.

## Host Contract

React sends:

```json
{
  "type": "devtools:run",
  "payload": {
    "tool": "json",
    "action": "format",
    "input": "{\"ok\":true}"
  }
}
```

The host responds:

```json
{
  "type": "devtools:run-result",
  "payload": {
    "status": "ok",
    "value": "{\n  \"ok\": true\n}"
  }
}
```

`status` is either:

- `ok`
- `error`

The UI may show error output, but copy should be disabled for error results.

## Current Tools

| Tool | Actions | C# owner |
| --- | --- | --- |
| `guid` | `generate` | `GuidGenerator` |
| `cpf` | `generate`, `validate` | `CpfTool` |
| `cnpj` | `generate`, `validate` | `CnpjTool` |
| `lorem` | `generate` | `LoremGenerator` |
| `string` | `generate` | `StringGenerator` |
| `jwt` | `decode` | `JwtDecoder` |
| `json` | `format`, `minify` | `JsonFormatter` |
| `base64` | `encode`, `decode` | `Base64Tool` |
| `url` | `encode`, `decode` | `UrlTool` |
| `regex` | `run` | `RegexTool` |

`DevToolRunner` is the single host-facing dispatcher. It should not contain tool algorithms.

## Auto-Generation

These tools should generate immediately when opened and regenerate when their generation options change:

- `guid`
- `cpf`
- `cnpj`
- `lorem`
- `string`

These tools require user input and should run only on an explicit action button:

- `jwt`
- `json`
- `base64`
- `url`
- `regex`

## Copying Results

The UI sends:

```json
{
  "type": "devtools:copy-generated",
  "payload": {
    "content": "...",
    "kind": "json"
  }
}
```

The host:

1. Saves the output as a clip.
2. Copies it to the system clipboard.
3. Notifies the UI that clips changed.

The UI should not persist DevTools output directly.

## Adding A Tool

1. Add or update one C# tool class in `TanoDevClip.DevTools`.
2. Return `DevToolResult` from the host-facing tool method.
3. Add an action branch in `DevToolRunner` that only delegates to that class.
4. Add the `ToolKind` value in `src/TanoDevClip.UI/src/types.ts`.
5. Add a tab and controls in `DevToolsView.tsx`.
6. Decide whether it auto-generates on open or requires explicit action.
7. Add unit tests in `tests/TanoDevClip.Tests`.
8. Run:

```powershell
dotnet test TanoDevClip.sln
cd src/TanoDevClip.UI
npm run build
npm run lint
```

## Regex Semantics

Regex currently uses .NET regular expressions, not JavaScript regular expressions. Supported flags:

- `i`: ignore case
- `m`: multiline
- `s`: singleline
- `n`: explicit capture

If JavaScript regex compatibility becomes a product requirement, make that an explicit tool mode instead of silently moving regex execution into React.
