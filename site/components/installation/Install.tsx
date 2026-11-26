import { site } from "@/lib/site";

function Step({ n, children }: { n: number; children: React.ReactNode }) {
  return (
    <li className="grid grid-cols-[2rem_1fr] gap-2">
      <span className="label pt-1 text-sky">{String(n).padStart(2, "0")}</span>
      <span className="leading-relaxed text-deep/85">{children}</span>
    </li>
  );
}

function Code({ children }: { children: string }) {
  return (
    <pre className="overflow-x-auto rounded-lg bg-midnight p-5 font-mono text-[0.82rem] leading-relaxed text-ice">
      <code>{children}</code>
    </pre>
  );
}

export function Install() {
  return (
    <section id="install" className="bg-snow py-24 md:py-36">
      <div className="mx-auto max-w-[1240px] px-5 md:px-8">
        <div className="grid gap-8 lg:grid-cols-12">
          <h2 className="display text-[clamp(2.8rem,6vw,5rem)] lg:col-span-8">
            Your winter is closer
            <br />
            <em>than you think.</em>
          </h2>
          <p className="self-end leading-relaxed text-deep/80 lg:col-span-4">
            Version <span className="font-semibold">{site.version}</span>, ready to download. No account, no setup, and
            nothing it ever needs from the internet.{" "}
            <a href={site.releases} className="underline decoration-glacier underline-offset-4 hover:decoration-sky">All releases</a>
          </p>
        </div>

        <div className="mt-16 grid gap-16 md:mt-20 lg:grid-cols-2 lg:gap-0">
          {/* Windows */}
          <article className="lg:border-r lg:border-hairline lg:pr-14">
            <p className="label text-muted">Windows 10 or 11 · x64 or Arm</p>
            <h3 className="mt-3 font-display text-[2.6rem] leading-none">Windows</h3>
            <a href={site.download.windows} className="group mt-8 flex items-center justify-between rounded-2xl bg-deep px-6 py-5 text-snow transition-colors hover:bg-midnight">
              <span>
                <span className="block text-[1.1rem] font-semibold">Download the installer</span>
                <span className="label mt-1 block text-ice/70">desktopsnowfall-setup-x64.exe · 6.5 MB</span>
              </span>
              <span aria-hidden className="text-2xl transition-transform group-hover:translate-y-0.5">↓</span>
            </a>
            <p className="mt-4 text-[0.92rem] text-deep/70">
              Or a single program with nothing to install:{" "}
              <a className="underline decoration-glacier underline-offset-4" href={site.download.windowsPortableX64}>portable x64</a> ·{" "}
              <a className="underline decoration-glacier underline-offset-4" href={site.download.windowsPortableArm}>portable Arm</a>
            </p>
            <ol className="mt-10 space-y-4">
              <Step n={1}>Open the installer. It installs just for you, so it never asks for administrator rights.</Step>
              <Step n={2}>
                Windows may say <q>Windows protected your PC</q>: the app isn&rsquo;t code-signed yet. Click <b>More info</b>, then <b>Run anyway</b>.
              </Step>
              <Step n={3}>Snow starts falling. The ❄ in the system tray (bottom right, perhaps behind the <b>^</b>) holds every setting.</Step>
            </ol>
            <details className="group mt-8 border-t border-hairline pt-5">
              <summary className="cursor-pointer list-none font-semibold marker:hidden">
                <span className="mr-2 inline-block text-sky transition-transform group-open:rotate-45">+</span>If something&rsquo;s not right
              </summary>
              <ul className="mt-4 space-y-2.5 text-[0.95rem] leading-relaxed text-deep/75">
                <li><b>No ❄ icon?</b> Click the <b>^</b> next to the clock and drag the ❄ onto the taskbar.</li>
                <li><b>The snow vanished?</b> It hides while a fullscreen video or game is in front, and comes back after. Also check ❄ → Pause snow.</li>
                <li><b>Feels heavy on an old laptop?</b> ❄ → Performance → Battery saver, or Snow → Light.</li>
                <li><b>Quit it and want it back?</b> Open <b>Desktop Snowfall</b> from the Start menu or the desktop shortcut. No need to download it again.</li>
                <li><b>Uninstall:</b> Settings → Apps → Desktop Snowfall → Uninstall. That removes the app, its shortcuts, the autostart entry and its settings.</li>
              </ul>
            </details>
          </article>

          {/* macOS */}
          <article className="lg:pl-14">
            <p className="label text-muted">macOS 12 or later · Apple silicon &amp; Intel</p>
            <h3 className="mt-3 font-display text-[2.6rem] leading-none">macOS</h3>
            <a href={site.download.mac} className="group mt-8 flex items-center justify-between rounded-2xl bg-frost px-6 py-5 text-deep ring-1 ring-ice transition-colors hover:bg-ice">
              <span>
                <span className="block text-[1.1rem] font-semibold">Download for Mac</span>
                <span className="label mt-1 block text-muted">desktopsnowfall-mac.zip · 2.4 MB · universal</span>
              </span>
              <span aria-hidden className="text-2xl transition-transform group-hover:translate-y-0.5">↓</span>
            </a>
            <p className="mt-4 text-[0.92rem] text-deep/70">Checksums for every file: <a className="underline decoration-glacier underline-offset-4" href={site.download.checksums}>SHA256SUMS.txt</a></p>
            <ol className="mt-10 space-y-4">
              <Step n={1}>Unzip it and drag <b>desktopsnowfall</b> into Applications.</Step>
              <Step n={2}>
                The first time, macOS says it can&rsquo;t open it: the app isn&rsquo;t notarized yet. Click <b>Done</b>, open <b>System Settings → Privacy &amp; Security</b>, scroll down and click <b>Open Anyway</b>. Only once.
              </Step>
              <Step n={3}>For snow on the Dock, allow it under <b>Privacy &amp; Security → Accessibility</b>, then restart the app.</Step>
            </ol>
            <details className="group mt-8 border-t border-hairline pt-5">
              <summary className="cursor-pointer list-none font-semibold marker:hidden">
                <span className="mr-2 inline-block text-sky transition-transform group-open:rotate-45">+</span>If something&rsquo;s not right
              </summary>
              <ul className="mt-4 space-y-2.5 text-[0.95rem] leading-relaxed text-deep/75">
                <li><b>No snow on the Dock?</b> Check the Accessibility permission (or give it to Terminal if you run it from <code>build.sh</code>), then restart the app.</li>
                <li><b>Only one screen gets snow?</b> That&rsquo;s expected for now: the Mac version snows on the main screen.</li>
                <li><b>Too much going on?</b> ❄︎ → Snow Amount → Light, or Pause Snow.</li>
              </ul>
            </details>
          </article>
        </div>

        {/* For developers */}
        <div className="mt-24 grid gap-10 border-t border-hairline pt-14 md:mt-32 lg:grid-cols-12">
          <div className="min-w-0 lg:col-span-4">
            <p className="label text-sky">From source</p>
            <h3 className="mt-3 font-display text-[2.2rem] leading-tight">Build it yourself</h3>
            <p className="mt-4 leading-relaxed text-deep/75">
              Windows needs the .NET 8 SDK (<code className="font-mono text-[0.85em]">winget install Microsoft.DotNet.SDK.8</code>).
              The Mac needs the Xcode command line tools (<code className="font-mono text-[0.85em]">xcode-select --install</code>).
            </p>
          </div>
          <div className="grid min-w-0 gap-5 lg:col-span-7 lg:col-start-6">
            <div className="min-w-0">
              <p className="label mb-2 text-muted">Windows</p>
              <Code>{`git clone ${site.repo}.git
cd desktopsnowfall/windows
dotnet run -c Release`}</Code>
            </div>
            <div className="min-w-0">
              <p className="label mb-2 text-muted">macOS</p>
              <Code>{`git clone ${site.repo}.git
cd desktopsnowfall
zsh build.sh`}</Code>
            </div>
          </div>
        </div>
      </div>
    </section>
  );
}
