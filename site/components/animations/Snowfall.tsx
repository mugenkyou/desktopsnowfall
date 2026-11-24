// CSS-only snowfall: no JavaScript runs in the browser. The flakes are placed from a fixed seed, so the
// server and the browser agree, and with reduced motion they simply rest where they are, scattered.

type Props = {
  count?: number;
  seed?: number;
  color?: string; // flakes on a pale sky need a touch of blue to be seen at all
  maxSize?: number;
  className?: string;
};

function rng(seed: number) {
  let s = seed >>> 0;
  return () => {
    s = (s * 1664525 + 1013904223) >>> 0;
    return s / 4294967296;
  };
}

export function Snowfall({ count = 36, seed = 1, color = "#ffffff", maxSize = 7, className = "" }: Props) {
  const r = rng(seed);
  const flakes = Array.from({ length: count }, () => {
    const depth = r(); // 0 far .. 1 near
    const size = 2 + depth * (maxSize - 2);
    const dur = 26 - depth * 14 + r() * 6; // near flakes fall faster
    return {
      left: r() * 100,
      size,
      dur,
      delay: -r() * dur,
      rest: 4 + r() * 90,
      drift: 6 + r() * 18,
      sway: 3 + r() * 4,
      alpha: 0.45 + depth * 0.5,
      blur: depth > 0.85 ? 0.6 : 0,
    };
  });

  return (
    <div aria-hidden="true" className={`pointer-events-none absolute inset-0 overflow-hidden ${className}`}>
      {flakes.map((f, i) => (
        <span
          key={i}
          className="flake-track"
          style={
            {
              left: `${f.left}%`,
              "--dur": `${f.dur.toFixed(1)}s`,
              "--delay": `${f.delay.toFixed(1)}s`,
              "--rest": `${f.rest.toFixed(0)}%`,
            } as React.CSSProperties
          }
        >
          <span
            className="flake"
            style={
              {
                "--size": `${f.size.toFixed(1)}px`,
                "--drift": `${f.drift.toFixed(0)}px`,
                "--sway-dur": `${f.sway.toFixed(1)}s`,
                "--alpha": f.alpha.toFixed(2),
                "--blur": `${f.blur}px`,
                "--flake-color": color,
              } as React.CSSProperties
            }
          />
        </span>
      ))}
    </div>
  );
}
