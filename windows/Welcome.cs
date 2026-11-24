// The welcome window: shown the first time the app opens, and again from the tray menu's How to Play.
// A normal window (it takes focus, clicks and keys; it is never click-through), drawn by hand: shapes and the
// winter scene with the same renderer as the snow, text with GDI (Segoe UI). Everything scales with the DPI.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace desktopsnowfall;

static unsafe class Welcome
{
    // MARK: Look

    const uint Snow = 0xFFFFFF, Frost = 0xF5FAFE, Ice = 0xE8F5FD, Glacier = 0xB9E5FA, Accent = 0x319EDB,
               Navy = 0x18364D, Slate = 0x698196, Mint = 0xB8EAD9;
    const uint AccentHover = 0x2489C6, AccentDown = 0x1D74A8, TipFill = 0xEEF9F5, TipLine = 0xCDEFE3, Hairline = 0xE2EEF6;

    // Layout in logical px (multiplied by the DPI scale when drawn)
    internal const int W = 600, ContentH = 776;
    const int M = 32, BarH = 56;
    const int HeroX = 24, HeroY = 56, HeroW = W - 48, HeroH = 168;
    const int HeadY = 250, SubY = 294, FeatY = 338, RowH = 84, ColGap = 24, ColW = (W - 2 * M - ColGap) / 2, Tile = 40;
    const int TipY = FeatY + 3 * RowH, TipH = 60;
    const int BtnY = TipY + TipH + 20, BtnH = 48, SecW = 132;
    const int FootY = BtnY + BtnH + 18;

    // The three things you can press, in Tab order
    internal const int CloseX = 0, CloseBtn = 1, Go = 2;
    static readonly (float X, float Y, float W, float H)[] Buttons =
    {
        (W - 24 - 32, 12, 32, 32),
        (M, BtnY, SecW, BtnH),
        (M + SecW + 12, BtnY, W - 2 * M - SecW - 12, BtnH),
    };

    static readonly (string Title, string Text)[] Features =
    {
        ("Roll snowballs", "Press and hold on the snow, then slide sideways. The snowball grows as it rolls."),
        ("Throw snowballs", "Pick up a snowball, flick your mouse and let go. Hold Shift to aim like a slingshot."),
        ("Build snowmen", "Stack three snowballs and create your own little snowman."),
        ("Meet winter visitors", "Watch penguins, snow foxes and rabbits wander across your desktop."),
        ("Fairy lights", "Hang colorful twinkling lights along your windows."),
        ("Beautiful snowfall", "Enjoy drifting flakes, changing winds and sparkling crystals."),
    };

    // MARK: State

    static IntPtr hwnd, baseDC, baseDib, frameDC, frameDib, oldBase, oldFrame;
    static uint* baseBits, frameBits;
    static uint[] heroBase, heroFrame;
    static int cw, ch, contentPx, hw, hh, scroll;     // physical px
    internal static float S = 1;                     // DPI scale
    static IntPtr fBrand, fSmall, fHead, fSub, fNum, fTitle, fBody, fKey, fButton, fButton2;
    static string version = "";
    static Action dismissed;
    static int hover = -1, pressed = -1, focus = Go;
    static bool showFocus, tracking, reducedMotion;
    static readonly float[] hoverK = new float[3];
    static readonly Stopwatch clock = new();
    static double lastT;
    static WndProcFn proc;
    static bool registered;

    // A few flakes drifting through the illustration (not another full-screen snowfall)
    static readonly (float X, float Y, float R, float V, float A, float Ph)[] Flakes = MakeFlakes();
    static (float, float, float, float, float, float)[] MakeFlakes()
    {
        var r = new Random(3);
        var f = new (float, float, float, float, float, float)[40];
        for (int i = 0; i < f.Length; i++)
            f[i] = ((float)r.NextDouble(), (float)r.NextDouble(), 1.1f + 1.6f * (float)r.NextDouble(),
                    12 + 22 * (float)r.NextDouble(), 0.65f + 0.35f * (float)r.NextDouble(), 6.28f * (float)r.NextDouble());
        return f;
    }

    // MARK: Showing and closing

    public static bool IsOpen => hwnd != IntPtr.Zero;

    // Open the window (or bring it to the front if it's already open). onDismiss runs when the user closes it.
    public static void Show(IntPtr icon, string ver, Action onDismiss)
    {
        dismissed = onDismiss;
        if (hwnd != IntPtr.Zero) { ShowWindow(hwnd, SW_RESTORE); SetForegroundWindow(hwnd); return; }
        version = ver;
        IntPtr inst = GetModuleHandleW(null);
        if (!registered)
        {
            proc = WndProc;
            var wc = new WNDCLASSEXW
            {
                cbSize = (uint)Marshal.SizeOf<WNDCLASSEXW>(),
                lpfnWndProc = Marshal.GetFunctionPointerForDelegate(proc),
                hInstance = inst, hIcon = icon, hIconSm = icon,
                hCursor = LoadCursorW(IntPtr.Zero, (IntPtr)IDC_ARROW),
                lpszClassName = "desktopsnowfallWelcome",
            };
            registered = RegisterClassExW(ref wc) != 0;
        }

        // On the screen with the mouse, centered in its work area (above the taskbar)
        GetCursorPos(out POINT pt);
        IntPtr mon = MonitorFromPoint(pt, 2);
        var mi = new MONITORINFO { cbSize = Marshal.SizeOf<MONITORINFO>() };
        GetMonitorInfoW(mon, ref mi);
        S = GetDpiForMonitor(mon, 0, out uint dpi, out _) == 0 && dpi > 0 ? dpi / 96f : 1;
        var (w, h) = WindowSize(mi.rcWork, S);
        int x = mi.rcWork.Left + (mi.rcWork.Right - mi.rcWork.Left - w) / 2;
        int y = mi.rcWork.Top + (mi.rcWork.Bottom - mi.rcWork.Top - h) / 2;

        hwnd = CreateWindowExW(WS_EX_APPWINDOW, "desktopsnowfallWelcome", "Welcome to desktopsnowfall",
                               WS_POPUP | WS_CAPTION | WS_SYSMENU | WS_MINIMIZEBOX, x, y, w, h,
                               IntPtr.Zero, IntPtr.Zero, inst, IntPtr.Zero);
        if (hwnd == IntPtr.Zero) return;
        // No title bar, but keep the system shadow; Windows 11 rounds the corners
        var margins = new MARGINS { Top = 1 };
        DwmExtendFrameIntoClientArea(hwnd, ref margins);
        int round = DWMWCP_ROUND;
        DwmSetWindowAttribute(hwnd, DWMWA_WINDOW_CORNER_PREFERENCE, ref round, 4);
        SetWindowPos(hwnd, IntPtr.Zero, x, y, w, h, SWP_NOZORDER | SWP_FRAMECHANGED);

        SystemParametersInfoW(SPI_GETCLIENTAREAANIMATION, 0, out int anim, 0);
        reducedMotion = anim == 0;                 // "Animation effects" switched off in Windows settings
        hover = pressed = -1; focus = Go; showFocus = false; tracking = false;
        Array.Clear(hoverK);
        Build(S, h);
        clock.Restart(); lastT = 0;
        SetTimer(hwnd, (UIntPtr)1, 33, IntPtr.Zero);
        ShowWindow(hwnd, SW_SHOW);
        // Once, when it opens (never again afterwards). If Windows won't hand over the focus (another app is
        // busy in front), it still goes on top of the other windows, without taking the focus from them.
        if (!SetForegroundWindow(hwnd))
        {
            SetWindowPos(hwnd, (IntPtr)(-1), 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE);   // HWND_TOPMOST...
            SetWindowPos(hwnd, (IntPtr)(-2), 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE);   // ...then back to normal
        }
    }

    // Close it without counting as dismissed (the app is quitting)
    public static void Close()
    {
        dismissed = null;
        if (hwnd != IntPtr.Zero) DestroyWindow(hwnd);
    }

    static void Dismiss()
    {
        var d = dismissed;
        dismissed = null;
        d?.Invoke();
        if (hwnd != IntPtr.Zero) DestroyWindow(hwnd);
    }

    // Full height if the screen has room, otherwise as tall as fits (and the content scrolls)
    static (int, int) WindowSize(RECT work, float s)
    {
        int w = (int)MathF.Round(W * s), full = (int)MathF.Round(ContentH * s);
        int room = work.Bottom - work.Top - (int)(40 * s);
        return (w, Math.Max((int)(240 * s), Math.Min(full, room)));
    }

    // MARK: Buffers, fonts and the parts that never change

    // Make the fonts and buffers for a DPI scale and window height, and draw everything that stays still.
    // (Works without a window, so the self-tests can render it.)
    internal static void Build(float s, int clientH)
    {
        Free();
        S = s;
        cw = (int)MathF.Round(W * s);
        contentPx = (int)MathF.Round(ContentH * s);
        ch = Math.Min(clientH, contentPx);
        scroll = Math.Clamp(scroll, 0, contentPx - ch);
        hw = (int)MathF.Round(HeroW * s); hh = (int)MathF.Round(HeroH * s);
        heroBase = new uint[hw * hh]; heroFrame = new uint[hw * hh];

        baseDC = CreateCompatibleDC(IntPtr.Zero); frameDC = CreateCompatibleDC(IntPtr.Zero);
        baseDib = Dib(baseDC, cw, contentPx, out baseBits, out oldBase);
        frameDib = Dib(frameDC, cw, ch, out frameBits, out oldFrame);

        fBrand = Font(14, 600); fSmall = Font(12, 400); fHead = Font(30, 600); fSub = Font(15, 400);
        fNum = Font(11, 700); fTitle = Font(14, 600); fBody = Font(13, 400); fKey = Font(12, 600);
        fButton = Font(15, 600); fButton2 = Font(14, 600);
        SetBkMode(baseDC, 1); SetBkMode(frameDC, 1);   // TRANSPARENT
        DrawBase();
        fixed (uint* hb = heroBase) DrawHeroScene(hb);
    }

    internal static void Free()
    {
        if (baseDC != IntPtr.Zero) { SelectObject(baseDC, oldBase); DeleteObject(baseDib); DeleteDC(baseDC); baseDC = IntPtr.Zero; }
        if (frameDC != IntPtr.Zero) { SelectObject(frameDC, oldFrame); DeleteObject(frameDib); DeleteDC(frameDC); frameDC = IntPtr.Zero; }
        foreach (var f in new[] { fBrand, fSmall, fHead, fSub, fNum, fTitle, fBody, fKey, fButton, fButton2 })
            if (f != IntPtr.Zero) DeleteObject(f);
        fBrand = fSmall = fHead = fSub = fNum = fTitle = fBody = fKey = fButton = fButton2 = IntPtr.Zero;
        baseBits = frameBits = null;
    }

    static IntPtr Dib(IntPtr dc, int w, int h, out uint* bits, out IntPtr old)
    {
        var bmi = new BITMAPINFOHEADER { biSize = (uint)Marshal.SizeOf<BITMAPINFOHEADER>(), biWidth = w, biHeight = -h, biPlanes = 1, biBitCount = 32 };
        IntPtr dib = CreateDIBSection(dc, ref bmi, 0, out IntPtr p, IntPtr.Zero, 0);
        bits = (uint*)p;
        old = SelectObject(dc, dib);
        return dib;
    }

    static IntPtr Font(float px, int weight) =>
        CreateFontW(-(int)MathF.Round(px * S), 0, 0, 0, weight, 0, 0, 0, 1, 0, 0, 5 /* CLEARTYPE_QUALITY */, 0,
                    weight >= 600 ? "Segoe UI Semibold" : "Segoe UI");

    static float R(uint c) => ((c >> 16) & 255) / 255f;
    static float G(uint c) => ((c >> 8) & 255) / 255f;
    static float B(uint c) => (c & 255) / 255f;
    static float P(float v) => v * S;

    static void Rect(uint* buf, int bw, int bh, float x, float y, float w, float h, float rad, uint c, float a = 1, float oy = 0) =>
        Render.RoundRect(buf, bw, bh, P(x), P(y) - oy, P(x + w), P(y + h) - oy, P(rad), R(c), G(c), B(c), a);

    static void Fill(uint* buf, int n, uint c) => new Span<uint>(buf, n).Fill(0xFF000000 | c);

    static void Text(IntPtr dc, IntPtr font, uint color, string s, float x, float y, float w, float h, uint flags, float oy = 0)
    {
        SelectObject(dc, font);
        SetTextColor(dc, ((color & 0xFF) << 16) | (color & 0xFF00) | ((color >> 16) & 0xFF));
        var r = new RECT { Left = (int)P(x), Top = (int)(P(y) - oy), Right = (int)P(x + w), Bottom = (int)(P(y + h) - oy) };
        DrawTextW(dc, s, -1, ref r, flags);
    }

    // Width of a single line of text, in logical px
    static float TextW(IntPtr dc, IntPtr font, string s)
    {
        SelectObject(dc, font);
        var r = new RECT();
        DrawTextW(dc, s, -1, ref r, DT_CALCRECT | DT_SINGLELINE | DT_NOPREFIX);
        return r.Right / S;
    }

    static void DrawBase()
    {
        uint* b = baseBits;
        int bw = cw, bh = contentPx;
        GdiFlush();
        Render.Origin(0, 0);
        Fill(b, bw * bh, Snow);

        // Top bar: the snowflake badge, name and version
        Badge(b, bw, bh, P(M + 11), P(28), P(11));
        // Feature tiles and their little pictures
        for (int i = 0; i < 6; i++)
        {
            var (fx, fy) = FeaturePos(i);
            Rect(b, bw, bh, fx, fy, Tile, Tile, 10, Ice);
            Glyph(b, bw, bh, i, P(fx + Tile / 2f), P(fy + Tile / 2f));
        }
        // The tip: a mint panel with a Shift key
        Rect(b, bw, bh, M, TipY, W - 2 * M, TipH, 12, TipLine);
        Rect(b, bw, bh, M + 1, TipY + 1, W - 2 * M - 2, TipH - 2, 11, TipFill);
        Rect(b, bw, bh, M + 16, TipY + 14, 64, 32, 7, 0x9ED9C4);
        Rect(b, bw, bh, M + 17, TipY + 15, 62, 28, 6, Snow);
        // Footer band
        Fill(b + (long)(int)P(FootY) * bw, bw * (bh - (int)P(FootY)), Frost);
        Fill(b + (long)(int)P(FootY) * bw, bw * Math.Max(1, (int)S), Hairline);

        GdiFlush();
        IntPtr dc = baseDC;
        Text(dc, fBrand, Navy, "Desktop Snowfall", M + 30, 18, 200, 22, DT_SINGLELINE | DT_VCENTER | DT_NOPREFIX);
        float bwid = TextW(dc, fBrand, "Desktop Snowfall");
        Text(dc, fSmall, Slate, version, M + 30 + bwid + 8, 18, 120, 22, DT_SINGLELINE | DT_VCENTER | DT_NOPREFIX);

        Text(dc, fHead, Navy, "A little winter has arrived.", M, HeadY, W - 2 * M, 42, DT_SINGLELINE | DT_NOPREFIX);
        Text(dc, fSub, Slate, "Your desktop is now a tiny winter wonderland.", M, SubY, W - 2 * M, 24, DT_SINGLELINE | DT_NOPREFIX);

        for (int i = 0; i < 6; i++)
        {
            var (fx, fy) = FeaturePos(i);
            float tx = fx + Tile + 14, tw = ColW - Tile - 14;
            Text(dc, fNum, Accent, $"0{i + 1}", tx, fy - 1, 24, 20, DT_SINGLELINE | DT_VCENTER | DT_NOPREFIX);
            Text(dc, fTitle, Navy, Features[i].Title, tx + 22, fy - 1, tw - 22, 20, DT_SINGLELINE | DT_VCENTER | DT_NOPREFIX | DT_END_ELLIPSIS);
            Text(dc, fBody, Slate, Features[i].Text, tx, fy + 21, tw, 62, DT_WORDBREAK | DT_NOPREFIX);
        }

        Text(dc, fKey, Navy, "Shift", M + 17, TipY + 15, 62, 28, DT_SINGLELINE | DT_CENTER | DT_VCENTER | DT_NOPREFIX);
        Text(dc, fTitle, Navy, "Snowball trick", M + 96, TipY + 9, W - 2 * M - 112, 20, DT_SINGLELINE | DT_NOPREFIX);
        Text(dc, fBody, Slate, "Hold Shift while dragging a snowball to aim. Release to launch it!",
             M + 96, TipY + 30, W - 2 * M - 112, 20, DT_SINGLELINE | DT_NOPREFIX | DT_END_ELLIPSIS);

        const string foot = "Find all your controls under the snowflake icon in the system tray.";
        float fw = TextW(dc, fSmall, foot), fx0 = (W - fw - 20) / 2;
        Text(dc, fSmall, Slate, foot, fx0 + 20, FootY, fw + 4, ContentH - FootY, DT_SINGLELINE | DT_VCENTER | DT_NOPREFIX);
        GdiFlush();
        Render.Origin(0, 0);
        Crystal(b, bw, bh, P(fx0 + 7), P(FootY + (ContentH - FootY) / 2f), P(6), P(1.4f), Accent, 1);
    }

    static (float, float) FeaturePos(int i) => (M + (i % 2) * (ColW + ColGap), FeatY + (i / 2) * RowH);

    // The app's badge: a white six-armed flake on a blue circle
    static void Badge(uint* b, int bw, int bh, float cx, float cy, float r)
    {
        Render.Circle(b, bw, bh, cx, cy, r, 1, R(Accent), G(Accent), B(Accent));
        Crystal(b, bw, bh, cx, cy, r * 0.68f, MathF.Max(1, r * 0.13f), Snow, 1);
    }

    static void Crystal(uint* b, int bw, int bh, float cx, float cy, float arm, float width, uint c, float a)
    {
        for (int k = 0; k < 3; k++)
        {
            float ang = k * MathF.PI / 3 + MathF.PI / 2, dx = MathF.Cos(ang) * arm, dy = MathF.Sin(ang) * arm;
            Render.Line(b, bw, bh, cx - dx, cy - dy, cx + dx, cy + dy, width, a, R(c), G(c), B(c));
        }
        for (int k = 0; k < 6; k++)
        {
            float ang = k * MathF.PI / 3 + MathF.PI / 2, ex = MathF.Cos(ang), ey = MathF.Sin(ang);
            float mx = cx + ex * arm * 0.58f, my = cy + ey * arm * 0.58f, br = arm * 0.3f;
            for (int sgn = -1; sgn <= 1; sgn += 2)
            {
                float ba = ang + sgn * 0.8f;
                Render.Line(b, bw, bh, mx, my, mx + MathF.Cos(ba) * br, my + MathF.Sin(ba) * br, width * 0.85f, a, R(c), G(c), B(c));
            }
        }
    }

    // Six small pictures for the features, drawn around (cx, cy) in physical px
    static void Glyph(uint* b, int bw, int bh, int i, float cx, float cy)
    {
        float u = S;
        void Ball(float x, float y, float r)
        {
            Render.Circle(b, bw, bh, x, y, r, 1, R(Accent), G(Accent), B(Accent));
            Render.Circle(b, bw, bh, x, y, r - 1.6f * u, 1, 1, 1, 1);
            Render.Circle(b, bw, bh, x - r * 0.3f, y + r * 0.25f, r * 0.12f, 0.6f, R(Glacier), G(Glacier), B(Glacier));
        }
        switch (i)
        {
            case 0:   // a ball rolling along, with speed lines
                Render.Line(b, bw, bh, cx - 12 * u, cy + 10 * u, cx + 12 * u, cy + 10 * u, 1.6f * u, 1, R(Glacier), G(Glacier), B(Glacier));
                for (int k = 0; k < 3; k++)
                    Render.Line(b, bw, bh, cx - (14 - k * 2) * u, cy + (-3 + k * 4) * u, cx - (8 - k * 2) * u, cy + (-3 + k * 4) * u,
                                1.5f * u, 0.8f, R(Accent), G(Accent), B(Accent));
                Ball(cx + 3 * u, cy + 1 * u, 8.5f * u);
                break;
            case 1:   // a dotted arc and the ball at its end
                for (int k = 0; k < 6; k++)
                {
                    float t = k / 6f, x = cx - 12 * u + 18 * u * t, y = cy + 10 * u - 26 * u * t + 18 * u * t * t;
                    Render.Circle(b, bw, bh, x, y, 1.3f * u, 0.85f, R(Accent), G(Accent), B(Accent));
                }
                Ball(cx + 8 * u, cy + 1 * u, 5.5f * u);
                break;
            case 2:   // a snowman
                Ball(cx, cy + 6 * u, 7.5f * u);
                Ball(cx, cy - 6.5f * u, 5.5f * u);
                Render.Circle(b, bw, bh, cx - 1.8f * u, cy - 7.5f * u, 0.9f * u, 1, R(Navy), G(Navy), B(Navy));
                Render.Circle(b, bw, bh, cx + 1.8f * u, cy - 7.5f * u, 0.9f * u, 1, R(Navy), G(Navy), B(Navy));
                Render.Triangle(b, bw, bh, cx, cy - 6 * u, cx + 5 * u, cy - 5.2f * u, cx, cy - 4.4f * u, 0.96f, 0.52f, 0.13f);
                break;
            case 3:   // a penguin
                Render.Ellipse(b, bw, bh, cx, cy + 1 * u, 8 * u, 11 * u, 1, R(Navy), G(Navy), B(Navy));
                Render.Ellipse(b, bw, bh, cx, cy + 3.5f * u, 5 * u, 7.5f * u, 1, 1, 1, 1);
                Render.Circle(b, bw, bh, cx - 2.2f * u, cy - 4.5f * u, 0.9f * u, 1, R(Navy), G(Navy), B(Navy));
                Render.Circle(b, bw, bh, cx + 2.2f * u, cy - 4.5f * u, 0.9f * u, 1, R(Navy), G(Navy), B(Navy));
                Render.Triangle(b, bw, bh, cx - 2 * u, cy - 3 * u, cx + 2 * u, cy - 3 * u, cx, cy - 0.5f * u, 0.97f, 0.62f, 0.2f);
                Render.Ellipse(b, bw, bh, cx - 3.5f * u, cy + 12 * u, 2.8f * u, 1.2f * u, 1, 0.97f, 0.62f, 0.2f);
                Render.Ellipse(b, bw, bh, cx + 3.5f * u, cy + 12 * u, 2.8f * u, 1.2f * u, 1, 0.97f, 0.62f, 0.2f);
                break;
            case 4:   // a drooping wire with three bulbs
            {
                float px = cx - 13 * u, py = cy - 7 * u;
                for (int k = 1; k <= 12; k++)
                {
                    float t = k / 12f, x = cx - 13 * u + 26 * u * t, y = cy - 7 * u + 4 * 7 * u * t * (1 - t);
                    Render.Line(b, bw, bh, px, py, x, y, 1.3f * u, 1, R(Slate), G(Slate), B(Slate));
                    px = x; py = y;
                }
                (float t, float r, float g, float bl)[] bulbs = { (0.22f, 0.91f, 0.34f, 0.31f), (0.5f, 0.95f, 0.76f, 0.31f), (0.78f, 0.36f, 0.79f, 0.65f) };
                foreach (var (t, r, g, bl) in bulbs)
                {
                    float x = cx - 13 * u + 26 * u * t, y = cy - 7 * u + 28 * u * t * (1 - t);
                    Render.Circle(b, bw, bh, x, y + 6 * u, 7 * u, 0.18f, r, g, bl);
                    Render.RoundRect(b, bw, bh, x - 1.6f * u, y, x + 1.6f * u, y + 2.6f * u, 0.6f * u, R(Slate), G(Slate), B(Slate));
                    Render.Ellipse(b, bw, bh, x, y + 6 * u, 3.2f * u, 4.2f * u, 1, r, g, bl);
                    Render.Circle(b, bw, bh, x - 1 * u, y + 5 * u, 1 * u, 0.7f, 1, 1, 1);
                }
                break;
            }
            default:  // a snow crystal
                Crystal(b, bw, bh, cx, cy, 12 * u, 2 * u, Accent, 1);
                break;
        }
    }

    // MARK: The winter scene

    // Sky, hills, pines, a snowman and the wire for the lights (the flakes and the bulbs move, so they're added per frame)
    static void DrawHeroScene(uint* h)
    {
        int bw = hw, bh = hh;
        Render.Origin(0, 0);
        for (int y = 0; y < bh; y++)
        {
            float k = y / (float)bh;
            uint c = Mix(0xC6E8FA, 0xEEF8FE, k);
            new Span<uint>(h + (long)y * bw, bw).Fill(0xFF000000 | c);
        }
        float u = S;
        // Soft aurora and moonlight
        Render.Disc(h, bw, bh, 90 * u, 10 * u, 120 * u, 0.22f, R(Mint), G(Mint), B(Mint));
        Render.Disc(h, bw, bh, 200 * u, -10 * u, 110 * u, 0.15f, R(Mint), G(Mint), B(Mint));
        Render.Disc(h, bw, bh, 392 * u, 40 * u, 74 * u, 0.45f, 1, 1, 1);
        Render.Circle(h, bw, bh, 392 * u, 40 * u, 14 * u, 0.95f, 1, 1, 1);
        Render.Circle(h, bw, bh, 387 * u, 37 * u, 3 * u, 0.35f, R(Glacier), G(Glacier), B(Glacier));
        // Hills far away, with pale trees
        Render.Ellipse(h, bw, bh, 140 * u, 200 * u, 270 * u, 74 * u, 1, R(0xDCEFFA), G(0xDCEFFA), B(0xDCEFFA));
        Render.Ellipse(h, bw, bh, 450 * u, 208 * u, 260 * u, 86 * u, 1, R(0xE4F3FC), G(0xE4F3FC), B(0xE4F3FC));
        foreach (var (x, by, ht) in new[] { (36f, 132f, 44f), (174f, 132f, 38f), (414f, 128f, 46f), (522f, 132f, 52f), (548f, 136f, 36f) })
            Pine(h, bw, bh, x * u, by * u, ht * u, 0xA9CFDF, 0.85f);
        // The snowy ground and a soft drift in front
        Render.Ellipse(h, bw, bh, 276 * u, 256 * u, 450 * u, 114 * u, 1, 1, 1, 1);
        Render.Ellipse(h, bw, bh, 470 * u, 190 * u, 160 * u, 34 * u, 0.6f, R(Frost), G(Frost), B(Frost));
        // Pines in front, each with its shadow
        foreach (var (x, by, ht) in new[] { (84f, 152f, 102f), (130f, 154f, 70f), (472f, 152f, 92f) })
        {
            Render.Ellipse(h, bw, bh, (x + 6) * u, (by + 1) * u, ht * 0.32f * u, 3.5f * u, 0.7f, R(0xD5EAF5), G(0xD5EAF5), B(0xD5EAF5));
            Pine(h, bw, bh, x * u, by * u, ht * u, 0x2F6E62, 1);
        }
        // The snowman
        Render.Ellipse(h, bw, bh, 322 * u, 149 * u, 28 * u, 4.5f * u, 0.75f, R(0xD2E7F3), G(0xD2E7F3), B(0xD2E7F3));
        var body = new Snowball { X = 318, Y = 128, R = 20, Decor = 1 };
        var head = new Snowball { X = 318, Y = 128 - 20 - 12 * 0.82f, R = 12, Decor = 2 };
        Render.Ball(h, bw, bh, body, u, 0);
        Render.Ball(h, bw, bh, head, u, 0);
        // The fairy-light wire between the tree tops
        float px = 0, py = 0;
        for (int k = 0; k <= 40; k++)
        {
            var (x, y) = Wire(k / 40f);
            if (k > 0) Render.Line(h, bw, bh, px, py, x, y, 1.1f * u, 0.55f, R(Slate), G(Slate), B(Slate));
            px = x; py = y;
        }
    }

    static (float, float) Wire(float t) => ((86 + 386 * t) * S, (54 + 10 * t + 18 * 4 * t * (1 - t)) * S);

    static void Pine(uint* h, int bw, int bh, float x, float baseY, float ht, uint c, float a)
    {
        float u = S;
        Render.RoundRect(h, bw, bh, x - 2.4f * u, baseY - ht * 0.16f, x + 2.4f * u, baseY, 1 * u, 0.43f, 0.36f, 0.31f, a);
        for (int k = 0; k < 3; k++)
        {
            float top = baseY - ht + k * ht * 0.24f, bot = top + ht * 0.46f, half = ht * (0.2f + 0.075f * k);
            Render.Triangle(h, bw, bh, x, top, x - half, bot, x + half, bot, R(c), G(c), B(c), a);
            // snow resting on each tier: a cap at the tip and a ledge along the bottom
            Render.Triangle(h, bw, bh, x, top, x - half * 0.42f, top + (bot - top) * 0.42f, x + half * 0.42f, top + (bot - top) * 0.42f, 1, 1, 1, 0.95f * a);
            Render.Ellipse(h, bw, bh, x, bot - 0.5f * u, half * 0.92f, MathF.Max(1.5f * u, ht * 0.03f), 0.95f * a, 1, 1, 1);
        }
    }

    static uint Mix(uint a, uint b, float k)
    {
        uint Ch(int sh) => (uint)MathF.Round(((a >> sh) & 255) * (1 - k) + ((b >> sh) & 255) * k) << sh;
        return Ch(16) | Ch(8) | Ch(0);
    }

    // MARK: Each frame

    // Draw the visible part of the window at time t (seconds since it opened) into the frame buffer
    internal static void Compose(double t)
    {
        if (frameBits == null) return;
        float dt = (float)Math.Clamp(t - lastT, 0, 0.1);
        lastT = t;
        for (int i = 0; i < 3; i++)
        {
            float target = hover == i ? 1 : 0;
            hoverK[i] = reducedMotion ? target : hoverK[i] + (target - hoverK[i]) * MathF.Min(1, dt * 14);
        }
        float tt = reducedMotion ? 4 : (float)t;   // reduced motion: a still picture

        GdiFlush();
        Buffer.MemoryCopy(baseBits + (long)scroll * cw, frameBits, (long)cw * ch * 4, (long)cw * ch * 4);
        uint* f = frameBits;
        Render.Origin(0, 0);

        // The scene, with drifting flakes and twinkling bulbs, inside its rounded frame
        fixed (uint* hb = heroBase, hf = heroFrame)
        {
            Buffer.MemoryCopy(hb, hf, (long)hw * hh * 4, (long)hw * hh * 4);
            float u = S;
            for (int i = 0; i < 9; i++)
            {
                var (x, y) = Wire((i + 0.5f) / 9);
                (float r, float g, float b) col = (i % 4) switch { 0 => (0.91f, 0.34f, 0.31f), 1 => (0.95f, 0.76f, 0.31f), 2 => (0.36f, 0.79f, 0.65f), _ => (0.19f, 0.62f, 0.86f) };
                float tw = 0.6f + 0.4f * MathF.Sin(tt * 2.1f + i * 1.7f);
                Render.Disc(hf, hw, hh, x, y + 5 * u, 10 * u, 0.35f * tw, col.r, col.g, col.b);
                Render.RoundRect(hf, hw, hh, x - 1.3f * u, y, x + 1.3f * u, y + 2.2f * u, 0.5f * u, R(Slate), G(Slate), B(Slate));
                Render.Ellipse(hf, hw, hh, x, y + 5 * u, 2.6f * u, 3.4f * u, 1, col.r + (1 - col.r) * 0.3f * tw, col.g + (1 - col.g) * 0.3f * tw, col.b + (1 - col.b) * 0.3f * tw);
            }
            foreach (var (fx, fy, fr, fv, fa, ph) in Flakes)
            {
                float y = (fy * (HeroH + 10) + tt * fv) % (HeroH + 10) - 5;
                float x = fx * HeroW + MathF.Sin(tt * 0.8f + ph) * 6;
                Render.Disc(hf, hw, hh, x * u, y * u, fr * u, fa);
            }
            CompositeHero(f, hf);
        }

        // Close button (top right)
        var (cx0, cy0, cwd, cht) = Buttons[CloseX];
        float oy = scroll;
        if (hoverK[CloseX] > 0.01f) Rect(f, cw, ch, cx0, cy0, cwd, cht, 8, Ice, hoverK[CloseX], oy);
        uint xc = Mix(Slate, Navy, hoverK[CloseX]);
        float xm = P(cx0 + cwd / 2), ym = P(cy0 + cht / 2) - oy, d = P(5.5f);
        Render.Line(f, cw, ch, xm - d, ym - d, xm + d, ym + d, P(1.6f), 1, R(xc), G(xc), B(xc));
        Render.Line(f, cw, ch, xm - d, ym + d, xm + d, ym - d, P(1.6f), 1, R(xc), G(xc), B(xc));

        // Secondary: an outlined button; primary: solid blue, darker on hover and while pressed
        var (sx, sy, sw, sh) = Buttons[CloseBtn];
        Rect(f, cw, ch, sx, sy, sw, sh, 12, Mix(0xD3E5F1, Glacier, hoverK[CloseBtn]), 1, oy);
        Rect(f, cw, ch, sx + 1, sy + 1, sw - 2, sh - 2, 11, Mix(Snow, Ice, hoverK[CloseBtn]), 1, oy);
        var (gx, gy, gw, gh) = Buttons[Go];
        uint gc = pressed == Go && hover == Go ? AccentDown : Mix(Accent, AccentHover, hoverK[Go]);
        Rect(f, cw, ch, gx, gy + 2, gw, gh, 12, 0x1F6E9C, 0.25f, oy);   // soft drop shadow
        Rect(f, cw, ch, gx, gy, gw, gh, 12, gc, 1, oy);

        // Keyboard focus ring
        if (showFocus && focus >= 0)
        {
            var (fx, fy, fw, fh) = Buttons[focus];
            float g = 3;
            var box = new Render.BoxSdf { Cx = P(fx + fw / 2), Cy = P(fy + fh / 2) - oy, Hx = P(fw / 2 + g - 12), Hy = P(fh / 2 + g - 12), Rad = P(12) };
            if (focus == CloseX) { box.Hx = P(fw / 2 + g - 8); box.Hy = P(fh / 2 + g - 8); box.Rad = P(8); }
            Render.Fill(f, cw, ch, P(fx - g - 2), P(fy - g - 2) - oy, P(fx + fw + g + 2), P(fy + fh + g + 2) - oy,
                        new Outline { Box = box, W = P(2) }, 1, R(Navy), G(Navy), B(Navy));
        }

        // A thin scroll bar when the screen is too short for the whole window
        if (contentPx > ch)
        {
            float frac = ch / (float)contentPx, top = scroll / (float)contentPx * ch;
            Render.RoundRect(f, cw, ch, cw - P(7), top + P(4), cw - P(3), top + frac * ch - P(4), P(2), R(Glacier), G(Glacier), B(Glacier));
        }

        GdiFlush();
        Text(frameDC, fButton2, Navy, "Close", sx, sy, sw, sh, DT_SINGLELINE | DT_CENTER | DT_VCENTER | DT_NOPREFIX, oy);
        Text(frameDC, fButton, Snow, "Let's go snow!", gx, gy, gw, gh, DT_SINGLELINE | DT_CENTER | DT_VCENTER | DT_NOPREFIX, oy);

        // Opening: everything fades in from white
        float k = reducedMotion ? 1 : (float)Math.Clamp(t / 0.35, 0, 1);
        if (k < 1)
        {
            GdiFlush();
            k = 1 - (1 - k) * (1 - k);
            uint kk = (uint)(k * 256), inv = 256 - kk;
            for (long i = 0, n = (long)cw * ch; i < n; i++)
            {
                uint p = f[i];
                uint rb = (((p & 0xFF00FF) * kk + 0xFF00FF * inv) >> 8) & 0xFF00FF;
                uint gg = (((p & 0x00FF00) * kk + 0x00FF00 * inv) >> 8) & 0x00FF00;
                f[i] = 0xFF000000 | rb | gg;
            }
        }
    }

    struct Outline : Render.ISdf
    {
        public Render.BoxSdf Box; public float W;
        public float D(float x, float y) => MathF.Abs(Box.D(x, y)) - W / 2;
    }

    // Copy the scene into the frame, rounding its corners
    static void CompositeHero(uint* f, uint* hf)
    {
        int ox = (int)MathF.Round(P(HeroX)), oy = (int)MathF.Round(P(HeroY)) - scroll;
        float rad = P(16);
        for (int y = 0; y < hh; y++)
        {
            int fy = oy + y;
            if (fy < 0 || fy >= ch) continue;
            uint* dst = f + (long)fy * cw + ox, src = hf + (long)y * hw;
            float ly = y + 0.5f;
            bool cornerRow = ly < rad || ly > hh - rad;
            if (!cornerRow) { Buffer.MemoryCopy(src, dst, hw * 4L, hw * 4L); continue; }
            for (int x = 0; x < hw; x++)
            {
                float lx = x + 0.5f;
                float cov = 1;
                if (lx < rad || lx > hw - rad)
                {
                    float qx = lx < rad ? rad - lx : lx - (hw - rad), qy = ly < rad ? rad - ly : ly - (hh - rad);
                    cov = Math.Clamp(rad + 0.5f - MathF.Sqrt(qx * qx + qy * qy), 0, 1);
                }
                if (cov >= 1) dst[x] = src[x];
                else if (cov > 0) dst[x] = 0xFF000000 | Mix(dst[x] & 0xFFFFFF, src[x] & 0xFFFFFF, cov);
            }
        }
    }

    // MARK: Input

    // Which button is at this point (logical px, content coordinates), or -1
    internal static int HitTest(float x, float y)
    {
        for (int i = 0; i < Buttons.Length; i++)
        {
            var (bx, by, bw, bh) = Buttons[i];
            if (x >= bx && x < bx + bw && y >= by && y < by + bh) return i;
        }
        return -1;
    }

    internal static int NextFocus(int from, int dir) => ((from < 0 ? Go : from) + dir + 3) % 3;

    static void Activate(int i) { if (i >= 0) Dismiss(); }   // every button closes the window; the snow carries on

    static void SetScroll(int v)
    {
        int s = Math.Clamp(v, 0, Math.Max(0, contentPx - ch));
        if (s != scroll) { scroll = s; Invalidate(); }
    }

    // Scroll a button into view when it gets the keyboard focus
    static void Reveal(int i)
    {
        var (_, by, _, bh) = Buttons[i];
        int top = (int)P(by - 12), bottom = (int)P(by + bh + 12);
        if (top < scroll) SetScroll(top);
        else if (bottom > scroll + ch) SetScroll(bottom - ch);
    }

    static void Invalidate() { if (hwnd != IntPtr.Zero) InvalidateRect(hwnd, IntPtr.Zero, false); }

    static (float, float) Logical(IntPtr lParam)
    {
        int x = (short)(lParam.ToInt64() & 0xFFFF), y = (short)((lParam.ToInt64() >> 16) & 0xFFFF);
        return (x / S, (y + scroll) / S);
    }

    static IntPtr WndProc(IntPtr h, uint msg, IntPtr wParam, IntPtr lParam)
    {
        switch (msg)
        {
            case WM_NCCALCSIZE:
                if (wParam != IntPtr.Zero) return IntPtr.Zero;   // no title bar or borders: the whole window is ours
                break;
            case WM_NCHITTEST:
            {
                GetCursorPos(out POINT p);
                var q = p;
                ScreenToClient(h, ref q);
                float lx = q.X / S, ly = (q.Y + scroll) / S;
                return (IntPtr)(ly < BarH && HitTest(lx, ly) < 0 ? HTCAPTION : HTCLIENT);   // drag it by the top bar
            }
            case WM_ERASEBKGND:
                return (IntPtr)1;
            case WM_PAINT:
            {
                IntPtr dc = BeginPaint(h, out PAINTSTRUCT ps);
                Compose(clock.Elapsed.TotalSeconds);
                BitBlt(dc, 0, 0, cw, ch, frameDC, 0, 0, SRCCOPY);
                EndPaint(h, ref ps);
                return IntPtr.Zero;
            }
            case WM_TIMER:
                bool still = reducedMotion && Math.Abs(hoverK[0] + hoverK[1] + hoverK[2] - (hover >= 0 ? 1 : 0)) < 0.001f;
                if (!still) Invalidate();
                return IntPtr.Zero;
            case WM_MOUSEMOVE:
            {
                if (!tracking)
                {
                    var tme = new TRACKMOUSEEVENT { cbSize = (uint)Marshal.SizeOf<TRACKMOUSEEVENT>(), dwFlags = TME_LEAVE, hwndTrack = h };
                    tracking = TrackMouseEvent(ref tme);
                }
                var (lx, ly) = Logical(lParam);
                int now = HitTest(lx, ly);
                if (now != hover) { hover = now; Invalidate(); }
                return IntPtr.Zero;
            }
            case WM_MOUSELEAVE:
                tracking = false;
                if (pressed < 0) { hover = -1; Invalidate(); }
                return IntPtr.Zero;
            case WM_LBUTTONDOWN:
            {
                var (lx, ly) = Logical(lParam);
                pressed = HitTest(lx, ly);
                if (pressed >= 0) { focus = pressed; showFocus = false; SetCapture(h); Invalidate(); }
                return IntPtr.Zero;
            }
            case WM_LBUTTONUP:
            {
                var (lx, ly) = Logical(lParam);
                int was = pressed;
                pressed = -1;
                if (was >= 0) ReleaseCapture();
                if (was >= 0 && HitTest(lx, ly) == was) Activate(was);
                else Invalidate();
                return IntPtr.Zero;
            }
            case WM_CAPTURECHANGED:
                pressed = -1;
                return IntPtr.Zero;
            case WM_SETCURSOR:
                if ((lParam.ToInt64() & 0xFFFF) == HTCLIENT)
                {
                    SetCursor(LoadCursorW(IntPtr.Zero, (IntPtr)(hover >= 0 ? IDC_HAND : IDC_ARROW)));
                    return (IntPtr)1;
                }
                break;
            case WM_MOUSEWHEEL:
                SetScroll(scroll - (short)((wParam.ToInt64() >> 16) & 0xFFFF) * (int)P(60) / 120);
                return IntPtr.Zero;
            case WM_KEYDOWN:
            {
                int key = (int)wParam;
                bool shift = (GetKeyState(0x10) & 0x8000) != 0;
                switch (key)
                {
                    case 0x1B: Dismiss(); break;                                   // Esc
                    case 0x0D: Activate(focus >= 0 ? focus : Go); break;           // Enter
                    case 0x20: if (focus >= 0) Activate(focus); break;             // Space
                    case 0x09:                                                     // Tab / Shift+Tab
                    case 0x25: case 0x27:                                          // Left / Right
                        focus = NextFocus(focus, key == 0x25 || (key == 0x09 && shift) ? -1 : 1);
                        showFocus = true; Reveal(focus); Invalidate(); break;
                    case 0x26: SetScroll(scroll - (int)P(40)); break;             // Up
                    case 0x28: SetScroll(scroll + (int)P(40)); break;             // Down
                    case 0x21: SetScroll(scroll - ch * 9 / 10); break;            // Page Up
                    case 0x22: SetScroll(scroll + ch * 9 / 10); break;            // Page Down
                    case 0x24: SetScroll(0); break;                               // Home
                    case 0x23: SetScroll(contentPx); break;                       // End
                }
                return IntPtr.Zero;
            }
            case WM_CLOSE:                       // Alt+F4, or closed from the taskbar
                Dismiss();
                return IntPtr.Zero;
            case WM_DPICHANGED:                  // moved to a screen with another scale, or the scale changed
            {
                var r = Marshal.PtrToStructure<RECT>(lParam);
                float s = (wParam.ToInt64() & 0xFFFF) / 96f;
                IntPtr mon = MonitorFromWindow(h, 2);
                var mi = new MONITORINFO { cbSize = Marshal.SizeOf<MONITORINFO>() };
                GetMonitorInfoW(mon, ref mi);
                var (w, ht) = WindowSize(mi.rcWork, s);
                scroll = (int)(scroll * s / S);
                Build(s, ht);
                SetWindowPos(h, IntPtr.Zero, r.Left, r.Top, w, ht, SWP_NOZORDER | SWP_NOACTIVATE);
                Invalidate();
                return IntPtr.Zero;
            }
            case WM_DESTROY:
                KillTimer(h, (UIntPtr)1);
                Free();
                heroBase = heroFrame = null;
                hwnd = IntPtr.Zero;
                return IntPtr.Zero;
        }
        return DefWindowProcW(h, msg, wParam, lParam);
    }

    // MARK: Self-test helpers

    // The color at a point (logical px, window coordinates) of the last composed frame
    internal static uint PixelAt(float x, float y) => frameBits[(long)(int)P(y) * cw + (int)P(x)] & 0xFFFFFF;
    internal static (float X, float Y, float W, float H) ButtonRect(int i) => Buttons[i];
    internal static int FrameHeight => ch;
    internal static int FrameWidth => cw;
    internal static uint* FrameBits => frameBits;

    // How many lines each feature description needs at the current scale (they have room for three)
    internal static int MostLines()
    {
        int most = 0;
        float tw = ColW - Tile - 14;
        SelectObject(baseDC, fBody);
        foreach (var (_, text) in Features)
        {
            var r = new RECT { Right = (int)P(tw) };
            DrawTextW(baseDC, text, -1, ref r, DT_CALCRECT | DT_WORDBREAK | DT_NOPREFIX);
            var one = new RECT { Right = (int)P(tw) };
            DrawTextW(baseDC, "Ag", -1, ref one, DT_CALCRECT | DT_SINGLELINE);
            most = Math.Max(most, (int)MathF.Round(r.Bottom / (float)one.Bottom));
        }
        return most;
    }

    internal static float HeadingWidth() => TextW(baseDC, fHead, "A little winter has arrived.");

    // MARK: Win32

    delegate IntPtr WndProcFn(IntPtr h, uint msg, IntPtr w, IntPtr l);

    const uint WM_DESTROY = 0x2, WM_PAINT = 0xF, WM_CLOSE = 0x10, WM_ERASEBKGND = 0x14, WM_SETCURSOR = 0x20, WM_NCCALCSIZE = 0x83,
               WM_NCHITTEST = 0x84, WM_KEYDOWN = 0x100, WM_TIMER = 0x113, WM_MOUSEMOVE = 0x200, WM_LBUTTONDOWN = 0x201,
               WM_LBUTTONUP = 0x202, WM_MOUSEWHEEL = 0x20A, WM_CAPTURECHANGED = 0x215, WM_MOUSELEAVE = 0x2A3, WM_DPICHANGED = 0x2E0;
    const int HTCLIENT = 1, HTCAPTION = 2, IDC_ARROW = 32512, IDC_HAND = 32649, SW_SHOW = 5, SW_RESTORE = 9;
    const uint WS_POPUP = 0x80000000, WS_CAPTION = 0xC00000, WS_SYSMENU = 0x80000, WS_MINIMIZEBOX = 0x20000, WS_EX_APPWINDOW = 0x40000;
    const uint SWP_NOSIZE = 0x1, SWP_NOMOVE = 0x2, SWP_NOZORDER = 0x4, SWP_NOACTIVATE = 0x10, SWP_FRAMECHANGED = 0x20, TME_LEAVE = 2, SRCCOPY = 0xCC0020;
    const uint DT_CENTER = 0x1, DT_VCENTER = 0x4, DT_WORDBREAK = 0x10, DT_SINGLELINE = 0x20, DT_CALCRECT = 0x400, DT_NOPREFIX = 0x800, DT_END_ELLIPSIS = 0x8000;
    const uint SPI_GETCLIENTAREAANIMATION = 0x1042;
    const int DWMWA_WINDOW_CORNER_PREFERENCE = 33, DWMWCP_ROUND = 2;

    [StructLayout(LayoutKind.Sequential)] struct RECT { public int Left, Top, Right, Bottom; }
    [StructLayout(LayoutKind.Sequential)] struct POINT { public int X, Y; }
    [StructLayout(LayoutKind.Sequential)] struct MARGINS { public int Left, Right, Top, Bottom; }
    [StructLayout(LayoutKind.Sequential)] struct MONITORINFO { public int cbSize; public RECT rcMonitor, rcWork; public uint dwFlags; }
    [StructLayout(LayoutKind.Sequential)] struct TRACKMOUSEEVENT { public uint cbSize, dwFlags; public IntPtr hwndTrack; public uint dwHoverTime; }
    [StructLayout(LayoutKind.Sequential)] struct PAINTSTRUCT { public IntPtr hdc; public int fErase; public RECT rcPaint; public int fRestore, fIncUpdate; public fixed byte reserved[32]; }
    [StructLayout(LayoutKind.Sequential)]
    struct BITMAPINFOHEADER
    {
        public uint biSize; public int biWidth, biHeight; public ushort biPlanes, biBitCount;
        public uint biCompression, biSizeImage; public int biXPelsPerMeter, biYPelsPerMeter; public uint biClrUsed, biClrImportant;
        public uint pad0, pad1, pad2, pad3;   // room for the color table entry BITMAPINFO carries
    }
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    struct WNDCLASSEXW
    {
        public uint cbSize, style; public IntPtr lpfnWndProc; public int cbClsExtra, cbWndExtra;
        public IntPtr hInstance, hIcon, hCursor, hbrBackground; public string lpszMenuName, lpszClassName; public IntPtr hIconSm;
    }

    [DllImport("user32.dll", CharSet = CharSet.Unicode)] static extern ushort RegisterClassExW(ref WNDCLASSEXW wc);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] static extern IntPtr CreateWindowExW(uint ex, string cls, string name, uint style, int x, int y, int w, int h, IntPtr parent, IntPtr menu, IntPtr inst, IntPtr param);
    [DllImport("user32.dll")] static extern IntPtr DefWindowProcW(IntPtr h, uint msg, IntPtr w, IntPtr l);
    [DllImport("user32.dll")] static extern bool DestroyWindow(IntPtr h);
    [DllImport("user32.dll")] static extern bool ShowWindow(IntPtr h, int cmd);
    [DllImport("user32.dll")] static extern bool SetForegroundWindow(IntPtr h);
    [DllImport("user32.dll")] static extern bool SetWindowPos(IntPtr h, IntPtr after, int x, int y, int w, int ht, uint flags);
    [DllImport("user32.dll")] static extern bool InvalidateRect(IntPtr h, IntPtr rect, bool erase);
    [DllImport("user32.dll")] static extern IntPtr BeginPaint(IntPtr h, out PAINTSTRUCT ps);
    [DllImport("user32.dll")] static extern bool EndPaint(IntPtr h, ref PAINTSTRUCT ps);
    [DllImport("user32.dll")] static extern UIntPtr SetTimer(IntPtr h, UIntPtr id, uint ms, IntPtr fn);
    [DllImport("user32.dll")] static extern bool KillTimer(IntPtr h, UIntPtr id);
    [DllImport("user32.dll")] static extern IntPtr SetCapture(IntPtr h);
    [DllImport("user32.dll")] static extern bool ReleaseCapture();
    [DllImport("user32.dll")] static extern bool TrackMouseEvent(ref TRACKMOUSEEVENT e);
    [DllImport("user32.dll")] static extern IntPtr LoadCursorW(IntPtr inst, IntPtr name);
    [DllImport("user32.dll")] static extern IntPtr SetCursor(IntPtr c);
    [DllImport("user32.dll")] static extern bool GetCursorPos(out POINT p);
    [DllImport("user32.dll")] static extern bool ScreenToClient(IntPtr h, ref POINT p);
    [DllImport("user32.dll")] static extern short GetKeyState(int key);
    [DllImport("user32.dll")] static extern IntPtr MonitorFromPoint(POINT p, uint flags);
    [DllImport("user32.dll")] static extern IntPtr MonitorFromWindow(IntPtr h, uint flags);
    [DllImport("user32.dll")] static extern bool GetMonitorInfoW(IntPtr mon, ref MONITORINFO mi);
    [DllImport("user32.dll")] static extern bool SystemParametersInfoW(uint action, uint param, out int value, uint ini);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] static extern int DrawTextW(IntPtr dc, string text, int n, ref RECT r, uint flags);
    [DllImport("shcore.dll")] static extern int GetDpiForMonitor(IntPtr mon, int type, out uint dpiX, out uint dpiY);
    [DllImport("dwmapi.dll")] static extern int DwmExtendFrameIntoClientArea(IntPtr h, ref MARGINS m);
    [DllImport("dwmapi.dll")] static extern int DwmSetWindowAttribute(IntPtr h, int attr, ref int value, int size);
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)] static extern IntPtr GetModuleHandleW(string name);
    [DllImport("gdi32.dll")] static extern IntPtr CreateCompatibleDC(IntPtr dc);
    [DllImport("gdi32.dll")] static extern bool DeleteDC(IntPtr dc);
    [DllImport("gdi32.dll")] static extern IntPtr CreateDIBSection(IntPtr dc, ref BITMAPINFOHEADER bmi, uint usage, out IntPtr bits, IntPtr section, uint offset);
    [DllImport("gdi32.dll")] static extern IntPtr SelectObject(IntPtr dc, IntPtr obj);
    [DllImport("gdi32.dll")] static extern bool DeleteObject(IntPtr obj);
    [DllImport("gdi32.dll")] static extern bool BitBlt(IntPtr dst, int x, int y, int w, int h, IntPtr src, int sx, int sy, uint rop);
    [DllImport("gdi32.dll", CharSet = CharSet.Unicode)] static extern IntPtr CreateFontW(int h, int w, int esc, int orient, int weight, uint italic, uint underline, uint strike, uint charset, uint outPrec, uint clipPrec, uint quality, uint pitch, string face);
    [DllImport("gdi32.dll")] static extern uint SetTextColor(IntPtr dc, uint color);
    [DllImport("gdi32.dll")] static extern int SetBkMode(IntPtr dc, int mode);
    [DllImport("gdi32.dll")] static extern bool GdiFlush();
}
