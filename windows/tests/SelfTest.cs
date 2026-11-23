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

    static void Step(SnowWorld w, int frames)
    {
        for (int i = 0; i < frames; i++) w.Update(Dt, () => Desk, () => null);
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
