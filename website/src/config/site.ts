export const siteConfig = {
  name: "TanoDev Clip",
  title: "TanoDev Clip — Clipboard manager for developers",
  description: "A fast, local-first clipboard manager for Windows, built for developers.",
  repositoryUrl: "https://github.com/Jvprotano/TanoDevClip",
  releasesUrl: "https://github.com/Jvprotano/TanoDevClip/releases",
  latestReleaseUrl: "https://github.com/Jvprotano/TanoDevClip/releases/latest",
  issuesUrl: "https://github.com/Jvprotano/TanoDevClip/issues",
  newIssueUrl: "https://github.com/Jvprotano/TanoDevClip/issues/new/choose",
  contributingUrl: "https://github.com/Jvprotano/TanoDevClip/blob/main/CONTRIBUTING.md",
  downloadUrl:
    "https://github.com/Jvprotano/TanoDevClip/releases/latest/download/ProtanoSoftware.TanoDevClip-Setup.exe",
  support: {
    label: "Buy me a coffee",
    // Add a Buy Me a Coffee, Ko-fi, GitHub Sponsors, or another support URL here.
    // The button remains visible but disabled while this value is empty.
    url: "",
  },
  media: {
    // Add files under website/public/media and set the paths below without a leading slash.
    appPreviewImage: "",
    appPreviewVideo: "",
    devToolsPreviewImage: "",
    socialPreviewImage: "",
  },
} as const;

export function withBase(path = ""): string {
  const configuredBase = import.meta.env.BASE_URL;
  const base = configuredBase.endsWith("/") ? configuredBase : `${configuredBase}/`;
  return `${base}${path.replace(/^\/+/, "")}`;
}
