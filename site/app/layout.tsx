import type { Metadata, Viewport } from "next";
import { Instrument_Serif, Manrope, Geist_Mono } from "next/font/google";
import Script from "next/script";
import { site } from "@/lib/site";
import "./globals.css";

const instrument = Instrument_Serif({
  variable: "--font-instrument",
  subsets: ["latin"],
  weight: "400",
  style: ["normal", "italic"],
});

const manrope = Manrope({
  variable: "--font-manrope",
  subsets: ["latin"],
});

const geistMono = Geist_Mono({
  variable: "--font-geist-mono",
  subsets: ["latin"],
});

const description =
  "Bring winter to your desktop with interactive snowfall, snowballs, snowmen, fairy lights and playful animal visitors. Free for Windows and macOS.";

export const metadata: Metadata = {
  metadataBase: new URL(site.url),
  title: "Desktop Snowfall – Interactive Snowfall for Windows & Mac",
  description,
  applicationName: site.name,
  authors: [{ name: "mugenkyou", url: "https://github.com/mugenkyou" }],
  keywords: [
    "desktop snowfall",
    "desktop snowfall app",
    "snowfall desktop application",
    "animated snow desktop",
    "snow overlay for Windows",
    "snowfall app for Windows",
    "snowfall app for macOS",
    "desktop winter effects",
    "snow animation for desktop",
    "interactive desktop snowfall",
    "free desktop snowfall",
    "desktop snowballs",
    "snowman desktop animation",
    "desktop fairy lights",
  ],
  alternates: { canonical: "/" },
  openGraph: {
    type: "website",
    title: "Desktop Snowfall – Interactive Snowfall for Windows & Mac",
    description,
    siteName: site.name,
    url: "/",
  },
  twitter: {
    card: "summary_large_image",
    title: "Desktop Snowfall – Interactive Snowfall for Windows & Mac",
    description,
  },
};

export const viewport: Viewport = {
  themeColor: "#f4f8fa",
};

const jsonLd = [
  {
    "@context": "https://schema.org",
    "@type": "Organization",
    name: "Desktop Snowfall",
    url: site.url,
    logo: `${site.url}/images/brand/icon.png`,
    sameAs: [site.repo],
  },
  {
    "@context": "https://schema.org",
    "@type": "WebSite",
    name: "Desktop Snowfall",
    url: site.url,
  },
  {
    "@context": "https://schema.org",
    "@type": "SoftwareApplication",
    name: "Desktop Snowfall",
    description:
      "A free desktop snowfall app for Windows and macOS. Roll snowballs, build snowmen, meet winter visitors and decorate your windows with fairy lights.",
    url: site.url,
    applicationCategory: "DesktopEnhancementApplication",
    operatingSystem: "Windows 10, Windows 11, macOS 12+",
    offers: {
      "@type": "Offer",
      price: "0",
      priceCurrency: "USD",
    },
    downloadUrl: site.download.windows,
    softwareVersion: site.version,
    author: {
      "@type": "Person",
      name: "mugenkyou",
      url: "https://github.com/mugenkyou",
    },
  },
];

export default function RootLayout({ children }: LayoutProps<"/">) {
  return (
    <html
      lang="en"
      className={`${instrument.variable} ${manrope.variable} ${geistMono.variable}`}
    >
      <body className="min-h-screen overflow-x-clip">
        <script
          type="application/ld+json"
          dangerouslySetInnerHTML={{ __html: JSON.stringify(jsonLd) }}
        />
        {children}
        <Script id="gtag-init" strategy="afterInteractive">
          {`window.dataLayer = window.dataLayer || [];
function gtag(){dataLayer.push(arguments);}
gtag('js', new Date());`}
        </Script>
        <Script
          src="https://www.googletagmanager.com/gtm.js?id=GTM-WN6W8WLP"
          strategy="afterInteractive"
        />
      </body>
    </html>
  );
}
