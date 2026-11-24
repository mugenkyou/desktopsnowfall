import type { Metadata, Viewport } from "next";
import { Instrument_Serif, Manrope, Geist_Mono } from "next/font/google";
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
  "A free desktop snowfall app for Windows and macOS. Roll snowballs, build snowmen, meet winter visitors and decorate your windows with fairy lights.";

export const metadata: Metadata = {
  metadataBase: new URL(site.url),
  title: "Desktop Snowfall — Bring a Little Winter to Your Desktop",
  description,
  applicationName: site.name,
  keywords: ["desktop snow", "snowfall app", "Windows snow", "macOS snow", "snowman", "fairy lights", "winter desktop"],
  alternates: { canonical: "/" },
  openGraph: {
    type: "website",
    title: "Desktop Snowfall",
    description,
    siteName: site.name,
  },
  twitter: {
    card: "summary_large_image",
    title: "Desktop Snowfall",
    description,
  },
};

export const viewport: Viewport = {
  themeColor: "#f4f8fa",
};

export default function RootLayout({ children }: LayoutProps<"/">) {
  return (
    <html lang="en" className={`${instrument.variable} ${manrope.variable} ${geistMono.variable}`}>
      <body className="min-h-screen overflow-x-clip">{children}</body>
    </html>
  );
}
