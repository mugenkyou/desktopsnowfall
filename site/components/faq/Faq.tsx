import { site } from "@/lib/site";

const faq: [string, React.ReactNode][] = [
  [
    "Is Desktop Snowfall free?",
    <>Yes, free for personal and other noncommercial use, with no ads, account or subscription. Commercial use needs a separate license; ask on <a href={site.issues}>GitHub</a>.</>,
  ],
  [
    "Which platforms are supported?",
    <>Windows 10 and 11 (x64 and Arm) and macOS 12 or later (Apple silicon and Intel). The Windows version has more features today; see <a href="#platforms">the comparison</a>.</>,
  ],
  [
    "Does it work with multiple monitors?",
    <>On Windows, yes: snow falls on every screen, drifts from an upper screen into a lower one, and snowballs and visitors cross between them. Screens can be plugged in or out while it runs. The Mac version snows on the main screen only, for now.</>,
  ],
  [
    "Can I throw snowballs?",
    <>On Windows, yes: pick one up, flick the mouse and let go, or hold Shift, pull back and release to aim. On the Mac you can roll, push, carry and stack them, but throwing hasn&rsquo;t arrived yet.</>,
  ],
  [
    "How do I turn on the fairy lights?",
    <>Open the ❄ menu (system tray on Windows, menu bar on the Mac) and choose Fairy Lights. On Windows, Light style picks the colours and the pattern. They&rsquo;re off until you turn them on, and the app remembers.</>,
  ],
  [
    "Can I control how much it snows?",
    <>Yes: Light, Medium or Heavy, on both. Pause it or clear all the snow from the same menu. Windows also lets you switch off gusts, crystal flakes and snow on desktop icons, and pick a 30 fps battery saver.</>,
  ],
  [
    "Does it start automatically?",
    <>On Windows, if you want it to: tick it in the installer, or choose Start with Windows in the ❄ menu. The Mac version doesn&rsquo;t offer this yet; it&rsquo;s planned.</>,
  ],
  [
    "Can I build it from source?",
    <>Yes: <a href="#install">two commands on each platform</a>. Windows needs the .NET 8 SDK; the Mac needs the Xcode command line tools.</>,
  ],
  [
    "Does it need the internet?",
    <>No. It never connects to anything. Everything happens on your computer.</>,
  ],
];

export function Faq() {
  return (
    <section className="bg-snow py-24 md:py-32">
      <div className="mx-auto grid max-w-[1240px] gap-12 px-5 md:px-8 lg:grid-cols-12">
        <div className="lg:col-span-4">
          <p className="label text-sky">Questions</p>
          <h2 className="display mt-5 text-[clamp(2.4rem,4.6vw,3.8rem)]">
            Before you <em>ask.</em>
          </h2>
        </div>
        <div className="lg:col-span-8">
          {faq.map(([q, a]) => (
            <details key={q} className="group border-b border-hairline py-5 first:border-t">
              <summary className="flex cursor-pointer list-none items-center justify-between gap-6 text-[1.08rem] font-semibold marker:hidden">
                {q}
                <span aria-hidden className="text-xl text-sky transition-transform group-open:rotate-45">+</span>
              </summary>
              <p className="mt-3 max-w-2xl leading-relaxed text-deep/75 [&_a]:underline [&_a]:decoration-glacier [&_a]:underline-offset-4">{a}</p>
            </details>
          ))}
        </div>
      </div>
    </section>
  );
}
