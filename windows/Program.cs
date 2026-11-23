// desktopsnowfall for Windows — snow on your desktop.
// Snow piles up on top of windows and on the taskbar, and flies off when you drag a window.
// Pure Win32 + a tiny software renderer: no WinForms, no extra packages.
//
// Run:     dotnet run -c Release
// Build:   dotnet publish -c Release -r win-x64   (or win-arm64) --self-contained -p:PublishSingleFile=true
// Hover over the snow to roll snowballs, push them into each other, drag them with the left mouse button and
// stack three of them into a snowman. A ❄ icon appears in the system tray (bottom right): pause / snow amount /
// fairy lights / clear / how to play / quit.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32;

[assembly: System.Runtime.Versioning.SupportedOSPlatform("windows")]

namespace desktopsnowfall;

// MARK: - Settings (feel free to tweak)

static class Cfg
{
    public static int TargetFlakes = 260;          // number of flakes falling at the same time
    public const float ColW = 3;                   // width of one column of the snow height map
    public const float WindowPileMax = 16;         // max snow height on top of a window
    public const float GroundPileMax = 34;         // max snow height at the bottom of the screen
    public const float TaskbarPileMax = 18;        // max snow height on the taskbar
    public const float TaskbarGain = 2.0f;         // snow on the taskbar builds up this much faster
    public const float Gravity = 900;              // gravity for snow sliding off
    public const float PollInterval = 1f / 30f;    // how often window positions are checked (s)
    public const int MaxClumps = 900;              // max number of falling snow clumps at once
    public const float FallSpeed = 1.5f;           // falling speed multiplier
    public const bool FlakesInFront = true;        // some of the smallest flakes fall in front of windows
    public const float InFrontFraction = 0.4f;     // share of the smallest layer that falls in front
    public const int FrameMs = 16;                 // timer interval (~60 fps)
    public const float BallStartRadius = 3;        // a new snowball starts this small
    public static int GrowthMode = 0;              // 0 Normal, 1 Large, 2 Giant, 3 Experimental (tray menu)
    public static readonly string[] GrowthNames = { "Normal", "Large", "Giant", "Experimental" };
    static readonly float[] GrowthRadius = { 28, 42, 70 };   // Normal, Large (1.5×), Giant (2.5×)
    public static float ExperimentalMaxRadius = 100;         // Experimental limit (registry), still screen-bounded
    public static int MaxBalls = 20;               // oldest resting snowballs are removed beyond this
    public const float BallMassRadius = 12;        // a ball this big has mass 1; mass grows with area
    public const float DriveStiffness = 400;       // how hard the cursor pulls the ball it rolls
    public const float RollFriction = 3;           // how fast a mass-1 ball stops by itself (1/s); heavy ones coast longer
    public const float BallRestitution = 0.35f;    // bounciness of ball-on-ball hits (snow is soft)
    public static bool Trails = true;              // rolling snowballs leave dents in the snow (tray menu)
    public const float TrailLife = 2.5f;           // how long a dent takes to fade (s)
    public static bool CarrotFalls = true;         // a good knock sends the snowman's nose flying (tray menu)
    public const float WobbleStiffness = 60;       // how quickly a struck snowman rocks back upright
    public const float WobbleDamping = 2.5f;       // how quickly the rocking dies down
    public const float WobbleKick = 0.02f;         // tilt speed (rad/s) per px/s of knock
    public const float ToppleAngle = 0.45f;        // lean past this (rad) and the snowman falls apart
    public const float NoseKnock = 80;             // knock (px/s) that sends the carrot flying
    public static bool Gusts = true;               // now and then a gentle gust sweeps the snow sideways (tray menu)
    public static bool FancyFlakes = true;         // some near flakes are six-armed crystals, a few sparkle (tray menu)
    public static readonly bool[] Visitors = { true, true, true };   // penguin, snow fox, rabbit (tray menu)
    public static readonly string[] VisitorNames = { "Penguin", "Snow fox", "Rabbit" };
    public const float FirstVisit = 15;            // the first visitor comes after this long (s)
    public const float VisitGapMin = 15, VisitGapMax = 40;    // then another every 15-40 s (sometimes two together)
    public const int MaxVisitors = 4;              // at most this many wandering about at once
    public const float ThrowSpeed = 450;           // letting go faster than this (px/s) throws; slower just sets the ball down
    public const float ThrowMax = 1800;            // fastest throw (px/s)
    public const float SlingGain = 6;              // Shift-aiming: launch speed (px/s) per px the mouse is pulled back
    public const float SoftLanding = 380;          // a ball landing slower than this (px/s, ~a 80 px drop) settles instead of bursting

    // Largest snowball allowed on a w × h screen: always finite, and never more than a small share of the
    // screen's shorter side, so even an Experimental ball can't bury the desktop
    public static float MaxRadius(float w, float h)
    {
        bool exp = GrowthMode == 3;
        float screenCap = MathF.Max(BallStartRadius + 1, MathF.Min(w, h) * (exp ? 0.12f : 0.09f));
        float want = exp ? ExperimentalMaxRadius : GrowthRadius[Math.Clamp(GrowthMode, 0, 2)];
        return Math.Clamp(want, BallStartRadius + 1, screenCap);
    }
    public const float LightSpacing = 22;          // distance between fairy light bulbs
    public const float LightSpan = 120;            // distance between the points where the wire is "hooked"
    public const float LightSag = 9;               // how far the wire droops between hooks
    public const float LightInset = 14;            // keep clear of the window's rounded corners
    public const float BulbGlow = 19;              // size of a bulb's soft glow
    public const float BulbCore = 5.5f;            // size of a bulb's bright center
    public static int LightPreset = 0;             // colors: see LightLook.PresetNames (tray menu)
    public static int LightPattern = 5;            // animation: see LightLook.PatternNames (tray menu); 5 = sparkle on & off
    public static bool WireSnow = true;            // snow slowly settles on the wires and shakes off (tray menu)
}

// Three depth layers: far = small, faint, slow; near = big, bright, fast
readonly record struct Layer(float SizeMin, float SizeMax, float SpeedMin, float SpeedMax,
                             float AlphaMin, float AlphaMax, float SwayMin, float SwayMax, float Wind);

static class Rnd
{
    static Random R = new();
    public static void Seed(int seed) => R = new Random(seed);   // same seed + same input = same snow (tests)
    public static float F(float a, float b) => a + (float)R.NextDouble() * (b - a);
}

// MARK: - Flake

sealed class Flake
{
    public float X, Y, Vx, Vy;
    public float Size = 4, Alpha = 1;
    public float SwayAmp, SwayFreq = 1, Phase, WindFactor = 1;
    public int Layer;                // 0 far, 1 middle, 2 near
    public bool Clump;               // sliding off a surface (affected by gravity)
    public bool InFront;             // falls in front of windows
    public bool Crystal, Sparkle;    // drawn as a turning six-armed crystal / glints now and then
    public long IgnoreId = -1;       // don't land right back on the surface it came from
    public float IgnoreUntil;
}

// MARK: - Snow pile (a height map drawn as a smooth white mound). y grows downwards.

sealed class Pile
{
    public float MinX, TopY;          // TopY = the edge the snow sits on
    public readonly float Width, MaxH;
    public readonly float[] H, Caps;  // current height / max height per column
    public readonly bool[] Occ;       // column hidden behind a window in front
    public bool Dirty;

    public Pile(float minX, float topY, float width, float maxH)
    {
        MinX = minX; TopY = topY; Width = width; MaxH = maxH;
        int n = Math.Max(1, (int)MathF.Ceiling(width / Cfg.ColW));
        H = new float[n]; Caps = new float[n]; Occ = new bool[n];
        for (int i = 0; i < n; i++)
        {
            float edgeDist = MathF.Min(i + 0.5f, n - i - 0.5f) * Cfg.ColW;
            float t = MathF.Min(1, edgeDist / (maxH * 2.2f));
            Caps[i] = maxH * MathF.Sqrt(t);   // taper at both ends so it looks rounded
        }
    }

    public int Count => H.Length;
    public float Total { get { float s = 0; foreach (var h in H) s += h; return s; } }
    public int Col(float x) { int c = (int)MathF.Floor((x - MinX) / Cfg.ColW); return c >= 0 && c < Count ? c : -1; }
    public float ColX(int c) => MinX + (c + 0.5f) * Cfg.ColW;
    public float Surface(int c) => TopY - H[c];

    static readonly (int, float)[] Spread = { (0, 1), (-1, .75f), (1, .75f), (-2, .45f), (2, .45f), (-3, .2f), (3, .2f) };

    public void Add(int c, float amount)
    {
        foreach (var (o, w) in Spread)
        {
            int i = c + o;
            if (i < 0 || i >= Count || Occ[i]) continue;
            H[i] = MathF.Min(Caps[i], H[i] + amount * w);
        }
        Dirty = true;
    }

    // Let snow slump sideways where the slope is too steep
    public void Relax()
    {
        if (!Dirty) return;
        bool moved = false;
        for (int i = 0; i < Count - 1; i++)
        {
            if (Occ[i] || Occ[i + 1]) continue;
            float d = H[i] - H[i + 1];
            if (MathF.Abs(d) > 1f)
            {
                float m = (MathF.Abs(d) - 1f) * 0.3f * MathF.Sign(d);
                H[i] = Math.Clamp(H[i] - m, 0, Caps[i]);
                H[i + 1] = Math.Clamp(H[i + 1] + m, 0, Caps[i + 1]);
                if (MathF.Abs(m) > 0.02f) moved = true;
            }
        }
        Dirty = moved;
    }

    public void Clear() { Array.Clear(H); Dirty = true; }
}

record struct Win(long Id, float L, float T, float R, float B)
{
    public float Width => R - L;
    public bool Contains(float x, float y) => x >= L && x < R && y >= T && y < B;
}

// MARK: - Snowball

sealed class Snowball
{
    public float X, Y, R = Cfg.BallStartRadius, Vx, Vy, Rot;
    public Pile Pile;                // the surface it rests on (null while falling, dragged or stacked)
    public Snowball Below;           // the ball it is stacked on (snowman), if any
    public bool Falling, Dragging, Hidden;
    public int Decor;                // 0 none, 1 snowman body, 2 snowman head
    public float MaxedAt = -1;       // when it reached its size limit (for the little "full size" flash)
    public bool Driven;              // pulled by the cursor this frame
    public float TrailAcc;           // distance rolled since the last trail mark
    public float Wobble, WobbleV;    // tilt of a stack resting on this ball (rad, + leans right) and its speed
    public bool NoseLost;            // snowman head whose carrot was knocked off
    public bool Thrown;              // flying after a throw: bursts on whatever it hits
    public float ThrownAt, FromX, FromY;   // when and where it was thrown from
    public Visitor ThrownBy;         // a visitor's throw doesn't hit the thrower
    public float Mass => MathF.Max(1, R * R / (Cfg.BallMassRadius * Cfg.BallMassRadius));
    public readonly (float Ang, float Dist, float Size, float Alpha)[] Specks = new (float, float, float, float)[20];

    public Snowball()
    {
        // Little grey-blue specks that turn with the ball so you can see it roll
        for (int i = 0; i < Specks.Length; i++)
            Specks[i] = (Rnd.F(0, 6.28f), MathF.Sqrt(Rnd.F(0, 1)) * 0.8f, Rnd.F(0.03f, 0.07f), Rnd.F(0.25f, 0.55f));
    }

    public void Drop(float vx, float vy)
    {
        Falling = true; Dragging = false; Hidden = false; Thrown = false;
        Pile = null; Below = null;
        Vx = vx; Vy = vy;
    }
}

// A small animal wandering along the bottom of the screen
sealed class Visitor
{
    public int Kind;                 // 0 penguin, 1 snow fox, 2 rabbit
    public float X, Y, Dir, Hop, Anim, PauseLeft, StepAcc, HopT = -1;
    public bool Hidden, TurnedBack;
    public int Foot;
    public int Act;                  // what it's busy with besides wandering (VisitorAct), 0 = nothing
    public float ActT, ActLen;       // time into the act, and how long it lasts
    public float PushDir = 1;        // which way the snowball that hit it was flying
    public float FromX, FromY;       // where that snowball was thrown from
    public float SlideV, JumpH, Cool, Hurry, NextHabit = 10, AwayUntil;
    public bool Away;                // the rabbit is hiding off-screen
    public Snowball Target;          // the ball the fox is chasing
    // drawing: which way it faces, a shake offset, and its pose (0 standing, 1 on its belly, 2 sitting, 3 flipper up)
    public float Face = 1, Jitter;
    public int Pose;
    public bool Walking => PauseLeft <= 0 && Act == 0;
    public bool Reacting => Act is VisitorAct.Tumble or VisitorAct.Leap or VisitorAct.Startle or VisitorAct.Flee or VisitorAct.ThrowBack;
}

static class VisitorAct
{
    public const int Tumble = 1, Slip = 2, Sit = 3, Kick = 4, ThrowBack = 5,   // penguin
                     Leap = 6, JumpOver = 7, Chase = 8,                       // snow fox
                     Startle = 9, Flee = 10;                                 // rabbit
}

// A snowman's nose after a good knock: it tumbles down, lies in the snow for a while, then fades
sealed class Carrot
{
    public float X, Y, Vx, Vy, Rot, Spin, Len, RestAt = -1;
}

// MARK: - Fairy lights

sealed class Bulb
{
    public float X, Y;
    public readonly float Phase = Rnd.F(0, 6.28f), Speed = Rnd.F(1.2f, 3f);
    public readonly float Phase2 = Rnd.F(0, 6.28f), Speed2 = Rnd.F(0.5f, 1.3f);
    public readonly float Base = Rnd.F(0.82f, 1f);   // every bulb is a little brighter or dimmer than the next
    public bool Hidden;
}

// A string of fairy lights hanging just below the top edge of one window
sealed class LightString
{
    public const float SwingTime = 1.4f;
    public Win Rect;
    public float HungAt, Swing, HangSway;
    public float Sway, SwayV;        // gentle sideways swing of the bulbs (a damped spring pushed by wind and window moves)
    public float Snow;               // snow settled on the wire, 0..1
    public readonly List<Bulb> Bulbs = new();
    public readonly List<(float X, float Y, bool Hidden)> Wire = new();

    public bool Animating(float t) => t - HungAt < SwingTime;

    // Just after being hung, the string bounces and the bulbs swing, then settle down (a damped wobble)
    public void UpdateSwing(float t)
    {
        float tau = t - HungAt;
        if (tau >= SwingTime) { Swing = 0; HangSway = 0; return; }
        float e = MathF.Exp(-4 * tau);
        Swing = 0.9f * e * MathF.Sin(13 * tau);
        HangSway = 3.5f * e * MathF.Sin(9.5f * tau);
    }

    // Bulbs light up one after another from left to right as the string is hung
    public float Reveal(int i, float t) => Math.Clamp((t - HungAt - i * 0.015f) / 0.08f, 0, 1);

    // The wire droops between hooks like a real string of lights
    public float WireY(float x)
    {
        float x0 = Rect.L + Cfg.LightInset;
        float w = MathF.Max(1, Rect.Width - 2 * Cfg.LightInset);
        float span = w / MathF.Max(1, MathF.Round(w / Cfg.LightSpan));
        float f = Math.Clamp(((x - x0) / span) % 1, 0, 1);
        return Rect.T + 3 + Cfg.LightSag * (1 + Swing) * MathF.Sin(MathF.PI * f);
    }

    public void Place(Win r)
    {
        float w = MathF.Max(0, r.Width - 2 * Cfg.LightInset);
        int count = Math.Max(2, (int)(w / Cfg.LightSpacing));
        Rect = r;
        if (Bulbs.Count != count)
        {
            Bulbs.Clear();
            for (int i = 0; i < count; i++) Bulbs.Add(new Bulb());
        }
        for (int i = 0; i < Bulbs.Count; i++)
        {
            var b = Bulbs[i];
            float x = r.L + Cfg.LightInset + (i + 0.5f) * w / Bulbs.Count;
            b.X = x + (HangSway + Sway) * (0.6f + 0.4f * MathF.Sin(b.Phase));   // each bulb swings a little differently
            b.Y = WireY(x) + 4;                                                  // bulbs hang just under the wire
        }
    }

    // Rebuild the wire and hide the parts that are behind a window in front
    public void ApplyVisibility(Func<float, float, bool> hidden)
    {
        Wire.Clear();
        for (float x = Rect.L + Cfg.LightInset * 0.5f; x <= Rect.R - Cfg.LightInset * 0.5f; x += 3)
        {
            float y = WireY(x);
            Wire.Add((x, y, hidden(x, y)));
        }
        foreach (var b in Bulbs) b.Hidden = hidden(b.X, b.Y);
    }
}

// How the lights look: color presets and gentle animation patterns (shared by the simulation and tests)
static class LightLook
{
    public static readonly string[] PresetNames = { "Classic Christmas", "Warm White", "Arctic Blue", "Candy Colors", "Rainbow", "Golden Christmas" };
    public static readonly string[] PatternNames = { "Gentle twinkle", "Slow breathing", "Cascade", "Alternating", "Festive sparkle", "Sparkle on && off" };
    public static readonly int[] MenuOrder = { 5, 0, 1, 2, 3, 4 };   // the favorite first

    static readonly (float R, float G, float B)[][] Palettes =
    {
        new[] { (1.00f, 0.10f, 0.12f), (1.00f, 0.62f, 0.00f), (0.05f, 0.95f, 0.20f), (0.10f, 0.42f, 1.00f), (1.00f, 0.15f, 0.62f) },
        new[] { (1.00f, 0.80f, 0.52f), (1.00f, 0.86f, 0.62f), (1.00f, 0.76f, 0.46f) },
        new[] { (0.45f, 0.75f, 1.00f), (0.65f, 0.88f, 1.00f), (0.35f, 0.60f, 1.00f), (0.82f, 0.94f, 1.00f) },
        new[] { (1.00f, 0.42f, 0.68f), (0.50f, 0.95f, 0.80f), (1.00f, 0.92f, 0.45f), (0.72f, 0.58f, 1.00f), (1.00f, 0.62f, 0.45f) },
        null,   // Rainbow: computed, slowly drifting along the string
        new[] { (1.00f, 0.72f, 0.22f), (1.00f, 0.84f, 0.40f), (0.95f, 0.60f, 0.12f), (1.00f, 0.90f, 0.55f) },
    };

    public static (float R, float G, float B) Color(int preset, int i, float t)
    {
        var pal = Palettes[Math.Clamp(preset, 0, Palettes.Length - 1)];
        if (pal != null) return pal[i % pal.Length];
        float h = (i / 8f + t * 0.02f) % 1 * 6;             // hue, 0..6
        float x = 1 - MathF.Abs(h % 2 - 1);
        var c = h < 1 ? (1, x, 0) : h < 2 ? (x, 1, 0) : h < 3 ? (0, 1, x) : h < 4 ? (0, x, 1) : h < 5 ? (x, 0, 1) : (1f, 0f, x);
        return (0.15f + 0.85f * c.Item1, 0.15f + 0.85f * c.Item2, 0.15f + 0.85f * c.Item3);
    }

    // How bright bulb i is at time t (0.3..1.15). Every pattern changes smoothly: no hard flicker.
    public static float Brightness(int pattern, Bulb b, int i, float t)
    {
        float k;
        switch (pattern)
        {
            case 1:                                          // slow breathing, all together
                k = 0.55f + 0.45f * (0.5f + 0.5f * MathF.Sin(t * 0.9f + b.Phase * 0.05f));
                break;
            case 2:                                          // a wave of light running along the string
            {
                float w = 0.5f + 0.5f * MathF.Sin(t * 2.2f - i * 0.45f);
                k = 0.4f + 0.6f * w * w;
                break;
            }
            case 3:                                          // odd and even bulbs take turns, fading over
            {
                float m = 0.5f + 0.5f * MathF.Sin(t * MathF.PI / 1.6f);
                k = 0.4f + 0.6f * (i % 2 == 0 ? m : 1 - m);
                break;
            }
            case 4:                                          // steady glow, with random bulbs flaring up briefly
            {
                k = 0.72f + 0.06f * MathF.Sin(t * b.Speed + b.Phase);
                float slot = t * 2.5f;
                int n = (int)MathF.Floor(slot);
                if (Hash(i, n) < 0.12f) k += 0.38f * MathF.Sin(MathF.PI * (slot - n));
                break;
            }
            case 5:                                          // each bulb blinks on and off on its own rhythm, with soft fades
                k = 0.08f + 1.02f * On(b, t);
                return Math.Clamp(k, 0.05f, 1.15f);
            default:                                         // gentle, irregular twinkle (two unrelated waves)
                k = b.Base * (0.8f + 0.12f * MathF.Sin(t * b.Speed + b.Phase) + 0.08f * MathF.Sin(t * b.Speed2 * 1.618f + b.Phase2));
                break;
        }
        return Math.Clamp(k, 0.3f, 1.15f);
    }

    // 0 = off, 1 = on, with a quick smooth fade between (about a fifth of a second)
    static float On(Bulb b, float t)
    {
        float x = Math.Clamp(MathF.Sin(t * b.Speed * 1.3f + b.Phase) * 2 + 0.6f, -0.5f, 0.5f) + 0.5f;   // on ~60% of the time
        return x * x * (3 - 2 * x);
    }

    // A star-shaped glint on a lit bulb now and then (0 = none, 1 = full)
    public static float Glint(int pattern, Bulb b, int i, float t)
    {
        float burst = MathF.Pow(MathF.Max(0, MathF.Sin(t * b.Speed2 * 2.1f + b.Phase2 + i)), 10);
        return pattern == 5 ? On(b, t) * burst : MathF.Max(0, Brightness(pattern, b, i, t) - 0.85f) / 0.3f * burst;
    }

    static float Hash(int i, int n)
    {
        uint h = (uint)(i * 73856093) ^ (uint)(n * 19349663);
        h ^= h >> 13; h *= 0x5bd1e995; h ^= h >> 15;
        return (h & 0xFFFF) / 65535f;
    }
}

// MARK: - The snow world (all coordinates in logical pixels, y down)

sealed class SnowWorld
{
    static readonly Layer[] Layers =
    {
        new(1.8f, 3.0f, 22, 38,  0.30f, 0.50f, 5, 10,  0.5f),
        new(3.0f, 4.8f, 40, 65,  0.60f, 0.80f, 9, 16,  0.8f),
        new(4.8f, 7.5f, 70, 105, 0.85f, 1.00f, 14, 24, 1.1f),
    };

    public float W, H;
    public readonly List<Flake> Flakes = new();
    readonly Stack<Flake> pool = new();
    public readonly Dictionary<long, Pile> Piles = new();
    readonly Dictionary<long, Win> lastRect = new();
    public List<Win> Order = new();              // windows from front to back
    public Pile Ground;
    public bool GroundIsTaskbar;
    float pollAcc, t;
    public float T => t;
    float ballMax = Cfg.MaxRadius(1920, 1080);   // snowball size limit for this screen, refreshed every frame
    const long GroundId = -2;

    public readonly List<Snowball> Balls = new();
    public readonly List<Carrot> Carrots = new();
    Snowball activeBall, dragBall;
    public Snowball DragBall => dragBall;
    public float MouseX, MouseY;                 // cursor, set by the caller every frame
    public bool AimKey;                          // Shift held: slingshot aiming (set by the caller)
    public bool SlingUsed;                       // the slingshot has been used (then the tip isn't needed)
    bool aiming;
    float aimX, aimY, launchVx, launchVy;
    public readonly float[] Path = new float[96];   // predicted flight (1.6 s) of the ball being thrown: x, y pairs
    public int PathCount;
    float lastMx = float.NaN, dragOffX, dragOffY, dragVx, dragVy;

    public bool LightsOn;
    public readonly Dictionary<long, LightString> Lights = new();

    // Rolling trails: a fixed ring of dents, so playing never allocates. A dent belongs to its surface
    // (it moves with the window) and is only drawn where snow is still lying.
    public struct Mark { public Pile Pile; public float Dx, Size, Born, Alpha, Life; }
    public readonly Mark[] Marks = new Mark[256];
    int markNext;

    public SnowWorld(float w, float h)
    {
        W = w; H = h;
        ResetGround(h, false);
        for (int i = 0; i < Cfg.TargetFlakes; i++) SpawnFlake(Rnd.F(0, H));
    }

    public void Resize(float w, float h)
    {
        W = w; H = h;
        ResetGround(h, false);
    }

    void ResetGround(float top, bool taskbar)
    {
        if (Ground != null) Release(Ground, 0, 0, GroundId);   // also drops the snowballs on it
        GroundIsTaskbar = taskbar;
        Ground = new Pile(0, top, W, taskbar ? Cfg.TaskbarPileMax : Cfg.GroundPileMax);
    }

    public void ClearAll()
    {
        foreach (var p in Piles.Values) p.Clear();
        Ground.Clear();
        Balls.Clear();
        Carrots.Clear();
        activeBall = dragBall = null;
        Array.Clear(Marks);
    }

    // MARK: Flake pool

    Flake Obtain()
    {
        var f = pool.Count > 0 ? pool.Pop() : new Flake();
        Flakes.Add(f);
        return f;
    }

    void Recycle(int i)
    {
        pool.Push(Flakes[i]);
        Flakes[i] = Flakes[^1];
        Flakes.RemoveAt(Flakes.Count - 1);
    }

    void SpawnFlake(float? y = null)
    {
        var f = Obtain();
        float r = Rnd.F(0, 1);
        int li = r < 0.40f ? 0 : (r < 0.68f ? 1 : 2);   // 40% far, 28% middle, 32% near
        var L = Layers[li];
        f.Layer = li;
        f.Clump = false;
        f.InFront = Cfg.FlakesInFront && li == 0 && Rnd.F(0, 1) < Cfg.InFrontFraction;
        f.IgnoreId = -1;
        f.Size = Rnd.F(L.SizeMin, L.SizeMax);
        f.Vx = 0;
        f.Vy = Rnd.F(L.SpeedMin, L.SpeedMax) * Cfg.FallSpeed;
        f.SwayAmp = Rnd.F(L.SwayMin, L.SwayMax);
        f.SwayFreq = Rnd.F(0.6f, 1.6f);
        f.Phase = Rnd.F(0, 6.28f);
        f.WindFactor = L.Wind;
        f.Alpha = Rnd.F(L.AlphaMin, L.AlphaMax);
        f.X = Rnd.F(-40, W + 40);
        f.Y = y ?? -Rnd.F(2, 30);
        float look = Rnd.F(0, 1);
        f.Crystal = Cfg.FancyFlakes && li == 2 && look < 0.3f;     // a third of the near flakes
        f.Sparkle = Cfg.FancyFlakes && Rnd.F(0, 1) < 0.06f;       // one flake in sixteen
    }

    void SpawnClump(float x, float y, float vx, float vy, long sourceId)
    {
        var f = Obtain();
        f.Clump = true;
        f.InFront = false;
        f.Crystal = f.Sparkle = false;
        f.Layer = 2;
        f.Size = Rnd.F(2.5f, 5);
        f.Alpha = 0.95f;
        f.Vx = vx; f.Vy = vy;
        f.IgnoreId = sourceId;
        f.IgnoreUntil = t + 0.35f;
        f.X = x; f.Y = y;
    }

    // Turn a pile into falling clumps; (vx, vy) is the window's velocity, so faster drags fling snow farther
    void Release(Pile p, float vx, float vy, long id)
    {
        float fx = Math.Clamp(vx * 0.5f, -900, 900);
        float fy = Math.Clamp(vy * 0.4f, -500, 300);
        // Snowballs sitting on this surface get flung off too
        foreach (var b in Balls) if (b.Pile == p) b.Drop(fx + Rnd.F(-30, 30), fy - Rnd.F(20, 80));
        float total = p.Total;
        if (total > 0.5f)
        {
            float budget = Math.Max(0, Cfg.MaxClumps - Flakes.Count + Cfg.TargetFlakes);
            float k = MathF.Min(1, budget / MathF.Max(1, total / 3.5f));
            for (int c = 0; c < p.Count; c++)
            {
                if (p.H[c] <= 0.6f) continue;
                float amount = p.H[c] / 3.5f * k;
                while (amount > 0)
                {
                    if (amount < 1 && Rnd.F(0, 1) > amount) break;
                    SpawnClump(p.ColX(c) + Rnd.F(-1.5f, 1.5f), p.TopY - Rnd.F(0, p.H[c]),
                               fx + Rnd.F(-40, 40), fy - Rnd.F(10, 80), id);
                    amount -= 1;
                }
            }
        }
        p.Clear();
    }

    // MARK: Per-frame update

    public void Update(float dt, Func<List<Win>> readWindows, Func<float?> readTaskbarTop)
    {
        t += dt;
        ballMax = Cfg.MaxRadius(W, H);
        pollAcc += dt;
        if (pollAcc >= Cfg.PollInterval)
        {
            PollWindows(pollAcc, readWindows(), readTaskbarTop());
            pollAcc = 0;
        }
        StepFlakes(dt);
        StepVisitors(dt);
        if (dragBall != null) StepDrag(dragBall, dt); else UpdateRolling(dt);
        StepBalls(dt);
        if (LightsOn)
        {
            foreach (var s in Lights.Values) if (s.Animating(t)) { UpdateLights(); break; }   // smooth hang-up bounce
            StepLights(dt);
        }
    }

    void PollWindows(float dt, List<Win> wins, float? taskbarTop)
    {
        // Taskbar at the bottom: the snow on the "ground" sits on top of it
        float groundTop = taskbarTop ?? H;
        bool taskbar = taskbarTop.HasValue;
        if (MathF.Abs(groundTop - Ground.TopY) > 1 || taskbar != GroundIsTaskbar) ResetGround(groundTop, taskbar);

        // Closed / minimized windows: their snow just falls
        var alive = new HashSet<long>();
        foreach (var w in wins) alive.Add(w.Id);
        var gone = new List<long>();
        foreach (var kv in Piles) if (!alive.Contains(kv.Key)) gone.Add(kv.Key);
        foreach (var id in gone)
        {
            Release(Piles[id], 0, 0, id);
            Piles.Remove(id);
            lastRect.Remove(id);
        }

        foreach (var r in wins)
        {
            if (Piles.TryGetValue(r.Id, out var pile) && lastRect.TryGetValue(r.Id, out var old))
            {
                if (MathF.Abs(old.Width - r.Width) > 0.5f)
                {
                    // Width changed: drop the snow and start a new pile
                    Release(pile, 0, 0, r.Id);
                    Piles[r.Id] = new Pile(r.L, r.T, r.Width, Cfg.WindowPileMax);
                }
                else if (MathF.Abs(old.L - r.L) > 0.3f || MathF.Abs(old.T - r.T) > 0.3f)
                {
                    // Window dragged: fling the snow off
                    float vx = (r.L - old.L) / dt, vy = (r.T - old.T) / dt;
                    pile.MinX = r.L;
                    pile.TopY = r.T;
                    Release(pile, vx, vy, r.Id);
                }
            }
            else
            {
                Piles[r.Id] = new Pile(r.L, r.T, r.Width, Cfg.WindowPileMax);
            }
            lastRect[r.Id] = r;
        }
        Order = wins;

        ComputeOcclusion();
        UpdateLights();
        foreach (var p in Piles.Values) p.Relax();
        Ground.Relax();
    }

    // MARK: Fairy lights

    // Hang a string of lights on every window, following it when it moves
    public void UpdateLights()
    {
        if (!LightsOn) { Lights.Clear(); return; }
        var alive = new HashSet<long>();
        for (int i = 0; i < Order.Count; i++)
        {
            var w = Order[i];
            alive.Add(w.Id);
            if (!Lights.TryGetValue(w.Id, out var s)) Lights[w.Id] = s = new LightString { HungAt = t, Rect = w };
            if (s.Rect != w || s.Animating(t))
            {
                float dx = w.L - s.Rect.L, dy = w.T - s.Rect.T;
                if (dx != 0 || dy != 0)
                {
                    s.SwayV -= dx * 4;                                   // the bulbs lag behind a moving window...
                    if (MathF.Abs(dx) + MathF.Abs(dy) > 1.5f) ShakeWire(s);   // ...and the snow on the wire falls off
                }
                s.UpdateSwing(t);
                s.Place(w);
            }
            int front = i;
            s.ApplyVisibility((x, y) =>
            {
                if (y < 0 || (GroundIsTaskbar && y >= Ground.TopY)) return true;
                for (int j = 0; j < front; j++) if (Order[j].Contains(x, y)) return true;
                return false;
            });
        }
        var gone = new List<long>();
        foreach (var id in Lights.Keys) if (!alive.Contains(id)) gone.Add(id);
        foreach (var id in gone) Lights.Remove(id);
    }

    // Every frame: the bulbs sway in the wind like a damped spring, and snow slowly settles on the wire
    void StepLights(float dt)
    {
        foreach (var s in Lights.Values)
        {
            s.SwayV += (-40 * s.Sway - 2.2f * s.SwayV + Wind * 1.2f) * dt;
            s.Sway = Math.Clamp(s.Sway + s.SwayV * dt, -7, 7);
            s.Place(s.Rect);
            if (!Cfg.WireSnow) s.Snow = 0;
            else if (MathF.Abs(s.SwayV) > 45) ShakeWire(s);        // a big swing shakes it off
            else s.Snow = MathF.Min(1, s.Snow + dt / 90 * Cfg.TargetFlakes / 260f);
        }
    }

    void ShakeWire(LightString s)
    {
        int n = (int)(s.Snow * 12);
        for (int k = 0; k < n && s.Wire.Count > 0; k++)
        {
            var p = s.Wire[(int)Rnd.F(0, s.Wire.Count - 0.01f)];
            if (!p.Hidden) SpawnClump(p.X, p.Y - 1, Rnd.F(-30, 30), Rnd.F(-20, 20), -3);
        }
        s.Snow = 0;
    }

    // MARK: Dragging & snowmen

    // The visible, resting snowball under a point (top of a stack first)
    public Snowball BallAt(float mx, float my)
    {
        Snowball best = null;
        foreach (var b in Balls)
        {
            if (b.Falling || b.Hidden) continue;
            float dx = mx - b.X, dy = my - b.Y;
            if (dx * dx + dy * dy <= (b.R + 2) * (b.R + 2) && (best == null || b.Y < best.Y)) best = b;
        }
        return best;
    }

    Snowball Above(Snowball b)
    {
        foreach (var x in Balls) if (x.Below == b) return x;
        return null;
    }

    // A stack (snowman) weighs as much as all of its balls
    float StackMass(Snowball b)
    {
        float m = 0;
        for (int n = 0; b != null && n < 10; n++, b = Above(b)) m += b.Mass;
        return m;
    }

    static Snowball Base(Snowball b) { while (b.Below != null) b = b.Below; return b; }

    // Balls in a stack from the bottom up
    List<Snowball> Stack(Snowball bottom)
    {
        var st = new List<Snowball> { bottom };
        for (var a = Above(bottom); a != null && st.Count < 10; a = Above(a)) st.Add(a);
        return st;
    }

    void RemoveDecor(Snowball b) { foreach (var x in Stack(Base(b))) x.Decor = 0; }

    public bool BeginDrag(float mx, float my)
    {
        var b = BallAt(mx, my);
        if (b == null) return false;
        // Anything stacked on top of the grabbed ball tumbles off
        for (var a = Above(b); a != null;)
        {
            var next = Above(a);
            a.Drop(Rnd.F(-50, 50), -Rnd.F(40, 100));
            a = next;
        }
        RemoveDecor(b);
        b.Pile = null;
        b.Below = null;
        b.Dragging = true;
        dragBall = b;
        activeBall = null;
        dragOffX = b.X - mx; dragOffY = b.Y - my;
        dragVx = dragVy = 0;
        return true;
    }

    public void EndDrag()
    {
        var b = dragBall;
        if (b == null) return;
        dragBall = null;
        b.Dragging = false;
        PathCount = 0;
        bool sling = aiming;
        aiming = false;
        SlingUsed |= sling;
        if (sling || dragVx * dragVx + dragVy * dragVy > Cfg.ThrowSpeed * Cfg.ThrowSpeed)
        {
            var (vx, vy) = sling ? Launch(b, launchVx, launchVy) : Launch(b, dragVx, dragVy);
            if (vx * vx + vy * vy > 60 * 60) { Throw(b, vx, vy); return; }   // a barely pulled slingshot just lets go
        }
        var target = StackTarget(b);
        if (target != null)
        {
            b.Below = target;
            b.Vx = b.Vy = 0;
            var st = Stack(Base(b));
            if (st.Count == 3) { st[1].Decor = 1; st[2].Decor = 2; st[2].NoseLost = false; }   // three balls make a snowman
        }
        else
        {
            // Let go: it falls (with a bit of the throw) and lands on whatever is below
            b.Drop(Math.Clamp(dragVx * 0.5f, -600, 600), Math.Clamp(dragVy * 0.5f, -400, 400));
        }
    }

    void StepDrag(Snowball b, float dt)
    {
        lastMx = MouseX;
        if (AimKey)
        {
            // Slingshot: the ball stays where it is; pull the mouse back and let go to throw
            if (!aiming) { aiming = true; aimX = b.X; aimY = b.Y; }
            b.X = aimX; b.Y = aimY;
            launchVx = (aimX - (MouseX + dragOffX)) * Cfg.SlingGain;
            launchVy = (aimY - (MouseY + dragOffY)) * Cfg.SlingGain;
            var (lx, ly) = Launch(b, launchVx, launchVy);
            Predict(b.X, b.Y, b.R, lx, ly);
            return;
        }
        if (aiming)
        {
            aiming = false;                  // Shift released: carry it on from here
            dragOffX = b.X - MouseX; dragOffY = b.Y - MouseY;
            dragVx = dragVy = 0;
        }
        float tx = MouseX + dragOffX, ty = MouseY + dragOffY;
        if (dt > 0)
        {
            dragVx = dragVx * 0.7f + (tx - b.X) / dt * 0.3f;
            dragVy = dragVy * 0.7f + (ty - b.Y) / dt * 0.3f;
        }
        b.Rot += (tx - b.X) / b.R;
        b.X = tx; b.Y = ty;
        // While it's being flicked, show where it would fly
        if (dragVx * dragVx + dragVy * dragVy > Cfg.ThrowSpeed * Cfg.ThrowSpeed)
        {
            var (vx, vy) = Launch(b, dragVx, dragVy);
            Predict(b.X, b.Y, b.R, vx, vy);
        }
        else PathCount = 0;
    }

    // How fast a ball leaves the hand: heavier balls need more force, so they fly slower
    static (float, float) Launch(Snowball b, float vx, float vy)
    {
        float k = 1 / MathF.Sqrt(b.Mass), sp = MathF.Sqrt(vx * vx + vy * vy) * k;
        if (sp > Cfg.ThrowMax) k *= Cfg.ThrowMax / sp;
        return (vx * k, vy * k);
    }

    public void Throw(Snowball b, float vx, float vy, Visitor by = null)
    {
        b.Drop(vx, vy);
        b.Thrown = true;
        b.ThrownAt = t;
        b.FromX = b.X; b.FromY = b.Y;
        b.ThrownBy = by;
    }

    // Trace the first 1.6 s of a throw (same gravity and surfaces as the real flight), for the dotted guide
    void Predict(float x, float y, float r, float vx, float vy)
    {
        PathCount = 0;
        const float h = 1f / 30;
        for (int i = 0; i < Path.Length / 2; i++)
        {
            float px = x, py = y;
            vy += Cfg.Gravity * h;
            x += vx * h;
            y += vy * h;
            if (x < -50 || x > W + 50 || y > H + 50) break;
            if (vy > 0 && LandingSurface(x, py + r, y + r, onEdge: true) != null) break;
            if (HitsWindow(x, y, px, py)) break;
            Path[2 * PathCount] = x;
            Path[2 * PathCount + 1] = y;
            PathCount++;
        }
    }

    // Did this step carry a point into a window from outside it?
    bool HitsWindow(float x, float y, float px, float py)
    {
        foreach (var w in Order) if (w.Contains(x, y) && !w.Contains(px, py)) return true;
        return false;
    }

    // A thrown snowball bursts on the first thing it meets: a visitor, a snowball or snowman, or a window
    bool ThrownHit(Snowball b, float px, float py)
    {
        foreach (var v in Visitors)
        {
            if (b.ThrownBy == v || !VisitorHit(v, b)) continue;   // (the penguin's throw can hit the others)
            Puff(b.X, b.Y, b.R, b.Vx * 0.2f);
            HitVisitor(v, b);
            return true;
        }
        foreach (var o in Balls)
        {
            if (o == b || o.Falling || o.Dragging || o.Hidden) continue;
            float dx = o.X - b.X, dy = o.Y - b.Y, rr = o.R + b.R;
            if (dx * dx + dy * dy > rr * rr) continue;
            var bottom = Base(o);
            if (bottom.Pile != null)
            {
                float j = b.Vx * b.Mass;
                bottom.Vx += j / StackMass(bottom) * 0.8f;
                // hit low, the top lags behind; hit high (the head), the top is knocked over
                Strike(bottom, MathF.Abs(j), o == bottom ? -MathF.Sign(b.Vx) : 2 * MathF.Sign(b.Vx));
            }
            Puff(b.X, b.Y, b.R, b.Vx * 0.2f);
            return true;
        }
        if (HitsWindow(b.X, b.Y, px, py)) { Puff(b.X, b.Y, b.R, -b.Vx * 0.15f); return true; }
        return false;
    }

    // Is a snowball touching a visitor's body?
    static bool VisitorHit(Visitor v, Snowball b)
    {
        if (v.Hidden) return false;
        float r = v.Kind == 0 ? 11 : v.Kind == 1 ? 12 : 9;
        float cy = v.Y - v.Hop - (v.Kind == 0 ? 13 : v.Kind == 1 ? 11 : 9);
        float dx = b.X - v.X, dy = b.Y - cy;
        return dx * dx + dy * dy <= (r + b.R) * (r + b.R);
    }


    // A burst of powder flying out from a point
    void Puff(float x, float y, float r, float vx)
    {
        for (int k = 0; k < Math.Min(24, (int)r + 6); k++)
        {
            float ang = Rnd.F(0, 6.28f), d = Rnd.F(0, r);
            SpawnClump(x + MathF.Cos(ang) * d, y + MathF.Sin(ang) * d,
                       vx + MathF.Cos(ang) * Rnd.F(60, 200), MathF.Sin(ang) * Rnd.F(60, 200) - 120, -3);
        }
    }

    // A ball dropped just above another ball (that isn't already a full snowman) gets stacked on it
    Snowball StackTarget(Snowball b)
    {
        Snowball best = null;
        float bestDist = float.MaxValue;
        foreach (var c in Balls)
        {
            if (c == b || c.Falling || c.Dragging || c.Hidden) continue;
            if (Above(c) != null || Stack(Base(c)).Count >= 3) continue;
            float dx = MathF.Abs(b.X - c.X), dy = c.Y - b.Y;
            if (dx >= c.R * 0.9f + b.R * 0.3f || dy <= 0 || dy >= c.R + b.R + 30) continue;
            if (dx < bestDist) { best = c; bestDist = dx; }
        }
        return best;
    }

    // MARK: Snowballs

    // Is the mouse inside the snow band of this pile (a bit below the edge up to a bit above the snow)?
    static bool MouseOver(Pile p, float mx, float my)
    {
        int c = p.Col(mx);
        if (c < 0 || p.Occ[c]) return false;
        return my <= p.TopY + 6 && my >= p.TopY - p.H[c] - 14;
    }

    // Which snow surface is the mouse over? Taskbar first, then windows front to back, then the ground.
    Pile SurfaceUnderMouse(float mx, float my)
    {
        // Hovering over a snowball counts as hovering over the snow it sits on, so big balls keep rolling smoothly
        foreach (var b in Balls)
        {
            if (b.Falling || b.Dragging || b.Hidden || b.Below != null || b.Pile == null) continue;
            if (MathF.Abs(mx - b.X) < b.R + 8 && my <= b.Pile.TopY + 6 && my >= b.Pile.TopY - 2 * b.R - 8) return b.Pile;
        }
        if (GroundIsTaskbar)
        {
            if (MouseOver(Ground, mx, my)) return Ground;
            if (my >= Ground.TopY) return null;          // over the taskbar itself
        }
        foreach (var w in Order)
        {
            if (Piles.TryGetValue(w.Id, out var p) && MouseOver(p, mx, my)) return p;
            if (w.Contains(mx, my)) return null;         // over a window's content
        }
        return MouseOver(Ground, mx, my) ? Ground : null;
    }

    void UpdateRolling(float dt)
    {
        float mx = MouseX, my = MouseY;
        float dx = float.IsNaN(lastMx) ? 0 : mx - lastMx;
        lastMx = mx;
        var p = SurfaceUnderMouse(mx, my);
        var b = activeBall;
        // Mouse left the snow (or the ball left its surface): the ball rolls on by itself
        if (b != null && (p == null || b.Pile != p || Above(b) != null)) b = activeBall = null;
        if (p == null) return;

        // A sideways movement picks up the resting ball under the mouse, or starts a new one. A heavy ball
        // trails the cursor, so how far it may fall behind before the cursor lets go grows with its weight.
        bool fresh = b == null || MathF.Abs(b.X - mx) > b.R + 60 + 40 * MathF.Sqrt(b.Mass);
        if (MathF.Abs(dx) > 0.2f && fresh)
        {
            b = null;
            foreach (var o in Balls)
                if (!o.Falling && o.Pile == p && MathF.Abs(o.X - mx) < o.R + 10) { b = o; break; }
            if (b != null && Above(b) != null) b = null;              // don't roll the bottom of a snowman
            else if (b == null)
            {
                int c = p.Col(mx);
                if (c >= 0 && p.H[c] > 1.5f)
                {
                    b = new Snowball { X = mx, Pile = p };
                    b.Y = p.TopY - b.R + 1.5f;
                    Balls.Add(b);
                    activeBall = b;          // set before trimming so the new ball is never the one removed
                    TrimBalls();
                }
            }
            activeBall = b;
        }
        if (b == null) return;

        // The cursor pulls the ball like a critically damped spring that also matches the mouse's speed:
        // a small ball follows at once, a heavy one takes its time to get going (and to stop)
        float m = b.Mass, lo = p.MinX + b.R, hi = p.MinX + p.Width - b.R;
        float target = lo < hi ? Math.Clamp(mx, lo, hi) : b.X;
        // Match the mouse's speed only once the ball is already rolling (a cursor arriving from far away is not a fling)
        float mouseV = target == mx && dt > 0 && !fresh ? Math.Clamp(dx / dt, -1500, 1500) : 0;
        b.Vx += (Cfg.DriveStiffness * (target - b.X) + 2 * MathF.Sqrt(Cfg.DriveStiffness * m) * (mouseV - b.Vx)) / m * dt;
        b.Driven = true;
    }

    // Roll a ball to newX: it picks up snow from the columns it passes over and grows
    void Roll(Snowball b, Pile p, float newX)
    {
        float moved = newX - b.X;
        if (MathF.Abs(moved) <= 0.01f) return;
        if (b.R < ballMax)
        {
            float collected = 0;
            float from = MathF.Min(b.X, newX) - b.R * 0.8f, to = MathF.Max(b.X, newX) + b.R * 0.8f;
            float frac = MathF.Min(0.6f, 0.6f * MathF.Abs(moved) / (1.6f * b.R));
            int c0 = p.Col(MathF.Max(from, p.MinX)), c1 = p.Col(MathF.Min(to, p.MinX + p.Width - 0.1f));
            if (c0 >= 0 && c1 >= c0)
            {
                for (int c = c0; c <= c1; c++)
                {
                    if (p.Occ[c]) continue;
                    float take = p.H[c] * frac;
                    p.H[c] -= take;
                    collected += take;
                }
                p.Dirty = true;
            }
            // Ball area grows by the snow area it picked up
            float area = MathF.PI * b.R * b.R + collected * Cfg.ColW * 0.8f;
            b.R = MathF.Min(ballMax, MathF.Sqrt(area / MathF.PI));
            if (b.R >= ballMax - 0.01f && b.MaxedAt < 0) Maxed(b);
        }
        b.X = newX;
        b.Rot += moved / b.R;
        if (Cfg.Trails)
        {
            b.TrailAcc += MathF.Abs(moved);
            if (b.TrailAcc >= MathF.Max(3, b.R * 0.5f))    // spacing grows with the ball so dents don't pile up
            {
                b.TrailAcc = 0;
                AddMark(p, b.X, b.R, 0.22f, Cfg.TrailLife);
            }
        }
    }

    // 1-D collisions between resting balls on the same surface; three passes settle chains of balls
    void ResolveCollisions()
    {
        for (int pass = 0; pass < 3; pass++)
            for (int i = 0; i < Balls.Count; i++)
            {
                var a = Balls[i];
                if (!OnSurface(a)) continue;
                for (int j = i + 1; j < Balls.Count; j++)
                {
                    var b = Balls[j];
                    if (b.Pile == a.Pile && OnSurface(b)) Collide(a, b, pass == 0);
                }
            }
    }

    static bool OnSurface(Snowball b) => b.Pile != null && !b.Falling && !b.Dragging && b.Below == null;

    void Collide(Snowball a, Snowball b, bool effects)
    {
        if (a.X > b.X) (a, b) = (b, a);
        float overlap = a.R + b.R - (b.X - a.X);
        if (overlap <= 0) return;
        float ma = StackMass(a), mb = StackMass(b), sum = ma + mb;
        float vrel = b.Vx - a.Vx;                                     // negative: closing in
        if (vrel < 0)
        {
            float j = -(1 + Cfg.BallRestitution) * vrel * ma * mb / sum;   // impulse; momentum is conserved
            a.Vx -= j / ma;
            b.Vx += j / mb;
            if (effects) Hit(a, b, -vrel, j);
        }
        // Push apart: the lighter ball gives way more
        Shift(a, -overlap * mb / sum);
        Shift(b, overlap * ma / sum);
    }

    // Move a resting ball sideways: a single ball rolls (and picks up snow), a snowman slides
    void Shift(Snowball b, float d)
    {
        if (Above(b) != null) b.X += d;
        else Roll(b, b.Pile, b.X + d);
    }

    // What a hit looks like: a burst of powder where the balls meet
    void Hit(Snowball a, Snowball b, float speed, float impulse)
    {
        Strike(a, impulse, +1);     // a was pushed left: a stack on it leans right
        Strike(b, impulse, -1);
        if (speed < 60) return;
        float x = a.X + a.R, y = a.Pile.TopY - MathF.Min(a.R, b.R);
        int n = Math.Min(12, (int)(speed / 40));
        for (int k = 0; k < n; k++)
            SpawnClump(x + Rnd.F(-2, 2), y + Rnd.F(-3, 3), Rnd.F(-80, 80), -Rnd.F(60, 160), -3);
    }

    void AddMark(Pile p, float x, float size, float alpha, float life)
    {
        Marks[markNext] = new Mark { Pile = p, Dx = x - p.MinX, Size = size, Born = t, Alpha = alpha, Life = life };
        markNext = (markNext + 1) % Marks.Length;
    }

    // MARK: Visitors

    public readonly List<Visitor> Visitors = new();
    public Visitor Visitor => Visitors.Count > 0 ? Visitors[0] : null;   // the first one (handy in tests)
    float visitAt = Cfg.FirstVisit;
    public int VisitorHits;          // snowballs that have hit a visitor

    // Send in a visitor now (kind -1: any enabled one), if there's room for one more
    public bool Invite(int kind = -1)
    {
        if (Visitors.Count >= Cfg.MaxVisitors) return false;
        if (kind < 0)
        {
            int n = 0;
            foreach (bool on in Cfg.Visitors) if (on) n++;
            if (n == 0) return false;
            int pick = Math.Min(n - 1, (int)Rnd.F(0, n));
            for (int k = 0; k < Cfg.Visitors.Length; k++) if (Cfg.Visitors[k] && pick-- == 0) { kind = k; break; }
        }
        else if (!Cfg.Visitors[kind]) return false;
        bool fromLeft = Rnd.F(0, 1) < 0.5f;
        float queue = 45 * Visitors.Count;           // arriving together: one a little behind the other
        Visitors.Add(new Visitor { Kind = kind, Dir = fromLeft ? 1 : -1, X = fromLeft ? -30 - queue : W + 30 + queue,
                                   NextHabit = t + Rnd.F(6, 14) });
        return true;
    }

    public void SendAway() { Visitors.Clear(); visitAt = t + Rnd.F(Cfg.VisitGapMin, Cfg.VisitGapMax); }

    void StepVisitors(float dt)
    {
        if (t >= visitAt)
        {
            int n = Rnd.F(0, 1) < 0.35f ? 2 : 1;     // sometimes a pair turns up
            for (int k = 0; k < n; k++) Invite();
            visitAt = t + Rnd.F(Cfg.VisitGapMin, Cfg.VisitGapMax);
        }
        for (int i = Visitors.Count - 1; i >= 0; i--) StepVisitor(Visitors[i], dt);
    }

    void StepVisitor(Visitor v, float dt)
    {
        if (!Cfg.Visitors[v.Kind]) { Visitors.Remove(v); return; }
        v.Anim += dt;
        v.Cool -= dt;
        v.Hurry -= dt;
        v.Jitter = 0;
        v.Pose = 0;
        v.Face = v.Dir;
        if (v.Away)
        {
            // The rabbit peeks back in from the side it ran off to, once it feels safe
            if (t < v.AwayUntil) { v.Hidden = true; return; }
            v.Away = false;
            v.Dir = v.Face = v.X < 0 ? 1 : -1;
            v.Cool = 2;
        }
        float moved = v.Act != 0 ? StepAct(v, dt) : Wander(v, dt);
        v.X += moved;
        if (v.Act == 0 && !v.TurnedBack && v.Walking && v.X > W * 0.25f && v.X < W * 0.75f && Rnd.F(0, 1) < dt * 0.02f)
        {
            v.Dir = -v.Dir;                  // sometimes it changes its mind halfway (once)
            v.TurnedBack = true;
        }
        int c = Ground.Col(v.X);
        v.Y = c >= 0 ? Ground.Surface(c) + 1 : Ground.TopY;
        // It walks behind windows: never drawn over anything you're working on
        v.Hidden = false;
        foreach (var w in Order)
            if (w.Contains(v.X, v.Y - 10) || w.Contains(v.X - 12, v.Y - 6) || w.Contains(v.X + 12, v.Y - 6)) { v.Hidden = true; break; }
        bool off = v.X < -40 || v.X > W + 40;
        if (off && v.Act == VisitorAct.Flee)
        {
            v.Away = true;                   // hides for a little while, then comes back
            v.AwayUntil = t + Rnd.F(5, 9);
            v.Act = 0;
            v.Hop = 0;
            v.HopT = -1;
            v.X = v.X < 0 ? -30 : W + 30;
        }
        else if ((v.Dir > 0 && v.X > W + 40 + 45 * Cfg.MaxVisitors) || (v.Dir < 0 && v.X < -40 - 45 * Cfg.MaxVisitors)) Visitors.Remove(v);
    }

    // Ordinary wandering, with each animal's own habits
    float Wander(Visitor v, float dt)
    {
        float moved = 0;
        if (v.PauseLeft > 0)
        {
            v.PauseLeft -= dt;
            // while standing still the fox and the rabbit look around (back over their shoulder for a moment)
            if (v.Kind != 0 && MathF.Sin(v.Anim * 1.7f) > 0.6f) v.Face = -v.Dir;
            return 0;
        }
        if (v.Kind == 2)
        {
            if (Hops(v, dt, 75, 11, 0.42f, out moved))
                v.PauseLeft = Rnd.F(0, 1) < 0.12f ? Rnd.F(1.5f, 3.5f) : Rnd.F(0.12f, 0.5f);
            if (RollingToward(v, 50, 150) != null) Startle(v, v.Dir * -1, v.X - v.Dir * 100, v.Y - 20);
            return moved;
        }
        float speed = (v.Kind == 0 ? 32 : 85) * (v.Hurry > 0 ? 1.7f : 1);
        moved = v.Dir * speed * dt;
        Prints(v, moved);
        if (Rnd.F(0, 1) < dt * 0.12f) v.PauseLeft = Rnd.F(1, 3);   // stops to look around now and then

        if (v.Kind == 0)
        {
            // Penguin: now and then slips or sits down for a rest; gives a small snowball ahead a kick
            if (t >= v.NextHabit)
            {
                v.NextHabit = t + Rnd.F(8, 18);
                float r = Rnd.F(0, 1);
                if (r < 0.4f) Begin(v, VisitorAct.Slip, 1.5f, v.SlideV = 60);
                else if (r < 0.8f) Begin(v, VisitorAct.Sit, Rnd.F(2.5f, 5));
            }
            var ball = GroundBallAhead(v, 16, 4);
            if (ball != null && ball.R < 16 && v.Cool <= 0)
            {
                ball.Vx += v.Dir * Rnd.F(70, 120);
                Begin(v, VisitorAct.Kick, 0.6f);
                v.Cool = 3;
            }
        }
        else
        {
            // Snow fox: hops over a small snowball in its way, turns back from a big one, chases a rolling one
            var ball = GroundBallAhead(v, 26, 0);
            if (ball != null && ball.R < 14) { v.JumpH = ball.R * 2 + 8; Begin(v, VisitorAct.JumpOver, 0.55f); }
            else if (ball != null && MathF.Abs(ball.X - v.X) < ball.R + 14) v.Dir = -v.Dir;
            else if (v.Cool <= 0 && (v.Target = RollingNear(v, 300, 40)) != null) Begin(v, VisitorAct.Chase, 5);
        }
        return moved;
    }

    void Begin(Visitor v, int act, float len, float _ = 0)
    {
        v.Act = act;
        v.ActT = 0;
        v.ActLen = len;
        v.PauseLeft = 0;
    }

    // One hop at a time: returns true when it lands (and leaves the rabbit's prints)
    bool Hops(Visitor v, float dt, float speed, float height, float time, out float moved)
    {
        moved = 0;
        if (v.HopT < 0) v.HopT = 0;
        v.HopT += dt / time;
        if (v.HopT >= 1)
        {
            v.HopT = -1;
            v.Hop = 0;
            // the rabbit's own track: two long hind prints side by side, two small front prints in front
            Footprint(v.X - 2.5f, 3.2f);
            Footprint(v.X + 2.5f, 3.2f);
            Footprint(v.X + v.Dir * 5.5f, 2);
            Footprint(v.X + v.Dir * 8.5f, 2);
            return true;
        }
        moved = v.Dir * speed * dt;
        v.Hop = MathF.Sin(MathF.PI * v.HopT) * height;
        return false;
    }

    void Prints(Visitor v, float moved)
    {
        v.StepAcc += MathF.Abs(moved);
        if (v.StepAcc < (v.Kind == 0 ? 7 : 12)) return;
        v.StepAcc = 0;
        v.Foot ^= 1;
        Footprint(v.X + (v.Foot == 0 ? -1.5f : 1.5f), v.Kind == 0 ? 4 : 3);
    }

    // A resting snowball on the ground just ahead of the visitor
    Snowball GroundBallAhead(Visitor v, float reach, float minAhead)
    {
        foreach (var b in Balls)
        {
            if (b.Pile != Ground || !OnSurface(b) || b.Hidden) continue;
            float ahead = (b.X - v.X) * v.Dir;
            if (ahead >= minAhead && ahead <= b.R + reach) return b;
        }
        return null;
    }

    // A snowball rolling along the ground near the visitor (faster than minSpeed)
    Snowball RollingNear(Visitor v, float range, float minSpeed)
    {
        foreach (var b in Balls)
            if (b.Pile == Ground && OnSurface(b) && MathF.Abs(b.Vx) > minSpeed && MathF.Abs(b.X - v.X) < range) return b;
        return null;
    }

    // ...and one rolling straight at it
    Snowball RollingToward(Visitor v, float range, float minSpeed)
    {
        var b = RollingNear(v, range, minSpeed);
        return b != null && (v.X - b.X) * b.Vx > 0 ? b : null;
    }

    // Hit by a snowball: each animal reacts its own way (once; a second hit mid-reaction just bursts)
    void HitVisitor(Visitor v, Snowball b)
    {
        VisitorHits++;
        if (v.Reacting || v.Cool > 0) return;
        float dir = b.Vx != 0 ? MathF.Sign(b.Vx) : MathF.Sign(v.X - b.FromX);
        if (v.Kind == 0) { Begin(v, VisitorAct.Tumble, 2.6f); v.SlideV = 110; }
        else if (v.Kind == 1) Begin(v, VisitorAct.Leap, 1.9f);
        if (v.Kind != 2) { v.PushDir = dir; v.FromX = b.FromX; v.FromY = b.FromY; v.HopT = -1; v.Hop = 0; }
        else Startle(v, dir, b.FromX, b.FromY);
    }

    void Startle(Visitor v, float dir, float fromX, float fromY)
    {
        v.PushDir = dir;
        v.FromX = fromX; v.FromY = fromY;
        v.HopT = -1;
        Begin(v, VisitorAct.Startle, 0.5f);
    }

    float StepAct(Visitor v, float dt)
    {
        v.ActT += dt;
        float T = v.ActT, moved = 0;
        bool done = T >= v.ActLen;
        switch (v.Act)
        {
            case VisitorAct.Tumble:          // penguin: wobbles, slides on its belly, shakes itself, gets back up
                if (T < 0.35f) { v.Jitter = MathF.Sin(T * 40) * 2.5f; moved = v.PushDir * 30 * dt; }
                else if (T < 1.3f) { v.Pose = 1; v.Face = v.PushDir; moved = Slide(v, dt); }
                else if (T < 2.2f)
                {
                    v.Pose = 2;
                    v.Jitter = MathF.Sin(T * 55) * 1.6f;
                    if (T - dt < 1.3f) Shake(v);
                }
                else v.Hop = MathF.Sin(MathF.PI * Math.Clamp((T - 2.2f) / 0.4f, 0, 1)) * 3;
                if (done)
                {
                    v.Hop = 0;
                    float r = Rnd.F(0, 1);
                    if (r < 0.35f) { Begin(v, VisitorAct.ThrowBack, 0.9f); return moved; }   // snowball fight!
                    if (r < 0.65f) { v.Dir = v.PushDir; v.Hurry = 4; }                       // waddles off, a bit quicker
                }
                break;
            case VisitorAct.Slip:            // penguin: a little slip on the ice, a short belly slide, back up
                if (T < 0.3f) v.Jitter = MathF.Sin(T * 40) * 2;
                else if (T < 0.9f) { v.Pose = 1; moved = Slide(v, dt); }
                else v.Hop = MathF.Sin(MathF.PI * Math.Clamp((T - 0.9f) / 0.4f, 0, 1)) * 2;
                break;
            case VisitorAct.Sit:
                v.Pose = 2;
                break;
            case VisitorAct.Kick:
                v.Jitter = MathF.Sin(T * 30) * 0.8f;
                break;
            case VisitorAct.ThrowBack:       // penguin: turns to where the snowball came from and throws one back
                v.Face = MathF.Sign(v.FromX - v.X);
                v.Pose = 3;
                if (T >= 0.45f && T - dt < 0.45f) ThrowBackAt(v);
                break;
            case VisitorAct.Leap:            // fox: springs aside, shakes the snow off, looks where it came from
                if (T < 0.45f) { v.Hop = MathF.Sin(MathF.PI * T / 0.45f) * 16; moved = v.PushDir * 120 * dt; }
                else if (T < 1.05f)
                {
                    v.Hop = 0;
                    v.Jitter = MathF.Sin(T * 60) * 1.5f;
                    if (T - dt < 0.45f) Shake(v);
                }
                else v.Face = MathF.Sign(v.FromX - v.X);
                break;
            case VisitorAct.JumpOver:        // fox: an arc high enough to clear the snowball
                v.Hop = MathF.Sin(MathF.PI * Math.Min(1, T / v.ActLen)) * v.JumpH;
                moved = v.Dir * 110 * dt;
                break;
            case VisitorAct.Chase:           // fox: dashes after a rolling snowball and bats it along
            {
                var b = v.Target;
                if (b == null || !Balls.Contains(b) || b.Pile != Ground || !OnSurface(b)) { done = true; break; }   // keeps after it even once it slows
                v.Dir = v.Face = MathF.Sign(b.X - v.X);
                moved = v.Dir * 140 * dt;
                Prints(v, moved);
                if (MathF.Abs(b.X - v.X) < b.R + 12)
                {
                    b.Vx += v.Dir * Rnd.F(80, 150);
                    v.JumpH = 6;
                    Begin(v, VisitorAct.JumpOver, 0.35f);
                    v.Cool = 6;
                    return moved;
                }
                break;
            }
            case VisitorAct.Startle:         // rabbit: one big startled hop away...
                v.Dir = v.Face = v.PushDir;
                v.Hop = MathF.Sin(MathF.PI * Math.Min(1, T / v.ActLen)) * 20;
                moved = v.PushDir * 170 * dt;
                if (done) { v.Hop = 0; Begin(v, VisitorAct.Flee, 30); return moved; }
                break;
            case VisitorAct.Flee:            // ...then hops off quickly to hide (it comes back later)
                Hops(v, dt, 170, 13, 0.32f, out moved);
                break;
        }
        if (done)
        {
            if (v.Act is VisitorAct.Tumble or VisitorAct.Leap or VisitorAct.ThrowBack) v.Cool = 1;
            if (v.Act == VisitorAct.Chase) v.Cool = 6;
            v.Act = 0;
            v.Hop = 0;
        }
        return moved;
    }

    float Slide(Visitor v, float dt)
    {
        v.SlideV *= MathF.Exp(-2.2f * dt);
        float moved = v.Face * v.SlideV * dt;
        v.StepAcc += MathF.Abs(moved);
        if (v.StepAcc >= 6) { v.StepAcc = 0; if (Ground.Col(v.X) >= 0) AddMark(Ground, v.X, 9, 0.35f, 7); }   // a belly track
        return moved;
    }

    void Shake(Visitor v)
    {
        for (int k = 0; k < 10; k++)
            SpawnClump(v.X + Rnd.F(-10, 10), v.Y - Rnd.F(6, 22), Rnd.F(-110, 110), -Rnd.F(40, 140), -3);
    }

    // The penguin's snowball fight: a small ball lobbed back to where the one that hit it came from
    void ThrowBackAt(Visitor v)
    {
        var b = new Snowball { R = 5, X = v.X + v.Face * 8, Y = v.Y - 24 };
        float dx = v.FromX - b.X, dy = v.FromY - b.Y;
        float time = Math.Clamp(MathF.Abs(dx) / 500, 0.5f, 1.4f);
        Balls.Add(b);
        Throw(b, dx / time, (dy - 0.5f * Cfg.Gravity * time * time) / time, v);
        TrimBalls();
    }

    void Footprint(float x, float size)
    {
        if (Ground.Col(x) >= 0) AddMark(Ground, x, size, 0.45f, 7);
    }

    // Put a resting snowball on a surface (used by the self-tests)
    public Snowball AddBall(Pile p, float x, float r)
    {
        var b = new Snowball { X = x, R = r, Pile = p };
        b.Y = p.TopY - r + 1.5f;
        Balls.Add(b);
        TrimBalls();
        return b;
    }

    // Full size: a one-time flash (drawn by Render) and a little puff of sparkly powder
    void Maxed(Snowball b)
    {
        b.MaxedAt = t;
        for (int k = 0; k < 8; k++)
            SpawnClump(b.X + Rnd.F(-b.R, b.R) * 0.7f, b.Y - b.R * Rnd.F(0.6f, 1), Rnd.F(-60, 60), -Rnd.F(80, 160), -3);
    }

    public void TrimBalls()
    {
        while (Balls.Count > Cfg.MaxBalls)
        {
            int i = Balls.FindIndex(b => !b.Falling && !b.Dragging && b.Decor == 0 && b != activeBall);
            if (i < 0) break;
            Balls.RemoveAt(i);
        }
    }

    void StepBalls(float dt)
    {
        for (int i = Balls.Count - 1; i >= 0; i--)
        {
            var b = Balls[i];
            bool gone = false;
            if (b.Falling)
            {
                b.Decor = 0;
                float prevBottom = b.Y + b.R, px = b.X, py = b.Y;
                b.Vy += Cfg.Gravity * dt;
                b.X += b.Vx * dt;
                b.Y += b.Vy * dt;
                b.Rot += b.Vx * dt / b.R;
                Pile p;
                if (b.Thrown && ThrownHit(b, px, py)) gone = true;
                else if (b.Vy > 0 && (p = LandingSurface(b.X, prevBottom, b.Y + b.R, onEdge: true)) != null)
                {
                    if (b.Vy < Cfg.SoftLanding && !b.Thrown) Land(b, p);   // set down gently: it stays a ball
                    else { Splat(b, p); gone = true; }        // a throw or a real fall bursts into a heap of snow
                }
                else if (b.Y > H + b.R * 2 || b.X < -100 || b.X > W + 100 || (b.Thrown && t - b.ThrownAt > 8)) gone = true;
            }
            else if (b.Dragging)
            {
                // position is set by StepDrag
            }
            else if (b.Below != null)
            {
                var u = b.Below;
                if (u.Falling || u.Dragging || !Balls.Contains(u))
                    b.Drop(u.Vx + Rnd.F(-40, 40), MathF.Min(u.Vy, 0) - Rnd.F(30, 80));
                else
                {
                    float th = Base(b).Wobble, d = u.R + b.R * 0.82f;   // sits slightly sunk into the ball below
                    b.X = u.X + MathF.Sin(th) * d;                       // and leans with the stack
                    b.Y = u.Y - MathF.Cos(th) * d;
                    b.Hidden = u.Hidden;
                }
            }
            else if (b.Pile != null)
            {
                var p = b.Pile;
                bool stack = Above(b) != null;
                if (stack)
                {
                    // A struck snowman rocks on its base like a damped spring; lean too far and it topples
                    b.WobbleV += (-Cfg.WobbleStiffness * b.Wobble - Cfg.WobbleDamping * b.WobbleV) * dt;
                    b.Wobble += b.WobbleV * dt;
                    if (MathF.Abs(b.Wobble) > Cfg.ToppleAngle) { Topple(b); stack = false; }
                }
                else b.Wobble = b.WobbleV = 0;
                if (!b.Driven)
                {
                    // Left alone it keeps rolling: heavy balls keep their speed longer, a snowman stays planted
                    b.Vx *= MathF.Exp(-Cfg.RollFriction / MathF.Sqrt(StackMass(b)) * (stack ? 4 : 1) * dt);
                    if (MathF.Abs(b.Vx) < 3) b.Vx = 0;
                }
                if (b.Vx != 0)
                {
                    if (stack) b.X += b.Vx * dt; else Roll(b, p, b.X + b.Vx * dt);
                }
                float lo = p.MinX + b.R, hi = p.MinX + p.Width - b.R;
                if (b.Driven && lo < hi && (b.X < lo || b.X > hi)) { b.X = Math.Clamp(b.X, lo, hi); b.Vx = 0; }   // the cursor's ball stays on
                b.Driven = false;
                if (b.X < p.MinX || b.X > p.MinX + p.Width) b.Drop(b.Vx, 0);   // rolled off the end
                else
                {
                    int c = p.Col(b.X);
                    b.Hidden = c < 0 || p.Occ[c];    // hidden behind a window in front
                    b.Y = p.TopY - b.R + 1.5f;       // sits slightly sunk into the snow
                }
            }
            else b.Drop(0, 0);                       // lost its support

            if (gone)
            {
                Balls.RemoveAt(i);
                if (b == activeBall) activeBall = null;
            }
        }
        ResolveCollisions();
        StepCarrots(dt);
    }

    // Leaned too far: everything above the base tumbles off to that side
    void Topple(Snowball baseBall)
    {
        float lean = MathF.Sign(baseBall.Wobble);
        for (var x = Above(baseBall); x != null;)
        {
            var next = Above(x);
            x.Drop(baseBall.Vx + lean * Rnd.F(120, 220), -Rnd.F(60, 140));
            x = next;
        }
        baseBall.Wobble = baseBall.WobbleV = 0;
    }

    // A stack whose base got knocked: the top lags behind, so it leans away from the push
    void Strike(Snowball baseBall, float impulse, float lean)
    {
        if (Above(baseBall) == null) return;
        float dv = impulse / StackMass(baseBall);
        baseBall.WobbleV += lean * dv * Cfg.WobbleKick;
        if (!Cfg.CarrotFalls || dv < Cfg.NoseKnock) return;
        var head = baseBall;
        while (Above(head) != null) head = Above(head);
        if (head.Decor != 2 || head.NoseLost) return;
        head.NoseLost = true;
        Carrots.Add(new Carrot
        {
            X = head.X + head.R * 0.05f, Y = head.Y + head.R * 0.03f, Len = head.R * 0.75f,
            Vx = lean * Rnd.F(60, 140) + baseBall.Vx * 0.3f, Vy = -Rnd.F(120, 200), Spin = lean * Rnd.F(6, 12),
        });
    }

    void StepCarrots(float dt)
    {
        for (int i = Carrots.Count - 1; i >= 0; i--)
        {
            var c = Carrots[i];
            if (c.RestAt < 0)
            {
                float prevY = c.Y;
                c.Vy += Cfg.Gravity * dt;
                c.X += c.Vx * dt;
                c.Y += c.Vy * dt;
                c.Rot += c.Spin * dt;
                Pile p;
                if (c.Vy > 0 && (p = LandingSurface(c.X, prevY, c.Y)) != null)
                {
                    int col = p.Col(c.X);
                    if (col >= 0) c.Y = p.Surface(col) - 1;
                    c.Rot = MathF.Round(c.Rot / MathF.PI) * MathF.PI;   // comes to rest lying flat
                    c.RestAt = t;
                }
            }
            if (c.Y > H + 50 || c.X < -100 || c.X > W + 100 || (c.RestAt >= 0 && t - c.RestAt > 6)) Carrots.RemoveAt(i);
        }
    }

    // First surface a falling ball hits between two heights: window tops (front to back), then the ground
    // Snowballs land on the edge they rest on (sunk into the snow), so one set down in place lands
    // right where it was instead of slipping through; smaller things (carrots) land on top of the snow.
    Pile LandingSurface(float x, float top, float bottom, bool onEdge = false)
    {
        foreach (var w in Order)
        {
            if (!Piles.TryGetValue(w.Id, out var p)) continue;
            int c = p.Col(x);
            if (c < 0 || p.Occ[c]) continue;
            float s = onEdge ? p.TopY + 1.5f : p.Surface(c);
            if (top <= s && bottom > s) return p;
        }
        // The ground catches everything that reaches the bottom, even where a window covers it
        int g = Ground.Col(x);
        return g >= 0 && bottom > (onEdge ? Ground.TopY + 1.5f : Ground.Surface(g)) ? Ground : null;
    }

    // A gently dropped ball comes to rest on the surface (keeping its sideways speed, so it rolls on)
    void Land(Snowball b, Pile p)
    {
        b.Falling = false;
        b.Vy = 0;
        b.Pile = p;
        float lo = p.MinX + b.R, hi = p.MinX + p.Width - b.R;
        if (lo < hi) b.X = Math.Clamp(b.X, lo, hi);
        b.Y = p.TopY - b.R + 1.5f;
    }

    // Right click: the snowball under the cursor bursts into powder; anything stacked on it tumbles off
    public bool Smash(float mx, float my)
    {
        var b = BallAt(mx, my);
        if (b == null || b.Dragging) return false;
        for (var a = Above(b); a != null;)
        {
            var next = Above(a);
            a.Drop(Rnd.F(-60, 60), -Rnd.F(60, 120));
            a = next;
        }
        RemoveDecor(b);
        var surface = Base(b).Pile;
        b.Below = null;
        if (surface != null) Splat(b, surface);
        Puff(b.X, b.Y, b.R, 0);
        Balls.Remove(b);
        if (b == activeBall) activeBall = null;
        return true;
    }

    // A falling ball bursts where it lands: its snow becomes a mound on that surface, plus a puff of powder
    void Splat(Snowball b, Pile p)
    {
        int c0 = p.Col(b.X);
        if (c0 >= 0)
        {
            float snow = MathF.PI * b.R * b.R * 0.7f / Cfg.ColW;   // ball area -> total column height
            int half = Math.Max(2, (int)(b.R * 1.4f / Cfg.ColW));
            float sum = 0;
            for (int o = -half; o <= half; o++) sum += 1 - MathF.Abs(o) / (half + 1f);
            for (int o = -half; o <= half; o++)
            {
                int i = c0 + o;
                if (i < 0 || i >= p.Count || p.Occ[i]) continue;
                float w = (1 - MathF.Abs(o) / (half + 1f)) / sum;
                p.H[i] = MathF.Min(p.Caps[i], p.H[i] + snow * w);
            }
            p.Dirty = true;
        }
        int n = Math.Min(14, (int)(b.R * 0.6f) + 2);
        for (int k = 0; k < n; k++)
            SpawnClump(b.X + Rnd.F(-b.R, b.R), b.Y + b.R * 0.5f - Rnd.F(0, b.R * 0.5f),
                       b.Vx * 0.2f + Rnd.F(-90, 90), -Rnd.F(60, 180), -3);
    }

    // Parts of a window top hidden behind a front window get no snow and aren't drawn
    void ComputeOcclusion()
    {
        for (int i = 0; i < Order.Count; i++)
        {
            if (!Piles.TryGetValue(Order[i].Id, out var p)) continue;
            for (int c = 0; c < p.Count; c++)
            {
                float x = p.ColX(c), y = p.TopY - 1;
                bool o = x < 0 || x > W || y < 0;
                if (!o) for (int j = 0; j < i; j++) if (Order[j].Contains(x, y)) { o = true; break; }
                if (o != p.Occ[c]) { p.Occ[c] = o; if (o) p.H[c] = 0; p.Dirty = true; }
            }
        }
        // The taskbar is always in front of windows; the plain screen bottom is not
        for (int c = 0; c < Ground.Count; c++)
        {
            bool o = false;
            if (!GroundIsTaskbar)
            {
                float x = Ground.ColX(c), y = Ground.TopY - 1;
                foreach (var w in Order) if (w.Contains(x, y)) { o = true; break; }
            }
            if (o != Ground.Occ[c]) { Ground.Occ[c] = o; if (o) Ground.H[c] = 0; Ground.Dirty = true; }
        }
    }

    // A few sines at different frequencies add up to a gentle breeze; every 25-70 s a gust swells and fades
    public float Wind { get; private set; }
    float gustAt = 30, gustStart = -1, gustLen, gustPeak;

    public void StartGust(float strength)
    {
        gustStart = t;
        gustLen = Rnd.F(3, 6);
        gustPeak = strength;
    }

    float WindNow()
    {
        float w = 14 * MathF.Sin(t * 0.11f) + 9 * MathF.Sin(t * 0.37f + 1.3f) + 6 * MathF.Sin(t * 0.9f + 0.4f);
        if (gustStart < 0 && Cfg.Gusts && t >= gustAt) StartGust(Rnd.F(45, 90) * (Rnd.F(0, 1) < 0.5f ? -1 : 1));
        if (gustStart >= 0)
        {
            float k = (t - gustStart) / gustLen;
            if (k >= 1) { gustStart = -1; gustAt = t + Rnd.F(25, 70); }
            else { float e = MathF.Sin(MathF.PI * k); w += gustPeak * e * e; }   // swells in, dies down
        }
        return w;
    }

    void StepFlakes(float dt)
    {
        // A few sines at different frequencies add up to gusty wind
        float wind = Wind = WindNow();
        int normal = 0;
        for (int i = Flakes.Count - 1; i >= 0; i--)
        {
            var f = Flakes[i];
            float prevY = f.Y;
            if (f.Clump)
            {
                f.Vy += Cfg.Gravity * dt;
                f.Vx *= MathF.Max(0, 1 - 1.2f * dt);
                f.X += f.Vx * dt;
                f.Y += f.Vy * dt;
            }
            else
            {
                float sway = MathF.Sin(t * f.SwayFreq + f.Phase) * f.SwayAmp;
                f.X += (wind * f.WindFactor + sway) * dt;
                f.Y += f.Vy * dt;
                if (f.X < -40) f.X += W + 80; else if (f.X > W + 40) f.X -= W + 80;
            }

            bool gone = false;
            if (f.Y > prevY)
            {
                // Window tops, front to back (flakes falling in front of windows skip them)
                if (!f.InFront)
                {
                    foreach (var w in Order)
                    {
                        if (!Piles.TryGetValue(w.Id, out var pile)) continue;
                        if (f.Clump && w.Id == f.IgnoreId && t < f.IgnoreUntil) continue;
                        int c = pile.Col(f.X);
                        if (c < 0 || pile.Occ[c]) continue;
                        float s = pile.Surface(c);
                        if (prevY <= s && f.Y > s)
                        {
                            pile.Add(c, f.Clump ? 1.6f : f.Size * 0.16f);
                            gone = true;
                            break;
                        }
                    }
                }
                // Then the taskbar / bottom of the screen
                if (!gone)
                {
                    int c = Ground.Col(f.X);
                    if (c >= 0 && f.Y > Ground.Surface(c))
                    {
                        if (!Ground.Occ[c])
                        {
                            float gain = GroundIsTaskbar ? Cfg.TaskbarGain : 1;
                            Ground.Add(c, (f.Clump ? 1.6f : f.Size * 0.12f) * gain);
                        }
                        gone = true;
                    }
                }
            }
            if (!gone)
            {
                // Flakes that fall "behind" a window disappear
                if (!f.InFront) foreach (var w in Order) if (w.Contains(f.X, f.Y)) { gone = true; break; }
                if (f.Y > H + 20) gone = true;
                if (f.Clump && (f.X < -80 || f.X > W + 80)) gone = true;
            }

            if (gone) Recycle(i);
            else if (!f.Clump) normal++;
        }

        // Top up flakes (max 6 per frame so they don't appear in a row)
        int deficit = Math.Min(6, Cfg.TargetFlakes - normal);
        for (int k = 0; k < deficit; k++) SpawnFlake();
    }
}

// MARK: - Software renderer into a premultiplied BGRA buffer

unsafe static class Render
{
    static void Blend(uint* p, float a, float r, float g, float b)
    {
        if (a <= 0.003f) return;
        if (a > 1) a = 1;
        uint d = *p;
        float inv = 1 - a;
        uint nb = (uint)MathF.Min(255, b * a * 255 + (d & 0xFF) * inv + 0.5f);
        uint ng = (uint)MathF.Min(255, g * a * 255 + ((d >> 8) & 0xFF) * inv + 0.5f);
        uint nr = (uint)MathF.Min(255, r * a * 255 + ((d >> 16) & 0xFF) * inv + 0.5f);
        uint na = (uint)MathF.Min(255, a * 255 + (d >> 24) * inv + 0.5f);
        *p = (na << 24) | (nr << 16) | (ng << 8) | nb;
    }

    // Soft round flake: solid center, fading edge
    public static void Disc(uint* buf, int bw, int bh, float cx, float cy, float radius, float alpha,
                            float cr = 1, float cg = 1, float cb = 1)
    {
        int x0 = Math.Max(0, (int)MathF.Floor(cx - radius)), x1 = Math.Min(bw - 1, (int)MathF.Ceiling(cx + radius));
        int y0 = Math.Max(0, (int)MathF.Floor(cy - radius)), y1 = Math.Min(bh - 1, (int)MathF.Ceiling(cy + radius));
        float inv = 1 / radius;
        for (int y = y0; y <= y1; y++)
        {
            float dy = (y + 0.5f - cy) * inv;
            uint* row = buf + (long)y * bw;
            for (int x = x0; x <= x1; x++)
            {
                float dx = (x + 0.5f - cx) * inv;
                float d = MathF.Sqrt(dx * dx + dy * dy);
                if (d >= 1) continue;
                float a = d < 0.45f ? 1 - 0.4f * (d / 0.45f) : 0.6f * (1 - (d - 0.45f) / 0.55f);
                Blend(row + x, a * alpha, cr, cg, cb);
            }
        }
    }

    // Anti-aliased fill of any shape given by its signed distance (physical px, negative inside).
    // Shapes are structs so drawing a frame allocates nothing.
    interface ISdf { float D(float x, float y); }

    struct CircleSdf : ISdf
    {
        public float X, Y, R;
        public float D(float x, float y) => MathF.Sqrt((x - X) * (x - X) + (y - Y) * (y - Y)) - R;
    }

    struct RingSdf : ISdf
    {
        public float X, Y, R, W;
        public float D(float x, float y) => MathF.Abs(MathF.Sqrt((x - X) * (x - X) + (y - Y) * (y - Y)) - R) - W / 2;
    }

    struct SegSdf : ISdf
    {
        public float Ax, Ay, Bx, By, W;
        public float D(float px, float py)
        {
            float vx = Bx - Ax, vy = By - Ay;
            float t = Math.Clamp(((px - Ax) * vx + (py - Ay) * vy) / MathF.Max(1e-6f, vx * vx + vy * vy), 0, 1);
            float dx = px - Ax - vx * t, dy = py - Ay - vy * t;
            return MathF.Sqrt(dx * dx + dy * dy) - W / 2;
        }
    }

    struct BoxSdf : ISdf
    {
        public float Cx, Cy, Hx, Hy, Rad;   // centre, half size minus corner radius, corner radius
        public float D(float x, float y)
        {
            float qx = MathF.Abs(x - Cx) - Hx, qy = MathF.Abs(y - Cy) - Hy;
            float ox = MathF.Max(qx, 0), oy = MathF.Max(qy, 0);
            return MathF.Sqrt(ox * ox + oy * oy) + MathF.Min(MathF.Max(qx, qy), 0) - Rad;
        }
    }

    // Convex triangle: distance = the largest distance outside any of its three edges
    struct TriSdf : ISdf
    {
        public float Ax, Ay, Bx, By, Cx, Cy, S;   // S: winding sign
        static float Edge(float s, float px, float py, float x0, float y0, float x1, float y1)
        {
            float ex = x1 - x0, ey = y1 - y0, len = MathF.Max(1e-6f, MathF.Sqrt(ex * ex + ey * ey));
            return -s * (ex * (py - y0) - ey * (px - x0)) / len;
        }
        public float D(float x, float y) =>
            MathF.Max(Edge(S, x, y, Ax, Ay, Bx, By), MathF.Max(Edge(S, x, y, Bx, By, Cx, Cy), Edge(S, x, y, Cx, Cy, Ax, Ay)));
    }

    // Axis-aligned ellipse (approximate distance, fine for anti-aliasing)
    struct EllipseSdf : ISdf
    {
        public float X, Y, A, B;
        public float D(float x, float y)
        {
            float u = (x - X) / A, v = (y - Y) / B;
            return (MathF.Sqrt(u * u + v * v) - 1) * MathF.Min(A, B);
        }
    }

    static void Fill<T>(uint* buf, int bw, int bh, float x0, float y0, float x1, float y1, in T sdf,
                        float alpha, float r, float g, float b) where T : struct, ISdf
    {
        int ix0 = Math.Max(0, (int)MathF.Floor(x0) - 1), ix1 = Math.Min(bw - 1, (int)MathF.Ceiling(x1) + 1);
        int iy0 = Math.Max(0, (int)MathF.Floor(y0) - 1), iy1 = Math.Min(bh - 1, (int)MathF.Ceiling(y1) + 1);
        for (int y = iy0; y <= iy1; y++)
            for (int x = ix0; x <= ix1; x++)
            {
                float cov = Math.Clamp(0.5f - sdf.D(x + 0.5f, y + 0.5f), 0, 1);
                if (cov > 0) Blend(buf + (long)y * bw + x, cov * alpha, r, g, b);
            }
    }

    static void Circle(uint* buf, int bw, int bh, float cx, float cy, float r, float a, float cr, float cg, float cb) =>
        Fill(buf, bw, bh, cx - r, cy - r, cx + r, cy + r, new CircleSdf { X = cx, Y = cy, R = r }, a, cr, cg, cb);

    static void Ring(uint* buf, int bw, int bh, float cx, float cy, float r, float width, float a, float cr, float cg, float cb) =>
        Fill(buf, bw, bh, cx - r - width, cy - r - width, cx + r + width, cy + r + width,
             new RingSdf { X = cx, Y = cy, R = r, W = width }, a, cr, cg, cb);

    static void Line(uint* buf, int bw, int bh, float ax, float ay, float bx, float by, float width, float a, float cr, float cg, float cb) =>
        Fill(buf, bw, bh, MathF.Min(ax, bx) - width, MathF.Min(ay, by) - width, MathF.Max(ax, bx) + width, MathF.Max(ay, by) + width,
             new SegSdf { Ax = ax, Ay = ay, Bx = bx, By = by, W = width }, a, cr, cg, cb);

    static void Ellipse(uint* buf, int bw, int bh, float cx, float cy, float ra, float rb, float a, float cr, float cg, float cb) =>
        Fill(buf, bw, bh, cx - ra, cy - rb, cx + ra, cy + rb, new EllipseSdf { X = cx, Y = cy, A = ra, B = rb }, a, cr, cg, cb);

    static void RoundRect(uint* buf, int bw, int bh, float x0, float y0, float x1, float y1, float rad, float cr, float cg, float cb) =>
        Fill(buf, bw, bh, x0, y0, x1, y1,
             new BoxSdf { Cx = (x0 + x1) / 2, Cy = (y0 + y1) / 2, Hx = (x1 - x0) / 2 - rad, Hy = (y1 - y0) / 2 - rad, Rad = rad },
             1, cr, cg, cb);

    static void Triangle(uint* buf, int bw, int bh, float ax, float ay, float bx, float by, float cx, float cy, float cr, float cg, float cb,
                         float alpha = 1) =>
        Fill(buf, bw, bh, MathF.Min(ax, MathF.Min(bx, cx)), MathF.Min(ay, MathF.Min(by, cy)),
             MathF.Max(ax, MathF.Max(bx, cx)), MathF.Max(ay, MathF.Max(by, cy)),
             new TriSdf { Ax = ax, Ay = ay, Bx = bx, By = by, Cx = cx, Cy = cy,
                          S = MathF.Sign((bx - ax) * (cy - ay) - (by - ay) * (cx - ax)) },
             alpha, cr, cg, cb);

    // A falling flake: a soft dot, or a slowly turning six-armed crystal; sparkly ones glint now and then
    static void Flake(uint* buf, int bw, int bh, Flake f, float s, float t)
    {
        float x = f.X * s, y = f.Y * s, r = f.Size * 1.1f * s;
        if (f.Crystal)
        {
            float a0 = f.Phase + t * 0.5f * (f.SwayFreq - 1.1f), arm = f.Size * 1.25f * s;
            float width = MathF.Max(0.9f, f.Size * 0.2f) * s;
            for (int k = 0; k < 3; k++)
            {
                float a = a0 + k * MathF.PI / 3, dx = MathF.Cos(a) * arm, dy = MathF.Sin(a) * arm;
                Line(buf, bw, bh, x - dx, y - dy, x + dx, y + dy, width, f.Alpha * 0.85f, 1, 1, 1);
            }
            Disc(buf, bw, bh, x, y, r * 0.45f, f.Alpha);
        }
        else Disc(buf, bw, bh, x, y, r, f.Alpha);
        if (!f.Sparkle) return;
        float glint = MathF.Pow(MathF.Max(0, MathF.Sin(t * 2.3f + f.Phase * 3)), 8);   // short, bright moments
        if (glint < 0.05f) return;
        float g = (f.Size * 2.2f + 2) * s * glint;
        Line(buf, bw, bh, x - g, y, x + g, y, 0.9f * s, glint, 1, 1, 1);
        Line(buf, bw, bh, x, y - g, x, y + g, 0.9f * s, glint, 1, 1, 1);
        Disc(buf, bw, bh, x, y, r * 0.9f, 0.6f * glint, 0.9f, 0.95f, 1);
    }

    // Snowball: lit from the top-left with a bluish shade at the bottom-right, plus specks that turn as it rolls
    public static void Ball(uint* buf, int bw, int bh, Snowball ball, float s, float t)
    {
        float cx = ball.X * s, cy = ball.Y * s, R = ball.R * s;
        float hx = cx - 0.24f * R, hy = cy - 0.28f * R, er = 1.1f * R;
        int x0 = Math.Max(0, (int)MathF.Floor(cx - R)), x1 = Math.Min(bw - 1, (int)MathF.Ceiling(cx + R));
        int y0 = Math.Max(0, (int)MathF.Floor(cy - R)), y1 = Math.Min(bh - 1, (int)MathF.Ceiling(cy + R));
        for (int y = y0; y <= y1; y++)
            for (int x = x0; x <= x1; x++)
            {
                float px = x + 0.5f, py = y + 0.5f;
                float cov = Math.Clamp(R + 0.5f - MathF.Sqrt((px - cx) * (px - cx) + (py - cy) * (py - cy)), 0, 1);
                if (cov <= 0) continue;
                float u = MathF.Min(1, MathF.Sqrt((px - hx) * (px - hx) + (py - hy) * (py - hy)) / er);
                float r, g, b;
                if (u < 0.55f) { float k = u / 0.55f; r = 1 - 0.06f * k; g = 1 - 0.04f * k; b = 1; }
                else { float k = (u - 0.55f) / 0.45f; r = 0.94f - 0.26f * k; g = 0.96f - 0.21f * k; b = 1 - 0.12f * k; }
                Blend(buf + (long)y * bw + x, cov, r, g, b);
            }
        foreach (var sp in ball.Specks)
        {
            float a = sp.Ang + ball.Rot;
            Circle(buf, bw, bh, cx + MathF.Cos(a) * sp.Dist * R, cy + MathF.Sin(a) * sp.Dist * R,
                   MathF.Max(0.5f, sp.Size * R), sp.Alpha, 0.66f, 0.73f, 0.87f);
        }
        if (ball.Decor == 2) Head(buf, bw, bh, cx, cy, R, !ball.NoseLost);
        else if (ball.Decor == 1) Body(buf, bw, bh, cx, cy, R);
        // Reached full size: a soft ring that grows and fades (0.6 s)
        float age = t - ball.MaxedAt;
        if (ball.MaxedAt >= 0 && age < 0.6f)
        {
            float k = age / 0.6f;
            Ring(buf, bw, bh, cx, cy, R + (2 + 10 * k) * s, 1.6f * s, 0.8f * (1 - k), 0.85f, 0.93f, 1);
        }
    }

    // Snowman head: eyes, carrot nose, a smile and a red scarf
    static void Head(uint* buf, int bw, int bh, float cx, float cy, float R, bool nose)
    {
        for (int sx = -1; sx <= 1; sx += 2)
            Circle(buf, bw, bh, cx + sx * R * 0.3f, cy - R * 0.22f, MathF.Max(1, R * 0.09f), 1, 0.08f, 0.08f, 0.08f);
        if (nose)
            Triangle(buf, bw, bh, cx + R * 0.02f, cy - R * 0.06f, cx + R * 0.75f, cy + R * 0.04f, cx + R * 0.02f, cy + R * 0.12f,
                     0.96f, 0.52f, 0.13f);
        else
            Circle(buf, bw, bh, cx + R * 0.04f, cy + R * 0.03f, MathF.Max(0.8f, R * 0.06f), 0.5f, 0.55f, 0.62f, 0.75f);   // a little hole
        for (int k = 0; k < 5; k++)
        {
            float a = MathF.PI * (1.2f + 0.15f * k);
            Circle(buf, bw, bh, cx + MathF.Cos(a) * R * 0.42f, cy - (MathF.Sin(a) * R * 0.42f - R * 0.05f),
                   MathF.Max(0.7f, R * 0.05f), 1, 0.1f, 0.1f, 0.1f);
        }
        RoundRect(buf, bw, bh, cx - R * 0.85f, cy + R * 0.68f, cx + R * 0.85f, cy + R * 0.98f, R * 0.12f, 0.82f, 0.16f, 0.18f);
        RoundRect(buf, bw, bh, cx + R * 0.3f, cy + R * 0.85f, cx + R * 0.58f, cy + R * 1.55f, R * 0.08f, 0.82f, 0.16f, 0.18f);
    }

    // Snowman body: three buttons and two stick arms
    static void Body(uint* buf, int bw, int bh, float cx, float cy, float B)
    {
        for (int k = 0; k < 3; k++)
            Circle(buf, bw, bh, cx, cy - B * (0.35f - 0.33f * k), MathF.Max(0.8f, B * 0.07f), 1, 0.08f, 0.08f, 0.08f);
        float lw = MathF.Max(1.2f, B * 0.09f);
        for (int sx = -1; sx <= 1; sx += 2)
        {
            Line(buf, bw, bh, cx + sx * B * 0.85f, cy - B * 0.15f, cx + sx * B * 1.75f, cy - B * 0.65f, lw, 1, 0.42f, 0.28f, 0.16f);
            Line(buf, bw, bh, cx + sx * B * 1.4f, cy - B * 0.45f, cx + sx * B * 1.55f, cy - B * 0.85f, lw, 1, 0.42f, 0.28f, 0.16f);
        }
    }


    // Draws in a visitor's own space: x forward (it faces Dir), y up from its feet, in logical px
    struct Pen
    {
        public uint* Buf; public int Bw, Bh; public float S, X0, Y0, D;
        float Px(float lx) => (X0 + D * lx) * S;
        float Py(float ly) => (Y0 + ly) * S;
        public void Ell(float lx, float ly, float a, float b, float r, float g, float bl) =>
            Ellipse(Buf, Bw, Bh, Px(lx), Py(ly), a * S, b * S, 1, r, g, bl);
        public void Cir(float lx, float ly, float rad, float r, float g, float bl) =>
            Circle(Buf, Bw, Bh, Px(lx), Py(ly), rad * S, 1, r, g, bl);
        public void Tri(float ax, float ay, float bx, float by, float cx, float cy, float r, float g, float bl) =>
            Triangle(Buf, Bw, Bh, Px(ax), Py(ay), Px(bx), Py(by), Px(cx), Py(cy), r, g, bl);
        public void Seg(float ax, float ay, float bx, float by, float wd, float r, float g, float bl) =>
            Line(Buf, Bw, Bh, Px(ax), Py(ay), Px(bx), Py(by), wd * S, 1, r, g, bl);
    }

    static void Visitor(uint* buf, int bw, int bh, Visitor v, float s)
    {
        if (v.Hidden) return;
        var p = new Pen { Buf = buf, Bw = bw, Bh = bh, S = s, X0 = v.X + v.Jitter, Y0 = v.Y - v.Hop, D = v.Face };
        if (v.Kind == 0) Penguin(p, v);
        else if (v.Kind == 1) Fox(p, v);
        else Rabbit(p, v);
    }

    static void Penguin(Pen p, Visitor v)
    {
        const float dr = 0.12f, dg = 0.13f, db = 0.18f;
        if (v.Pose == 1)
        {
            // on its belly, sliding head first
            p.Ell(-12, -3, 3, 1.2f, 0.98f, 0.55f, 0.15f);                     // feet trailing
            p.Ell(0, -6, 12, 6.5f, dr, dg, db);                               // body
            p.Ell(1, -3.8f, 9.5f, 3.2f, 0.97f, 0.97f, 1);                     // white belly underneath
            p.Ell(-2, -9.5f, 5.5f, 1.8f, 0.08f, 0.08f, 0.12f);                // flipper out to the side
            p.Cir(12, -7, 5.5f, dr, dg, db);                                  // head
            p.Cir(13.8f, -6.6f, 3, 0.97f, 0.97f, 1);
            p.Cir(14.5f, -7.6f, 0.9f, 0.05f, 0.05f, 0.08f);
            p.Tri(16.6f, -7.4f, 20.2f, -6.2f, 16.6f, -5, 0.98f, 0.6f, 0.15f);
            return;
        }
        if (v.Pose == 2)
        {
            // sitting in the snow, feet out in front
            p.Ell(6, -1.2f, 3.4f, 1.4f, 0.98f, 0.55f, 0.15f);
            p.Ell(0, -9, 9, 9, dr, dg, db);
            p.Ell(-6, -9, 2.2f, 5.5f, 0.08f, 0.08f, 0.12f);
            p.Ell(1.8f, -8, 6, 7, 0.97f, 0.97f, 1);
            p.Cir(0.8f, -19, 6, dr, dg, db);
            p.Cir(2.9f, -19, 3.4f, 0.97f, 0.97f, 1);
            p.Cir(3.7f, -19.9f, 1, 0.05f, 0.05f, 0.08f);
            p.Tri(5.9f, -19.7f, 9.8f, -18.3f, 5.9f, -16.9f, 0.98f, 0.6f, 0.15f);
            return;
        }
        if (v.Pose == 3) p.Seg(-3, -16, 4, -30, 3.2f, 0.08f, 0.08f, 0.12f);   // flipper raised to throw
        float k = v.Walking ? MathF.Sin(v.Anim * 9) : 0;
        float sway = k * 1.2f;                                         // waddle
        p.Ell(-3, -0.8f - MathF.Max(0, k) * 1.5f, 3.2f, 1.3f, 0.98f, 0.55f, 0.15f);   // feet, lifted in turn
        p.Ell(3, -0.8f - MathF.Max(0, -k) * 1.5f, 3.2f, 1.3f, 0.98f, 0.55f, 0.15f);
        p.Ell(sway, -12, 8, 12, 0.12f, 0.13f, 0.18f);                  // body
        p.Ell(sway - 5.5f, -12, 2.2f, 6.5f, 0.08f, 0.08f, 0.12f);      // flipper
        p.Ell(sway + 1.6f, -10.5f, 5.6f, 9, 0.97f, 0.97f, 1);          // belly
        p.Cir(sway + 0.5f, -22.5f, 6, 0.12f, 0.13f, 0.18f);            // head
        p.Cir(sway + 2.6f, -22.5f, 3.4f, 0.97f, 0.97f, 1);             // face
        p.Cir(sway + 3.4f, -23.4f, 1, 0.05f, 0.05f, 0.08f);            // eye
        p.Tri(sway + 5.6f, -23.2f, sway + 9.5f, -21.8f, sway + 5.6f, -20.4f, 0.98f, 0.6f, 0.15f);   // beak
    }

    static void Fox(Pen p, Visitor v)
    {
        float g = v.Anim * 11, sw = v.Walking ? MathF.Sin(g) * 3 : 0, bob = v.Walking ? MathF.Abs(MathF.Sin(g)) * 0.8f : 0;
        const float wr = 0.97f, wg = 0.97f, wb = 0.99f, sr = 0.78f, sg = 0.82f, sb = 0.91f;   // white fur, blue-grey shade
        p.Seg(-9, -6 - bob, -9 + sw, -0.5f, 2.2f, sr, sg, sb);          // legs (far pair first)
        p.Seg(5, -6 - bob, 5 - sw, -0.5f, 2.2f, sr, sg, sb);
        p.Ell(-15, -12 - bob, 8.6f, 4.8f, sr, sg, sb);                  // bushy tail with a soft outline
        p.Ell(-15, -12.4f - bob, 8, 4.2f, wr, wg, wb);
        p.Cir(-21, -13 - bob, 3.2f, wr, wg, wb);
        p.Ell(0, -9.6f - bob, 11.6f, 6.2f, sr, sg, sb);                 // body
        p.Ell(0, -10.2f - bob, 11, 5.6f, wr, wg, wb);
        p.Seg(-7, -6 - bob, -7 - sw, -0.5f, 2.2f, sr, sg, sb);          // near legs
        p.Seg(7, -6 - bob, 7 + sw, -0.5f, 2.2f, sr, sg, sb);
        p.Cir(11, -15 - bob, 5.6f, sr, sg, sb);                         // head
        p.Cir(11, -15 - bob, 5, wr, wg, wb);
        p.Tri(8, -18.5f - bob, 9.5f, -25 - bob, 11.5f, -19 - bob, wr, wg, wb);          // ears
        p.Tri(11, -19.5f - bob, 13.2f, -25.5f - bob, 14.2f, -18.5f - bob, wr, wg, wb);
        p.Tri(9, -19.5f - bob, 9.7f, -23 - bob, 10.8f, -19.5f - bob, 0.86f, 0.74f, 0.78f);
        p.Tri(14, -17.2f - bob, 20, -14.4f - bob, 14, -12.2f - bob, wr, wg, wb);       // snout
        p.Cir(19.6f, -14.5f - bob, 1.3f, 0.08f, 0.08f, 0.1f);           // nose
        p.Cir(12.8f, -16.3f - bob, 1, 0.08f, 0.08f, 0.1f);              // eye
    }

    static void Rabbit(Pen p, Visitor v)
    {
        float k = v.Hop / 11;                                          // stretches out mid-hop
        const float br = 0.62f, bg = 0.55f, bb = 0.50f;
        p.Cir(-8, -9, 3.2f, 0.98f, 0.98f, 1);                           // cotton tail
        p.Ell(0, -8, 8 + 2 * k, 7 - k, br, bg, bb);                      // body
        p.Ell(2, -5.5f, 5, 3.5f, 0.82f, 0.77f, 0.72f);                  // belly
        p.Ell(1.5f, -1.2f + k, 5.5f, 1.8f, br, bg, bb);                 // foot
        float twitch = v.PauseLeft > 0 ? MathF.Max(0, MathF.Sin(v.Anim * 5)) * 0.8f : 0;   // ears twitch while it listens
        p.Ell(5 - twitch, -23 + k, 1.7f, 5.8f, br, bg, bb);             // ears
        p.Ell(7.8f + twitch, -22.3f + k, 1.7f, 5.8f, br, bg, bb);
        p.Ell(7.8f + twitch, -22.3f + k, 0.7f, 4.2f, 0.92f, 0.7f, 0.72f);
        p.Cir(7, -14, 5, br, bg, bb);                                   // head
        p.Cir(9.2f, -15, 1, 0.08f, 0.08f, 0.1f);                        // eye
        p.Cir(11.8f, -13.5f, 0.9f, 0.92f, 0.62f, 0.66f);                // nose
    }

    // A loose carrot: the same orange cone as the nose, pointing along its spin; fades in its last second
    static void Carrot(uint* buf, int bw, int bh, Carrot c, float s, float t)
    {
        float alpha = c.RestAt < 0 ? 1 : Math.Clamp(6 - (t - c.RestAt), 0, 1);
        if (alpha <= 0) return;
        float ux = MathF.Cos(c.Rot), uy = MathF.Sin(c.Rot), half = c.Len * 0.12f;
        float x = c.X * s, y = c.Y * s;
        Triangle(buf, bw, bh, x - uy * half * s, y + ux * half * s, x + ux * c.Len * s, y + uy * c.Len * s,
                 x + uy * half * s, y - ux * half * s, 0.96f, 0.52f, 0.13f, alpha);
    }

    // Dents left by rolling snowballs: soft bluish hollows that fade away
    static void Trails(uint* buf, int bw, int bh, SnowWorld w, float s)
    {
        float t = w.T;
        for (int i = 0; i < w.Marks.Length; i++)
        {
            var m = w.Marks[i];
            float age = t - m.Born;
            if (m.Pile == null || age < 0 || age >= m.Life) continue;
            var p = m.Pile;
            float x = p.MinX + m.Dx;
            int c = p.Col(x);
            if (c < 0 || p.Occ[c] || p.H[c] < 0.5f) continue;
            float ra = MathF.Max(1.5f, m.Size * 0.45f), rb = MathF.Max(0.8f, MathF.Min(p.H[c] * 0.5f, m.Size * 0.12f));
            Ellipse(buf, bw, bh, x * s, (p.Surface(c) + rb * 0.6f) * s, ra * s, rb * s,
                    m.Alpha * (1 - age / m.Life), 0.72f, 0.80f, 0.93f);
        }
    }

    static void Lights(uint* buf, int bw, int bh, SnowWorld w, float s)
    {
        float t = w.T;
        foreach (var ls in w.Lights.Values)
        {
            // The wire: thin and dark, with a white line of snow on top once some has settled
            float snow = MathF.Min(1, ls.Snow * 1.5f);
            for (int i = 1; i < ls.Wire.Count; i++)
            {
                var a = ls.Wire[i - 1]; var b = ls.Wire[i];
                if (a.Hidden || b.Hidden) continue;
                Line(buf, bw, bh, a.X * s, a.Y * s, b.X * s, b.Y * s, 1.1f * s, 0.85f, 0.10f, 0.12f, 0.10f);
                if (snow > 0.05f)
                    Line(buf, bw, bh, a.X * s, (a.Y - 0.9f) * s, b.X * s, (b.Y - 0.9f) * s, (0.6f + 1.4f * ls.Snow) * s, 0.9f * snow, 1, 1, 1);
            }
            for (int i = 0; i < ls.Bulbs.Count; i++)
            {
                var b = ls.Bulbs[i];
                if (b.Hidden) continue;
                float k = LightLook.Brightness(Cfg.LightPattern, b, i, t) * ls.Reveal(i, t);   // 0 until it lights up
                var c = LightLook.Color(Cfg.LightPreset, i, t);
                Bulb(buf, bw, bh, b.X * s, b.Y * s, s, k, c.R, c.G, c.B, snow);
                float g = LightLook.Glint(Cfg.LightPattern, b, i, t) * ls.Reveal(i, t);
                if (g > 0.03f) Star(buf, bw, bh, b.X * s, (b.Y + 0.8f) * s, s, g, c.R, c.G, c.B);
            }
        }
    }

    // A four-pointed sparkle: long thin rays across, short ones on the diagonals, white at the heart
    static void Star(uint* buf, int bw, int bh, float x, float y, float s, float g, float r, float gr, float b)
    {
        float len = (5 + 6 * g) * s, d = len * 0.45f, wd = 0.8f * s;
        float wr = r + (1 - r) * 0.7f, wg = gr + (1 - gr) * 0.7f, wb = b + (1 - b) * 0.7f;
        Line(buf, bw, bh, x - len, y, x + len, y, wd, 0.85f * g, wr, wg, wb);
        Line(buf, bw, bh, x, y - len, x, y + len, wd, 0.85f * g, wr, wg, wb);
        Line(buf, bw, bh, x - d, y - d, x + d, y + d, wd * 0.8f, 0.5f * g, wr, wg, wb);
        Line(buf, bw, bh, x - d, y + d, x + d, y - d, wd * 0.8f, 0.5f * g, wr, wg, wb);
        Disc(buf, bw, bh, x, y, 2.6f * s, 0.9f * g, 1, 1, 1);
    }

    // One glass bulb: a faint pool of colored light below it, a soft bloom, the socket, the glass itself
    // (darker when dim), a hot bright center and a little glint
    static void Bulb(uint* buf, int bw, int bh, float x, float y, float s, float k, float r, float g, float b, float snow)
    {
        float lit = MathF.Min(1, k);
        if (k > 0.01f)
        {
            for (int j = 0; j < 3; j++)
                Ellipse(buf, bw, bh, x, y + (9 + j) * s, (10 - 3 * j) * s, (3.6f - j) * s, 0.05f * k, r, g, b);
            Disc(buf, bw, bh, x, y + 0.5f * s, 11 * s, 0.2f * k, r, g, b);
            Disc(buf, bw, bh, x, y + 0.5f * s, 5.5f * s, 0.38f * k, r, g, b);
        }
        RoundRect(buf, bw, bh, x - 1.4f * s, y - 5.4f * s, x + 1.4f * s, y - 2.6f * s, 0.6f * s, 0.18f, 0.2f, 0.18f);
        float dim = 0.35f + 0.65f * lit;
        Ellipse(buf, bw, bh, x, y + 0.6f * s, 2.5f * s, 3.5f * s, 0.92f, r * dim, g * dim, b * dim);
        if (k > 0.01f)
            Ellipse(buf, bw, bh, x, y + 1 * s, 1.2f * s, 1.9f * s, 0.25f + 0.75f * lit, r + (1 - r) * 0.6f, g + (1 - g) * 0.6f, b + (1 - b) * 0.6f);
        Circle(buf, bw, bh, x - 0.9f * s, y - 0.4f * s, 0.6f * s, 0.55f, 1, 1, 1);
        if (snow > 0.05f) Ellipse(buf, bw, bh, x, y - 5.6f * s, (1.2f + 1.2f * snow) * s, (0.5f + 0.6f * snow) * s, 0.95f, 1, 1, 1);
    }

    // A pile: white mound above its edge with a thin blue-grey shadow just below it
    public static void Pile(uint* buf, int bw, int bh, Pile p, float s)
    {
        int px0 = Math.Max(0, (int)MathF.Floor(p.MinX * s)), px1 = Math.Min(bw - 1, (int)MathF.Ceiling((p.MinX + p.Width) * s));
        float baseY = p.TopY * s;
        for (int px = px0; px <= px1; px++)
        {
            // Interpolate the height between column centres for a smooth outline
            float u = ((px + 0.5f) / s - p.MinX) / Cfg.ColW - 0.5f;
            int c0 = (int)MathF.Floor(u);
            float fr = u - c0;
            int ca = Math.Clamp(c0, 0, p.Count - 1), cb = Math.Clamp(c0 + 1, 0, p.Count - 1);
            if (p.Occ[fr < 0.5f ? ca : cb]) continue;
            float h = (p.H[ca] * (1 - fr) + p.H[cb] * fr) * s;
            if (h < 0.3f * s) continue;

            float top = baseY - h;
            // shadow: 1.5 logical px below the edge
            float shEnd = baseY + 1.5f * s;
            for (int y = Math.Max(0, (int)MathF.Floor(baseY)); y < Math.Min(bh, (int)MathF.Ceiling(shEnd)); y++)
            {
                float cov = MathF.Min(1, shEnd - y);
                Blend(buf + (long)y * bw + px, 0.9f * cov, 0.70f, 0.78f, 0.92f);
            }
            // body
            int yTop = (int)MathF.Floor(top);
            for (int y = Math.Max(0, yTop); y < Math.Min(bh, (int)MathF.Ceiling(baseY)); y++)
            {
                float cov = y == yTop ? 1 - (top - yTop) : 1;   // anti-aliased top edge
                Blend(buf + (long)y * bw + px, 0.97f * cov, 1, 1, 1);
            }
        }
    }

    public static void World(uint* buf, int bw, int bh, SnowWorld w, float s)
    {
        new Span<uint>(buf, bw * bh).Clear();
        // far + middle flakes, piles, lights, clumps, snowballs, near flakes, then the ball being carried
        foreach (var f in w.Flakes) if (!f.Clump && f.Layer < 2) Flake(buf, bw, bh, f, s, w.T);
        foreach (var p in w.Piles.Values) Pile(buf, bw, bh, p, s);
        Pile(buf, bw, bh, w.Ground, s);
        Trails(buf, bw, bh, w, s);
        foreach (var v in w.Visitors) Visitor(buf, bw, bh, v, s);
        Lights(buf, bw, bh, w, s);
        foreach (var f in w.Flakes) if (f.Clump) Disc(buf, bw, bh, f.X * s, f.Y * s, f.Size * 1.1f * s, f.Alpha);
        drawBalls.Clear();
        foreach (var b in w.Balls) if (!b.Hidden && b != w.DragBall) drawBalls.Add(b);
        drawBalls.Sort((u, v) => v.Y.CompareTo(u.Y));   // lower balls first, so a snowman's head sits on top
        foreach (var b in drawBalls) Ball(buf, bw, bh, b, s, w.T);
        foreach (var c in w.Carrots) Carrot(buf, bw, bh, c, s, w.T);
        foreach (var f in w.Flakes) if (!f.Clump && f.Layer == 2) Flake(buf, bw, bh, f, s, w.T);
        if (w.DragBall != null)
        {
            // Dotted guide of where a throw would go: bright dots with a dark rim (visible on light and dark
            // backgrounds), fading along the path, and a ring where it would land
            for (int i = 0; i < w.PathCount; i++)
            {
                float x = w.Path[2 * i] * s, y = w.Path[2 * i + 1] * s, a = 1 - 0.6f * i / (w.Path.Length / 2);
                Circle(buf, bw, bh, x, y, 3.1f * s, 0.45f * a, 0.08f, 0.12f, 0.28f);
                Circle(buf, bw, bh, x, y, 2.2f * s, 0.95f * a, 0.9f, 0.96f, 1);
            }
            if (w.PathCount > 0)
            {
                float x = w.Path[2 * w.PathCount - 2] * s, y = w.Path[2 * w.PathCount - 1] * s;
                Ring(buf, bw, bh, x, y, 6 * s, 2.6f * s, 0.4f, 0.08f, 0.12f, 0.28f);
                Ring(buf, bw, bh, x, y, 6 * s, 1.5f * s, 0.9f, 0.9f, 0.96f, 1);
            }
            Ball(buf, bw, bh, w.DragBall, s, w.T);
        }
    }

    static readonly List<Snowball> drawBalls = new();

    // 32×32 tray icon: white six-armed snowflake on a blue circle (straight alpha, as icons expect)
    public static void TrayIcon(uint* buf, int n)
    {
        float c = n / 2f;
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float px = x + 0.5f - c, py = y + 0.5f - c;
                float r = MathF.Sqrt(px * px + py * py);
                float bgA = Math.Clamp(c - 0.5f - r, 0, 1);
                float dist = 99;
                for (int k = 0; k < 6; k++)
                {
                    float ang = k * MathF.PI / 3, ex = MathF.Cos(ang), ey = MathF.Sin(ang);
                    float tt = Math.Clamp(px * ex + py * ey, 0, c * 0.72f);
                    float qx = px - ex * tt, qy = py - ey * tt;
                    dist = MathF.Min(dist, MathF.Sqrt(qx * qx + qy * qy));
                }
                float flake = Math.Clamp(1.7f - dist, 0, 1) * bgA;
                float rr = 0.25f + 0.75f * flake, gg = 0.47f + 0.53f * flake, bb = 0.85f + 0.15f * flake;
                uint a = (uint)(bgA * 255);
                buf[y * n + x] = (a << 24) | ((uint)(rr * 255) << 16) | ((uint)(gg * 255) << 8) | (uint)(bb * 255);
            }
    }
}

// MARK: - Win32 plumbing

static unsafe class Program
{
    // --- constants
    const uint WS_POPUP = 0x80000000;
    const uint WS_EX_LAYERED = 0x80000, WS_EX_TRANSPARENT = 0x20, WS_EX_TOPMOST = 0x8,
               WS_EX_TOOLWINDOW = 0x80, WS_EX_NOACTIVATE = 0x08000000;
    const int GWL_EXSTYLE = -20;
    const uint WM_DESTROY = 0x2, WM_TIMER = 0x113, WM_DISPLAYCHANGE = 0x7E, WM_NULL = 0,
               WM_APP = 0x8000, WM_LBUTTONDOWN = 0x201, WM_LBUTTONUP = 0x202, WM_RBUTTONDOWN = 0x204, WM_RBUTTONUP = 0x205,
               WM_MOUSEACTIVATE = 0x21, WM_CAPTURECHANGED = 0x215;
    const int MA_NOACTIVATE = 3;
    const uint MB_ICONINFORMATION = 0x40, MB_SETFOREGROUND = 0x10000, MB_TOPMOST = 0x40000;
    const uint WM_TRAY = WM_APP + 1;
    const int SW_HIDE = 0, SW_SHOWNOACTIVATE = 4;
    const uint ULW_ALPHA = 2;
    const int DWMWA_EXTENDED_FRAME_BOUNDS = 9, DWMWA_CLOAKED = 14;
    const uint NIM_ADD = 0, NIM_MODIFY = 1, NIM_DELETE = 2, NIF_MESSAGE = 1, NIF_ICON = 2, NIF_TIP = 4, NIF_INFO = 0x10, NIIF_INFO = 1;
    const uint MF_STRING = 0, MF_CHECKED = 8, MF_POPUP = 0x10, MF_SEPARATOR = 0x800;
    const uint TPM_RIGHTBUTTON = 0x2, TPM_RETURNCMD = 0x100;
    const int CMD_PAUSE = 1, CMD_CLEAR = 2, CMD_QUIT = 3, CMD_LIGHTS = 4, CMD_GUIDE = 5,
              CMD_LIGHT = 10, CMD_MEDIUM = 11, CMD_HEAVY = 12,
              CMD_GROWTH = 20,      // + growth mode 0..3
              CMD_MAXBALLS = 30,    // + index into MaxBallChoices
              CMD_TRAILS = 40, CMD_CARROT = 41, CMD_GUSTS = 42, CMD_FANCY = 43,
              CMD_VISITOR = 50,     // + kind 0..2
              CMD_INVITE = 55,
              CMD_PRESET = 60,      // + preset
              CMD_PATTERN = 70,     // + pattern
              CMD_WIRESNOW = 80;
    static readonly int[] MaxBallChoices = { 10, 20, 40 };
    const string RegKey = @"Software\desktopsnowfall";

    // --- structs
    [StructLayout(LayoutKind.Sequential)] struct RECT { public int Left, Top, Right, Bottom; }
    [StructLayout(LayoutKind.Sequential)] struct POINT { public int X, Y; }
    [StructLayout(LayoutKind.Sequential)] struct SIZE { public int cx, cy; }
    [StructLayout(LayoutKind.Sequential, Pack = 1)] struct BLENDFUNCTION { public byte BlendOp, BlendFlags, SourceConstantAlpha, AlphaFormat; }
    [StructLayout(LayoutKind.Sequential)]
    struct BITMAPINFOHEADER
    {
        public uint biSize; public int biWidth, biHeight; public ushort biPlanes, biBitCount;
        public uint biCompression, biSizeImage; public int biXPelsPerMeter, biYPelsPerMeter; public uint biClrUsed, biClrImportant;
    }
    [StructLayout(LayoutKind.Sequential)] struct BITMAPINFO { public BITMAPINFOHEADER bmiHeader; public uint bmiColors; }
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    struct WNDCLASSEXW
    {
        public uint cbSize, style; public IntPtr lpfnWndProc; public int cbClsExtra, cbWndExtra;
        public IntPtr hInstance, hIcon, hCursor, hbrBackground; public string lpszMenuName, lpszClassName; public IntPtr hIconSm;
    }
    [StructLayout(LayoutKind.Sequential)] struct MSG { public IntPtr hwnd; public uint message; public IntPtr wParam, lParam; public uint time; public POINT pt; }
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    struct NOTIFYICONDATAW
    {
        public uint cbSize; public IntPtr hWnd; public uint uID, uFlags, uCallbackMessage; public IntPtr hIcon;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)] public string szTip;
        public uint dwState, dwStateMask;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)] public string szInfo;
        public uint uVersion;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)] public string szInfoTitle;
        public uint dwInfoFlags; public Guid guidItem; public IntPtr hBalloonIcon;
    }
    [StructLayout(LayoutKind.Sequential)]
    struct ICONINFO { public int fIcon; public int xHotspot, yHotspot; public IntPtr hbmMask, hbmColor; }

    delegate IntPtr WndProcFn(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);
    delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    // --- imports
    [DllImport("user32.dll", SetLastError = true)] static extern IntPtr SetProcessDpiAwarenessContext(IntPtr value);
    [DllImport("user32.dll")] static extern uint GetDpiForWindow(IntPtr hwnd);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] static extern ushort RegisterClassExW(ref WNDCLASSEXW wc);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    static extern IntPtr CreateWindowExW(uint exStyle, string cls, string name, uint style, int x, int y, int w, int h,
                                         IntPtr parent, IntPtr menu, IntPtr inst, IntPtr param);
    [DllImport("user32.dll")] static extern IntPtr DefWindowProcW(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);
    [DllImport("user32.dll")] static extern int GetMessageW(out MSG msg, IntPtr hWnd, uint min, uint max);
    [DllImport("user32.dll")] static extern bool TranslateMessage(ref MSG msg);
    [DllImport("user32.dll")] static extern IntPtr DispatchMessageW(ref MSG msg);
    [DllImport("user32.dll")] static extern void PostQuitMessage(int code);
    [DllImport("user32.dll")] static extern bool ShowWindow(IntPtr hWnd, int cmd);
    [DllImport("user32.dll")] static extern bool DestroyWindow(IntPtr hWnd);
    [DllImport("user32.dll")]
    static extern bool UpdateLayeredWindow(IntPtr hwnd, IntPtr hdcDst, ref POINT pptDst, ref SIZE psize, IntPtr hdcSrc,
                                           ref POINT pptSrc, uint crKey, ref BLENDFUNCTION pblend, uint flags);
    [DllImport("user32.dll")] static extern IntPtr GetDC(IntPtr hWnd);
    [DllImport("user32.dll")] static extern UIntPtr SetTimer(IntPtr hWnd, UIntPtr id, uint ms, IntPtr proc);
    [DllImport("user32.dll")] static extern bool KillTimer(IntPtr hWnd, UIntPtr id);
    [DllImport("user32.dll")] static extern bool EnumWindows(EnumWindowsProc cb, IntPtr lParam);
    [DllImport("user32.dll")] static extern bool IsWindowVisible(IntPtr hWnd);
    [DllImport("user32.dll")] static extern bool IsIconic(IntPtr hWnd);
    [DllImport("user32.dll")] static extern IntPtr GetWindowLongPtrW(IntPtr hWnd, int index);
    [DllImport("user32.dll")] static extern IntPtr SetWindowLongPtrW(IntPtr hWnd, int index, IntPtr value);
    [DllImport("user32.dll")] static extern IntPtr SetCapture(IntPtr hWnd);
    [DllImport("user32.dll")] static extern bool ReleaseCapture();
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] static extern int MessageBoxW(IntPtr hWnd, string text, string caption, uint type);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] static extern int GetClassNameW(IntPtr hWnd, StringBuilder sb, int max);
    [DllImport("user32.dll")] static extern int GetWindowTextLengthW(IntPtr hWnd);
    [DllImport("user32.dll")] static extern bool GetWindowRect(IntPtr hWnd, out RECT rc);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] static extern IntPtr FindWindowW(string cls, string name);
    [DllImport("user32.dll")] static extern int GetSystemMetrics(int index);
    [DllImport("user32.dll")] static extern IntPtr CreatePopupMenu();
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] static extern bool AppendMenuW(IntPtr menu, uint flags, UIntPtr id, string text);
    [DllImport("user32.dll")] static extern int TrackPopupMenuEx(IntPtr menu, uint flags, int x, int y, IntPtr hwnd, IntPtr tpm);
    [DllImport("user32.dll")] static extern bool DestroyMenu(IntPtr menu);
    [DllImport("user32.dll")] static extern bool SetForegroundWindow(IntPtr hWnd);
    [DllImport("user32.dll")] static extern bool PostMessageW(IntPtr hWnd, uint msg, IntPtr w, IntPtr l);
    [DllImport("user32.dll")] static extern bool GetCursorPos(out POINT pt);
    [DllImport("user32.dll")] static extern short GetKeyState(int key);
    [DllImport("user32.dll")] static extern IntPtr CreateIconIndirect(ref ICONINFO info);
    [DllImport("user32.dll")] static extern IntPtr LoadCursorW(IntPtr inst, IntPtr name);
    [DllImport("gdi32.dll")] static extern IntPtr CreateCompatibleDC(IntPtr hdc);
    [DllImport("gdi32.dll")] static extern IntPtr CreateDIBSection(IntPtr hdc, ref BITMAPINFO bmi, uint usage, out IntPtr bits, IntPtr section, uint offset);
    [DllImport("gdi32.dll")] static extern IntPtr SelectObject(IntPtr hdc, IntPtr obj);
    [DllImport("gdi32.dll")] static extern bool DeleteObject(IntPtr obj);
    [DllImport("gdi32.dll")] static extern IntPtr CreateBitmap(int w, int h, uint planes, uint bpp, IntPtr bits);
    [DllImport("dwmapi.dll")] static extern int DwmGetWindowAttribute(IntPtr hwnd, int attr, out RECT value, int size);
    [DllImport("dwmapi.dll")] static extern int DwmGetWindowAttribute(IntPtr hwnd, int attr, out int value, int size);
    [DllImport("shell32.dll", CharSet = CharSet.Unicode)] static extern bool Shell_NotifyIconW(uint msg, ref NOTIFYICONDATAW data);
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)] static extern IntPtr GetModuleHandleW(string name);

    // --- state
    static WndProcFn wndProc;                 // kept alive so the GC doesn't collect the callback
    static EnumWindowsProc enumProc;
    static IntPtr overlay, tray, screenDC, memDC, dib, bits, trayIcon;
    static int bw, bh;                        // buffer size in physical pixels
    static float scale = 1;                   // physical pixels per logical pixel (DPI / 96)
    static SnowWorld world;
    static readonly Stopwatch clock = Stopwatch.StartNew();
    static double lastT;
    static bool paused;
    static bool catching;                     // overlay currently takes clicks (mouse is over a snowball)
    static bool aimTipShown;                  // the "flick to throw, Shift to aim" tip, once per launch
    static readonly List<Win> found = new();
    static readonly StringBuilder clsName = new(64);

    [STAThread]
    static void Main()
    {
        SetProcessDpiAwarenessContext((IntPtr)(-4));   // per-monitor DPI aware v2: real pixel coordinates
        IntPtr inst = GetModuleHandleW(null);

        wndProc = WndProc;
        enumProc = EnumProc;
        var wc = new WNDCLASSEXW
        {
            cbSize = (uint)Marshal.SizeOf<WNDCLASSEXW>(),
            lpfnWndProc = Marshal.GetFunctionPointerForDelegate(wndProc),
            hInstance = inst,
            hCursor = LoadCursorW(IntPtr.Zero, (IntPtr)32512),
            lpszClassName = "desktopsnowfallWnd",
        };
        RegisterClassExW(ref wc);

        bw = GetSystemMetrics(0);   // SM_CXSCREEN (primary monitor)
        bh = GetSystemMetrics(1);   // SM_CYSCREEN
        // Transparent, click-through, always on top, never takes focus, not in Alt+Tab
        overlay = CreateWindowExW(WS_EX_LAYERED | WS_EX_TRANSPARENT | WS_EX_TOPMOST | WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE,
                                  "desktopsnowfallWnd", "desktopsnowfall", WS_POPUP, 0, 0, bw, bh,
                                  IntPtr.Zero, IntPtr.Zero, inst, IntPtr.Zero);
        // A hidden window that owns the tray icon and its menu
        tray = CreateWindowExW(WS_EX_TOOLWINDOW, "desktopsnowfallWnd", "desktopsnowfall tray", WS_POPUP, 0, 0, 0, 0,
                               IntPtr.Zero, IntPtr.Zero, inst, IntPtr.Zero);

        uint dpi = GetDpiForWindow(overlay);
        scale = dpi > 0 ? dpi / 96f : 1;
        screenDC = GetDC(IntPtr.Zero);
        CreateSurface();
        LoadSettings();
        world = new SnowWorld(bw / scale, bh / scale) { LightsOn = ReadSetting("FairyLights") };   // off by default

        ShowWindow(overlay, SW_SHOWNOACTIVATE);
        AddTrayIcon();
        SetTimer(overlay, (UIntPtr)1, Cfg.FrameMs, IntPtr.Zero);
        // Show the how-to-play tips once, the very first time the app opens
        if (!ReadSetting("SeenGuide"))
        {
            WriteSetting("SeenGuide", true);
            SetTimer(tray, (UIntPtr)2, 1000, IntPtr.Zero);
        }

        while (GetMessageW(out var msg, IntPtr.Zero, 0, 0) > 0)
        {
            TranslateMessage(ref msg);
            DispatchMessageW(ref msg);
        }
    }

    static void CreateSurface()
    {
        if (memDC == IntPtr.Zero) memDC = CreateCompatibleDC(IntPtr.Zero);
        if (dib != IntPtr.Zero) DeleteObject(dib);
        var bmi = new BITMAPINFO
        {
            bmiHeader = new BITMAPINFOHEADER
            {
                biSize = (uint)Marshal.SizeOf<BITMAPINFOHEADER>(),
                biWidth = bw, biHeight = -bh,   // negative = top-down rows
                biPlanes = 1, biBitCount = 32, biCompression = 0,
            }
        };
        dib = CreateDIBSection(memDC, ref bmi, 0, out bits, IntPtr.Zero, 0);
        SelectObject(memDC, dib);
    }

    static IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
    {
        switch (msg)
        {
            case WM_TIMER:
                if (hWnd == overlay) Tick();
                else if (hWnd == tray) { KillTimer(tray, (UIntPtr)2); ShowGuide(); }
                return IntPtr.Zero;
            case WM_MOUSEACTIVATE:
                return (IntPtr)MA_NOACTIVATE;   // grabbing a snowball never steals focus from your apps
            case WM_RBUTTONDOWN:
                if (hWnd == overlay) world.Smash(world.MouseX, world.MouseY);   // right click breaks a snowball
                return IntPtr.Zero;
            case WM_LBUTTONDOWN:
                if (hWnd == overlay && world.BeginDrag(world.MouseX, world.MouseY))
                {
                    SetCapture(overlay);
                    if (!aimTipShown && !ReadSetting("UsedAim")) { aimTipShown = true; ShowAimTip(); }
                }
                return IntPtr.Zero;
            case WM_LBUTTONUP:
                if (hWnd == overlay && world.DragBall != null)
                {
                    world.EndDrag();
                    ReleaseCapture();
                    if (world.SlingUsed && !ReadSetting("UsedAim")) WriteSetting("UsedAim", true);   // got it: no more tips
                }
                return IntPtr.Zero;
            case WM_CAPTURECHANGED:
                if (hWnd == overlay) world.EndDrag();
                return IntPtr.Zero;
            case WM_DISPLAYCHANGE:
                if (hWnd == overlay)
                {
                    bw = GetSystemMetrics(0);
                    bh = GetSystemMetrics(1);
                    CreateSurface();
                    world.Resize(bw / scale, bh / scale);
                }
                return IntPtr.Zero;
            case WM_TRAY:
                uint ev = (uint)(lParam.ToInt64() & 0xFFFF);
                if (ev == WM_RBUTTONUP || ev == WM_LBUTTONUP) ShowMenu();
                return IntPtr.Zero;
        }
        return DefWindowProcW(hWnd, msg, wParam, lParam);
    }

    static void Tick()
    {
        double now = clock.Elapsed.TotalSeconds;
        float dt = (float)Math.Min(now - lastT, 1.0 / 20);
        lastT = now;
        if (paused) return;

        GetCursorPos(out POINT m);
        world.MouseX = m.X / scale;
        world.AimKey = (GetKeyState(0x10) & 0x8000) != 0;   // Shift: slingshot aiming
        world.MouseY = m.Y / scale;
        world.Update(dt, ReadWindows, ReadTaskbarTop);
        UpdateMouseCapture();
        Render.World((uint*)bits, bw, bh, world, scale);

        var dst = new POINT();
        var src = new POINT();
        var size = new SIZE { cx = bw, cy = bh };
        var blend = new BLENDFUNCTION { BlendOp = 0, SourceConstantAlpha = 255, AlphaFormat = 1 };   // AC_SRC_ALPHA
        UpdateLayeredWindow(overlay, screenDC, ref dst, ref size, memDC, ref src, 0, ref blend, ULW_ALPHA);
    }

    // The overlay normally lets every click through. Only while the mouse is over a snowball (or carrying one)
    // does it catch clicks, so you can grab the ball without clicking the window underneath.
    static void UpdateMouseCapture()
    {
        bool want = world.DragBall != null || world.BallAt(world.MouseX, world.MouseY) != null;
        if (want == catching) return;
        catching = want;
        long ex = GetWindowLongPtrW(overlay, GWL_EXSTYLE).ToInt64();
        ex = want ? ex & ~(long)WS_EX_TRANSPARENT : ex | WS_EX_TRANSPARENT;
        SetWindowLongPtrW(overlay, GWL_EXSTYLE, (IntPtr)ex);
    }

    // MARK: Windows on screen

    static List<Win> ReadWindows()
    {
        found.Clear();
        EnumWindows(enumProc, IntPtr.Zero);       // top-level windows, front to back
        return new List<Win>(found);
    }

    static bool EnumProc(IntPtr h, IntPtr _)
    {
        if (h == overlay || h == tray) return true;
        if (!IsWindowVisible(h) || IsIconic(h)) return true;
        long ex = GetWindowLongPtrW(h, GWL_EXSTYLE).ToInt64();
        if ((ex & WS_EX_TOOLWINDOW) != 0) return true;                       // tool windows, popups
        if ((ex & WS_EX_TRANSPARENT) != 0 && (ex & WS_EX_LAYERED) != 0) return true;  // other overlays
        if (DwmGetWindowAttribute(h, DWMWA_CLOAKED, out int cloaked, 4) == 0 && cloaked != 0) return true; // other desktops, hidden UWP
        if (GetWindowTextLengthW(h) == 0) return true;
        clsName.Clear();
        GetClassNameW(h, clsName, clsName.Capacity);
        string cls = clsName.ToString();
        if (cls == "Progman" || cls == "WorkerW" || cls == "Shell_TrayWnd" || cls == "Shell_SecondaryTrayWnd") return true;

        // The visible frame (GetWindowRect includes invisible resize borders on Windows 10/11)
        if (DwmGetWindowAttribute(h, DWMWA_EXTENDED_FRAME_BOUNDS, out RECT rc, Marshal.SizeOf<RECT>()) != 0)
            GetWindowRect(h, out rc);
        float l = rc.Left / scale, t = rc.Top / scale, r = rc.Right / scale, b = rc.Bottom / scale;
        if (r - l < 80 || b - t < 40) return true;
        found.Add(new Win(h.ToInt64(), l, t, r, b));
        return true;
    }

    // Top edge of the taskbar when it sits at the bottom and is visible; otherwise null
    static float? ReadTaskbarTop()
    {
        IntPtr tb = FindWindowW("Shell_TrayWnd", null);
        if (tb == IntPtr.Zero || !IsWindowVisible(tb) || !GetWindowRect(tb, out RECT rc)) return null;
        int w = rc.Right - rc.Left, h = rc.Bottom - rc.Top;
        if (w <= h || rc.Top < bh / 2 || rc.Top >= bh - 4) return null;   // not at the bottom, or auto-hidden
        return rc.Top / scale;
    }

    // MARK: Tray icon and menu

    static void AddTrayIcon()
    {
        const int n = 32;
        var bmi = new BITMAPINFO
        {
            bmiHeader = new BITMAPINFOHEADER
            {
                biSize = (uint)Marshal.SizeOf<BITMAPINFOHEADER>(),
                biWidth = n, biHeight = -n, biPlanes = 1, biBitCount = 32,
            }
        };
        IntPtr color = CreateDIBSection(IntPtr.Zero, ref bmi, 0, out IntPtr px, IntPtr.Zero, 0);
        Render.TrayIcon((uint*)px, n);
        IntPtr mask = CreateBitmap(n, n, 1, 1, IntPtr.Zero);
        var info = new ICONINFO { fIcon = 1, hbmMask = mask, hbmColor = color };
        trayIcon = CreateIconIndirect(ref info);
        DeleteObject(color);
        DeleteObject(mask);

        var nid = TrayData();
        nid.uFlags = NIF_MESSAGE | NIF_ICON | NIF_TIP;
        Shell_NotifyIconW(NIM_ADD, ref nid);
    }

    static NOTIFYICONDATAW TrayData() => new()
    {
        cbSize = (uint)Marshal.SizeOf<NOTIFYICONDATAW>(),
        hWnd = tray, uID = 1, uCallbackMessage = WM_TRAY, hIcon = trayIcon,
        szTip = "desktopsnowfall ❄", szInfo = "", szInfoTitle = "",
    };

    static void ShowMenu()
    {
        IntPtr menu = CreatePopupMenu(), amount = CreatePopupMenu();
        AppendMenuW(menu, MF_STRING, (UIntPtr)CMD_PAUSE, paused ? "Resume snow" : "Pause snow");
        AppendMenuW(amount, MF_STRING | (Cfg.TargetFlakes == 120 ? MF_CHECKED : 0), (UIntPtr)CMD_LIGHT, "Light");
        AppendMenuW(amount, MF_STRING | (Cfg.TargetFlakes == 260 ? MF_CHECKED : 0), (UIntPtr)CMD_MEDIUM, "Medium");
        AppendMenuW(amount, MF_STRING | (Cfg.TargetFlakes == 500 ? MF_CHECKED : 0), (UIntPtr)CMD_HEAVY, "Heavy");
        AppendMenuW(amount, MF_SEPARATOR, UIntPtr.Zero, null);
        AppendMenuW(amount, MF_STRING | (Cfg.Gusts ? MF_CHECKED : 0), (UIntPtr)CMD_GUSTS, "Wind &gusts");
        AppendMenuW(amount, MF_STRING | (Cfg.FancyFlakes ? MF_CHECKED : 0), (UIntPtr)CMD_FANCY, "&Crystal and sparkly flakes");
        AppendMenuW(menu, MF_POPUP, (UIntPtr)(ulong)amount.ToInt64(), "S&now");
        IntPtr balls = CreatePopupMenu();
        for (int i = 0; i < 4; i++)
        {
            string name = i == 3 ? $"Experimental (up to {(int)Cfg.ExperimentalMaxRadius} px)" : Cfg.GrowthNames[i];
            AppendMenuW(balls, MF_STRING | (Cfg.GrowthMode == i ? MF_CHECKED : 0), (UIntPtr)(uint)(CMD_GROWTH + i), name);
        }
        AppendMenuW(balls, MF_SEPARATOR, UIntPtr.Zero, null);
        for (int i = 0; i < MaxBallChoices.Length; i++)
            AppendMenuW(balls, MF_STRING | (Cfg.MaxBalls == MaxBallChoices[i] ? MF_CHECKED : 0), (UIntPtr)(uint)(CMD_MAXBALLS + i),
                        $"At most {MaxBallChoices[i]} snowballs");
        AppendMenuW(balls, MF_SEPARATOR, UIntPtr.Zero, null);
        AppendMenuW(balls, MF_STRING | (Cfg.Trails ? MF_CHECKED : 0), (UIntPtr)CMD_TRAILS, "Rolling &trails");
        AppendMenuW(balls, MF_STRING | (Cfg.CarrotFalls ? MF_CHECKED : 0), (UIntPtr)CMD_CARROT, "Snowman's &nose can fall off");
        AppendMenuW(menu, MF_POPUP, (UIntPtr)(ulong)balls.ToInt64(), "&Snowballs");
        IntPtr visitors = CreatePopupMenu();
        for (int i = 0; i < Cfg.Visitors.Length; i++)
            AppendMenuW(visitors, MF_STRING | (Cfg.Visitors[i] ? MF_CHECKED : 0), (UIntPtr)(uint)(CMD_VISITOR + i), Cfg.VisitorNames[i]);
        AppendMenuW(visitors, MF_SEPARATOR, UIntPtr.Zero, null);
        AppendMenuW(visitors, MF_STRING, (UIntPtr)CMD_INVITE, "&Invite one now");
        AppendMenuW(menu, MF_POPUP, (UIntPtr)(ulong)visitors.ToInt64(), "&Visitors 🐧");
        AppendMenuW(menu, MF_STRING | (world.LightsOn ? MF_CHECKED : 0), (UIntPtr)CMD_LIGHTS, "Fairy &Lights ✨");
        IntPtr style = CreatePopupMenu();
        for (int i = 0; i < LightLook.PresetNames.Length; i++)
            AppendMenuW(style, MF_STRING | (Cfg.LightPreset == i ? MF_CHECKED : 0), (UIntPtr)(uint)(CMD_PRESET + i), LightLook.PresetNames[i]);
        AppendMenuW(style, MF_SEPARATOR, UIntPtr.Zero, null);
        foreach (int i in LightLook.MenuOrder)
            AppendMenuW(style, MF_STRING | (Cfg.LightPattern == i ? MF_CHECKED : 0), (UIntPtr)(uint)(CMD_PATTERN + i), LightLook.PatternNames[i]);
        AppendMenuW(style, MF_SEPARATOR, UIntPtr.Zero, null);
        AppendMenuW(style, MF_STRING | (Cfg.WireSnow ? MF_CHECKED : 0), (UIntPtr)CMD_WIRESNOW, "Snow on the wires");
        AppendMenuW(menu, MF_POPUP, (UIntPtr)(ulong)style.ToInt64(), "Light st&yle");
        AppendMenuW(menu, MF_STRING, (UIntPtr)CMD_CLEAR, "&Clear snow");
        AppendMenuW(menu, MF_STRING, (UIntPtr)CMD_GUIDE, "&How to Play ❔");
        AppendMenuW(menu, MF_SEPARATOR, UIntPtr.Zero, null);
        AppendMenuW(menu, MF_STRING, (UIntPtr)CMD_QUIT, "Quit");

        GetCursorPos(out POINT pt);
        SetForegroundWindow(tray);   // so the menu closes when you click elsewhere
        int cmd = TrackPopupMenuEx(menu, TPM_RETURNCMD | TPM_RIGHTBUTTON, pt.X, pt.Y, tray, IntPtr.Zero);
        PostMessageW(tray, WM_NULL, IntPtr.Zero, IntPtr.Zero);
        DestroyMenu(menu);   // also destroys the submenu

        switch (cmd)
        {
            case CMD_PAUSE:
                paused = !paused;
                ShowWindow(overlay, paused ? SW_HIDE : SW_SHOWNOACTIVATE);
                break;
            case CMD_LIGHT: Cfg.TargetFlakes = 120; break;
            case CMD_MEDIUM: Cfg.TargetFlakes = 260; break;
            case CMD_HEAVY: Cfg.TargetFlakes = 500; break;
            case CMD_CLEAR: world.ClearAll(); break;
            case CMD_LIGHTS:
                world.LightsOn = !world.LightsOn;
                WriteSetting("FairyLights", world.LightsOn);
                world.UpdateLights();
                break;
            case CMD_GUIDE: ShowGuide(); break;
            case CMD_QUIT: Quit(); break;
            case >= CMD_VISITOR and < CMD_VISITOR + 3:
                Cfg.Visitors[cmd - CMD_VISITOR] = !Cfg.Visitors[cmd - CMD_VISITOR];
                WriteSetting("Visitor" + (cmd - CMD_VISITOR), Cfg.Visitors[cmd - CMD_VISITOR]);
                break;
            case CMD_INVITE:
                if (!world.Invite() && world.Visitors.Count > 0) { world.Visitors.RemoveAt(0); world.Invite(); }
                break;
            case >= CMD_PRESET and < CMD_PRESET + 6:
                Cfg.LightPreset = cmd - CMD_PRESET;
                WriteInt("LightPreset", Cfg.LightPreset);
                break;
            case >= CMD_PATTERN and < CMD_PATTERN + 6:
                Cfg.LightPattern = cmd - CMD_PATTERN;
                WriteInt("LightPattern", Cfg.LightPattern);
                break;
            case CMD_WIRESNOW:
                Cfg.WireSnow = !Cfg.WireSnow;
                WriteSetting("WireSnow", Cfg.WireSnow);
                break;
            case CMD_GUSTS:
                Cfg.Gusts = !Cfg.Gusts;
                WriteSetting("Gusts", Cfg.Gusts);
                break;
            case CMD_FANCY:
                Cfg.FancyFlakes = !Cfg.FancyFlakes;   // new flakes pick it up as they fall in
                WriteSetting("FancyFlakes", Cfg.FancyFlakes);
                break;
            case CMD_CARROT:
                Cfg.CarrotFalls = !Cfg.CarrotFalls;
                WriteSetting("CarrotFalls", Cfg.CarrotFalls);
                break;
            case CMD_TRAILS:
                Cfg.Trails = !Cfg.Trails;
                WriteSetting("Trails", Cfg.Trails);
                break;
            case >= CMD_GROWTH and < CMD_GROWTH + 4:
                Cfg.GrowthMode = cmd - CMD_GROWTH;
                WriteInt("GrowthMode", Cfg.GrowthMode);
                break;
            case >= CMD_MAXBALLS and < CMD_MAXBALLS + 3:
                Cfg.MaxBalls = MaxBallChoices[cmd - CMD_MAXBALLS];
                WriteInt("MaxBalls", Cfg.MaxBalls);
                world.TrimBalls();
                break;
        }
    }

    // A small notification from the tray icon the first time a snowball is picked up
    static void ShowAimTip()
    {
        var nid = TrayData();
        nid.uFlags = NIF_INFO;
        nid.dwInfoFlags = NIIF_INFO;
        nid.szInfoTitle = "You've got a snowball ❄";
        nid.szInfo = "Flick the mouse and let go to throw it.\nTo aim: hold Shift, pull back and let go, like a slingshot.\nRight-click a snowball to break it.";
        Shell_NotifyIconW(NIM_MODIFY, ref nid);
    }

    // The app has no window, so this is where people learn the tricks
    static void ShowGuide()
    {
        const string text =
            "It's snowing on your desktop! Here's what you can do:\n\n" +
            "⚪  Roll a snowball: slide your mouse sideways over snow\n" +
            "🎳  Push it: roll one snowball into another\n" +
            "🖐️  Carry it: hold the left mouse button on a snowball and drag\n" +
            "🎯  Throw it: carry it, flick the mouse and let go\n" +
            "🏹  Aim it: carry it, hold Shift, pull the mouse back and let go (a dotted line shows the path)\n" +
            "💥  Break it: right-click a snowball\n" +
            "⛄  Build a snowman: stack three snowballs\n" +
            "🪟  Make snow fly: drag a window\n" +
            "✨  Hang fairy lights: ❄ tray icon → Fairy Lights\n" +
            "🐧  Watch for visitors: penguins, snow foxes and rabbits drop by, sometimes a few at once.\n" +
            "      Throw a snowball at one and see what it does!\n\n" +
            "Find everything under the ❄ icon in the system tray (bottom right).";
        MessageBoxW(IntPtr.Zero, text, "Welcome to desktopsnowfall ❄️", MB_ICONINFORMATION | MB_SETFOREGROUND | MB_TOPMOST);
    }

    // Settings live in HKCU\Software\desktopsnowfall; values from the registry are clamped to sane ranges
    static void LoadSettings()
    {
        Cfg.GrowthMode = Math.Clamp(ReadInt("GrowthMode", 0), 0, 3);
        Cfg.ExperimentalMaxRadius = Math.Clamp(ReadInt("ExperimentalMaxRadius", 100), 4, 1000);
        int mb = ReadInt("MaxBalls", 20);
        Cfg.MaxBalls = Array.IndexOf(MaxBallChoices, mb) >= 0 ? mb : 20;
        Cfg.Trails = ReadInt("Trails", 1) != 0;
        Cfg.CarrotFalls = ReadInt("CarrotFalls", 1) != 0;
        Cfg.Gusts = ReadInt("Gusts", 1) != 0;
        Cfg.LightPreset = Math.Clamp(ReadInt("LightPreset", 0), 0, LightLook.PresetNames.Length - 1);
        Cfg.LightPattern = Math.Clamp(ReadInt("LightPattern", 5), 0, LightLook.PatternNames.Length - 1);
        Cfg.WireSnow = ReadInt("WireSnow", 1) != 0;
        for (int i = 0; i < Cfg.Visitors.Length; i++) Cfg.Visitors[i] = ReadInt("Visitor" + i, 1) != 0;
        Cfg.FancyFlakes = ReadInt("FancyFlakes", 1) != 0;
    }

    static int ReadInt(string name, int def)
    {
        using var k = Registry.CurrentUser.OpenSubKey(RegKey);
        return k?.GetValue(name) is int v ? v : def;
    }

    static void WriteInt(string name, int value)
    {
        using var k = Registry.CurrentUser.CreateSubKey(RegKey);
        k.SetValue(name, value, RegistryValueKind.DWord);
    }

    static bool ReadSetting(string name) => ReadInt(name, 0) != 0;
    static void WriteSetting(string name, bool on) => WriteInt(name, on ? 1 : 0);

    static void Quit()
    {
        KillTimer(overlay, (UIntPtr)1);
        var nid = TrayData();
        Shell_NotifyIconW(NIM_DELETE, ref nid);
        DestroyWindow(overlay);
        DestroyWindow(tray);
        PostQuitMessage(0);
    }
}
