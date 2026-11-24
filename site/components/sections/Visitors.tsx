import Image from "next/image";
import cast from "@/public/images/renders/cast.png";
import penguin from "@/public/images/renders/react-0.png";
import fox from "@/public/images/renders/react-1.png";
import rabbit from "@/public/images/renders/react-2.png";

const visitors = [
  {
    name: "The penguin",
    line: "A little clumsy. Surprisingly brave.",
    text: "Waddles along, slips now and then, sits down for a rest and gives small snowballs a kick. Hit it and it tumbles, slides on its belly, sits up, shakes itself off, and sometimes throws one back at you.",
    img: penguin,
    alt: "The penguin sliding on its belly across the snow after being hit by a snowball",
  },
  {
    name: "The snow fox",
    line: "Quick on its feet. Always curious.",
    text: "Hops over small snowballs in its way, turns back from big ones, and chases anything rolling, batting it along. Hit it and it leaps aside, shakes off the snow and looks round for whoever threw it.",
    img: fox,
    alt: "The snow fox in mid-leap, jumping aside after a snowball hit",
  },
  {
    name: "The rabbit",
    line: "Mostly peaceful. Easily startled.",
    text: "Hops, stops to listen, ears twitching, and sometimes bolts for no reason at all. Hit it and it makes one big startled hop, runs off the edge of the screen to hide, and peeks back in a little later.",
    img: rabbit,
    alt: "The rabbit in a startled hop after a snowball hit",
  },
];

export function Visitors() {
  return (
    <section id="visitors" className="relative bg-paper">
      {/* a band of night sky that runs straight into the app's own render of the cast */}
      <div className="bg-[#0c1b2b]">
        <div className="mx-auto max-w-[1240px] px-5 pt-20 md:px-8 md:pt-28">
          <p className="label text-glacier">Windows · up to four at once</p>
          <h2 className="display mt-4 max-w-2xl text-[clamp(2.6rem,6vw,5rem)] text-snow">
            Meet the <em>visitors.</em>
          </h2>
        </div>
        <div className="mx-auto max-w-[1440px] overflow-hidden">
          <Image src={cast} alt="A snowman, a penguin, a snow fox and a rabbit together on the snowy ground at night" sizes="(min-width: 1440px) 1440px, 100vw" quality={90} className="mt-6 h-auto w-full min-w-[640px] -translate-x-[8%] sm:translate-x-0" />
        </div>
      </div>

      <div className="mx-auto max-w-[1240px] px-5 py-20 md:px-8 md:py-28">
        <p className="max-w-2xl text-[1.125rem] leading-relaxed text-deep/80">
          The first one turns up about fifteen seconds after you start, then another every half a minute or so. They wander along the
          bottom of the screen, leave footprints that slowly fill back in, and always walk <em>behind</em> your windows,
          never over what you&rsquo;re working on. Each has its own way of moving, and its own idea of what to do
          when a snowball hits it.
        </p>

        <div className="mt-16 grid gap-14 md:grid-cols-3 md:gap-8">
          {visitors.map((v, i) => (
            <article key={v.name} className={`arrive ${i === 1 ? "md:mt-20" : ""} ${i === 2 ? "md:mt-8" : ""}`}>
              <Image src={v.img} alt={v.alt} sizes="(min-width: 768px) 380px, 96vw" quality={90} className="print h-auto w-full" />
              <p className="label mt-6 text-sky">{v.name}</p>
              <h3 className="display mt-2 text-[1.9rem] leading-tight">
                <em>{v.line}</em>
              </h3>
              <p className="mt-3 leading-relaxed text-deep/75">{v.text}</p>
            </article>
          ))}
        </div>
        <p className="label mt-14 text-muted">Pictures drawn by the app&rsquo;s renderer, mid-reaction. Invite one any time: ❄ → Visitors → Invite one now</p>
      </div>
    </section>
  );
}
