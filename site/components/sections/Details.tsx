// Product craft, told plainly. Every number here comes from the source (windows/Program.cs).

const stroke = "#4CAEDC";

const details: { title: string; meta: string; text: string; art: React.ReactNode }[] = [
  {
    title: "Snow at three depths",
    meta: "near · middle · far",
    text: "Far flakes are small, faint and slow; near ones are bigger, brighter and quicker. Your eye reads the difference as distance.",
    art: (
      <svg viewBox="0 0 120 60" aria-hidden>
        {[ [20, 18, 1.4, 0.35], [44, 34, 1.4, 0.35], [80, 14, 1.4, 0.35], [30, 44, 2.4, 0.6], [66, 28, 2.4, 0.6], [98, 42, 2.4, 0.6], [52, 12, 3.6, 0.95], [88, 50, 3.6, 0.95] ].map(([x, y, r, o], i) => (
          <circle key={i} cx={x} cy={y} r={r} fill={stroke} opacity={o} />
        ))}
      </svg>
    ),
  },
  {
    title: "Wind that never quite repeats",
    meta: "three slow waves · a gust every 25–70 s",
    text: "The breeze is a few slow waves added together, so it drifts rather than loops. Every so often a gust swells for a few seconds and dies down again.",
    art: (
      <svg viewBox="0 0 120 60" aria-hidden>
        <path d={Array.from({ length: 61 }, (_, i) => { const x = i * 2; const t = i / 60; const y = 32 - 8 * Math.sin(t * 6) - 4 * Math.sin(t * 17 + 1) - 18 * Math.exp(-((t - 0.68) ** 2) / 0.006); return `${i ? "L" : "M"}${x},${y.toFixed(1)}`; }).join(" ")} fill="none" stroke={stroke} strokeWidth="1.5" />
        <text x="76" y="10" className="fill-muted font-mono text-[6px]">gust</text>
      </svg>
    ),
  },
  {
    title: "Piles, one column at a time",
    meta: "a column every 3 px · slumps past 1 px",
    text: "Each surface keeps a row of tiny columns. A landing flake adds to its column and its neighbours; where the slope gets too steep, snow slides sideways until it settles.",
    art: (
      <svg viewBox="0 0 120 60" aria-hidden>
        {Array.from({ length: 24 }, (_, i) => { const h = 6 + 14 * Math.sin((i / 23) * Math.PI) + (i % 3) * 1.5; return <rect key={i} x={6 + i * 4.5} y={50 - h} width={3.6} height={h} fill={stroke} opacity={0.75} />; })}
        <line x1="4" y1="50" x2="116" y2="50" stroke="#17334B" strokeWidth="1.2" />
      </svg>
    ),
  },
  {
    title: "Snow that remembers how fast you moved",
    meta: "fling follows the drag",
    text: "When you drag a window, its pile breaks into clumps that leave with the window's own speed. A lazy drag spills it; a quick flick throws it across the screen.",
    art: (
      <svg viewBox="0 0 120 60" aria-hidden>
        <rect x="50" y="26" width="56" height="28" rx="3" fill="none" stroke="#17334B" strokeWidth="1.2" />
        {[ [44, 20, 2.4], [34, 14, 2], [24, 20, 1.8], [16, 10, 1.6], [8, 18, 1.4] ].map(([x, y, r], i) => <circle key={i} cx={x} cy={y} r={r} fill={stroke} />)}
        <path d="M56 18 h30" stroke={stroke} strokeDasharray="2 3" />
      </svg>
    ),
  },
  {
    title: "Weight, and what it does",
    meta: "mass grows with area · bounce 0.35",
    text: "A snowball's mass grows with its size. The mouse pulls it like a spring, so a small ball follows at once and a heavy one lags, then keeps rolling. When two collide, momentum is shared, with only a little bounce: snow is soft.",
    art: (
      <svg viewBox="0 0 120 60" aria-hidden>
        <circle cx="38" cy="38" r="16" fill="none" stroke="#17334B" strokeWidth="1.2" />
        <circle cx="72" cy="46" r="8" fill="none" stroke="#17334B" strokeWidth="1.2" />
        <path d="M82 46 h26" stroke={stroke} strokeWidth="1.5" />
        <path d="M104 42 l5 4 -5 4" fill="none" stroke={stroke} strokeWidth="1.5" />
      </svg>
    ),
  },
  {
    title: "A snowman that rocks back",
    meta: "damped spring · topples past ~26°",
    text: "A knocked snowman leans and rocks back upright like a weight on a spring. Lean it far enough and it comes apart, ball by ball.",
    art: (
      <svg viewBox="0 0 120 60" aria-hidden>
        <g transform="rotate(14 60 56)">
          <circle cx="60" cy="44" r="11" fill="none" stroke="#17334B" strokeWidth="1.2" />
          <circle cx="60" cy="26" r="8" fill="none" stroke="#17334B" strokeWidth="1.2" />
          <circle cx="60" cy="13" r="6" fill="none" stroke="#17334B" strokeWidth="1.2" />
        </g>
        <path d="M60 56 V6" stroke={stroke} strokeDasharray="2 3" />
        <path d="M60 14 A42 42 0 0 1 72 15" fill="none" stroke={stroke} />
      </svg>
    ),
  },
  {
    title: "Lights on a real wire",
    meta: "sway · swing · snow on the wire",
    text: "The bulbs hang on a sagging wire that sways in the wind and swings when you move the window. Leave it be and a little snow settles along it, then shakes off.",
    art: (
      <svg viewBox="0 0 120 60" aria-hidden>
        <path d="M6 14 Q60 46 114 14" fill="none" stroke="#17334B" strokeWidth="1.2" />
        {[0.15, 0.32, 0.5, 0.68, 0.85].map((t, i) => { const x = 6 + 108 * t; const y = 14 + 64 * t * (1 - t); return <ellipse key={i} cx={x} cy={y + 6} rx="2.6" ry="3.6" fill={["#E8575F", "#F3C979", "#5CC9A7", "#4CAEDC", "#F3C979"][i]} />; })}
      </svg>
    ),
  },
];

export function Details() {
  return (
    <section className="bg-snow py-24 md:py-36">
      <div className="mx-auto grid max-w-[1240px] gap-14 px-5 md:px-8 lg:grid-cols-12">
        <div className="lg:col-span-4">
          <div className="lg:sticky lg:top-28">
            <p className="label text-sky">The craft</p>
            <h2 className="display mt-5 text-[clamp(2.8rem,5.6vw,4.6rem)]">
              Made of
              <br />
              <em>little things.</em>
            </h2>
            <p className="mt-7 max-w-xs leading-relaxed text-deep/80">
              None of this is a video. Every flake, pile and snowball is simulated and drawn in real time, by a few
              thousand lines of code with no game engine underneath.
            </p>
            <p className="label mt-6 text-muted">The numbers are from the Windows app&rsquo;s source</p>
          </div>
        </div>

        <ol className="grid gap-x-10 gap-y-14 sm:grid-cols-2 lg:col-span-8">
          {details.map((d, i) => (
            <li key={d.title} className="arrive border-t border-hairline pt-6">
              <div className="flex items-start justify-between gap-6">
                <span className="label text-muted">{String(i + 1).padStart(2, "0")}</span>
                <div className="h-[60px] w-[120px] shrink-0">{d.art}</div>
              </div>
              <h3 className="mt-4 text-[1.15rem] font-semibold tracking-tight">{d.title}</h3>
              <p className="label mt-1.5 text-sky">{d.meta}</p>
              <p className="mt-3 leading-relaxed text-deep/75">{d.text}</p>
            </li>
          ))}
        </ol>
      </div>
    </section>
  );
}
