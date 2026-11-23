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
