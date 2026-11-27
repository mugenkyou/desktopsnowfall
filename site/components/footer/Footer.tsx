import Image from "next/image";
import icon from "@/public/images/brand/icon.png";
import { site } from "@/lib/site";

export function Footer() {
  return (
    <footer className="bg-snow">
      <div className="mx-auto grid max-w-[1240px] gap-10 px-5 py-14 md:grid-cols-12 md:px-8">
        <div className="md:col-span-5">
          <div className="flex items-center gap-2.5">
            <Image src={icon} alt="" width={30} height={30} className="rounded-[8px]" />
            <span className="font-display text-[1.45rem] leading-none">Desktop Snowfall</span>
          </div>
          <p className="mt-4 max-w-xs text-[0.95rem] leading-relaxed text-muted">
            Desktop Snowfall brings interactive snow, snowballs, snowmen and fairy lights to your Windows and macOS desktop. Free, open-source, and made by mugenkyou.
          </p>
        </div>
        <nav aria-label="Footer" className="grid grid-cols-2 gap-x-8 gap-y-2.5 text-[0.95rem] md:col-span-5 md:col-start-8">
          <a href="#experience" className="hover:text-sky">Experience</a>
          <a href="#features" className="hover:text-sky">Features</a>
          <a href="#platforms" className="hover:text-sky">Platform Comparison</a>
          <a href="#install" className="hover:text-sky">Installation</a>
          <a href={site.download.windows} className="hover:text-sky">Download for Windows</a>
          <a href={site.download.mac} className="hover:text-sky">Download for macOS</a>
          <a href={site.repo} className="hover:text-sky">GitHub</a>
          <a href={site.license} className="hover:text-sky">License</a>
          <a href={site.issues} className="hover:text-sky">Report a bug</a>
          <a href={site.features} className="hover:text-sky">Every feature</a>
        </nav>
      </div>
      <div className="mx-auto flex max-w-[1240px] flex-wrap justify-between gap-3 border-t border-hairline px-5 py-6 md:px-8">
        <p className="label text-muted">Version {site.version}</p>
        <p className="label text-muted">Free for personal use · PolyForm Noncommercial 1.0.0</p>
      </div>
    </footer>
  );
}
