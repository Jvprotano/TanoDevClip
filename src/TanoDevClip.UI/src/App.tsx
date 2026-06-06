import { useCallback, useEffect, useMemo, useRef, useState } from "react";
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
  const [isDevToolsOpen, setIsDevToolsOpen] = useState(false);
  const [appInfo, setAppInfo] = useState<AppInfo | null>(null);
  const [clips, setClips] = useState<ClipItem[]>([]);
  const [selectedClipId, setSelectedClipId] = useState<string | null>(null);
  const [query, setQuery] = useState("");
  const [clipType, setClipType] = useState("All");
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
    const unsubscribe = tanoDevBridge.onMessage((message: BridgeMessage) => {
      if (message.type === "app:info") {
        setAppInfo(message.payload as AppInfo);
      }

      if (message.type === "clips:list-result") {
        const payload = message.payload as { clips: ClipItem[] };
        setClips(payload.clips ?? []);
      }

      if (message.type === "devtools:generate-guid-result") {
        const payload = message.payload as { value: string };
        setGuidValue(payload.value);
      }

      if (message.type === "app:focus-search") {
        setIsDevToolsOpen(false);
        window.setTimeout(() => searchInputRef.current?.focus(), 0);
      }
    });

    tanoDevBridge.send({ type: "app:get-info" });
    requestClips();

    return unsubscribe;
  }, [requestClips]);

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
  }

  return (
    <div className="app-shell">
      <header className="mini-titlebar">
        <div className="brand">
          <div className="brand-icon">&gt;_</div>
          <div>
            <strong>{appInfo?.name ?? "TanoDev Clip"}</strong>
          </div>
        </div>
      </header>

      <section className="search-strip">
        <button
          className={isDevToolsOpen ? "dev-button active" : "dev-button"}
          onClick={() => setIsDevToolsOpen((current) => !current)}
          title="Dev Tools"
          aria-label="Toggle Dev Tools"
        >
          {"</>"}
        </button>
        <input
          ref={searchInputRef}
          value={query}
          onChange={(event) => setQuery(event.target.value)}
          onKeyDown={(event) => {
            if (event.key === "Enter") {
              handleSearchSubmit();
            }
          }}
          placeholder="grep clipboard"
        />
      </section>

      <section className="filter-row">
        <select
          value={clipType}
          onChange={(event) => handleTypeChange(event.target.value)}
          aria-label="Clip type"
        >
          {clipTypes.map((type) => (
            <option key={type} value={type}>
              {type}
            </option>
          ))}
        </select>
        <span>stdout: {clips.length}/100</span>
      </section>

      <main className="compact-main">
        <ClipboardView
          clips={clips}
          selectedClip={selectedClip}
          selectedClipId={selectedClip?.id ?? null}
          onSelectClip={setSelectedClipId}
          onCopyClip={handleCopyClip}
          onTogglePin={handleTogglePin}
        />

        {isDevToolsOpen && (
          <DevToolsView
            guidFormat={guidFormat}
            guidValue={guidValue}
            onGuidFormatChange={setGuidFormat}
            onGenerateGuid={handleGenerateGuid}
            onCopyGuid={handleCopyGuid}
          />
        )}
      </main>
    </div>
  );
}

function ClipboardView({
  clips,
  selectedClip,
  selectedClipId,
  onSelectClip,
  onCopyClip,
  onTogglePin,
}: {
  clips: ClipItem[];
  selectedClip: ClipItem | null;
  selectedClipId: string | null;
  onSelectClip: (id: string) => void;
  onCopyClip: (id: string) => void;
  onTogglePin: (id: string) => void;
}) {
  return (
    <>
      <div className="clip-list" aria-label="Clipboard history">
        {clips.length === 0 ? (
          <div className="empty-state">
            Copy text anywhere in Windows to fill the history.
          </div>
        ) : (
          clips.map((clip) => (
            <button
              key={clip.id}
              className={
                clip.id === selectedClipId ? "clip-item active" : "clip-item"
              }
              onClick={() => onSelectClip(clip.id)}
              onDoubleClick={() => onCopyClip(clip.id)}
            >
              <span className={`clip-type type-${clip.clipType.toLowerCase()}`}>
                {clip.clipType}
              </span>
              <span className="clip-body">
                <span className="clip-title">
                  {clip.isPinned && <span className="pin-mark">PIN</span>}
                  {clip.title || summarize(clip.content)}
                </span>
                <span className="clip-meta">
                  {clip.sourceApp ?? "Unknown"} | {formatDate(clip.createdAt)}
                </span>
              </span>
            </button>
          ))
        )}
      </div>

      <section className="clip-detail">
        {selectedClip ? (
          <>
            <div className="detail-actions">
              <span
                className={`clip-type type-${selectedClip.clipType.toLowerCase()}`}
              >
                {selectedClip.clipType}
              </span>
              <button onClick={() => onTogglePin(selectedClip.id)}>
                {selectedClip.isPinned ? "unpin" : "pin"}
              </button>
              <button
                className="primary-button"
                onClick={() => onCopyClip(selectedClip.id)}
              >
                copy
              </button>
            </div>
            <pre>{selectedClip.content}</pre>
          </>
        ) : (
          <div className="empty-state small">Select a clip to preview.</div>
        )}
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
    <section className="dev-drawer">
      <div className="drawer-title">
        <strong>./tools.sh</strong>
        <span>guid --format</span>
      </div>

      <div className="segmented-control">
        <button
          className={guidFormat === "default" ? "active" : ""}
          onClick={() => onGuidFormatChange("default")}
        >
          default
        </button>
        <button
          className={guidFormat === "no-hyphens" ? "active" : ""}
          onClick={() => onGuidFormatChange("no-hyphens")}
        >
          compact
        </button>
        <button
          className={guidFormat === "uppercase" ? "active" : ""}
          onClick={() => onGuidFormatChange("uppercase")}
        >
          upper
        </button>
      </div>

      <div className="guid-output">
        {guidValue || "550e8400-e29b-41d4-a716-446655440000"}
      </div>

      <div className="tool-actions">
        <button className="primary-button" onClick={onGenerateGuid}>
          generate
        </button>
        <button disabled={!guidValue} onClick={onCopyGuid}>
          copy
        </button>
      </div>
    </section>
  );
}

function summarize(content: string) {
  const compact = content.replace(/\s+/g, " ").trim();
  return compact.length > 72 ? `${compact.slice(0, 72)}...` : compact;
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
