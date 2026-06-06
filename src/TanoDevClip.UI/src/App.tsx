import {
  useCallback,
  useEffect,
  useMemo,
  useRef,
  useState,
  type RefObject,
} from "react";
import { tanoDevBridge, type BridgeMessage } from "./bridge/tanoDevBridge";

type AppInfo = {
  name: string;
  version: string;
  environment: string;
  hotkey: string;
};

type ClipItem = {
  id: string;
  content: string;
  contentHash: string;
  clipType: string;
  title?: string | null;
  sourceApp?: string | null;
  sourceWindowTitle?: string | null;
  sourceUrl?: string | null;
  isPinned: boolean;
  createdAt: string;
  lastUsedAt?: string | null;
  useCount: number;
};

type ViewName = "clipboard" | "devtools" | "settings";
type GuidFormat = "default" | "no-hyphens" | "uppercase";

const clipTypes = [
  "All",
  "Text",
  "Json",
  "Sql",
  "Url",
  "Jwt",
  "Guid",
  "Email",
  "Code",
  "Markdown",
  "Unknown",
];

export default function App() {
  const searchInputRef = useRef<HTMLInputElement>(null);
  const [activeView, setActiveView] = useState<ViewName>("clipboard");
  const [appInfo, setAppInfo] = useState<AppInfo | null>(null);
  const [bridgeAvailable, setBridgeAvailable] = useState(false);
  const [clips, setClips] = useState<ClipItem[]>([]);
  const [selectedClipId, setSelectedClipId] = useState<string | null>(null);
  const [query, setQuery] = useState("");
  const [clipType, setClipType] = useState("All");
  const [status, setStatus] = useState("Ready");
  const [guidFormat, setGuidFormat] = useState<GuidFormat>("default");
  const [guidValue, setGuidValue] = useState("");

  const requestClips = useCallback(
    (nextQuery = query, nextType = clipType) => {
      tanoDevBridge.send({
        type: "clips:list",
        payload: {
          query: nextQuery,
          clipType: nextType === "All" ? null : nextType,
          limit: 100,
        },
      });
    },
    [clipType, query],
  );

  useEffect(() => {
    setBridgeAvailable(tanoDevBridge.isAvailable());

    const unsubscribe = tanoDevBridge.onMessage((message: BridgeMessage) => {
      if (message.type === "app:info") {
        setAppInfo(message.payload as AppInfo);
      }

      if (message.type === "clips:list-result") {
        const payload = message.payload as { clips: ClipItem[] };
        setClips(payload.clips ?? []);
        setStatus(`${payload.clips?.length ?? 0} clips loaded`);
      }

      if (message.type === "clips:updated") {
        const payload = message.payload as { reason?: string } | undefined;
        setStatus(
          payload?.reason ? `Updated from ${payload.reason}` : "Updated",
        );
      }

      if (message.type === "devtools:generate-guid-result") {
        const payload = message.payload as { value: string };
        setGuidValue(payload.value);
      }

      if (message.type === "app:error") {
        const payload = message.payload as { message?: string } | undefined;
        setStatus(payload?.message ?? "Host error");
      }

      if (message.type === "app:focus-search") {
        setActiveView("clipboard");
        window.setTimeout(() => searchInputRef.current?.focus(), 0);
      }
    });

    tanoDevBridge.send({ type: "app:get-info" });
    requestClips();

    return unsubscribe;
  }, [requestClips]);

  useEffect(() => {
    if (clips.length === 0) {
      setSelectedClipId(null);
      return;
    }

    if (!selectedClipId || !clips.some((clip) => clip.id === selectedClipId)) {
      setSelectedClipId(clips[0].id);
    }
  }, [clips, selectedClipId]);

  const selectedClip = useMemo(
    () => clips.find((clip) => clip.id === selectedClipId) ?? clips[0] ?? null,
    [clips, selectedClipId],
  );

  function handleSearchSubmit() {
    requestClips(query, clipType);
  }

  function handleTypeChange(value: string) {
    setClipType(value);
    requestClips(query, value);
  }

  function handleCopyClip(id: string) {
    tanoDevBridge.send({ type: "clips:copy", payload: { id } });
  }

  function handleTogglePin(id: string) {
    tanoDevBridge.send({ type: "clips:toggle-pin", payload: { id } });
  }

  function handleGenerateGuid() {
    tanoDevBridge.send({
      type: "devtools:generate-guid",
      payload: { format: guidFormat },
    });
  }

  function handleCopyGuid() {
    if (!guidValue) {
      return;
    }

    tanoDevBridge.send({
      type: "devtools:copy-guid",
      payload: { content: guidValue },
    });
    setStatus("GUID copied");
  }

  return (
    <div className="app-shell">
      <aside className="sidebar">
        <nav className="nav" aria-label="Main navigation">
          <button
            className={
              activeView === "clipboard" ? "nav-item active" : "nav-item"
            }
            onClick={() => setActiveView("clipboard")}
          >
            Clipboard
          </button>
          <button
            className={
              activeView === "devtools" ? "nav-item active" : "nav-item"
            }
            onClick={() => setActiveView("devtools")}
          >
            Dev Tools
          </button>
          <button
            className={
              activeView === "settings" ? "nav-item active" : "nav-item"
            }
            onClick={() => setActiveView("settings")}
          >
            Settings
          </button>
        </nav>

        <div className="sidebar-footer">
          <span>{appInfo?.hotkey ?? "Ctrl+Shift+V"}</span>
          <span>{bridgeAvailable ? "Host connected" : "Browser preview"}</span>
        </div>
      </aside>

      <main className="main">
        {activeView === "clipboard" && (
          <ClipboardView
            clips={clips}
            selectedClip={selectedClip}
            selectedClipId={selectedClipId}
            query={query}
            clipType={clipType}
            status={status}
            searchInputRef={searchInputRef}
            onQueryChange={setQuery}
            onSearch={handleSearchSubmit}
            onTypeChange={handleTypeChange}
            onSelectClip={setSelectedClipId}
            onCopyClip={handleCopyClip}
            onTogglePin={handleTogglePin}
          />
        )}

        {activeView === "devtools" && (
          <DevToolsView
            guidFormat={guidFormat}
            guidValue={guidValue}
            onGuidFormatChange={setGuidFormat}
            onGenerateGuid={handleGenerateGuid}
            onCopyGuid={handleCopyGuid}
          />
        )}

        {activeView === "settings" && (
          <SettingsView appInfo={appInfo} bridgeAvailable={bridgeAvailable} />
        )}
      </main>
    </div>
  );
}

function ClipboardView({
  clips,
  selectedClip,
  selectedClipId,
  query,
  clipType,
  status,
  searchInputRef,
  onQueryChange,
  onSearch,
  onTypeChange,
  onSelectClip,
  onCopyClip,
  onTogglePin,
}: {
  clips: ClipItem[];
  selectedClip: ClipItem | null;
  selectedClipId: string | null;
  query: string;
  clipType: string;
  status: string;
  searchInputRef: RefObject<HTMLInputElement | null>;
  onQueryChange: (value: string) => void;
  onSearch: () => void;
  onTypeChange: (value: string) => void;
  onSelectClip: (id: string) => void;
  onCopyClip: (id: string) => void;
  onTogglePin: (id: string) => void;
}) {
  return (
    <>
      <header className="topbar">
        <div>
          <h1>Clipboard</h1>
          <p>Fast local history for code, SQL, JSON, URLs and everyday text.</p>
        </div>
        <div className="status-pill">{status}</div>
      </header>

      <section className="search-row">
        <input
          ref={searchInputRef}
          value={query}
          onChange={(event) => onQueryChange(event.target.value)}
          onKeyDown={(event) => {
            if (event.key === "Enter") {
              onSearch();
            }
          }}
          placeholder="Search content, type, title or source..."
        />
        <select
          value={clipType}
          onChange={(event) => onTypeChange(event.target.value)}
        >
          {clipTypes.map((type) => (
            <option key={type} value={type}>
              {type}
            </option>
          ))}
        </select>
        <button className="primary-button" onClick={onSearch}>
          Search
        </button>
      </section>

      <section className="content-grid">
        <div className="panel clip-panel">
          <div className="panel-header">
            <span>Recent clips</span>
            <small>{clips.length}/100</small>
          </div>

          <div className="clip-list">
            {clips.length === 0 ? (
              <div className="empty-state">
                Copy text anywhere in Windows to fill the history.
              </div>
            ) : (
              clips.map((clip) => (
                <button
                  key={clip.id}
                  className={
                    clip.id === selectedClipId
                      ? "clip-item active"
                      : "clip-item"
                  }
                  onClick={() => onSelectClip(clip.id)}
                >
                  <span
                    className={`clip-type type-${clip.clipType.toLowerCase()}`}
                  >
                    {clip.clipType}
                  </span>
                  <span className="clip-body">
                    <span className="clip-title">
                      {clip.isPinned && (
                        <span className="pin-mark">Pinned</span>
                      )}
                      {clip.title || summarize(clip.content)}
                    </span>
                    <span className="clip-meta">
                      {clip.sourceApp ?? "Unknown source"} ·{" "}
                      {formatDate(clip.createdAt)}
                    </span>
                  </span>
                </button>
              ))
            )}
          </div>
        </div>

        <div className="panel preview-panel">
          <div className="panel-header">
            <span>Preview</span>
            {selectedClip && <small>{selectedClip.useCount} uses</small>}
          </div>

          {selectedClip ? (
            <div className="preview">
              <div className="preview-toolbar">
                <span
                  className={`clip-type type-${selectedClip.clipType.toLowerCase()}`}
                >
                  {selectedClip.clipType}
                </span>
                <div className="preview-actions">
                  <button onClick={() => onTogglePin(selectedClip.id)}>
                    {selectedClip.isPinned ? "Unpin" : "Pin"}
                  </button>
                  <button
                    className="primary-button"
                    onClick={() => onCopyClip(selectedClip.id)}
                  >
                    Copy
                  </button>
                </div>
              </div>

              <h2>{selectedClip.title || summarize(selectedClip.content)}</h2>
              <div className="source-line">
                <span>{selectedClip.sourceApp ?? "Unknown app"}</span>
                <span>
                  {selectedClip.sourceWindowTitle ?? "No window title"}
                </span>
                <span>{formatDate(selectedClip.createdAt)}</span>
              </div>

              <pre>{selectedClip.content}</pre>
            </div>
          ) : (
            <div className="empty-state">
              Select a clip to inspect its content.
            </div>
          )}
        </div>
      </section>
    </>
  );
}

function DevToolsView({
  guidFormat,
  guidValue,
  onGuidFormatChange,
  onGenerateGuid,
  onCopyGuid,
}: {
  guidFormat: GuidFormat;
  guidValue: string;
  onGuidFormatChange: (format: GuidFormat) => void;
  onGenerateGuid: () => void;
  onCopyGuid: () => void;
}) {
  return (
    <>
      <header className="topbar">
        <div>
          <h1>Dev Tools</h1>
          <p>Small utilities that feed the clipboard history.</p>
        </div>
      </header>

      <section className="tool-surface">
        <div className="tool-header">
          <div>
            <h2>GUID Generator</h2>
            <p>Generate a GUID and copy it into your local clip history.</p>
          </div>
        </div>

        <div className="segmented-control">
          <button
            className={guidFormat === "default" ? "active" : ""}
            onClick={() => onGuidFormatChange("default")}
          >
            Default
          </button>
          <button
            className={guidFormat === "no-hyphens" ? "active" : ""}
            onClick={() => onGuidFormatChange("no-hyphens")}
          >
            No hyphens
          </button>
          <button
            className={guidFormat === "uppercase" ? "active" : ""}
            onClick={() => onGuidFormatChange("uppercase")}
          >
            Uppercase
          </button>
        </div>

        <div className="guid-output">
          {guidValue || "550e8400-e29b-41d4-a716-446655440000"}
        </div>

        <div className="tool-actions">
          <button className="primary-button" onClick={onGenerateGuid}>
            Generate GUID
          </button>
          <button disabled={!guidValue} onClick={onCopyGuid}>
            Copy
          </button>
        </div>
      </section>
    </>
  );
}

function SettingsView({
  appInfo,
  bridgeAvailable,
}: {
  appInfo: AppInfo | null;
  bridgeAvailable: boolean;
}) {
  return (
    <>
      <header className="topbar">
        <div>
          <h1>Settings</h1>
          <p>MVP status and local runtime information.</p>
        </div>
      </header>

      <section className="settings-grid">
        <div>
          <span>Application</span>
          <strong>{appInfo?.name ?? "TanoDev Clip"}</strong>
        </div>
        <div>
          <span>Version</span>
          <strong>{appInfo?.version ?? "0.1.0"}</strong>
        </div>
        <div>
          <span>Bridge</span>
          <strong>{bridgeAvailable ? "Connected" : "Unavailable"}</strong>
        </div>
        <div>
          <span>Hotkey</span>
          <strong>{appInfo?.hotkey ?? "Ctrl+Shift+V"}</strong>
        </div>
      </section>
    </>
  );
}

function summarize(content: string) {
  const compact = content.replace(/\s+/g, " ").trim();
  return compact.length > 90 ? `${compact.slice(0, 90)}...` : compact;
}

function formatDate(value: string) {
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) {
    return "";
  }

  return new Intl.DateTimeFormat(undefined, {
    month: "short",
    day: "2-digit",
    hour: "2-digit",
    minute: "2-digit",
  }).format(date);
}
