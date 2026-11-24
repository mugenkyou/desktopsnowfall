// Headless checks for the snow simulation and renderer. Run: dotnet run -c Release (in windows/tests)
// No windows are opened: a fake desktop (one window) is fed to SnowWorld and frames are rendered into memory.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Win32;

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
        Throwing();
        Personalities();
        FairyLights();
        Settings();
        Screens();
        WelcomeScreen();
        Console.WriteLine(fails == 0 ? "\nAll checks passed." : $"\n{fails} check(s) failed.");
        Environment.Exit(fails == 0 ? 0 : 1);
    }

    // The welcome window, drawn without opening it (at 100 %, 150 % and 200 % display scale)
    static void WelcomeScreen()
    {
        Check(Welcome.ContentH >= 650 && Welcome.ContentH <= 780 && Welcome.W >= 540 && Welcome.W <= 620,
              $"the welcome window has a sensible size ({Welcome.W} x {Welcome.ContentH})");
        foreach (float s in new[] { 1f, 1.5f, 2f })
        {
            Welcome.Build(s, (int)(Welcome.ContentH * s));
            Welcome.Compose(5);
            var (gx, gy, gw, gh) = Welcome.ButtonRect(Welcome.Go);
            uint go = Welcome.PixelAt(gx + 8, gy + gh / 2);
            uint sky = Welcome.PixelAt(40, 70);
            int dark = 0;
            for (float x = 34; x < 300; x += 1) if ((Welcome.PixelAt(x, 272) & 0xFF) < 0x80) dark++;
            Check(go == 0x319EDB && (sky & 0xFF) > ((sky >> 16) & 0xFF) && dark > 20,
                  $"welcome at {s * 100:0}%: blue button, blue sky, dark heading text (button {go:X6}, sky {sky:X6})");
            Check(Welcome.MostLines() <= 3 && Welcome.HeadingWidth() <= Welcome.W - 64,
                  $"welcome at {s * 100:0}%: every feature text fits in three lines, the heading in one");
            Check(Welcome.HitTest(gx + gw / 2, gy + gh / 2) == Welcome.Go && Welcome.HitTest(40, 280) == -1 &&
                  Welcome.HitTest(Welcome.ButtonRect(Welcome.CloseX).X + 16, 28) == Welcome.CloseX,
                  $"welcome at {s * 100:0}%: buttons are where they're drawn");
            if (s == 1.5f && Environment.GetEnvironmentVariable("DSF_WELCOME_BMP") is string path) SaveBmp(path);
            Welcome.Free();
        }
        Check(Welcome.NextFocus(Welcome.Go, 1) == Welcome.CloseX && Welcome.NextFocus(Welcome.CloseX, -1) == Welcome.Go &&
              Welcome.NextFocus(Welcome.CloseX, 1) == Welcome.CloseBtn, "Tab and Shift+Tab cycle through the three buttons");
        // A short screen: the window gets shorter and scrolls
        Welcome.Build(1, 500);
        Welcome.Compose(5);
        Check(Welcome.FrameHeight == 500, "on a small screen the welcome window fits and scrolls");
        Welcome.Free();
    }

    static void SaveBmp(string path)
    {
        int w = Welcome.FrameWidth, h = Welcome.FrameHeight;
        using var f = new System.IO.BinaryWriter(System.IO.File.Create(path));
        f.Write((ushort)0x4D42); f.Write(54 + w * h * 4); f.Write(0); f.Write(54);
        f.Write(40); f.Write(w); f.Write(-h); f.Write((ushort)1); f.Write((ushort)32); f.Write(0); f.Write(w * h * 4); f.Write(0); f.Write(0); f.Write(0); f.Write(0);
        for (long i = 0; i < (long)w * h; i++) f.Write(Welcome.FrameBits[i] | 0xFF000000);
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
            int first = -1, most = 0, arrivals = 0, last = 0;
            for (int i = 0; i < 60 * 300; i++)
            {
                Step(w, 1);
                if (first < 0 && w.Visitors.Count > 0) first = i;
                most = Math.Max(most, w.Visitors.Count);
                if (w.Visitors.Count > last) arrivals += w.Visitors.Count - last;
                last = w.Visitors.Count;
            }
            Check(first > 0 && first <= 60 * 16, $"the first visitor arrives soon ({first / 60f:0} s)");
            Check(arrivals >= 8, $"visitors come often ({arrivals} in 5 minutes)");
            Check(most >= 2 && most <= Cfg.MaxVisitors, $"several can be about at once, never more than {Cfg.MaxVisitors} (most seen: {most})");
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
            while (w.Visitors.Contains(v) && frames < 60 * 240)
            {
                Step(w, 1); frames++;
                lo = MathF.Min(lo, v.X); hi = MathF.Max(hi, v.X);
                hiddenOnOpenGround |= v.Hidden;
            }
            int prints = 0;
            foreach (var m in w.Marks) if (m.Pile == w.Ground) prints++;
            Check(!w.Visitors.Contains(v) && hi - lo > 1000, $"{Cfg.VisitorNames[kind]} wanders across and leaves ({frames / 60} s, {hi - lo:0} px)");
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
                    w.Visitor.Dir = 1;
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

    // Pick a ball up and flick it: dx, dy per frame for a few frames, then let go
    static float Flick(SnowWorld w, Snowball b, float dx, float dy, out int path)
    {
        w.MouseX = b.X; w.MouseY = b.Y - 40;
        b.Y -= 40;                                  // lifted a little above the snow first
        w.MouseY = b.Y;
        w.BeginDrag(w.MouseX, w.MouseY);
        path = 0;
        for (int i = 0; i < 8; i++) { w.MouseX += dx; w.MouseY += dy; Step(w, 1); path = Math.Max(path, w.PathCount); }
        float x = b.X;
        w.EndDrag();
        w.MouseX = w.MouseY = -999;
        return x;
    }

    static void Throwing()
    {
        {
            var w = Bare(out var p);
            var b = w.AddBall(p, 500, 8);
            float from = Flick(w, b, 12, -4, out int path);    // ~700 px/s to the right, a little up
            Check(b.Thrown && b.Vx > 400, $"a fast flick throws the snowball ({b.Vx:0} px/s)");
            Check(path > 5, $"a dotted path shows where it will fly while flicking ({path} dots)");
            float far = b.X;
            int clumps = 0;
            for (int i = 0; i < 240 && w.Balls.Contains(b); i++) { Step(w, 1); far = MathF.Max(far, b.X); }
            Step(w, 1);
            clumps = Clumps(w);
            Check(!w.Balls.Contains(b) && far - from > 150 && clumps > 0, $"it flies {far - from:0} px and bursts into powder where it lands");
        }
        {
            float Speed(float r) { var w = Bare(out var p); var b = w.AddBall(p, 500, r); Flick(w, b, 20, -6, out _); return b.Vx; }
            float small = Speed(8), big = Speed(26);
            Check(big < small * 0.6f, $"a bigger snowball needs more force: same flick, {big:0} vs {small:0} px/s");
        }
        {
            var w = Bare(out var p);
            var b = w.AddBall(p, 500, 8);
            float x0 = b.X, y0 = b.Y - 40;
            w.AimKey = true;
            w.MouseX = b.X; w.MouseY = b.Y - 40; b.Y -= 40;
            w.BeginDrag(w.MouseX, w.MouseY);
            for (int i = 0; i < 10; i++) { w.MouseX -= 8; w.MouseY += 5; Step(w, 1); }   // pull back down-left
            bool stayed = MathF.Abs(b.X - x0) < 0.01f && MathF.Abs(b.Y - y0) < 0.01f;
            int path = w.PathCount;
            if (Environment.GetEnvironmentVariable("SNOW_PNG_DIR") != null)
            {
                var img = new uint[1920 * 1080];
                fixed (uint* px = img) Render.World(px, 1920, 1080, w, 1);
                Png(img, 1920, 1080, 380, 200, 400, 260, "aim-guide.png");
            }
            w.EndDrag();
            w.AimKey = false;
            Check(w.SlingUsed, "using the slingshot is noticed (so the Shift tip stops showing)");
            Check(stayed && path > 5, "Shift-aiming: the ball stays put while you pull back, with a dotted guide");
            Check(b.Thrown && b.Vx > 0 && b.Vy < 0, $"letting go launches it the other way, like a slingshot ({b.Vx:0}, {b.Vy:0})");
        }
        {
            var w = Bare(out var p);
            var b = w.AddBall(w.Ground, 100, 8);
            b.Y = 600;
            float clumps = Clumps(w);
            w.Throw(b, 900, -50);
            float far = 0;
            for (int i = 0; i < 60 && w.Balls.Contains(b); i++) { Step(w, 1); far = MathF.Max(far, b.X); }
            Check(!w.Balls.Contains(b) && far < 230 && Clumps(w) > clumps, $"a snowball thrown at a window splats against its side (stopped at x {far:0})");
        }
        {
            var w = Bare(out var p);
            var (bottom, mid, head) = Snowman(w, p);
            var b = w.AddBall(p, 400, 8);
            b.Y = head.Y;
            w.Throw(b, 900, -30);
            float lean = 0;
            for (int i = 0; i < 120; i++) { Step(w, 1); lean = MathF.Max(lean, MathF.Abs(bottom.Wobble)); }
            Check(!w.Balls.Contains(b) && (lean > 0.05f || head.Below == null), $"a snowball thrown at a snowman's head rocks it (lean {lean:0.00}) or knocks it over");
        }
        {
            var w = Bare(out var p);
            var b = w.AddBall(w.Ground, 60, 8);
            b.Y = 300;
            w.Throw(b, -1500, -600);
            Step(w, 180);
            Check(!w.Balls.Contains(b), "a snowball thrown off the screen doesn't fly on forever");
        }
        {
            var w = Bare(out var p);
            w.Invite(1);
            w.Visitor.X = 1700;
            Step(w, 1, new List<Win>());
            var b = w.AddBall(w.Ground, 1550, 7);
            b.Y = w.Visitor.Y - 12;
            w.Throw(b, 700, -80);
            float far = 0;
            for (int i = 0; i < 40 && w.Balls.Contains(b); i++) { Step(w, 1, new List<Win>()); far = MathF.Max(far, b.X); }
            Check(w.VisitorHits >= 1 && far < w.Visitor.X + 15, "a thrown snowball hits a visitor (and bursts there)");
        }
    }

    static readonly List<Win> Open = new();          // an empty desktop: visitors in plain view

    // A visitor standing at x, and a snowball thrown at it from the left
    static (SnowWorld, Visitor) Target(int kind, float x)
    {
        var w = Bare(out _);
        FillSnow(w.Ground);
        w.Invite(kind);
        var v = w.Visitor;
        v.X = x; v.Dir = 1; v.NextHabit = 1e9f;
        Step(w, 1, Open);
        return (w, v);
    }

    static void ThrowAt(SnowWorld w, Visitor v)
    {
        var b = w.AddBall(w.Ground, v.X - 150, 7);
        b.Y = v.Y - 14;
        w.Throw(b, 750, -90);
    }

    static void Personalities()
    {
        {
            var (w, v) = Target(0, 900);
            ThrowAt(w, v);
            float x0 = v.X;
            bool tumbled = false, lay = false, sat = false;
            int frames = 0;
            while (frames < 60 * 8 && !(tumbled && v.Act == 0)) { Step(w, 1, Open); frames++; tumbled |= v.Act == VisitorAct.Tumble; lay |= v.Pose == 1; sat |= v.Pose == 2; }
            Check(tumbled && lay && sat, "penguin hit by a snowball: loses its balance, slides on its belly, sits up and shakes");
            Check(MathF.Abs(v.X - x0) > 15, $"the penguin slides with the hit ({v.X - x0:0} px)");
            Check(v.Act == 0 && w.Visitor == v, $"the penguin always gets back up ({frames / 60f:0.0} s)");
        }
        {
            var (w, v) = Target(0, 900);
            ThrowAt(w, v);
            Step(w, 30, Open);
            float tBefore = v.ActT;
            ThrowAt(w, v);
            Step(w, 20, Open);
            Check(w.VisitorHits == 2 && v.Act == VisitorAct.Tumble && v.ActT > tBefore, "a second hit mid-reaction doesn't restart it");
        }
        {
            // A short toss (let go, not thrown) right onto an animal still hits it
            var (w, v) = Target(0, 900);
            var b = w.AddBall(w.Ground, v.X - 10, 7);
            b.Y = v.Y - 60;
            b.Drop(40, 0);
            Step(w, 40, Open);
            Check(w.VisitorHits == 1 && !w.Balls.Contains(b) && v.Act == VisitorAct.Tumble, "a snowball dropped on an animal from close by hits it");
        }
        {
            // Right after its own habit (the fox's chase) an animal still feels a hit
            var (w, v) = Target(1, 900);
            v.Cool = 6;
            ThrowAt(w, v);
            Step(w, 30, Open);
            Check(v.Act == VisitorAct.Leap, "an animal reacts to a hit even while its habits are cooling down");
        }
        {
            // A big snowball rolling along the ground bowls an animal over
            var (w, v) = Target(0, 900);
            var b = w.AddBall(w.Ground, v.X - 120, 18);
            b.Vx = 400;
            bool hit = false;
            for (int i = 0; i < 60 && !hit; i++) { Step(w, 1, Open); hit = v.Act == VisitorAct.Tumble; }
            Check(hit, "a big rolling snowball knocks an animal over");
        }
        {
            int fights = 0;
            for (int seed = 0; seed < 12; seed++)
            {
                var (w, v) = Target(0, 900);
                Rnd.Seed(100 + seed);                   // after Target, which seeds its own world
                ThrowAt(w, v);
                bool threw = false;
                for (int i = 0; i < 60 * 5 && !threw; i++) { Step(w, 1, Open); foreach (var b in w.Balls) if (b.ThrownBy == v && b.Vx < 0) threw = true; }
                if (threw) fights++;
            }
            Check(fights >= 1 && fights < 12, $"sometimes the penguin throws a snowball back ({fights} of 12 times), toward the thrower");
        }
        {
            var (w, v) = Target(1, 900);
            float dir0 = v.Dir, x0 = v.X, hop = 0;
            ThrowAt(w, v);
            bool leapt = false, looked = false;
            for (int i = 0; i < 60 * 3; i++)
            {
                Step(w, 1, Open);
                leapt |= v.Act == VisitorAct.Leap;
                hop = MathF.Max(hop, v.Hop);
                if (v.Act == VisitorAct.Leap && v.ActT > 1.1f) looked |= v.Face == MathF.Sign(v.FromX - v.X);
            }
            Check(leapt && hop > 10, $"snow fox hit: jumps aside ({hop:0} px high)");
            Check(looked, "the fox shakes off the snow and looks back toward the thrower");
            Check(v.Act == 0 && v.Dir == dir0 && v.X > x0, "then it goes back to wandering");
        }
        {
            var (w, v) = Target(2, 900);
            ThrowAt(w, v);
            bool startled = false, away = false, back = false;
            for (int i = 0; i < 60 * 25 && !back; i++)
            {
                Step(w, 1, Open);
                startled |= v.Act == VisitorAct.Startle;
                if (v.Away) away = true;
                if (away && !v.Away && !v.Hidden) back = true;
            }
            Check(startled && away, "rabbit hit: a startled hop, then it runs off to hide");
            Check(back && w.Visitor == v, "the rabbit comes back after a short while");
        }
        {
            var (w, v) = Target(1, 600);
            var ball = w.AddBall(w.Ground, 700, 8);
            float hopAtBall = 0;
            for (int i = 0; i < 60 * 10 && v.X < ball.X + 20; i++) { Step(w, 1, Open); if (MathF.Abs(v.X - ball.X) < 4) hopAtBall = MathF.Max(hopAtBall, v.Hop); }   // (it may stop to look around first)
            Check(hopAtBall > ball.R, $"the fox jumps over a small snowball in its way ({hopAtBall:0} px over a {ball.R * 2:0} px ball)");
        }
        {
            var (w, v) = Target(1, 600);
            var ball = w.AddBall(w.Ground, 800, 8);
            ball.Vx = 160;
            bool chased = false, batted = false;
            float lastV = ball.Vx;
            for (int i = 0; i < 60 * 5; i++)
            {
                Step(w, 1, Open);
                chased |= v.Act == VisitorAct.Chase;
                if (ball.Vx > lastV + 40) batted = true;
                lastV = ball.Vx;
            }
            Check(chased && batted, "the fox chases a rolling snowball and bats it along");
        }
        {
            var (w, v) = Target(0, 600);
            var ball = w.AddBall(w.Ground, 640, 8);
            for (int i = 0; i < 60 * 12 && ball.Vx <= 0; i++) Step(w, 1, Open);   // (it may stop or sit down on the way)
            Check(ball.Vx > 0, "the penguin plays with a snowball: gives it a kick");
        }
        {
            Rnd.Seed(5);
            var w = Bare(out _);
            bool slipped = false, sat = false;
            for (int i = 0; i < 60 * 240; i++)
            {
                if (w.Visitor == null) w.Invite(0);
                Step(w, 1, Open);
                slipped |= w.Visitor?.Act == VisitorAct.Slip;
                sat |= w.Visitor?.Act == VisitorAct.Sit;
            }
            Check(slipped && sat, "left alone, the penguin sometimes slips and sometimes sits down");
        }
        {
            var (w, v) = Target(2, 900);
            for (int i = 0; i < 120; i++) Step(w, 1, Open);
            int small = 0, big = 0;
            foreach (var m in w.Marks) if (m.Pile == w.Ground) { if (m.Size == 2) small++; else if (m.Size > 3) big++; }
            Check(small > 0 && big > 0, "the rabbit leaves its own tracks: long hind prints and small front prints");
        }
        {
            // Every pose drawn: still no allocation per frame
            const int bw = 1920, bh = 1080;
            var buf = new uint[bw * bh];
            long alloc = 0;
            fixed (uint* px = buf)
                for (int pose = 0; pose < 4; pose++)
                {
                    var (w, v) = Target(0, 900);
                    Step(w, 2, Open);
                    v.Pose = pose;
                    Render.World(px, bw, bh, w, 1);
                    long before = GC.GetAllocatedBytesForCurrentThread();
                    for (int i = 0; i < 10; i++) { v.Pose = pose; Render.World(px, bw, bh, w, 1); }
                    alloc += GC.GetAllocatedBytesForCurrentThread() - before;
                    Png(buf, bw, bh, 860, 990, 80, 90, $"penguin-pose{pose}.png");
                }
            Check(alloc == 0, $"drawing the penguin's poses allocates nothing ({alloc} bytes)");
        }
    }

    static void FairyLights()
    {
        {
            bool bounded = true, smooth = true;
            float worst = 0;
            var bulbs = new Bulb[8];
            Rnd.Seed(3);
            for (int i = 0; i < bulbs.Length; i++) bulbs[i] = new Bulb();
            float blinkLo = 1, blinkHi = 0, blinkWorst = 0, glints = 0;
            for (int pat = 0; pat < LightLook.PatternNames.Length; pat++)
                for (int i = 0; i < bulbs.Length; i++)
                {
                    float last = LightLook.Brightness(pat, bulbs[i], i, 0);
                    for (int f = 1; f < 60 * 30; f++)
                    {
                        float k = LightLook.Brightness(pat, bulbs[i], i, f * Dt), d = MathF.Abs(k - last);
                        last = k;
                        if (pat == 5)
                        {
                            blinkLo = MathF.Min(blinkLo, k); blinkHi = MathF.Max(blinkHi, k);
                            blinkWorst = MathF.Max(blinkWorst, d);
                            if (LightLook.Glint(pat, bulbs[i], i, f * Dt) > 0.5f) glints++;
                            continue;
                        }
                        bounded &= k >= 0.3f && k <= 1.15f;
                        worst = MathF.Max(worst, d);
                    }
                }
            smooth = worst < 0.08f;
            Check(bounded, "the steady light patterns stay between soft and bright (never off, never glaring)");
            Check(smooth, $"no steady pattern flickers: the biggest change in one frame is {worst:0.000}");
            Check(blinkLo < 0.15f && blinkHi > 1, $"\"sparkle on & off\" really goes off and on ({blinkLo:0.00} .. {blinkHi:0.00})");
            Check(blinkWorst < 0.2f, $"...with soft fades, not a hard flicker (biggest step per frame {blinkWorst:0.00})");
            Check(glints > 100, $"lit bulbs flash a star-shaped glint now and then ({glints / 60f:0} bulb-seconds in 30 s)");
            Check(Cfg.LightPattern == 5, "sparkle on & off is the default pattern");
            float mean = 0, sq = 0;
            for (int i = 0; i < bulbs.Length; i++) { float k = LightLook.Brightness(0, bulbs[i], i, 5); mean += k; sq += k * k; }
            mean /= bulbs.Length;
            float sd = MathF.Sqrt(sq / bulbs.Length - mean * mean);
            Check(sd > 0.02f, $"bulbs twinkle independently (spread {sd:0.000})");
        }
        {
            var w = NewWorld(out _);
            w.LightsOn = true;
            w.UpdateLights();
            var ls = w.Lights[1];
            Check(ls.Reveal(0, w.T + 0.05f) > ls.Reveal(ls.Bulbs.Count - 1, w.T + 0.05f), "bulbs light up one after another when the lights are hung");
            Step(w, 60 * 3);
            var moved = new List<Win> { new Win(1, 260, 400, 1460, 900) };
            float peak = 0;
            for (int i = 0; i < 60; i++) { Step(w, 1, moved); peak = MathF.Max(peak, MathF.Abs(ls.Sway)); }
            Check(peak > 1, $"moving the window makes the bulbs swing ({peak:0.0} px)");
            Step(w, 60 * 8, moved);
            Check(MathF.Abs(ls.SwayV) < 3, "the swinging settles down again");
            w.StartGust(80);
            float gust = 0;
            for (int i = 0; i < 60 * 3; i++) { Step(w, 1, moved); gust = MathF.Max(gust, MathF.Abs(ls.Sway)); }
            Check(gust > 1.5f, $"a gust of wind swings the lights ({gust:0.0} px)");
        }
        {
            var w = NewWorld(out _);
            w.LightsOn = true;
            Step(w, 60 * 60);
            var ls = w.Lights[1];
            float settled = ls.Snow;
            int clumps = Clumps(w);
            Step(w, 2, new List<Win> { new Win(1, 300, 400, 1500, 900) });
            Check(settled > 0.4f && ls.Snow == 0 && Clumps(w) > clumps, $"snow settles on the wire ({settled:0.00} after a minute) and falls off when the window moves");
            Cfg.WireSnow = false;
            Step(w, 60 * 30);
            Check(ls.Snow == 0, "snow on the wires can be switched off");
            Cfg.WireSnow = true;
        }
        {
            var w = NewWorld(out _);
            w.LightsOn = true;
            Step(w, 60 * 40);
            const int bw = 1920, bh = 1080;
            var buf = new uint[bw * bh];
            long alloc = 0;
            fixed (uint* px = buf)
            {
                for (int pre = 0; pre < LightLook.PresetNames.Length; pre++)
                    for (int pat = 0; pat < LightLook.PatternNames.Length; pat++)
                    {
                        Cfg.LightPreset = pre; Cfg.LightPattern = pat;
                        Render.World(px, bw, bh, w, 1);
                        long before = GC.GetAllocatedBytesForCurrentThread();
                        Render.World(px, bw, bh, w, 1);
                        alloc += GC.GetAllocatedBytesForCurrentThread() - before;
                        if (pat == 5) Png(buf, bw, bh, 400, 395, 200, 40, $"lights-{pre}.png");
                    }
                Cfg.LightPreset = Cfg.LightPattern = 0;
                var sw = Stopwatch.StartNew();
                for (int i = 0; i < 30; i++) Render.World(px, bw, bh, w, 1);
                Console.WriteLine($"INFO  a frame with fairy lights on a 1200 px window: {sw.Elapsed.TotalMilliseconds / 30:0.00} ms");
            }
            Check(alloc == 0, $"every color and pattern draws without allocating ({alloc} bytes)");
        }
    }

    // Settings are saved in the registry; bad values (edited by hand, from an old version) are made safe
    static void Settings()
    {
        string keep = Program.RegKey;
        Program.RegKey = @"Software\desktopsnowfall-selftest";
        try
        {
            Program.WriteInt("GrowthMode", 99);
            Program.WriteInt("ExperimentalMaxRadius", -5);
            Program.WriteInt("MaxBalls", 7);
            Program.WriteInt("LightPreset", 50);
            Program.WriteInt("LightPattern", -3);
            Program.WriteInt("SnowAmount", 12345);
            Program.WriteInt("PerfMode", 9);
            Program.LoadSettings();
            Check(Cfg.GrowthMode == 3 && Cfg.ExperimentalMaxRadius == 4 && Cfg.MaxBalls == 20 && Cfg.LightPreset == 5 &&
                  Cfg.LightPattern == 0 && Cfg.TargetFlakes == 260 && Cfg.PerfMode == 1,
                  "invalid saved settings are clamped to safe values");

            Program.WriteInt("GrowthMode", 2);
            Program.WriteInt("SnowAmount", 500);
            Program.WriteInt("Visitor1", 0);
            Program.LoadSettings();
            Check(Cfg.GrowthMode == 2 && Cfg.TargetFlakes == 500 && !Cfg.Visitors[1], "settings are read back after a restart");

            Program.ResetSettings();
            Check(Cfg.GrowthMode == 0 && Cfg.TargetFlakes == 260 && Cfg.Visitors[1] && Cfg.LightPattern == 5 && Cfg.MaxBalls == 20,
                  "reset all settings goes back to the defaults");
        }
        finally
        {
            Registry.CurrentUser.DeleteSubKeyTree(Program.RegKey, false);
            Program.RegKey = keep;
            Program.LoadSettings();
        }
    }

    static SnowWorld Desktop(params Mon[] mons)
    {
        Rnd.Seed(11);
        var w = new SnowWorld(new List<Mon>(mons)) { MouseX = -999, MouseY = -999 };
        return w;
    }

    static void Run(SnowWorld w, int frames, Func<float[]> taskbars = null)
    {
        for (int i = 0; i < frames; i++) w.Update(Dt, () => Open, taskbars ?? (() => null));
    }

    static float SnowOn(Pile p) => p.Total;

    // Several screens: one snow world, each screen showing its part
    static void Screens()
    {
        {
            // Windows reports a screen to the left of the main one with negative coordinates
            var mons = Layout.ToWorld(new[] { (0, 0, 1920, 1080), (-1280, 100, 0, 1124) }, 1, out int ox, out int oy);
            Check(ox == -1280 && oy == 0 && mons[0] == new Mon(1280, 0, 3200, 1080) && mons[1] == new Mon(0, 100, 1280, 1124),
                  "screens left of the main one (negative coordinates) are placed correctly");
            var scaled = Layout.ToWorld(new[] { (0, 0, 2880, 1620) }, 1.5f, out _, out _);
            Check(scaled[0] == new Mon(0, 0, 1920, 1080), "display scaling is applied to screen sizes");
        }
        {
            // Two screens side by side, different sizes: each gets its own floor at its own bottom
            var w = Desktop(new Mon(0, 0, 1920, 1080), new Mon(1920, 0, 3200, 1024));
            Check(w.Grounds.Count == 2 && w.Grounds[0].TopY == 1080 && w.Grounds[1].TopY == 1024 && w.W == 3200,
                  "side by side: each screen has a floor at its own bottom edge");
            Run(w, 60 * 40);
            Check(SnowOn(w.Grounds[0]) > 50 && SnowOn(w.Grounds[1]) > 50,
                  $"snow piles up on both screens ({SnowOn(w.Grounds[0]):0} and {SnowOn(w.Grounds[1]):0})");
        }
        {
            // One screen on top of another: no floor between them, the snow falls through to the lower one
            var w = Desktop(new Mon(0, 0, 1920, 1080), new Mon(0, 1080, 1920, 2160));
            Check(w.Grounds.Count == 1 && w.Grounds[0].TopY == 2160, "stacked: no floor between the screens, only at the bottom");
            Run(w, 60 * 40);
            int below = 0;
            foreach (var f in w.Flakes) if (f.Y > 1100) below++;
            Check(below > 30 && SnowOn(w.Grounds[0]) > 50, $"snow falls from the upper screen into the lower one ({below} flakes down there)");
        }
        {
            // Upper screen only half over the lower one: the uncovered half keeps a floor
            var w = Desktop(new Mon(0, 0, 1920, 1080), new Mon(960, 1080, 2880, 2160));
            bool ok = w.Grounds.Count == 2;
            foreach (var g in w.Grounds) if (g.TopY == 1080) ok &= g.MinX == 0 && MathF.Abs(g.Width - 960) < 1;
            Check(ok, "offset stack: only the part of the edge with nothing below is a floor");
        }
        {
            var w = Desktop(new Mon(0, 0, 1080, 1920));
            Check(w.Grounds.Count == 1 && w.Grounds[0].Width == 1080 && w.Grounds[0].TopY == 1920, "a portrait screen works");
        }
        {
            // Taskbars on both screens
            var w = Desktop(new Mon(0, 0, 1920, 1080), new Mon(1920, 0, 3200, 1024));
            Run(w, 3, () => new[] { 1040f, 990f });
            Check(w.Grounds.Count == 2 && w.Grounds[0].Taskbar && w.Grounds[0].TopY == 1040 && w.Grounds[1].Taskbar && w.Grounds[1].TopY == 990,
                  "each screen's own taskbar becomes its floor");
        }
        {
            // A snowball thrown across the edge between two screens flies on smoothly and lands on the other one
            var w = Desktop(new Mon(0, 0, 1920, 1080), new Mon(1920, 0, 3200, 1024));
            Run(w, 3);
            var b = w.AddBall(w.Grounds[0], 1700, 8);
            b.Y = 700;
            float before = SnowOn(w.Grounds[1]), last = b.X, jump = 0, far = 0;
            w.Throw(b, 500, -300);
            for (int i = 0; i < 180 && w.Balls.Contains(b); i++) { Run(w, 1); jump = MathF.Max(jump, MathF.Abs(b.X - last)); last = b.X; far = MathF.Max(far, b.X); }
            Check(far > 1950 && jump < 20 && SnowOn(w.Grounds[1]) > before,
                  $"a snowball flies from one screen to the next without jumping, and lands there (reached x {far:0})");
        }
        {
            // The display turns off and on (or the PC wakes up): the same screens again, the taskbar blinks away
            var mon = new Mon(0, 0, 1920, 1080);
            var w = Desktop(mon);
            Func<float[]> bar = () => new[] { 1040f };
            Run(w, 3, bar);
            var g = w.Grounds[0];
            var bottom = w.AddBall(g, 700, 20); var mid = w.AddBall(g, 700, 14); var head = w.AddBall(g, 700, 10);
            mid.Below = bottom; head.Below = mid; mid.Decor = 1; head.Decor = 2;
            Run(w, 30, bar);
            w.SetMonitors(new List<Mon> { mon });
            Run(w, 20, () => new[] { float.NaN });
            Run(w, 60, bar);
            Check(w.Grounds[0] == g && head.Below == mid && mid.Below == bottom && bottom.Pile == g && head.Decor == 2,
                  "display off and on: the snowman on the taskbar stays standing");
        }
        {
            // A maximized browser above the taskbar: the taskbar keeps a layer of snow, nothing is drawn over the browser
            var w = Desktop(new Mon(0, 0, 1920, 1080));
            var cover = new List<Win> { new Win(3, 0, 0, 1920, 1040) };
            Run(w, 3, () => new[] { 1040f });
            w.Ground.Clear();
            for (int i = 0; i < 60 * 6; i++) w.Update(Dt, () => cover, () => new[] { 1040f });
            Check(w.Ground.Taskbar && SnowOn(w.Ground) > Cfg.GroundBase * w.Ground.Count * 0.8f, "with a maximized browser the taskbar keeps a few inches of snow");
            var bare = Desktop(new Mon(0, 0, 1920, 1080));
            var full = new List<Win> { new Win(3, 0, 0, 1920, 1080) };
            Step(bare, 60 * 6, full);
            bool hidden = true;
            foreach (bool o in bare.Ground.Occ) hidden &= o;
            Check(hidden && SnowOn(bare.Ground) == 0, "no snow is drawn over an app covering the screen bottom");
        }
        {
            // Desktop icons: snow piles up on them, flakes aren't swallowed by them, a window in front hides them
            var w = Desktop(new Mon(0, 0, 1920, 1080));
            var desk = new List<Win> { new Win(-1000, 40, 40, 120, 100, Icon: true) };
            Step(w, 60 * 20, desk);
            float onIcon = SnowOn(w.Piles[-1000]);
            Step(w, 60, new List<Win> { new Win(5, 0, 0, 800, 600), desk[0] });
            Check(onIcon > 20 && SnowOn(w.Piles[-1000]) == 0 && w.Lights.Count == 0, $"snow settles on desktop icons ({onIcon:0}), hidden behind windows");
            // Moving the icon: its snow drops where it was instead of flying off
            var w2 = Desktop(new Mon(0, 0, 1920, 1080));
            Step(w2, 60 * 20, desk);
            Step(w2, 3, new List<Win> { new Win(-1000, 900, 40, 980, 100, Icon: true) });
            float maxV = 0, minX = 1e9f, maxX = 0;
            foreach (var f in w2.Flakes) if (f.Clump) { maxV = MathF.Max(maxV, MathF.Abs(f.Vx)); minX = MathF.Min(minX, f.X); maxX = MathF.Max(maxX, f.X); }
            Check(maxX > 0 && maxV < 60 && minX > 30 && maxX < 130, $"a moved icon's snow falls just below where it was (x {minX:0}-{maxX:0}, sideways {maxV:0} px/s)");
        }
        {
            // A screen is unplugged and plugged back in
            var w = Desktop(new Mon(0, 0, 1920, 1080), new Mon(1920, 0, 3200, 1024));
            Run(w, 60 * 10);
            var b = w.AddBall(w.Grounds[1], 2500, 12);
            w.SetMonitors(new List<Mon> { new(0, 0, 1920, 1080) });
            Run(w, 60 * 3);
            Check(w.Grounds.Count == 1 && w.W == 1920 && !w.Balls.Contains(b), "unplugging a screen: its snow and snowballs go away, no crash");
            w.SetMonitors(new List<Mon> { new(0, 0, 1920, 1080), new(1920, 0, 3200, 1024) });
            Run(w, 60 * 20);
            Check(w.Grounds.Count == 2 && SnowOn(w.Grounds[1]) > 20, "plugging it back in: it snows there again");
        }
        {
            // A gap between two screens (they only touch at a corner): visitors aren't drawn where there's no screen
            var w = Desktop(new Mon(0, 0, 1000, 1000), new Mon(2000, 0, 3000, 1000));
            w.Invite(1);
            var v = w.Visitor;
            v.X = 1500; v.Dir = 1; v.NextHabit = 1e9f;
            Run(w, 1);
            bool gapHidden = v.Hidden;
            v.X = 2500;
            Run(w, 1);
            Check(gapHidden && !v.Hidden, "a visitor walking between screens is hidden in the gap and seen again on the next screen");
        }
        {
            // The same picture whether drawn as one screen or as two halves (each screen draws its part)
            var w = NewWorld(out var p);
            w.LightsOn = true;
            var (bottom, _, _) = Snowman(w, p);
            w.Invite(0);
            w.Visitor.X = 900;
            Step(w, 30);
            const int bw = 1920, bh = 1080;
            var full = new uint[bw * bh];
            var left = new uint[960 * bh];
            var right = new uint[960 * bh];
            var top = new uint[bw * 540];
            var low = new uint[bw * 540];
            int diff = 0;
            fixed (uint* f = full) fixed (uint* l = left) fixed (uint* r = right) fixed (uint* tp = top) fixed (uint* lw = low)
            {
                Render.World(f, bw, bh, w, 1);
                Render.World(l, 960, bh, w, 1, 0, 0);
                Render.World(r, 960, bh, w, 1, 960, 0);
                Render.World(tp, bw, 540, w, 1, 0, 0);
                Render.World(lw, bw, 540, w, 1, 0, 540);
            }
            for (int y = 0; y < bh; y++)
                for (int x = 0; x < bw; x++)
                {
                    uint a = full[y * bw + x];
                    if (a != (x < 960 ? left[y * 960 + x] : right[y * 960 + x - 960])) diff++;
                    if (a != (y < 540 ? top[y * bw + x] : low[(y - 540) * bw + x])) diff++;
                }
            Check(diff == 0, $"drawing a screen's part of the world matches the whole picture pixel for pixel ({diff} differences)");
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
