import { useMemo, useState } from "react";
import { devToolDefinitions } from "../constants";
import type { AppSettings, AppSettingsUpdate, ToolKind } from "../types";

type SettingsModalProps = {
  settings: AppSettings;
  onClose: () => void;
  onSave: (settings: AppSettingsUpdate) => void;
  onReset: () => void;
};

const keyOptions = [
  "Space",
  "A",
  "B",
  "C",
  "D",
  "E",
  "F",
  "G",
  "H",
  "I",
  "J",
  "K",
  "L",
  "M",
  "N",
  "O",
  "P",
  "Q",
  "R",
  "S",
  "T",
  "U",
  "V",
  "W",
  "X",
  "Y",
  "Z",
  "F1",
  "F2",
  "F3",
  "F4",
  "F5",
  "F6",
  "F7",
  "F8",
  "F9",
  "F10",
  "F11",
  "F12",
];

export function SettingsModal({
  settings,
  onClose,
  onSave,
  onReset,
}: SettingsModalProps) {
  const parsedInitialHotKey = useMemo(
    () => parseHotKey(settings.hotKey),
    [settings.hotKey],
  );
  const [enabledTools, setEnabledTools] = useState<ToolKind[]>(
    settings.enabledTools,
  );
  const [ctrl, setCtrl] = useState(parsedInitialHotKey.ctrl);
  const [alt, setAlt] = useState(parsedInitialHotKey.alt);
  const [shift, setShift] = useState(parsedInitialHotKey.shift);
  const [key, setKey] = useState(parsedInitialHotKey.key);

  const hotKey = buildHotKey(ctrl, alt, shift, key);
  const canSave = Boolean(hotKey);

  function toggleTool(tool: ToolKind) {
    setEnabledTools((current) =>
      current.includes(tool)
        ? current.filter((enabledTool) => enabledTool !== tool)
        : [...current, tool],
    );
  }

  function handleSave() {
    if (!hotKey) {
      return;
    }

    onSave({
      hotKey,
      enabledTools,
    });
  }

  return (
    <div className="settings-backdrop" role="presentation">
      <section
        className="settings-modal"
        role="dialog"
        aria-modal="true"
        aria-labelledby="settings-title"
      >
        <header className="settings-header">
          <div>
            <strong id="settings-title">settings</strong>
            <span>runtime preferences</span>
          </div>
          <button onClick={onClose} aria-label="Close settings">
            x
          </button>
        </header>

        <div className="settings-body">
          <section className="settings-section">
            <div className="settings-section-title">
              <strong>tools</strong>
              <span>
                {enabledTools.length}/{devToolDefinitions.length} enabled
              </span>
            </div>

            <div className="tool-toggle-grid">
              {devToolDefinitions.map((tool) => (
                <label key={tool.id} className="tool-toggle">
                  <input
                    type="checkbox"
                    checked={enabledTools.includes(tool.id)}
                    onChange={() => toggleTool(tool.id)}
                  />
                  <span>{tool.label}</span>
                </label>
              ))}
            </div>
          </section>

          <section className="settings-section">
            <div className="settings-section-title">
              <strong>hotkey</strong>
              <span>{hotKey || "select a modifier"}</span>
            </div>

            <div className="hotkey-grid">
              <label>
                <input
                  type="checkbox"
                  checked={ctrl}
                  onChange={(event) => setCtrl(event.target.checked)}
                />
                Ctrl
              </label>
              <label>
                <input
                  type="checkbox"
                  checked={alt}
                  onChange={(event) => setAlt(event.target.checked)}
                />
                Alt
              </label>
              <label>
                <input
                  type="checkbox"
                  checked={shift}
                  onChange={(event) => setShift(event.target.checked)}
                />
                Shift
              </label>
              <select
                value={key}
                onChange={(event) => setKey(event.target.value)}
                aria-label="Hotkey key"
              >
                {keyOptions.map((option) => (
                  <option key={option} value={option}>
                    {option}
                  </option>
                ))}
              </select>
            </div>
          </section>
        </div>

        <footer className="settings-footer">
          <button onClick={onReset}>reset default</button>
          <div>
            <button onClick={onClose}>cancel</button>
            <button
              className="primary-button"
              disabled={!canSave}
              onClick={handleSave}
            >
              save
            </button>
          </div>
        </footer>
      </section>
    </div>
  );
}

function parseHotKey(value: string) {
  const parts = value.split("+").map((part) => part.trim().toLowerCase());
  const key = value.split("+").at(-1)?.trim() || "Space";

  return {
    ctrl: parts.includes("ctrl") || parts.includes("control"),
    alt: parts.includes("alt"),
    shift: parts.includes("shift"),
    key: keyOptions.includes(key) ? key : "Space",
  };
}

function buildHotKey(ctrl: boolean, alt: boolean, shift: boolean, key: string) {
  const parts: string[] = [];

  if (ctrl) {
    parts.push("Ctrl");
  }

  if (alt) {
    parts.push("Alt");
  }

  if (shift) {
    parts.push("Shift");
  }

  if (parts.length === 0) {
    return "";
  }

  parts.push(key);
  return parts.join("+");
}
