"use client";

import Image from "next/image";
import { useEffect, useState } from "react";
import icon from "@/public/images/brand/icon.png";
import { site } from "@/lib/site";

const links = [
  { href: "#experience", label: "Experience" },
  { href: "#features", label: "Features" },
  { href: "#platforms", label: "Platforms" },
  { href: "#install", label: "Installation" },
];

export function Nav() {
  const [scrolled, setScrolled] = useState(false);
  const [open, setOpen] = useState(false);

  useEffect(() => {
    const onScroll = () => setScrolled(window.scrollY > 24);
    onScroll();
    window.addEventListener("scroll", onScroll, { passive: true });
    return () => window.removeEventListener("scroll", onScroll);
  }, []);

  useEffect(() => {
    if (!open) return;
    const onKey = (e: KeyboardEvent) => e.key === "Escape" && setOpen(false);
    window.addEventListener("keydown", onKey);
    return () => window.removeEventListener("keydown", onKey);
  }, [open]);

  return (
    <header
      className={`fixed inset-x-0 top-0 z-50 transition-[background-color,box-shadow,backdrop-filter] duration-300 ${
        scrolled || open
          ? "bg-paper/85 shadow-[0_1px_0_rgb(23_51_75/0.08)] backdrop-blur-md"
          : "bg-transparent"
      }`}
    >
      <nav aria-label="Main" className="mx-auto flex h-16 max-w-[1240px] items-center justify-between px-5 md:px-8">
        <a href="#top" className="flex items-center gap-2.5" onClick={() => setOpen(false)}>
          <Image src={icon} alt="" width={28} height={28} className="rounded-[7px]" preload />
          <span className="font-display text-[1.35rem] leading-none tracking-tight">Desktop Snowfall</span>
        </a>

        <ul className="hidden items-center gap-8 text-[0.9rem] text-deep/80 lg:flex">
          {links.map((l) => (
            <li key={l.href}>
              <a href={l.href} className="transition-colors hover:text-deep">
                {l.label}
              </a>
            </li>
          ))}
        </ul>

        <div className="hidden items-center gap-5 lg:flex">
          <a href={site.repo} className="text-[0.9rem] text-deep/80 transition-colors hover:text-deep">
            GitHub
          </a>
          <a
            href="#install"
            className="rounded-full bg-deep px-4 py-2 text-[0.85rem] font-semibold text-snow transition-colors hover:bg-midnight"
          >
            Download
          </a>
        </div>

        <button
          type="button"
          className="-mr-2 flex h-11 w-11 items-center justify-center lg:hidden"
          aria-expanded={open}
          aria-controls="mobile-menu"
          aria-label={open ? "Close menu" : "Open menu"}
          onClick={() => setOpen((o) => !o)}
        >
          <span className="relative block h-3 w-5">
            <span className={`absolute left-0 h-px w-5 bg-deep transition-transform ${open ? "top-1.5 rotate-45" : "top-0"}`} />
            <span className={`absolute left-0 h-px w-5 bg-deep transition-transform ${open ? "top-1.5 -rotate-45" : "top-3"}`} />
          </span>
        </button>
      </nav>

      <div id="mobile-menu" hidden={!open} className="border-t border-hairline bg-paper/95 px-5 pb-6 backdrop-blur-md lg:hidden">
        <ul className="flex flex-col">
          {links.map((l) => (
            <li key={l.href}>
              <a href={l.href} onClick={() => setOpen(false)} className="block border-b border-hairline py-4 font-display text-2xl">
                {l.label}
              </a>
            </li>
          ))}
        </ul>
        <div className="mt-5 flex items-center gap-3">
          <a href="#install" onClick={() => setOpen(false)} className="rounded-full bg-deep px-5 py-3 text-sm font-semibold text-snow">
            Download
          </a>
          <a href={site.repo} className="rounded-full px-5 py-3 text-sm font-semibold text-deep ring-1 ring-hairline">
            GitHub
          </a>
        </div>
      </div>
    </header>
  );
}
