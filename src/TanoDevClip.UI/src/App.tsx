import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import { tanoDevBridge, type BridgeMessage } from "./bridge/tanoDevBridge";
import { ClipboardView } from "./components/ClipboardView";
import { DevToolsView, type DevToolPayload } from "./components/DevToolsView";
import { clipTypes } from "./constants";
import type {
  AppInfo,
  ClipItem,
  ToolKind,
  ToolResult,
} from "./types";

export default function App() {
  const searchInputRef = useRef<HTMLInputElement>(null);
  const [isDevToolsOpen, setIsDevToolsOpen] = useState(false);
  const [isCollapsed, setIsCollapsed] = useState(true);
  const [appInfo, setAppInfo] = useState<AppInfo | null>(null);
  const [bridgeAvailable] = useState(() => tanoDevBridge.isAvailable());
  const [clips, setClips] = useState<ClipItem[]>([]);
  const [selectedClipId, setSelectedClipId] = useState<string | null>(null);
  const [query, setQuery] = useState("");
  const [clipType, setClipType] = useState("All");
  const [status, setStatus] = useState("Ready");
  const [activeTool, setActiveTool] = useState<ToolKind>("guid");
  const [toolResult, setToolResult] = useState<ToolResult>({
    status: "ok",
    value: "",
  });

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
        setStatus(`${payload.clips?.length ?? 0} clips`);
      }

      if (message.type === "devtools:run-result") {
        const payload = message.payload as ToolResult;
        setToolResult(payload);
      }

      if (message.type === "clips:updated") {
        const payload = message.payload as { reason?: string } | undefined;
        setStatus(payload?.reason ? `Updated: ${payload.reason}` : "Updated");
      }

      if (message.type === "app:error") {
        const payload = message.payload as { message?: string } | undefined;
        setStatus(payload?.message ?? "Host error");
      }

      if (message.type === "app:focus-search") {
        setIsDevToolsOpen(false);
        window.setTimeout(() => searchInputRef.current?.focus(), 0);
      }
    });

    function handleKeyDown(event: KeyboardEvent) {
      const target = event.target as HTMLElement;

      const isTypingSomewhere =
        target.tagName === "INPUT" ||
        target.tagName === "TEXTAREA" ||
        target.tagName === "SELECT" ||
        target.isContentEditable;

      const isCtrlD =
        event.ctrlKey &&
        !event.altKey &&
        !event.shiftKey &&
        !event.metaKey &&
        event.key.toLowerCase() === "d";

      if (isCtrlD) {
        event.preventDefault();
        setIsDevToolsOpen((current) => !current);
        return;
      }

      if (isTypingSomewhere) return;
      if (event.ctrlKey || event.altKey || event.metaKey) return;
      if (event.key.length !== 1) return;

      event.preventDefault();

      searchInputRef.current?.focus();
      setQuery((current) => current + event.key);
    }

    tanoDevBridge.send({ type: "app:get-info" });
    requestClips();

    window.addEventListener("keydown", handleKeyDown);

    return () => {
      unsubscribe();
      window.removeEventListener("keydown", handleKeyDown);
    };
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
    setStatus("Copied");
  }

  function handlePasteClip(id: string) {
    tanoDevBridge.send({ type: "clips:paste", payload: { id } });
    setStatus("Pasted");
  }

  function handleTogglePin(id: string) {
    tanoDevBridge.send({ type: "clips:toggle-pin", payload: { id } });
  }

  const handleRunDevTool = useCallback((payload: DevToolPayload) => {
    tanoDevBridge.send({ type: "devtools:run", payload });
  }, []);

  const handleCopyGenerated = useCallback((content: string, kind: ToolKind) => {
    if (!content) {
      return;
    }

    tanoDevBridge.send({
      type: "devtools:copy-generated",
      payload: {
        content,
        kind,
      },
    });
    setStatus(`${kind} copied`);
  }, []);

  function handleHideApp() {
    tanoDevBridge.send({ type: "app:hide" });
  }

  function handleOpenDevTools() {
    setIsDevToolsOpen((current) => !current);
  }

  return (
    <div className="app-shell">
      <header
        className="mini-titlebar"
        onMouseDown={(event) => {
          if (event.button !== 0) return;

          const target = event.target as HTMLElement;
          const isInteractiveElement = target.closest(
            "button, input, textarea, select, a, [data-no-drag]",
          );

          if (isInteractiveElement) return;

          tanoDevBridge.send({ type: "app:drag-window" });
        }}
      >
        <div className="brand">
          <div className="brand-icon">&gt;_</div>
          <div>
            <strong>{appInfo?.name ?? "TanoDev Clip"}</strong>
            <span>
              {appInfo?.hotkey ?? "Ctrl+Alt+Space"} |{" "}
              {bridgeAvailable ? "host" : "preview"}
            </span>
          </div>
        </div>

        <div className="title-actions">
          <button
            className={
              isDevToolsOpen ? "config-button active" : "config-button"
            }
            onClick={handleOpenDevTools}
            title="Dev Tools | Toggle Ctrl + D"
            aria-label="Open Dev Tools"
          >
            {"</>"}
          </button>
          <button
            className="close-button"
            onClick={handleHideApp}
            title="Hide TanoDev Clip"
            aria-label="Hide TanoDev Clip"
          >
            x
          </button>
        </div>
      </header>

      <section className="search-strip">
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
        <span>
          stdout: {clips.length}/100 | {status}
        </span>
      </section>

      <main className="compact-main">
        <ClipboardView
          clips={clips}
          selectedClip={selectedClip}
          selectedClipId={selectedClip?.id ?? null}
          isCollapsed={isCollapsed}
          onSelectClip={setSelectedClipId}
          onCopyClip={handleCopyClip}
          onPasteClip={handlePasteClip}
          onTogglePin={handleTogglePin}
          onCollapse={setIsCollapsed}
        />

        {isDevToolsOpen && (
          <DevToolsView
            activeTool={activeTool}
            result={toolResult}
            onToolChange={setActiveTool}
            onRun={handleRunDevTool}
            onCopy={handleCopyGenerated}
          />
        )}
      </main>
    </div>
  );
}
