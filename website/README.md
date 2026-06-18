# TanoDev Clip website

The landing page is an Astro static site deployed through GitHub Pages.

## Local development

```powershell
cd website
npm ci
npm run dev
```

Validate and build the production site:

```powershell
npm run check
npm run build
```

## Project links and support

Edit:

```text
src/config/site.ts
```

This file contains the download URL, repository links, contribution link, support links, PIX data, and optional media paths.

The support area uses `siteConfig.support.stripeUrl` for the Buy me a coffee button and `siteConfig.support.pix` for the QR Code plus PIX copia e cola. Place the QR Code image under `public/media/` and configure the relative path without a leading slash.

## Screenshots and videos

Place public media files under:

```text
public/media/
```

Then configure their paths in `src/config/site.ts`, without a leading slash. Example:

```ts
media: {
  appPreviewImage: "media/app-preview.png",
  appPreviewVideo: "media/app-demo.mp4",
  devToolsPreviewImage: "media/devtools-preview.png",
  socialPreviewImage: "media/social-preview.png",
}
```

The main preview prefers the configured video and uses the image as its poster. If neither is configured, the page renders a deliberate placeholder without making a broken network request.

Recommended assets:

- Product preview image: 16:9 PNG or WebP, at least 1440 px wide.
- Product demo: short muted MP4 or WebM showing copy, open, search, and paste.
- DevTools preview: PNG or WebP showing the actual in-app drawer.
- Social preview: 1200 × 630 PNG for Open Graph and social sharing.

Do not use mock screenshots that could be mistaken for the real application.
