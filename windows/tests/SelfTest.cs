// Headless checks for the snow simulation and renderer. Run: dotnet run -c Release (in windows/tests)
// No windows are opened: a fake desktop (one window) is fed to SnowWorld and frames are rendered into memory.

using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace desktopsnowfall;

static unsafe class SelfTest
{
    static int fails;
    const float Dt = 1f / 60;
    static readonly List<Win> Desk = new() { new Win(1, 200, 400, 1400, 900) };   // one 1200 px wide window

    static void Check(bool ok, string what)
    {
        Console.WriteLine((ok ? "PASS  " : "FAIL  ") + what);
        if (!ok) fails++;
    }

    static void Step(SnowWorld w, int frames) => Step(w, frames, Desk);

    static void Step(SnowWorld w, int frames, List<Win> desk)
    {
        for (int i = 0; i < frames; i++) w.Update(Dt, () => desk, () => null);
    }

    static SnowWorld NewWorld(out Pile pile, float sw = 1920, float sh = 1080)
    {
        Rnd.Seed(7);
        var w = new SnowWorld(sw, sh) { MouseX = -999, MouseY = -999 };
        Step(w, 3);                       // first poll creates the window's pile
        pile = w.Piles[1];
        FillSnow(pile);
        return w;
    }

    static void FillSnow(Pile p) { for (int c = 0; c < p.Count; c++) if (!p.Occ[c]) p.H[c] = p.Caps[c]; }

    // Slide the mouse sideways along the window top (the "no clicking" rolling gesture), refilling the snow
    static void SweepMouse(SnowWorld w, Pile p, int passes, float speed = 6)
    {
        w.MouseY = p.TopY - 2;
        for (int k = 0; k < passes; k++)
        {
            FillSnow(p);
            bool right = k % 2 == 0;
            for (float x = right ? 300 : 1300; right ? x < 1300 : x > 300; x += right ? speed : -speed)
            {
                w.MouseX = x;
                Step(w, 1);
            }
        }
        w.MouseX = w.MouseY = -999;
    }

    static float Biggest(SnowWorld w) { float r = 0; foreach (var b in w.Balls) r = MathF.Max(r, b.R); return r; }

    static void Main()
    {
        Baseline();
        Limits();
        Momentum();
        Trails();
        Snowmen();
        Clicks();
        Weather();
        Visitors();
        Console.WriteLine(fails == 0 ? "\nAll checks passed." : $"\n{fails} check(s) failed.");
        Environment.Exit(fails == 0 ? 0 : 1);
    }

    static void Baseline()
    {
        // Rolling: sliding the mouse over snow makes a snowball that grows
        var w = NewWorld(out var p);
        SweepMouse(w, p, 1);
        Check(w.Balls.Count >= 1 && Biggest(w) > Cfg.BallStartRadius + 2, $"rolling makes a growing snowball (r = {Biggest(w):0.0})");

        // Click-through: the overlay only claims the mouse right over a snowball
        var ball = w.Balls[0];
        Check(w.BallAt(ball.X, ball.Y) == ball, "cursor over a snowball is detected (overlay catches the click)");
        Check(w.BallAt(ball.X + ball.R + 30, ball.Y) == null, "cursor away from snowballs is not (clicks pass through)");

        // Determinism: same seed and inputs give the same world
        var a = NewWorld(out var pa); SweepMouse(a, pa, 2);
        var sa = Snapshot(a);
        var b = NewWorld(out var pb); SweepMouse(b, pb, 2);
        Check(sa == Snapshot(b), "simulation is deterministic for a fixed seed");

        RenderChecks(a);
    }

    // A + B: growth modes, screen-aware limits, full-size feedback, snowball count limit
    static void Limits()
    {
        float[] expect = { 28, 42, 70, 100 };
        for (int mode = 0; mode < 4; mode++)
        {
            Cfg.GrowthMode = mode;
            var w = NewWorld(out var p);
            float last = -1;
            for (int pass = 0; pass < 80 && Biggest(w) > last + 0.01f; pass++) { last = Biggest(w); SweepMouse(w, p, 2); }   // back and forth
            float r = Biggest(w);
            Snowball big = null;
            foreach (var b in w.Balls) if (b.R == r) big = b;
            Check(r <= expect[mode] + 0.01f && r >= expect[mode] - 0.5f,
                  $"{Cfg.GrowthNames[mode]} mode grows to its limit and no further (r = {r:0.0}, limit {expect[mode]})");
            Check(big != null && big.MaxedAt >= 0, $"{Cfg.GrowthNames[mode]}: reaching the limit triggers the full-size flash");
        }
        Cfg.GrowthMode = 3;
        Cfg.ExperimentalMaxRadius = 10000;
        Check(Cfg.MaxRadius(1920, 1080) <= 1080 * 0.12f + 0.01f, $"Experimental 10000 px is held to 12% of the screen ({Cfg.MaxRadius(1920, 1080):0} px)");
        Cfg.ExperimentalMaxRadius = 100;
        Cfg.GrowthMode = 2;
        Check(Cfg.MaxRadius(800, 600) <= 600 * 0.09f + 0.01f, $"Giant on an 800x600 screen is held to 9% ({Cfg.MaxRadius(800, 600):0} px)");
        Cfg.GrowthMode = 0;
        Check(Cfg.MaxRadius(1920, 1080) == 28, "Normal keeps the original 28 px limit");

        Cfg.MaxBalls = 10;
        var m = NewWorld(out var mp);
        for (int i = 0; i < 25; i++) m.AddBall(mp, 250 + i * 40, 8);
        Check(m.Balls.Count == 10, $"max snowball count is respected ({m.Balls.Count} of 10)");
        Cfg.MaxBalls = 20;
    }

    static SnowWorld Bare(out Pile p) { var w = NewWorld(out p); Array.Clear(p.H); return w; }   // window with no snow
    static int Clumps(SnowWorld w) { int n = 0; foreach (var f in w.Flakes) if (f.Clump) n++; return n; }

    // C: mass, momentum, collisions
    static void Momentum()
    {
        float Coast(float r) { var w = Bare(out var p); var b = w.AddBall(p, 300, r); b.Vx = 300; Step(w, 240); return b.X - 300; }
        float light = Coast(8), heavy = Coast(40);
        Check(heavy > light * 2, $"a heavy ball keeps its momentum longer ({heavy:0} px vs {light:0} px from the same push)");

        float Pull(float r)
        {
            var w = Bare(out var p);
            var b = w.AddBall(p, 600, r);
            w.MouseY = p.TopY - 2;
            w.MouseX = 600; Step(w, 1);
            w.MouseX = 601; Step(w, 1);          // a little sideways movement grabs the ball
            w.MouseX = 640; Step(w, 6);          // then the cursor jumps 40 px ahead; look 0.1 s later
            return b.X - 600;
        }
        float pl = Pull(8), ph = Pull(40);
        Check(ph < pl * 0.7f, $"a heavy ball accelerates more slowly ({ph:0.0} px vs {pl:0.0} px in 0.1 s)");

        {
            var w = Bare(out var p);
            var hv = w.AddBall(p, 400, 40);
            var lt = w.AddBall(p, 700, 8);
            hv.Vx = 300;
            int clumps0 = Clumps(w);
            float before = 0;
            bool hit = false;
            for (int i = 0; i < 120 && !hit; i++) { before = hv.Mass * hv.Vx + lt.Mass * lt.Vx; Step(w, 1); hit = lt.Vx > 0; }
            float after = hv.Mass * hv.Vx + lt.Mass * lt.Vx;
            Check(hit && MathF.Abs(after - before) / before < 0.05f, $"a collision transfers momentum without creating any ({before:0} -> {after:0})");
            Check(lt.Vx > hv.Vx, $"a big ball knocks a small one away faster than itself ({lt.Vx:0} vs {hv.Vx:0} px/s)");
            Check(Clumps(w) > clumps0, "a fast collision throws a burst of powder");
        }
        {
            var w = Bare(out var p);
            var lt = w.AddBall(p, 400, 8);
            var hv = w.AddBall(p, 700, 40);
            lt.Vx = 400;
            Step(w, 90);
            Check(MathF.Abs(hv.X - 700) < 20, $"a small ball can't shove a big one far ({hv.X - 700:0.0} px)");
        }
        {
            var w = Bare(out var p);
            var a = w.AddBall(p, 400, 10);
            w.AddBall(p, 600, 10);
            var last = w.AddBall(p, 621, 10);
            a.Vx = 900;                           // enough to coast the 180 px to the row
            Step(w, 60);
            Check(last.X - 621 > 20, $"a hit travels down a row of balls ({last.X - 621:0} px)");
        }
        {
            var w = Bare(out var p);
            var b = w.AddBall(p, p.MinX + p.Width - 30, 10);
            b.Vx = 300;
            Step(w, 60);
            Check(b.Falling || !w.Balls.Contains(b), "a ball rolling past the end of a window falls off");
        }
        {
            var w = NewWorld(out var p);
            var b = w.AddBall(p, 400, 8);
            b.Vx = 300;
            Step(w, 60);
            Check(b.R > 9, $"a coasting ball still picks up snow (area model kept, r 8 -> {b.R:0.0})");
        }
    }

    static int LiveMarks(SnowWorld w)
    {
        int n = 0;
        foreach (var m in w.Marks) if (m.Pile != null && m.Pile != w.Ground && w.T - m.Born < m.Life) n++;
        return n;
    }

    // D: rolling trails
    static void Trails()
    {
        var w = NewWorld(out var p);
        SweepMouse(w, p, 1);
        int live = LiveMarks(w);
        Check(live > 10, $"rolling leaves a trail ({live} dents)");
        bool attached = true;
        foreach (var m in w.Marks) if (m.Pile != null && m.Pile != p) attached = false;
        Check(attached, "trail dents belong to the surface they were made on (they move with the window)");
        Step(w, 120);                                    // let the ball coast to a stop (it keeps rolling, and marking)
        Step(w, (int)(Cfg.TrailLife * 60) + 10);
        Check(LiveMarks(w) == 0, "trail dents fade away once the ball stops");

        Cfg.Trails = false;
        var off = NewWorld(out var po);
        SweepMouse(off, po, 1);
        Check(LiveMarks(off) == 0, "rolling trails can be switched off");
        Cfg.Trails = true;
    }

    // Carry a ball with the mouse (left button held) and let go just above another one
    static void DragOnto(SnowWorld w, Snowball b, Snowball onto)
    {
        w.MouseX = b.X; w.MouseY = b.Y;
        Check(w.BeginDrag(w.MouseX, w.MouseY), "a snowball can be picked up with the mouse");
        float tx = onto.X, ty = onto.Y - onto.R - b.R - 4;
        for (int i = 1; i <= 30; i++)
        {
            w.MouseX = b.X + (tx - b.X) * i / 30f;
            w.MouseY = b.Y + (ty - b.Y) * i / 30f;
            Step(w, 1);
        }
        w.EndDrag();
        w.MouseX = w.MouseY = -999;
    }

    static (Snowball bottom, Snowball mid, Snowball head) Snowman(SnowWorld w, Pile p)
    {
        var bottom = w.AddBall(p, 700, 20);
        var mid = w.AddBall(p, 400, 14);
        var head = w.AddBall(p, 1000, 10);
        DragOnto(w, mid, bottom);
        DragOnto(w, head, mid);
        Step(w, 30);
        return (bottom, mid, head);
    }

    // Roll a ball into the snowman's base and watch for 3 s
    static float Strike(SnowWorld w, Pile p, Snowball bottom, float r, float v)
    {
        var s = w.AddBall(p, bottom.X - bottom.R - r - 30, r);
        s.Vx = v;
        float maxLean = 0;
        for (int i = 0; i < 180; i++) { Step(w, 1); maxLean = MathF.Max(maxLean, MathF.Abs(bottom.Wobble)); }
        return maxLean;
    }

    // E: snowman interactions
    static void Snowmen()
    {
        {
            var w = Bare(out var p);
            var (bottom, mid, head) = Snowman(w, p);
            Check(mid.Below == bottom && head.Below == mid && mid.Decor == 1 && head.Decor == 2,
                  "three stacked balls make a snowman");
            float lean = Strike(w, p, bottom, 6, 150);
            Check(lean > 0.01f && head.Below == mid && !head.NoseLost,
                  $"a gentle knock makes the snowman wobble (max lean {lean:0.000} rad) without breaking it");
            Check(MathF.Abs(bottom.Wobble) < 0.005f, "the wobble settles back upright");
            Check(MathF.Abs(head.X - bottom.X) < 0.5f, "a settled snowman stands straight (stacking is stable)");
        }
        {
            var w = Bare(out var p);
            var (bottom, mid, head) = Snowman(w, p);
            w.AddBall(p, bottom.X - 60, 12).Vx = 500;   // arrives at ~400 px/s: a ~95 px/s knock, over the 80 threshold
            bool flew = false;
            for (int i = 0; i < 30; i++) { Step(w, 1); flew |= w.Carrots.Count > 0; }
            Check(head.NoseLost && flew && head.Below == mid, "a solid knock sends the carrot flying, the snowman stays up");
            Step(w, 60 * 8);
            Check(w.Carrots.Count == 0, "the fallen carrot rests a while, then fades away");
        }
        {
            var w = Bare(out var p);
            var (bottom, mid, head) = Snowman(w, p);
            Strike(w, p, bottom, 40, 600);
            Check(head.Below == null && mid.Below == null, "bowling: a big fast ball knocks the snowman over");
        }
        {
            Cfg.CarrotFalls = false;
            var w = Bare(out var p);
            var (bottom, _, head) = Snowman(w, p);
            w.AddBall(p, bottom.X - 60, 12).Vx = 400;
            Step(w, 60);
            Check(!head.NoseLost && w.Carrots.Count == 0, "the falling carrot can be switched off");
            Cfg.CarrotFalls = true;
        }
        {
            // Everything new on screen at once: snowman, loose carrot, trails, lights; drawing must still not allocate
            var w = NewWorld(out var p);
            w.LightsOn = true;
            var (bottom, _, _) = Snowman(w, p);
            w.AddBall(p, bottom.X - 60, 12).Vx = 400;
            SweepMouse(w, p, 1);
            Step(w, 10);
            const int bw = 1920, bh = 1080;
            var buf = new uint[bw * bh];
            fixed (uint* px = buf)
            {
                for (int i = 0; i < 5; i++) Render.World(px, bw, bh, w, 1);
                long before = GC.GetAllocatedBytesForCurrentThread();
                for (int i = 0; i < 30; i++) Render.World(px, bw, bh, w, 1);
                long alloc = GC.GetAllocatedBytesForCurrentThread() - before;
                Check(alloc == 0, $"drawing snowmen, carrots and trails allocates nothing ({alloc} bytes)");
            }
        }
    }

    // Left click only picks up; right click breaks
    static void Clicks()
    {
        {
            var w = NewWorld(out var p);
            var b = w.AddBall(p, 700, 15);
            w.MouseX = b.X; w.MouseY = b.Y;
            w.BeginDrag(w.MouseX, w.MouseY);
            Step(w, 3);
            w.EndDrag();                                   // a plain left click: press and release in place
            w.MouseX = w.MouseY = -999;
            Step(w, 60);
            Check(w.Balls.Contains(b) && !b.Falling && b.Pile == p && b.R == 15,
                  "a left click on a snowball doesn't break it (it settles back on the snow)");
        }
        {
            var w = NewWorld(out var p);
            var b = w.AddBall(p, 700, 15);
            Array.Clear(p.H);
            float snow = p.Total;
            w.MouseX = b.X; w.MouseY = b.Y;
            w.BeginDrag(w.MouseX, w.MouseY);
            for (int i = 0; i < 20; i++) { w.MouseY -= 15; Step(w, 1); }   // lift it 300 px
            w.EndDrag();
            w.MouseX = w.MouseY = -999;
            Step(w, 180);
            Check(!w.Balls.Contains(b) && p.Total > snow, "dropped from high up, a snowball still goes poof into snow");
        }
        {
            var w = NewWorld(out var p);
            var b = w.AddBall(p, 700, 15);
            Array.Clear(p.H);
            int clumps = Clumps(w);
            Check(w.Smash(b.X, b.Y) && !w.Balls.Contains(b) && p.Total > 0 && Clumps(w) > clumps,
                  "a right click breaks a snowball into powder and snow");
            Check(!w.Smash(100, 100), "a right click away from snowballs does nothing (passes through)");
        }
        {
            var w = Bare(out var p);
            var (bottom, mid, head) = Snowman(w, p);
            w.Smash(mid.X, mid.Y);
            Step(w, 90);
            Check(!w.Balls.Contains(mid) && head.Below == null && w.Balls.Contains(bottom),
                  "breaking a snowman's middle lets its head tumble off");
        }
    }

    // Gusts and fancy flakes
    static void Weather()
    {
        var w = NewWorld(out _);
        int crystals = 0, sparkles = 0;
        foreach (var f in w.Flakes) { if (f.Crystal) crystals++; if (f.Sparkle) sparkles++; }
        Check(crystals > 10 && sparkles > 3, $"some flakes are crystals ({crystals}) and some sparkle ({sparkles})");
        bool layered = true;
        foreach (var f in w.Flakes) if (f.Crystal && f.Layer != 2) layered = false;
        Check(layered, "crystals are only near flakes (the three depth layers stay as they were)");

        Cfg.FancyFlakes = false;
        var plain = NewWorld(out _);
        int fancy = 0;
        foreach (var f in plain.Flakes) if (f.Crystal || f.Sparkle) fancy++;
        Check(fancy == 0, "crystal and sparkly flakes can be switched off");
        Cfg.FancyFlakes = true;

        var g = NewWorld(out _);
        g.StartGust(80);
        float peak = 0;
        for (int i = 0; i < 180; i++) { Step(g, 1); peak = MathF.Max(peak, MathF.Abs(g.Wind)); }
        Check(peak > 45, $"a gust blows noticeably harder than the breeze (peak {peak:0} vs at most 29)");

        Cfg.Gusts = false;
        var calm = NewWorld(out _);
        float most = 0;
        for (int i = 0; i < 60 * 120; i++) { Step(calm, 1); most = MathF.Max(most, MathF.Abs(calm.Wind)); }
        Check(most <= 29.01f, $"with gusts off the wind stays a breeze for 2 minutes (max {most:0.0})");
        Cfg.Gusts = true;
    }

    // Penguin, snow fox, rabbit
    static void Visitors()
    {
        {
            var w = NewWorld(out _);
            Step(w, 60 * 30);
            Check(w.Visitor == null, "visitors are rare: nobody shows up in the first 30 s");
        }
        for (int kind = 0; kind < 3; kind++)
        {
            var w = NewWorld(out _);
            FillSnow(w.Ground);
            Check(w.Invite(kind) && w.Visitor.Kind == kind, $"{Cfg.VisitorNames[kind]} can be invited");
            var v = w.Visitor;
            float lo = v.X, hi = v.X;
            bool hiddenOnOpenGround = false;
            int frames = 0;
            while (w.Visitor == v && frames < 60 * 240)
            {
                Step(w, 1); frames++;
                lo = MathF.Min(lo, v.X); hi = MathF.Max(hi, v.X);
                hiddenOnOpenGround |= v.Hidden;
            }
            int prints = 0;
            foreach (var m in w.Marks) if (m.Pile == w.Ground) prints++;
            Check(w.Visitor == null && hi - lo > 1000, $"{Cfg.VisitorNames[kind]} wanders across and leaves ({frames / 60} s, {hi - lo:0} px)");
            Check(prints > 20, $"{Cfg.VisitorNames[kind]} leaves footprints in the snow ({prints})");
            Check(!hiddenOnOpenGround, $"{Cfg.VisitorNames[kind]} is visible on open ground");
        }
        {
            var cover = new List<Win> { new Win(3, 0, 600, 1920, 1080) };   // a maximized window down to the screen bottom
            var w = NewWorld(out _);
            w.Invite(1);
            bool everShown = false;
            for (int i = 0; i < 600; i++) { Step(w, 1, cover); everShown |= w.Visitor != null && !w.Visitor.Hidden && w.Visitor.X > 10 && w.Visitor.X < 1910; }
            Check(!everShown, "a visitor walks behind windows, never on top of them");
        }
        {
            var w = NewWorld(out _);
            for (int i = 0; i < 3; i++) Cfg.Visitors[i] = false;
            Check(!w.Invite(), "with every visitor switched off, nobody comes");
            Cfg.Visitors[2] = true;
            Check(!w.Invite(0) && w.Invite() && w.Visitor.Kind == 2, "each visitor can be switched on or off on its own");
            for (int i = 0; i < 3; i++) Cfg.Visitors[i] = true;
        }
        {
            var w = NewWorld(out _);
            const int bw = 1920, bh = 1080;
            var buf = new uint[bw * bh];
            long alloc = 0;
            fixed (uint* px = buf)
                for (int kind = 0; kind < 3; kind++)
                {
                    w.SendAway();
                    w.Invite(kind);
                    w.Visitor.X = 900;
                    Step(w, 2);
                    Render.World(px, bw, bh, w, 1);
                    long before = GC.GetAllocatedBytesForCurrentThread();
                    for (int i = 0; i < 20; i++) Render.World(px, bw, bh, w, 1);
                    alloc += GC.GetAllocatedBytesForCurrentThread() - before;
                    Png(buf, bw, bh, 860, 1030, 80, 50, $"visitor-{kind}.png");
                }
            Check(alloc == 0, $"drawing visitors allocates nothing ({alloc} bytes)");
        }
    }

    // Save a crop of the frame as a PNG (for looking at the drawings); only when SNOW_PNG_DIR is set
    static void Png(uint[] buf, int bw, int bh, int x0, int y0, int w, int h, string name)
    {
        string dir = Environment.GetEnvironmentVariable("SNOW_PNG_DIR");
        if (string.IsNullOrEmpty(dir)) return;
        var raw = new byte[h * (w * 4 + 1)];
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                uint c = buf[(y0 + y) * bw + x0 + x];
                float a = (c >> 24) / 255f;
                // composite the premultiplied overlay onto a dark-blue desktop so white things are visible
                byte Mix(uint ch, float bg) => (byte)Math.Min(255, ch + bg * 255 * (1 - a));
                int o = y * (w * 4 + 1) + 1 + x * 4;
                raw[o] = Mix((c >> 16) & 0xFF, 0.16f); raw[o + 1] = Mix((c >> 8) & 0xFF, 0.22f);
                raw[o + 2] = Mix(c & 0xFF, 0.36f); raw[o + 3] = 255;
            }
        using var ms = new System.IO.MemoryStream();
        using (var z = new System.IO.Compression.ZLibStream(ms, System.IO.Compression.CompressionLevel.Optimal, true)) z.Write(raw);
        var png = new System.IO.MemoryStream();
        png.Write(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 });
        void Chunk(string type, byte[] data)
        {
            var be = BitConverter.GetBytes(data.Length); Array.Reverse(be); png.Write(be);
            var td = System.Text.Encoding.ASCII.GetBytes(type);
            png.Write(td); png.Write(data);
            uint crc = 0xFFFFFFFF;
            foreach (var bt in td) crc = Crc(crc, bt);
            foreach (var bt in data) crc = Crc(crc, bt);
            var cb = BitConverter.GetBytes(crc ^ 0xFFFFFFFF); Array.Reverse(cb); png.Write(cb);
        }
        var ihdr = new byte[13];
        var wb = BitConverter.GetBytes(w); Array.Reverse(wb); wb.CopyTo(ihdr, 0);
        var hb = BitConverter.GetBytes(h); Array.Reverse(hb); hb.CopyTo(ihdr, 4);
        ihdr[8] = 8; ihdr[9] = 6;
        Chunk("IHDR", ihdr);
        Chunk("IDAT", ms.ToArray());
        Chunk("IEND", Array.Empty<byte>());
        System.IO.File.WriteAllBytes(System.IO.Path.Combine(dir, name), png.ToArray());
    }

    static uint Crc(uint crc, byte b)
    {
        crc ^= b;
        for (int k = 0; k < 8; k++) crc = (crc & 1) != 0 ? 0xEDB88320 ^ (crc >> 1) : crc >> 1;
        return crc;
    }

    static string Snapshot(SnowWorld w)
    {
        var s = new System.Text.StringBuilder();
        foreach (var b in w.Balls) s.Append($"{b.X:0.000},{b.Y:0.000},{b.R:0.000};");
        s.Append(w.Flakes.Count);
        return s.ToString();
    }

    // Draw frames into memory: must not allocate, and report the cost of a heavy-snow frame
    static void RenderChecks(SnowWorld w)
    {
        w.LightsOn = true;
        Step(w, 60);
        const int bw = 1920, bh = 1080;
        var buf = new uint[bw * bh];
        fixed (uint* px = buf)
        {
            for (int i = 0; i < 5; i++) Render.World(px, bw, bh, w, 1);   // warm up (JIT, list capacity)
            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < 30; i++) Render.World(px, bw, bh, w, 1);
            long alloc = GC.GetAllocatedBytesForCurrentThread() - before;
            Check(alloc == 0, $"rendering a frame allocates nothing ({alloc} bytes over 30 frames)");

            int keep = Cfg.TargetFlakes;
            Cfg.TargetFlakes = 500;                                          // "Heavy" snow
            Step(w, 240);
            var sw = Stopwatch.StartNew();
            for (int i = 0; i < 60; i++) { w.Update(Dt, () => Desk, () => null); Render.World(px, bw, bh, w, 1); }
            double ms = sw.Elapsed.TotalMilliseconds / 60;
            Cfg.TargetFlakes = keep;
            Console.WriteLine($"INFO  heavy snow at 1920x1080: {ms:0.00} ms per frame (simulate + render), {w.Flakes.Count} particles");
            Check(ms < 16, "heavy snow frame fits in the 16 ms frame budget");
        }
    }
}
