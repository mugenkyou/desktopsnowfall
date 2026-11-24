import Image, { type StaticImageData } from "next/image";
import l0 from "@/public/images/renders/lights-0.png";
import l1 from "@/public/images/renders/lights-1.png";
import l2 from "@/public/images/renders/lights-2.png";
import l3 from "@/public/images/renders/lights-3.png";
import l4 from "@/public/images/renders/lights-4.png";
import l5 from "@/public/images/renders/lights-5.png";
import { Snowfall } from "@/components/animations/Snowfall";

const styles: { name: string; note: string; img: StaticImageData }[] = [
  { name: "Classic Christmas", note: "red, amber, green, blue, pink", img: l0 },
  { name: "Warm White", note: "three shades of candlelight", img: l1 },
  { name: "Arctic Blue", note: "ice and pale sky", img: l2 },
  { name: "Candy Colors", note: "pink, mint, lemon, lilac, peach", img: l3 },
  { name: "Rainbow", note: "the colours drift along the string", img: l4 },
  { name: "Golden Christmas", note: "amber and gold", img: l5 },
];

const patterns = [
  ["Sparkle on & off", "Bulbs fade out and back, with a small star glint now and then. The default."],
  ["Gentle twinkle", "Every bulb at its own slow pace."],
  ["Slow breathing", "The whole string brightens and dims together."],
  ["Cascade", "A wave of light runs along the wire."],
  ["Alternating", "Every other bulb takes a turn."],
  ["Festive sparkle", "Quicker, livelier, still never a hard flicker."],
];

export function Lights() {
  return (
    <section id="lights" className="relative overflow-hidden bg-midnight py-24 text-snow md:py-36">
      <Snowfall count={26} seed={31} color="#ffffff" maxSize={5} className="opacity-60" />
      <div className="relative mx-auto max-w-[1240px] px-5 md:px-8">
        <div className="grid gap-10 lg:grid-cols-12">
          <h2 className="display text-[clamp(2.8rem,6.4vw,5.6rem)] lg:col-span-7">
            Cold outside.
            <br />
            <em className="text-warm">A little glow</em> inside.
          </h2>
          <p className="max-w-md self-end leading-relaxed text-snow/75 lg:col-span-4 lg:col-start-9">
            Turn on <span className="text-snow">Fairy Lights</span> in the ❄ menu and a string of glass bulbs swings
            into place along the top of every window, lighting up one at a time. Each bulb has a bright centre, a
            soft halo and a small pool of coloured light beneath it.
          </p>
        </div>

        {/* A specimen sheet: each style rendered by the app, labelled like a swatch */}
        <ul className="mt-16 divide-y divide-snow/10 border-y border-snow/10 md:mt-24">
          {styles.map((s, i) => (
            <li key={s.name} className="arrive grid items-center gap-4 py-6 md:grid-cols-12 md:gap-8">
              <div className="md:col-span-3">
                <p className="label text-snow/40">{String(i + 1).padStart(2, "0")}</p>
                <p className="mt-1 font-display text-[1.7rem] leading-tight">{s.name}</p>
                <p className="label mt-1 text-glacier/80">{s.note}</p>
              </div>
              <Image src={s.img} alt={`${s.name} fairy lights along the top of a window, drawn by the app`} sizes="(min-width: 768px) 900px, 96vw" quality={90} className="h-auto w-full rounded-lg md:col-span-9" />
            </li>
          ))}
        </ul>

        <div className="mt-20 grid gap-12 md:mt-28 lg:grid-cols-12">
          <div className="lg:col-span-4">
            <p className="label text-glacier">Six ways to twinkle</p>
            <p className="mt-5 max-w-sm leading-relaxed text-snow/70">
              Every pattern fades smoothly. Nothing strobes. The wires sway in the wind, swing when you drag a window,
              and gather a little snow if you leave them alone.
            </p>
          </div>
          <dl className="grid gap-x-10 gap-y-7 sm:grid-cols-2 lg:col-span-8">
            {patterns.map(([name, text]) => (
              <div key={name} className="border-l border-warm/40 pl-5">
                <dt className="font-semibold">{name}</dt>
                <dd className="mt-1 text-[0.95rem] leading-relaxed text-snow/65">{text}</dd>
              </div>
            ))}
          </dl>
        </div>

        <p className="label mt-16 text-snow/40">
          Colour styles and patterns: Windows (❄ → Light style). On the Mac the lights come in one classic style
        </p>
      </div>
    </section>
  );
}
