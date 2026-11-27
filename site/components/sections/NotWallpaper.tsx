import Image from "next/image";
import desktop from "@/public/images/screenshots/desktop.png";

// The right-hand side of the real desktop, closer up. Pins are percentages of the cropped frame.
const pins = [
  { n: "01", x: 82.2, y: 19.5 },
  { n: "03", x: 56.8, y: 31.2 },
];

export function NotWallpaper() {
  return (
    <section id="experience" className="relative overflow-x-clip bg-snow py-24 md:py-36">
      <div className="mx-auto grid max-w-[1240px] gap-14 px-5 md:px-8 lg:grid-cols-12 lg:gap-10">
        <div className="lg:col-span-4 lg:pt-6">
          <p className="label text-sky">On top of your real desktop</p>
          <h2 className="display mt-5 text-[clamp(3rem,6.4vw,5.4rem)]">
            Not another
            <br />
            <em>wallpaper.</em>
          </h2>
          <p className="mt-7 max-w-sm text-[1.0625rem] leading-relaxed text-deep/80">
            Desktop Snowfall is a native desktop application that lives on top of your actual desktop — not a wallpaper, screensaver, or browser extension. It reacts to your windows, your mouse and the little things you do. Everywhere but the snow itself, your clicks go straight through to whatever is underneath.
          </p>

          <ol className="mt-12 space-y-9 border-l border-hairline pl-6">
            <li>
              <p className="label text-sky">01 — Snow settles.</p>
              <p className="mt-2 max-w-sm leading-relaxed text-deep/80">
                It gathers on the tops of windows, on desktop icons and along the bottom of the screen, wherever nothing
                covers it.
              </p>
            </li>
            <li>
              <p className="label text-sky">02 — Windows move.</p>
              <div className="mt-2 flex items-start gap-4">
                <p className="max-w-[15rem] leading-relaxed text-deep/80">
                  Drag a window and its snow flies off. The faster you drag, the farther it goes.
                </p>
                <WindowFling />
              </div>
            </li>
            <li>
              <p className="label text-sky">03 — The desktop comes alive.</p>
              <p className="mt-2 max-w-sm leading-relaxed text-deep/80">
                The snow rolls into snowballs, snowballs stack into snowmen, and now and then a small visitor wanders by.
              </p>
            </li>
          </ol>
        </div>

        <figure className="arrive lg:col-span-8 lg:-mr-[8%]">
          <div className="print relative aspect-[1/1] overflow-hidden md:aspect-[16/13]">
            <Image
              src={desktop}
              alt="Close-up of the same desktop: snow resting on top of the Recycle Bin and a folder, a snowman standing on the folder, and a fox walking along the snowy taskbar"
              fill
              sizes="(min-width: 1024px) 760px, 96vw"
              quality={90}
              className="object-cover object-[100%_100%]"
            />
            {pins.map((p) => (
              <span key={p.n} className="absolute hidden -translate-x-1/2 -translate-y-1/2 md:block" style={{ left: `${p.x}%`, top: `${p.y}%` }}>
                <span className="label flex h-7 w-7 items-center justify-center rounded-full bg-warm text-[0.62rem] text-midnight shadow-lg">{p.n}</span>
              </span>
            ))}
          </div>
          <figcaption className="label mt-4 text-muted">Fig. 2 — The same desktop, closer: an icon, a folder, a snowman, a fox</figcaption>
        </figure>
      </div>
    </section>
  );
}

// A tiny diagram: a window being dragged, its snow thrown up and back
function WindowFling() {
  return (
    <svg viewBox="0 0 120 70" className="h-[70px] w-[120px] shrink-0" aria-hidden>
      <rect x="44" y="28" width="62" height="36" rx="4" fill="#fff" stroke="#83CBE9" />
      <rect x="20" y="30" width="62" height="36" rx="4" fill="none" stroke="#C9E8F6" strokeDasharray="3 3" />
      <path d="M30,24 L40,24 M60,20 L84,20" stroke="#83CBE9" strokeWidth="1.2" markerEnd="url(#arr)" />
      {[ [28, 16, 2.2], [20, 9, 1.8], [12, 18, 1.6], [36, 6, 1.4], [8, 8, 1.2] ].map(([x, y, r], i) => (
        <circle key={i} cx={x} cy={y} r={r} fill="#83CBE9" />
      ))}
      <defs>
        <marker id="arr" viewBox="0 0 6 6" refX="5" refY="3" markerWidth="6" markerHeight="6" orient="auto">
          <path d="M0,0 L6,3 L0,6" fill="none" stroke="#83CBE9" />
        </marker>
      </defs>
    </svg>
  );
}
