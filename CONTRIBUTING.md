# Contributing to TanoDev Clip

Thanks for considering a contribution to TanoDev Clip.

Contributions can include bug reports, feature proposals, documentation improvements, tests, UI changes, and code fixes. The goal is to keep the project focused, local-first, and useful for developer workflows.

## Before you start

For small bug fixes, documentation improvements, or tests, you can open a pull request directly.

For new features, architectural changes, new dependencies, or changes to clipboard, hotkey, paste, packaging, or update behavior, open an issue first. This avoids spending time on a change that may not fit the project direction.

When reporting bugs or sharing screenshots, redact sensitive clipboard content, tokens, credentials, personal data, and private window titles.

## Development requirements

You need:

- Windows 10 or Windows 11
- .NET 10 SDK
- Node.js and npm
- Git

Clone your fork and enter the repository:

```powershell
git clone https://github.com/YOUR_USERNAME/TanoDevClip.git
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

The script starts Vite with Hot Module Replacement and the WPF application through `dotnet watch`. Press `Ctrl+C` to stop both processes.

## Project architecture

TanoDev Clip combines a native Windows host with a web-based interface:

```text
WPF host
├── WebView2
│   └── React + Vite + TypeScript UI
├── Win32 clipboard and hotkey integration
├── SQLite persistence
└── C# DevTools
```

Keep these boundaries intact:

- `TanoDevClip.App` owns WPF, WebView2, tray behavior, global hotkeys, clipboard integration, native paste behavior, and bridge messages.
- `TanoDevClip.Core` owns domain models, interfaces, and clipboard classification.
- `TanoDevClip.Infrastructure` owns SQLite, repositories, settings storage, and local paths.
- `TanoDevClip.DevTools` owns generators, validators, and text transformations.
- `TanoDevClip.UI` owns rendering, UI state, and communication with the native host.
- `TanoDevClip.Tests` owns automated tests for domain and DevTools behavior.

Important rules:

- Do not duplicate DevTools business logic in React.
- Keep native clipboard, hotkey, tray, focus, and paste behavior in the WPF host.
- Keep WebView messages small, serializable, and tolerant of optional fields.
- Prefer extending existing contracts over changing the meaning of existing messages.
- Preserve local-first behavior. Do not add remote storage, analytics, or telemetry without prior discussion.
- Avoid adding dependencies when the same result can be achieved clearly with the current stack.

Read the detailed documentation before changing these areas:

- [Architecture](docs/ARCHITECTURE.md)
- [DevTools](docs/DEVTOOLS.md)
- [Agent guide](AGENTS.md)

## Making a change

Create a focused branch from the latest `main`:

```powershell
git switch main
git pull --ff-only
git switch -c fix/short-description
```

Use a descriptive branch prefix when possible:

```text
fix/
feature/
docs/
test/
refactor/
```

Keep pull requests focused. Avoid combining a feature, broad refactoring, dependency updates, and unrelated formatting in the same PR.

## Validation

Run the .NET tests:

```powershell
dotnet test TanoDevClip.sln
```

Validate the React UI:

```powershell
cd src/TanoDevClip.UI
npm run lint
npm run build
```

The continuous integration workflow runs equivalent checks for pull requests. A green CI run does not replace manual testing for Windows-specific behavior.

### Manual testing

Run the desktop application and manually test changes involving:

- clipboard capture
- copy and paste actions
- global hotkeys
- system tray behavior
- WebView2 communication
- focus restoration
- paste into the previously active application
- local database or settings persistence
- packaging or installation

For paste-related changes, test with real target applications such as VS Code, Notepad, Chrome, or Visual Studio.

For UI changes, verify:

- the relevant workflow in the running desktop application
- keyboard navigation
- focus states
- different window sizes
- long or unusual clipboard content
- empty and error states

Include screenshots or a short recording in the pull request when the visual behavior changes.

## Adding or changing a DevTool

DevTools logic belongs in `src/TanoDevClip.DevTools`.

When adding or changing a tool:

1. Implement the behavior in a focused C# class.
2. Keep `DevToolRunner` as a thin dispatcher.
3. Add or update unit tests in `tests/TanoDevClip.Tests`.
4. Update the UI types, controls, and settings defaults.
5. Decide whether the tool runs automatically or requires an explicit action.
6. Update `docs/DEVTOOLS.md` when the public contract or behavior changes.

Do not silently change regex behavior from .NET semantics to JavaScript semantics. Do not describe JWT decoding as signature validation.

## Pull requests

Before opening a pull request:

- Rebase or update your branch from the latest `main` when necessary.
- Run the required automated validation.
- Test affected Windows behavior manually.
- Remove debugging code, temporary files, generated artifacts, and unrelated formatting changes.
- Update documentation when behavior, setup, or architecture changes.

In the pull request description:

- explain the problem and the chosen solution
- link the related issue when one exists
- describe how the change was validated
- include screenshots or recordings for UI changes
- call out known limitations or follow-up work

Maintainers may request changes before merging. A pull request may be closed when it is out of scope, duplicates existing work, introduces unnecessary complexity, or does not follow the project architecture.

## Code style

Follow the style already used in the surrounding code.

General expectations:

- Prefer clear, focused classes and methods.
- Keep nullable reference types enabled and address warnings intentionally.
- Avoid large unrelated refactors inside feature or bug-fix pull requests.
- Add tests for business rules and regression fixes when practical.
- Use descriptive names instead of comments that restate the code.
- Keep UI copy concise and consistent with the rest of the application.

No contribution should include real secrets, API keys, access tokens, private certificates, personal clipboard history, or generated build artifacts.

## Licensing

TanoDev Clip is licensed under the [MIT License](LICENSE).

By submitting a contribution, you confirm that you have the right to submit it and agree that it will be distributed under the same MIT License.
