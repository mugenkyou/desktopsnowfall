// Hand-drawn winter scenery in the same shapes the app uses for its own pines and snowmen.

export function Pine({ x, base, h, color = "#426E61", snow = true }: { x: number; base: number; h: number; color?: string; snow?: boolean }) {
  const tiers = [0, 1, 2].map((k) => {
    const top = base - h + k * h * 0.24;
    const bot = top + h * 0.46;
    const half = h * (0.2 + 0.075 * k);
    const cap = top + (bot - top) * 0.42;
    return { top, bot, half, cap };
  });
  return (
    <g>
      <rect x={x - h * 0.03} y={base - h * 0.16} width={h * 0.06} height={h * 0.16} rx={1} fill="#6E5B4E" />
      {tiers.map(({ top, bot, half, cap }, i) => (
        <g key={i}>
          <path d={`M${x},${top} L${x - half},${bot} L${x + half},${bot} Z`} fill={color} />
          {snow && (
            <>
              <path d={`M${x},${top} L${x - half * 0.42},${cap} L${x + half * 0.42},${cap} Z`} fill="#fff" opacity={0.95} />
              <ellipse cx={x} cy={bot - 0.5} rx={half * 0.92} ry={Math.max(1.6, h * 0.028)} fill="#fff" opacity={0.95} />
            </>
          )}
        </g>
      ))}
    </g>
  );
}

export function Snowman({ x, ground, scale = 1 }: { x: number; ground: number; scale?: number }) {
  const r1 = 22 * scale, r2 = 15 * scale, r3 = 11 * scale;
  const c1 = ground - r1 + 3 * scale, c2 = c1 - r1 - r2 * 0.72, c3 = c2 - r2 - r3 * 0.72;
  const s = scale;
  return (
    <g>
      <ellipse cx={x + 4 * s} cy={ground + 1} rx={30 * s} ry={4 * s} fill="#83CBE9" opacity={0.25} />
      {[ [c1, r1], [c2, r2], [c3, r3] ].map(([cy, r], i) => (
        <circle key={i} cx={x} cy={cy} r={r} fill="url(#ballShade)" />
      ))}
      <path d={`M${x - 13 * s},${c2 - 4 * s} L${x - 33 * s},${c2 - 17 * s} M${x + 13 * s},${c2 - 4 * s} L${x + 33 * s},${c2 - 17 * s}`} stroke="#6B4A2E" strokeWidth={2 * s} strokeLinecap="round" />
      {[0, 1, 2].map((k) => <circle key={k} cx={x} cy={c2 - 6 * s + k * 6 * s} r={1.6 * s} fill="#17334B" />)}
      <circle cx={x - 3.8 * s} cy={c3 - 2.6 * s} r={1.4 * s} fill="#17334B" />
      <circle cx={x + 3.8 * s} cy={c3 - 2.6 * s} r={1.4 * s} fill="#17334B" />
      <path d={`M${x},${c3} L${x + 10 * s},${c3 + 1.5 * s} L${x},${c3 + 3 * s} Z`} fill="#F28C28" />
      <rect x={x - 12 * s} y={c3 + 7.5 * s} width={24 * s} height={4.5 * s} rx={2 * s} fill="#C9474F" />
      <rect x={x + 4 * s} y={c3 + 10 * s} width={4.5 * s} height={11 * s} rx={1.5 * s} fill="#C9474F" />
    </g>
  );
}

export function BallShadeDefs() {
  return (
    <defs>
      <radialGradient id="ballShade" cx="0.36" cy="0.32" r="0.75">
        <stop offset="0" stopColor="#fff" />
        <stop offset="0.6" stopColor="#F1F7FC" />
        <stop offset="1" stopColor="#BCD2E6" />
      </radialGradient>
    </defs>
  );
}

/** A string of warm bulbs hung between two points, sagging in the middle */
export function LightString({ x0, x1, y, y1 = y, sag, n, warm = false }: { x0: number; x1: number; y: number; y1?: number; sag: number; n: number; warm?: boolean }) {
  const at = (t: number) => [x0 + (x1 - x0) * t, y + (y1 - y) * t + 4 * sag * t * (1 - t)] as const;
  const d = Array.from({ length: 41 }, (_, i) => at(i / 40)).map(([px, py], i) => `${i ? "L" : "M"}${px.toFixed(1)},${py.toFixed(1)}`).join(" ");
  const colors = warm ? ["#F3C979", "#F7D99B", "#EDB55C"] : ["#E8575F", "#F3C979", "#5CC9A7", "#4CAEDC", "#F28C9B"];
  return (
    <g>
      <path d={d} fill="none" stroke="#2A4359" strokeWidth={1.2} opacity={0.8} />
      {Array.from({ length: n }, (_, i) => {
        const [bx, by] = at((i + 0.5) / n);
        const c = colors[i % colors.length];
        return (
          <g key={i}>
            <circle cx={bx} cy={by + 7} r={11} fill={c} opacity={0.22} className="twinkle" style={{ "--tw": `${2.4 + (i % 3) * 0.7}s`, "--twd": `-${(i * 0.37) % 2.4}s` } as React.CSSProperties} />
            <rect x={bx - 1.6} y={by} width={3.2} height={3} rx={0.8} fill="#2A4359" />
            <ellipse cx={bx} cy={by + 7} rx={3} ry={4.2} fill={c} />
            <ellipse cx={bx - 1} cy={by + 5.6} rx={0.9} ry={1.4} fill="#fff" opacity={0.7} />
          </g>
        );
      })}
    </g>
  );
}
