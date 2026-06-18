import type { ClipItem } from "../types";
import { formatDate, summarize } from "../utils/format";

type ClipboardViewProps = {
  clips: ClipItem[];
  selectedClip: ClipItem | null;
  selectedClipId: string | null;
  isCollapsed: boolean;
  onSelectClip: (id: string) => void;
  onPasteClip: (id: string) => void;
  onOpenSettings: () => void;
  onTogglePin: (id: string) => void;
  onCollapse: (isCollapsed: boolean) => void;
};

export function ClipboardView({
  clips,
  selectedClip,
  selectedClipId,
  isCollapsed,
  onSelectClip,
  onPasteClip,
  onOpenSettings,
  onTogglePin,
  onCollapse,
}: ClipboardViewProps) {
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
              title="double click to paste"
              className={
                clip.id === selectedClipId ? "clip-item active" : "clip-item"
              }
              onClick={() => onSelectClip(clip.id)}
              onDoubleClick={() => onPasteClip(clip.id)}
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

      <section
        className={`clip-detail ${isCollapsed ? "is-collapsed" : "is-expanded"}`}
      >
        {selectedClip && (
          <>
            <div className="detail-actions">
              <div className="detail-left">
                <span
                  className={`clip-type type-${selectedClip.clipType.toLowerCase()}`}
                >
                  {selectedClip.clipType}
                </span>
              </div>

              <button
                className="collapse-btn"
                onClick={() => onCollapse(!isCollapsed)}
                aria-expanded={!isCollapsed}
                title={isCollapsed ? "Show copied text" : "Hide copied text"}
              >
                <span aria-hidden="true">{isCollapsed ? "^^" : "vv"}</span>
              </button>

              <div className="detail-right">
                <button onClick={() => onTogglePin(selectedClip.id)}>
                  {selectedClip.isPinned ? "unpin" : "pin"}
                </button>
                <button
                  className="settings-gear-button"
                  onClick={onOpenSettings}
                  title="Settings"
                  aria-label="Open settings"
                >
                  ⚙
                </button>
              </div>
            </div>
            {!isCollapsed && <pre>{selectedClip.content}</pre>}
          </>
        )}
      </section>
    </>
  );
}
