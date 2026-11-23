#           ❄︎ desktopsnowfall ❄︎

Let it snow on your Mac! ☃️

Snow falls on your desktop and piles up on your windows and Dock. Roll it into snowballs and build a little snowman. It's all one Swift file, and every flake is drawn in code.

<!-- ![demo](docs/demo.gif) -->

## What it can do ✨

- **Cozy snowfall**: three layers of flakes (near, middle and far) drifting in a gentle wind
- **Snow piles up** on top of your windows, along the Dock and at the bottom of the screen
- **Drag a window and *whoosh*!** The snow flies off. The faster you drag, the farther it goes
- **Roll snowballs** just by moving your mouse sideways over the snow (no clicking!). They grow as they roll
- **Snowballs push each other** along, and one that's pushed off the edge falls down
- **Hold a snowball** with the left mouse button and carry it anywhere
- **Stack three snowballs** and they turn into a snowman with a carrot nose and a red scarf
- **A falling snowball goes *poof*** into a soft heap of snow, ready to be rolled again
- **Fairy lights**: colorful twinkling lights that swing into place along the top of your windows. They're off by default, so turn them on from the ❄︎ menu (or press `L`)

## How to play 🎮

- **Roll a snowball**: slide your mouse sideways over snow on a window, the Dock or the ground
- **Push it**: roll one snowball into another and it gets shoved along
- **Carry it**: hold the left mouse button on a snowball and drag
- **Build a snowman**: stack three snowballs on top of each other ⛄
- **Make it fly**: drag a window and watch the snow whoosh off
- **Hang the lights**: ❄︎ menu → **Fairy Lights** ✨
- **Forgot something?** ❄︎ menu → **How to Play**

## Download 🎁

1. Grab `desktopsnowfall-mac-vX.zip` from [Releases](https://github.com/mugenkyou/desktopsnowfall/releases/latest) (not the "Source code" ones)
2. Unzip it and drag **desktopsnowfall** into your Applications folder
3. Double-click it. The first time, macOS will say it can't open the app. Click **Done**, then go to **System Settings → Privacy & Security**, scroll down and click **Open Anyway**. You only need to do this once ✨

> 🙏 **Why does macOS block it?** This is an early version, and it isn't signed with a paid Apple Developer account yet, so macOS doesn't recognize it. It's not a virus, promise! The app never connects to the internet, and all the code is right here for you to read. Thanks for your patience while I work on it 💙

## Build it yourself 🛠️

```sh
git clone https://github.com/mugenkyou/desktopsnowfall.git
cd desktopsnowfall
zsh build.sh
```

Use the ❄︎ in the menu bar to pause, pick light, medium or heavy snow, switch the fairy lights on, clear the snow, or quit.

- No `swiftc`? Run `xcode-select --install` first.
- Want snow on the Dock too? Turn on **desktopsnowfall** (or **Terminal**, if you run it with `build.sh`) in System Settings → Privacy & Security → Accessibility.
- Want your own `.app`? Run `zsh make_app.sh 1.0`.

## Windows version 🪟 (beta)

Everything above works on Windows too (the taskbar takes the Dock's place), plus a lot that hasn't reached the Mac yet:

- **Snowballs have weight.** Big ones are slow to get going and roll on when you let go, they knock smaller ones flying, and they leave a trail in the snow
- **Throw snowballs.** Pick one up, flick the mouse and let go. To aim, hold **Shift**, pull back and let go like a slingshot; a dotted line shows where it'll land
- **Right-click a snowball to break it.** A left click only picks it up; set it down gently and it stays a snowball
- **Snowmen react.** Knock one and it wobbles, hit it harder and its carrot nose flies off, or bowl it over
- **Winter visitors.** Penguins, snow foxes and rabbits wander along the bottom of the screen and leave footprints. Hit one with a snowball: the penguin tumbles (and might throw one back), the fox leaps aside, the rabbit runs off to hide. They walk behind your windows, never over them
- **Livelier snow.** Turning crystal flakes, a few sparkling ones, and gusts of wind
- **Prettier fairy lights.** Glass bulbs that sparkle on and off, six color sets, six patterns, and snow that settles on the wires
- **Every screen.** Snow falls on all your screens, drifts from an upper screen into a lower one, and snowballs and visitors cross from one to the next
- **Stays out of the way.** The snow pauses while your screen is locked or the display is off, and hides while a fullscreen video, game or presentation is in front

See [FEATURES.md](FEATURES.md) for the full list per platform.

### Install

Download from [Releases](https://github.com/mugenkyou/desktopsnowfall/releases/latest) (once a release with Windows files is published):

- **`desktopsnowfall-setup-x64-vX.exe`**: installer for your user account, no administrator needed. It adds a Start menu entry, can start the snow with Windows, and can be removed again from **Settings → Apps**
- **`desktopsnowfall-win-x64-portable-vX.zip`** (or `win-arm64`): a single 10 MB program, nothing to install. Unzip it anywhere and double-click

Windows may show "Windows protected your PC" because the program isn't code-signed yet: click **More info → Run anyway**. Each download's SHA-256 checksum is listed in `SHA256SUMS.txt` on the release page.

### Build and run it yourself

You'll need the [.NET 8 SDK](https://dotnet.microsoft.com/download) (`winget install Microsoft.DotNet.SDK.8`).

```sh
cd desktopsnowfall/windows
dotnet run -c Release
```

Everything is in the ❄ menu in the system tray (bottom right, maybe behind the **^**): pause, **Snow** (amount, gusts, crystal flakes), **Snowballs** (size, how many, trails, falling nose), **Visitors** (each animal, or invite one now), **Fairy Lights** and **Light style**, **Performance** (smooth 60 fps or battery saver 30 fps, and saving power when unplugged), **Start with Windows**, **Reset all settings**, How to Play, and Quit. Your choices are remembered.

## Troubleshooting 🔧

- **I don't see the ❄ icon (Windows).** Click the **^** next to the clock; drag the ❄ onto the taskbar to keep it there
- **The snow disappeared (Windows).** Is a fullscreen app in front? The snow hides then and comes back when you leave fullscreen. Also check ❄ → **Pause snow**
- **No snow on the Dock (Mac).** Allow **desktopsnowfall** (or **Terminal**, if you run `build.sh`) in System Settings → Privacy & Security → Accessibility, then restart the app
- **It feels heavy on an old laptop.** ❄ → Performance → **Battery saver**, or Snow → **Light**
- **Something's stuck.** ❄ → **Clear snow**, or **Reset all settings** (Windows)

## Known limitations

- Mac: only the main screen gets snow. Windows covers every screen; with different display scaling per screen, snow is sized for the main screen
- The Dock or taskbar only gets snow when it sits at the bottom of the screen
- The Mac version doesn't have the Windows-only features listed above yet
- Neither version is code-signed or notarized yet, so both show a warning the first time you open them
- The Experimental snowball size can only be changed in the registry (`HKCU\Software\desktopsnowfall\ExperimentalMaxRadius`)

## Make it yours 🎨

Every setting lives at the top of `desktopsnowfall.swift` and `windows/Program.cs` (`Cfg`): how many flakes fall, how fast they fall, how tall the piles get, how big a snowball can grow… Change a number and build again.

## Contributing

See [CONTRIBUTING.md](CONTRIBUTING.md) for how to build, test and send a change, and [RELEASE.md](RELEASE.md) for how releases are made.

## License

[MIT](LICENSE)
