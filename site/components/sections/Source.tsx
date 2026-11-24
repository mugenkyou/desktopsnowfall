import { site } from "@/lib/site";

// A real excerpt: the top of the settings block in windows/Program.cs
const excerpt = `static class Cfg
{
    public static int TargetFlakes = 260;          // number of flakes falling at the same time
    public const float ColW = 3;                   // width of one column of the snow height map
    public const float WindowPileMax = 16;         // max snow height on top of a window
    public const float GroundPileMax = 34;         // max snow height at the bottom of the screen
    public const float TaskbarPileMax = 18;        // max snow height on the taskbar
    public const float TaskbarGain = 2.0f;         // snow on the taskbar builds up this much faster
    public const float GroundBase = 12;            // the floor always keeps at least this much snow where it can be seen
    public const float Gravity = 900;              // gravity for snow sliding off
    public const float PollInterval = 1f / 30f;    // how often window positions are checked (s)
    public const int MaxClumps = 900;              // max number of falling snow clumps at once
    …`;

export function Source() {
  return (
    <section className="bg-paper py-24 md:py-36">
      <div className="mx-auto grid max-w-[1240px] gap-14 px-5 md:px-8 lg:grid-cols-12 lg:items-center">
        <div className="min-w-0 lg:col-span-5">
          <h2 className="display text-[clamp(2.6rem,5.4vw,4.4rem)]">
            Free to use.
            <br />
            <em>Open to explore.</em>
          </h2>
          <div className="mt-8 max-w-md space-y-4 leading-relaxed text-deep/80">
            <p>
              Every line of both apps is on GitHub: one Swift file for the Mac, a C# app for Windows, no game engine and
              no packages to install. Read it, build it, change a number and see what happens.
            </p>
            <p>
              It&rsquo;s free for personal and other noncommercial use under the{" "}
              <a href={site.license} className="underline decoration-glacier underline-offset-4 hover:decoration-sky">PolyForm Noncommercial License</a>.
              No subscription, no account, no ads. Selling it or using it in a paid product needs a separate license,
              so just ask.
            </p>
            <p>Ideas, bug reports and pull requests are all welcome.</p>
          </div>
          <div className="mt-9 flex flex-wrap gap-x-6 gap-y-3">
            <a href={site.repo} className="rounded-full bg-deep px-6 py-3.5 font-semibold text-snow transition-colors hover:bg-midnight">Source on GitHub</a>
            <a href={site.issues} className="py-3.5 font-semibold underline decoration-glacier decoration-2 underline-offset-[6px] hover:decoration-sky">Report a bug</a>
            <a href={site.contributing} className="py-3.5 font-semibold underline decoration-glacier decoration-2 underline-offset-[6px] hover:decoration-sky">Contribute</a>
          </div>
        </div>

        <figure className="arrive min-w-0 lg:col-span-7">
          <div className="print overflow-hidden bg-midnight">
            <div className="flex items-center justify-between border-b border-snow/10 px-5 py-3">
              <span className="label text-snow/50">windows/Program.cs</span>
              <span className="label hidden text-snow/30 sm:inline">the settings, at the very top</span>
            </div>
            <pre className="overflow-x-auto p-5 font-mono text-[0.78rem] leading-[1.75] text-ice/90">
              <code>{excerpt}</code>
            </pre>
          </div>
          <figcaption className="label mt-3 text-muted">Every number above changes how the snow behaves. Change one, build again.</figcaption>
        </figure>
      </div>
    </section>
  );
}
