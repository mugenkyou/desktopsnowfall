import { Snowfall } from "@/components/animations/Snowfall";
import { BallShadeDefs, LightString, Pine, Snowman } from "@/components/illustrations/Landscape";
import { site } from "@/lib/site";

// The closing scene: dusk over a snowy hill, pines, a small snowman, and a string of warm lights
export function Finale() {
  return (
    <section className="relative overflow-hidden bg-[linear-gradient(180deg,#102333_0%,#17334b_45%,#3b6f8f_78%,#83cbe9_100%)] text-snow">
      <Snowfall count={50} seed={77} color="#ffffff" maxSize={6} />

      <div className="relative mx-auto max-w-[1240px] px-5 pt-28 md:px-8 md:pt-40">
        <h2 className="display text-[clamp(3.6rem,11vw,9rem)]">
          Go on.
          <br />
          <em className="text-warm">Let it snow.</em>
        </h2>
        <p className="mt-6 max-w-sm text-[1.15rem] leading-relaxed text-snow/80">A tiny winter world, right where you work.</p>
        <div className="mt-9 flex flex-wrap items-center gap-x-6 gap-y-4">
          <a href="#install" className="rounded-full bg-snow px-6 py-3.5 font-semibold text-deep transition-colors hover:bg-ice">
            Download Desktop Snowfall
          </a>
          <a href={site.repo} className="font-semibold text-snow underline decoration-snow/40 decoration-2 underline-offset-[6px] hover:decoration-warm">
            View on GitHub
          </a>
        </div>
      </div>

      <svg aria-hidden viewBox="0 0 1440 420" preserveAspectRatio="xMidYMax slice" className="relative mt-10 block h-[300px] w-full md:-mt-24 md:h-[420px]">
        <BallShadeDefs />
        <radialGradient id="warmGlow" cx="0.5" cy="0.5" r="0.5">
          <stop offset="0" stopColor="#F3C979" stopOpacity="0.35" />
          <stop offset="1" stopColor="#F3C979" stopOpacity="0" />
        </radialGradient>
        {/* centre: what a phone sees. Edges: extra forest for wide screens */}
        <ellipse cx="800" cy="250" rx="260" ry="110" fill="url(#warmGlow)" />
        <path d="M0,300 C 220,250 420,280 640,262 C 860,244 1080,282 1300,256 C 1370,248 1410,252 1440,250 L1440,420 L0,420 Z" fill="#c9e8f6" opacity="0.5" />
        <Pine x={90} base={300} h={150} color="#2f5a50" />
        <Pine x={180} base={306} h={104} color="#2f5a50" />
        <Pine x={1300} base={292} h={170} color="#2f5a50" />
        <Pine x={1390} base={298} h={120} color="#2f5a50" />
        <Pine x={560} base={318} h={84} color="#3b6a60" />
        <Pine x={830} base={316} h={132} color="#2f5a50" />
        <Pine x={905} base={322} h={92} color="#2f5a50" />
        <LightString x0={834} x1={905} y={196} y1={240} sag={12} n={6} warm />
        <path d="M0,350 C 260,318 520,344 780,330 C 1040,316 1260,342 1440,330 L1440,420 L0,420 Z" fill="#f9fcfe" />
        <Snowman x={700} ground={340} scale={1.15} />
      </svg>
    </section>
  );
}
