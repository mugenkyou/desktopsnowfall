import Image from "next/image";
import desktop from "@/public/images/screenshots/desktop.png";
import { site } from "@/lib/site";

// From FEATURES.md and the two apps' source. "–" means not on that platform (yet).
const rows: [string, string, string][] = [
  ["Three layers of snowfall · Light / Medium / Heavy", "✓", "✓"],
  ["Snow on window tops and the screen bottom; dragging a window throws it off", "✓", "✓"],
  ["Snow on the taskbar / the Dock", "Taskbar", "Dock, with Accessibility permission"],
  ["Snow on desktop icons", "✓", "–"],
  ["Wind gusts · crystal and sparkling flakes", "✓", "–"],
  ["Roll, push and carry snowballs · build snowmen", "✓", "✓"],
  ["Weight, collisions, rolling trails, size modes", "✓", "–"],
  ["Throwing and Shift-aiming", "✓", "–"],
  ["Snowmen that wobble, lose their nose, fall over", "✓", "–"],
  ["Penguins, snow foxes and rabbits", "✓", "–"],
  ["Fairy lights", "6 styles · 6 patterns", "One style"],
  ["Snow on every screen", "✓", "Main screen"],
  ["Hides for fullscreen apps · pauses when locked", "✓", "Pauses when locked or asleep"],
  ["Start with the computer · battery saver", "✓", "–"],
];

const trayMenu = ["Pause snow", "Snow ▸", "Snowballs ▸", "Visitors 🐧 ▸", "Fairy Lights ✨", "Light style ▸", "Clear snow", "How to Play ❔", "Performance ▸", "Start with Windows", "Reset all settings", "Quit"];
const macMenu = ["Pause Snow", "Snow Amount ▸", "Fairy Lights ✨", "Clear Snow", "How to Play ❔", "Quit"];

export function Platforms() {
  return (
    <section id="platforms" className="bg-paper py-24 md:py-36">
      <div className="mx-auto max-w-[1240px] px-5 md:px-8">
        <h2 className="display max-w-3xl text-[clamp(2.8rem,6vw,5rem)]">
          Two desktop worlds.
          <br />
          <em>One little winter.</em>
        </h2>
        <p className="mt-7 max-w-xl leading-relaxed text-deep/80">
          Two native apps, written separately: C# talking straight to Win32 on Windows, Swift with SpriteKit on the Mac.
          The Windows version is further along today, and the table below says so plainly.
        </p>

        <div className="mt-16 grid gap-16 md:mt-24 lg:grid-cols-12 lg:gap-10">
          {/* Windows: the real screenshot with the real tray menu set beside it */}
          <figure className="arrive lg:col-span-7">
            <div className="flex items-baseline justify-between">
              <p className="font-display text-[2rem]">Windows</p>
              <p className="label text-muted">10 &amp; 11 · x64 and Arm</p>
            </div>
            <div className="relative mt-5">
              <div className="print relative aspect-[16/10] overflow-hidden">
                <Image src={desktop} alt="The Windows app: snow on desktop icons and along the taskbar, with snowmen and visitors" fill sizes="(min-width: 1024px) 700px, 96vw" quality={90} className="object-cover object-bottom" />
              </div>
              <div className="absolute -bottom-8 right-4 w-44 rounded-lg bg-snow/95 p-2 text-[0.8rem] shadow-xl ring-1 ring-hairline backdrop-blur md:right-[-1.5rem]">
                <p className="label px-2 pb-1.5 pt-1 text-muted">❄ tray menu</p>
                {trayMenu.map((m) => (
                  <p key={m} className="rounded px-2 py-[3px] text-deep/85">{m}</p>
                ))}
              </div>
            </div>
            <figcaption className="mt-14 max-w-md leading-relaxed text-deep/75">
              A ❄ in the system tray holds everything, and remembers your choices. The installer needs no administrator
              rights and can start the snow with Windows.
            </figcaption>
          </figure>

          {/* macOS: no screenshot exists yet, so a labelled illustration of the real menu */}
          <figure className="arrive lg:col-span-5 lg:pt-24">
            <div className="flex items-baseline justify-between">
              <p className="font-display text-[2rem]">macOS</p>
              <p className="label text-muted">12 or later · Apple silicon &amp; Intel</p>
            </div>
            <MacSketch />
            <figcaption className="mt-5 max-w-md leading-relaxed text-deep/75">
              A ❄︎ in the menu bar. Snow settles on windows and, once you allow Accessibility access, along the Dock.
              Snowballs roll, carry and stack into snowmen; the rest is still on its way.
            </figcaption>
            <p className="label mt-3 text-muted">Illustration of the Mac app&rsquo;s real menu, not a screenshot</p>
          </figure>
        </div>

        <div className="arrive mt-24 overflow-x-auto md:mt-32">
          <table className="w-full min-w-[620px] border-collapse text-left text-[0.95rem]">
            <caption className="label mb-5 text-left text-muted">
              What each version does today · <a className="underline decoration-glacier underline-offset-4" href={site.features}>the full list</a>
            </caption>
            <thead>
              <tr className="border-b border-deep/20">
                <th scope="col" className="py-3 pr-6 font-semibold">Feature</th>
                <th scope="col" className="w-48 py-3 pr-6 font-semibold">Windows</th>
                <th scope="col" className="w-56 py-3 font-semibold">macOS</th>
              </tr>
            </thead>
            <tbody>
              {rows.map(([f, w, m]) => (
                <tr key={f} className="border-b border-hairline">
                  <th scope="row" className="py-3 pr-6 font-normal text-deep/85">{f}</th>
                  <td className="py-3 pr-6">{w}</td>
                  <td className={`py-3 ${m === "–" ? "text-muted" : ""}`}>{m}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>
    </section>
  );
}

function MacSketch() {
  return (
    <div className="print relative mt-5 aspect-[4/3] overflow-hidden bg-[linear-gradient(180deg,#9ccbe4_0%,#c9e8f6_55%,#e8f3f8_100%)]">
      {/* menu bar */}
      <div className="absolute inset-x-0 top-0 flex h-6 items-center justify-end gap-4 bg-snow/70 px-3 text-[0.7rem] text-deep backdrop-blur">
        <span className="rounded bg-deep/10 px-1.5">❄︎</span>
        <span className="font-mono">12:00</span>
      </div>
      {/* the ❄︎ menu, with the Mac app's real items */}
      <div className="absolute right-10 top-7 w-40 rounded-lg bg-snow/95 p-1.5 text-[0.72rem] shadow-xl ring-1 ring-hairline">
        {macMenu.map((m, i) => (
          <p key={m} className={`rounded px-2 py-[3px] ${i === 2 ? "bg-sky text-snow" : "text-deep/85"}`}>{m}</p>
        ))}
      </div>
      {/* a window with snow on top */}
      <div className="absolute left-[8%] top-[26%] h-[44%] w-[52%] rounded-lg bg-snow shadow-lg ring-1 ring-black/5">
        <div className="absolute -top-2 left-2 right-2 h-3 rounded-full bg-white shadow-[0_2px_4px_rgb(131_203_233/0.5)]" />
        <div className="flex gap-1.5 p-2.5">
          {["#ff5f57", "#febc2e", "#28c840"].map((c) => <span key={c} className="h-2.5 w-2.5 rounded-full" style={{ background: c }} />)}
        </div>
      </div>
      {/* the Dock, with its own little drift */}
      <div className="absolute bottom-3 left-1/2 h-11 w-[64%] -translate-x-1/2 rounded-2xl bg-snow/50 ring-1 ring-white/60 backdrop-blur">
        <div className="absolute -top-1.5 left-3 right-3 h-2.5 rounded-full bg-white" />
        <div className="flex h-full items-center justify-center gap-2 px-3">
          {["#4CAEDC", "#F3C979", "#426E61", "#E8575F", "#83CBE9"].map((c) => <span key={c} className="h-7 w-7 rounded-lg" style={{ background: c }} />)}
        </div>
      </div>
      {/* a snowman built on the screen bottom */}
      <svg viewBox="0 0 40 60" className="absolute bottom-16 right-[12%] w-8" aria-hidden>
        <circle cx="20" cy="46" r="12" fill="#fff" />
        <circle cx="20" cy="26" r="9" fill="#fff" />
        <circle cx="20" cy="11" r="7" fill="#fff" />
        <rect x="12" y="16" width="16" height="3" rx="1.5" fill="#C9474F" />
        <path d="M20 11 l6 1 -6 1z" fill="#F28C28" />
      </svg>
    </div>
  );
}
