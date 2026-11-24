import Image from "next/image";
import rolling from "@/public/images/renders/rolling.png";
import aimTaskbar from "@/public/images/screenshots/aim-taskbar.png";
import aimPenguin from "@/public/images/screenshots/aim-penguin.png";
import steps from "@/public/images/renders/snowman-steps.png";
import cast from "@/public/images/renders/cast.png";
import browserLights from "@/public/images/screenshots/browser-lights.png";
import weather from "@/public/images/screenshots/weather.png";

function Tag({ children }: { children: React.ReactNode }) {
  return <span className="label rounded-full px-2.5 py-1 text-[0.6rem] text-muted ring-1 ring-hairline">{children}</span>;
}

export function Play() {
  return (
    <section id="features" className="bg-paper pb-28 pt-24 md:pb-40 md:pt-32">
      <div className="mx-auto max-w-[1240px] px-5 md:px-8">
        <div className="max-w-3xl">
          <p className="label text-sky">Things to do</p>
          <h2 className="display mt-5 text-[clamp(2.8rem,6vw,5rem)]">
            There&rsquo;s more than one
            <br />
            way to <em>play.</em>
          </h2>
        </div>

        {/* A — Snowballs: a wide strip, text tucked underneath on the right */}
        <article className="arrive mt-20 grid gap-10 md:mt-28 md:grid-cols-12 md:items-end">
          <figure className="md:col-span-8">
            <Image src={rolling} alt="A snowball rolling along the snow; the snow behind it is lower where the ball picked it up" sizes="(min-width: 1024px) 780px, 96vw" quality={90} className="print h-auto w-full" />
            <figcaption className="label mt-3 text-muted">Drawn by the app itself: the groove behind the ball is the snow it picked up</figcaption>
          </figure>
          <div className="md:col-span-4">
            <h3 className="display text-[clamp(2.1rem,3.6vw,3rem)]">
              Roll a little.
              <br />
              <em>Watch it grow.</em>
            </h3>
            <div className="mt-6">
              <p className="text-[1.0625rem] leading-relaxed text-deep/80">
                Press the mouse button down on the snow and slide sideways, and a snowball starts rolling and picking
                up what it passes (on the Mac you don&rsquo;t even need to click). Press on a finished one to carry it
                somewhere else. Push one into another and it gets shoved
                along; on Windows a big one knocks a small one flying, and heavy balls keep rolling after you let go.
              </p>
              <div className="mt-5 flex flex-wrap gap-2">
                <Tag>Roll · carry · push: both</Tag>
                <Tag>Weight & collisions: Windows</Tag>
                <Tag>Size modes up to Giant: Windows</Tag>
              </div>
            </div>
          </div>
        </article>

        {/* B — Slingshot: text left, two real screenshots stacked and offset on the right */}
        <article className="arrive mt-28 grid gap-12 md:mt-40 md:grid-cols-12 md:items-center">
          <div className="md:col-span-4">
            <p className="label text-sky">Windows</p>
            <h3 className="display mt-4 text-[clamp(2.1rem,3.6vw,3rem)]">
              Pull back.
              <br />
              <em>Let go.</em>
            </h3>
            <p className="mt-6 leading-relaxed text-deep/80">
              Pick a snowball up, flick the mouse and let go to throw it. Or hold <kbd className="rounded border border-hairline bg-snow px-1.5 py-0.5 font-mono text-[0.8em]">Shift</kbd>,
              pull back like a slingshot and release. A dotted line shows where it&rsquo;ll fly, and it bursts into powder
              on whatever it hits first: a window, another snowball, a snowman, a penguin.
            </p>
          </div>
          <div className="relative md:col-span-8">
            <Image src={aimTaskbar} alt="A Shift-aimed snowball: a dotted arc curves over two penguins standing on the snowy taskbar" sizes="(min-width: 1024px) 780px, 96vw" quality={90} className="print h-auto w-full" />
            <Image src={aimPenguin} alt="Another aimed throw, its dotted path climbing from the cursor toward the snowball, with a penguin nearby" sizes="(min-width: 1024px) 360px, 60vw" quality={90} className="print relative -mt-10 ml-auto h-auto w-[58%] md:-mt-24 md:mr-[-4%] md:w-[46%]" />
          </div>
        </article>

        {/* C — Snowmen: centred, narrow, with step numbers */}
        <article className="arrive mx-auto mt-28 max-w-4xl text-center md:mt-40">
          <h3 className="display text-[clamp(2.1rem,3.6vw,3rem)]">
            Three snowballs <em>later.</em>
          </h3>
          <figure className="mt-10">
            <Image src={steps} alt="One snowball, then two stacked, then three: a snowman with a carrot nose, a red scarf, buttons and stick arms" sizes="(min-width: 900px) 880px, 96vw" quality={90} className="print h-auto w-full" />
            <figcaption className="label mt-3 grid grid-cols-3 text-muted">
              <span>One</span>
              <span>Two</span>
              <span>Snowman</span>
            </figcaption>
          </figure>
          <p className="mx-auto mt-8 max-w-xl leading-relaxed text-deep/80">
            Set a snowball down right on top of another, then a third. It gets eyes, a carrot nose, a red scarf, buttons
            and stick arms. On Windows it has opinions: a gentle knock makes it wobble, a solid one sends the carrot
            flying, and a big fast snowball bowls it over.
          </p>
        </article>

        {/* D — Visitors teaser and E — lights teaser: two uneven columns */}
        <div className="mt-28 grid gap-16 md:mt-40 md:grid-cols-12">
          <article className="arrive md:col-span-7">
            <Image src={cast} alt="A snowman, a penguin, a snow fox and a rabbit on the snowy ground at night" sizes="(min-width: 1024px) 680px, 96vw" quality={90} className="print h-auto w-full" />
            <h3 className="display mt-8 text-[clamp(1.9rem,3vw,2.6rem)]">
              You&rsquo;re not the only one
              <br />
              <em>enjoying the snow.</em>
            </h3>
            <p className="mt-4 max-w-md leading-relaxed text-deep/80">
              A penguin, a snow fox and a rabbit drop by on Windows. <a href="#visitors" className="underline decoration-glacier underline-offset-4 hover:decoration-sky">Meet them properly ↓</a>
            </p>
          </article>
          <article className="arrive md:col-span-4 md:col-start-9 md:pt-24">
            <p className="label text-sky">Fairy lights</p>
            <h3 className="display mt-4 text-[clamp(1.9rem,3vw,2.6rem)]">
              A little warmth
              <br />
              <em>in all that cold.</em>
            </h3>
            <p className="mt-4 leading-relaxed text-deep/80">
              Switch them on and a string of bulbs swings into place along the top of every window, lighting up one by
              one. <a href="#lights" className="underline decoration-glacier underline-offset-4 hover:decoration-sky">See all six styles ↓</a>
            </p>
          </article>
        </div>
        <figure className="arrive mt-12 overflow-hidden">
          <Image src={browserLights} alt="Fairy lights hanging along the top of a browser window, with a few star-shaped glints" sizes="100vw" quality={90} className="print h-auto w-full min-w-[720px]" />
        </figure>

        {/* F — Weather: image left, short text right */}
        <article className="arrive mt-28 grid gap-10 md:mt-40 md:grid-cols-12 md:items-end">
          <figure className="md:col-span-6">
            <Image src={weather} alt="Close-up of falling snow on a dark desktop: soft round flakes and a few six-armed crystals" sizes="(min-width: 1024px) 560px, 96vw" quality={90} className="print h-auto w-full" />
            <figcaption className="label mt-3 text-muted">Crystals and soft flakes, from the desktop above</figcaption>
          </figure>
          <div className="md:col-span-5 md:col-start-8">
            <h3 className="display text-[clamp(2.1rem,3.6vw,3rem)]">
              Some days are quieter
              <br />
              <em>than others.</em>
            </h3>
            <p className="mt-6 leading-relaxed text-deep/80">
              Flakes fall in three layers, near, middle and far, on a breeze that never quite repeats. Choose Light,
              Medium or Heavy. On Windows a gust sweeps through every minute or so, a few of the near flakes are
              slowly turning six-armed crystals, and now and then one catches the light.
            </p>
          </div>
        </article>
      </div>
    </section>
  );
}
