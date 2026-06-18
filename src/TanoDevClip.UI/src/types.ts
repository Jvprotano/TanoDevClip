export type AppInfo = {
  name: string;
  version: string;
  environment: string;
  hotkey: string;
  settings?: AppSettings;
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

export type ToolKind =
  | "guid"
  | "cpf"
  | "cnpj"
  | "lorem"
  | "string"
  | "jwt"
  | "json"
  | "base64"
  | "url"
  | "regex";
export type GuidFormat = "default" | "no-hyphens" | "uppercase";

export type ToolResult = {
  status: "ok" | "error";
  value: string;
};

export type AppSettings = {
  hotKey: string;
  enabledTools: ToolKind[];
  defaults: {
    hotKey: string;
    enabledTools: ToolKind[];
  };
};

export type AppSettingsUpdate = {
  hotKey: string;
  enabledTools: ToolKind[];
};
