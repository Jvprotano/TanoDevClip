export const siteConfig = {
  name: "TanoDev Clip",
  title: "TanoDev Clip — Clipboard manager for developers",
  description:
    "A fast, local-first clipboard manager for Windows, built for developers.",
  repositoryUrl: "https://github.com/Jvprotano/TanoDevClip",
  releasesUrl: "https://github.com/Jvprotano/TanoDevClip/releases",
  latestReleaseUrl: "https://github.com/Jvprotano/TanoDevClip/releases/latest",
  issuesUrl: "https://github.com/Jvprotano/TanoDevClip/issues",
  newIssueUrl: "https://github.com/Jvprotano/TanoDevClip/issues/new/choose",
  contributingUrl:
    "https://github.com/Jvprotano/TanoDevClip/blob/main/CONTRIBUTING.md",
  downloadUrl:
    "https://github.com/Jvprotano/TanoDevClip/releases/latest/download/ProtanoSoftware.TanoDevClip-win-Setup.exe",
  support: {
    label: "Buy me a coffee",
    stripeUrl: "https://buy.stripe.com/7sY7sMdiCfLJ3JPbpuak000",
    pix: {
      qrCodeImage: "media/pix-qr-code.jpg",
      copyPasteCode:
        "00020126580014BR.GOV.BCB.PIX01367deadba1-a8f0-4dba-a928-3f04dd546fdb5204000053039865802BR5925Jose Vinicius Protano Sil6009SAO PAULO62140510ficx01PrbO63046534",
    },
  },
  media: {
    // Add files under website/public/media and set the paths below without a leading slash.
    appPreviewImage: "media/app-preview.png",
    appPreviewVideo: "media/app-demo.mp4",
    devToolsPreviewImage: "media/devtools-preview.png",
    socialPreviewImage: "media/social-preview.png",
  },
} as const;

export function withBase(path = ""): string {
  const configuredBase = import.meta.env.BASE_URL;
  const base = configuredBase.endsWith("/")
    ? configuredBase
    : `${configuredBase}/`;
  return `${base}${path.replace(/^\/+/, "")}`;
}
