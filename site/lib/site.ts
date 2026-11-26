// Facts the page relies on, in one place. Every link here was checked against the live GitHub release.
const repo = "https://github.com/mugenkyou/desktopsnowfall";
const latest = `${repo}/releases/latest/download`;

export const site = {
  name: "Desktop Snowfall",
  repo,
  releases: `${repo}/releases`,
  issues: `${repo}/issues`,
  license: `${repo}/blob/main/LICENSE`,
  features: `${repo}/blob/main/FEATURES.md`,
  contributing: `${repo}/blob/main/CONTRIBUTING.md`,
  version: "1.1.3", // the latest published release (github.com/mugenkyou/desktopsnowfall/releases/tag/v1.1.3)
  download: {
    windows: `${latest}/desktopsnowfall-setup-x64.exe`,
    windowsPortableX64: `${latest}/desktopsnowfall-win-x64-portable.zip`,
    windowsPortableArm: `${latest}/desktopsnowfall-win-arm64-portable.zip`,
    mac: `${latest}/desktopsnowfall-mac.zip`,
    checksums: `${latest}/SHA256SUMS.txt`,
  },
  // Where the site will live; set SITE_URL when deploying (used for social previews and the sitemap)
  url: process.env.SITE_URL ?? "http://localhost:3000",
};
