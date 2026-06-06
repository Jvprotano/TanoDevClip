export type AppInfo = {
  name: string;
  version: string;
  environment: string;
  hotkey: string;
};

export type ClipItem = {
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

export type ToolKind = "guid" | "string" | "lorem";
export type GuidFormat = "default" | "no-hyphens" | "uppercase";
export type LoremMode = "words" | "characters";
