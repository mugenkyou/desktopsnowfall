import Image from "next/image";
import { Snowfall } from "@/components/animations/Snowfall";
import { Pine } from "@/components/illustrations/Landscape";
import { site } from "@/lib/site";

// Where the notes point on the real screenshot (percent of its width and height)
const notes = [
  { x: 17, y: 12, text: "A little snowfall.", side: "right" },
  { x: 54.2, y: 19, text: "Snow, settling on a desktop icon.", side: "right" },
  { x: 13, y: 89, text: "Three snowballs later.", side: "right" },
  { x: 72, y: 94.2, text: "Winter, right on the taskbar.", side: "left" },
] as const;

export function Hero() {
  return (
    <section id="top" className="relative overflow-hidden bg-[linear-gradient(180deg,#e3f0f6_0%,#eef5f9_38%,#f4f8fa_70%)] pt-28 md:pt-36">
      <Snowfall count={44} seed={7} color="#9fd2ea" maxSize={6} />

      <div className="relative mx-auto max-w-[1240px] px-5 md:px-8">
        <p className="label text-muted">A free desktop experience · Windows + macOS</p>

        <div className="mt-6 grid gap-10 lg:grid-cols-12 lg:items-end">
          <h1 className="display text-[clamp(4.2rem,13vw,10.5rem)] text-deep lg:col-span-7">
            Let a little
            <br />
            <em className="text-sky">winter</em> in.
          </h1>

          <div className="max-w-[26rem] pb-3 lg:col-span-5 lg:justify-self-end">
            <p className="text-[1.125rem] leading-relaxed text-deep/85">
              Snow falls. Windows gather frost. Snowballs roll. Somewhere on your desktop, a penguin is having a very
              good day.
            </p>
            <div className="mt-8 flex flex-wrap items-center gap-x-6 gap-y-4">
              <a
                href="#install"
                className="group inline-flex items-center gap-3 rounded-full bg-deep py-3.5 pl-6 pr-5 font-semibold text-snow transition-colors hover:bg-midnight"
              >
                Bring on the snow
                <span aria-hidden className="transition-transform group-hover:translate-y-0.5">↓</span>
              </a>
              <a href={site.repo} className="font-semibold text-deep underline decoration-glacier decoration-2 underline-offset-[6px] hover:decoration-sky">
                Explore the project
              </a>
            </div>
            <p className="label mt-6 text-muted">Version {site.version} · Free for personal use</p>
          </div>
        </div>
      </div>

      {/* The screenshot, set into a small winter landscape and shifted off-centre */}
      <div className="relative mt-16 md:mt-20">
        <svg aria-hidden viewBox="0 0 1440 360" preserveAspectRatio="none" className="absolute inset-x-0 bottom-0 h-[42%] w-full">
          <path d="M0,170 C 200,120 380,150 560,128 C 760,104 960,150 1150,120 C 1280,100 1380,112 1440,108 L1440,360 L0,360 Z" fill="#dcedf6" />
          <path d="M0,250 C 240,210 520,246 760,226 C 1010,206 1220,240 1440,220 L1440,360 L0,360 Z" fill="#f9fcfe" />
        </svg>
        <svg aria-hidden viewBox="0 0 300 220" className="absolute bottom-[14%] left-[1%] hidden w-[19%] md:block">
          <Pine x={70} base={206} h={190} />
          <Pine x={175} base={210} h={130} />
          <Pine x={250} base={212} h={80} color="#8fb9c9" />
        </svg>

        <figure className="relative mx-auto max-w-[1240px] px-5 md:px-8">
          <div className="relative md:ml-[14%] md:mr-[-6%]">
            <video
              src="/videos/mainvideo.mp4"
              poster="/videos/mainvideo.jpg"
              autoPlay
              muted
              loop
              playsInline
              aria-label="A real Windows desktop running Desktop Snowfall: snow falling over a dark wallpaper, snow on top of the desktop icons, snowmen, and a rabbit, a penguin and a snow fox along the taskbar"
              className="print aspect-video h-auto w-full"
            />
            {notes.map((n) => (
              <span
                key={n.text}
                className="absolute hidden items-center gap-2 lg:flex"
                style={{ left: `${n.x}%`, top: `${n.y}%`, transform: n.side === "left" ? "translate(-100%, -50%)" : "translate(0, -50%)", flexDirection: n.side === "left" ? "row-reverse" : "row" }}
              >
                <span className="h-2 w-2 shrink-0 rounded-full bg-warm ring-4 ring-warm/25" />
                <span className="h-px w-8 bg-warm/70" />
                <span className="label whitespace-nowrap rounded bg-midnight/70 px-2 py-1 text-[0.65rem] text-snow backdrop-blur-sm">{n.text}</span>
              </span>
            ))}
          </div>
          <figcaption className="mt-4 flex flex-wrap justify-between gap-2 md:ml-[14%]">
            <span className="label text-muted">Fig. 1 — A real desktop, a few minutes in</span>
            <span className="label text-muted lg:hidden">Snow on the icons, the taskbar and two snowmen</span>
          </figcaption>
        </figure>
        <div className="h-16 md:h-24" />
      </div>
    </section>
  );
}
