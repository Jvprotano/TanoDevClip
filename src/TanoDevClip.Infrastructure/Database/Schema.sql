CREATE TABLE IF NOT EXISTS clips (
    id TEXT PRIMARY KEY,
    content TEXT NOT NULL,
    content_hash TEXT NOT NULL,
    clip_type TEXT NOT NULL,
    title TEXT NULL,
    source_app TEXT NULL,
    source_window_title TEXT NULL,
    source_url TEXT NULL,
    is_pinned INTEGER NOT NULL DEFAULT 0,
    created_at TEXT NOT NULL,
    last_used_at TEXT NULL,
    use_count INTEGER NOT NULL DEFAULT 0
);

CREATE INDEX IF NOT EXISTS idx_clips_created_at 
ON clips(created_at DESC);

CREATE INDEX IF NOT EXISTS idx_clips_content_hash 
ON clips(content_hash);

CREATE INDEX IF NOT EXISTS idx_clips_clip_type 
ON clips(clip_type);

CREATE INDEX IF NOT EXISTS idx_clips_is_pinned
ON clips(is_pinned);
