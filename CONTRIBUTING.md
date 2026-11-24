# Contributing

Thanks for wanting to make it snow better ❄️ Issues and pull requests are welcome.

## The layout

| Path | What it is |
|---|---|
| `desktopsnowfall.swift` | The whole macOS app (AppKit + SpriteKit), one file |
| `windows/Program.cs` | The Windows app (plain Win32 + a small software renderer) |
| `windows/Welcome.cs` | The Windows welcome window (drawn with the same renderer, text with GDI) |
| `windows/tests/` | Headless self-tests for the Windows app: they compile `Program.cs` and `Welcome.cs` and run the simulation and the renderer without opening any windows |
| `installer/desktopsnowfall.iss` | Windows installer (Inno Setup) |
| `build.sh`, `make_app.sh` | Build and run on a Mac / package the `.app` |
| `.github/workflows/` | CI on every push and pull request, and the release workflow |

Both apps avoid extra packages on purpose. Please keep it that way: no game engines, no Electron, no NuGet or Swift packages unless there's a strong reason.

## Build and test

**Windows** (needs the .NET 8 SDK):

```sh
cd windows
dotnet build -c Release -warnaserror     # the build must have no warnings
cd tests
dotnet run -c Release                    # all self-tests must pass; exit code 0
                                         # (set SNOW_PNG_DIR=<folder> to also save pictures of the drawings)
```

**macOS** (needs the Xcode command line tools: `xcode-select --install`):

```sh
zsh build.sh                 # compile and run
zsh make_app.sh 1.0          # package dist/desktopsnowfall.app and a zip
```

No Mac? Push to a branch: CI compiles the Swift app on a real Mac (both Apple silicon and Intel), packages it, and starts it for 15 seconds. That catches compile errors and crashes at launch, but not how anything looks or feels, so Mac changes still need a person with a Mac to try them (see the checklist below).

## What CI checks

| Job | Checks |
|---|---|
| Windows | Build with warnings as errors, all self-tests, a trimmed single-file exe, and the installer |
| macOS | Compile for arm64 and x86_64 (errors show up as annotations), package the `.app`, launch for 15 s and take a screenshot if the runner allows it |

The builds are attached to each run as artifacts.

## Writing self-tests (Windows)

Add a check to `windows/tests/SelfTest.cs`. Tests drive `SnowWorld` with a fake desktop and a fake mouse, step it frame by frame (`Step(world, frames)`), and assert on what happened. The random generator is seeded, so a test always sees the same snow. Two rules:

- Never weaken an existing check to make a change pass. If a check is wrong, explain why in the pull request
- Drawing a frame must not allocate memory; there's a check for that

## Hands-on checklist (both platforms)

Before a release, someone runs through this on a real machine:

- [ ] Snow falls in three layers and piles up on window tops, the taskbar/Dock and the screen bottom
- [ ] Dragging a window throws its snow off; closing a window drops its snow
- [ ] Sliding the mouse over snow rolls a snowball that grows; it pushes other balls
- [ ] Left-click picks a ball up without breaking it; flick to throw; Shift-pull to aim; right-click breaks it (Windows)
- [ ] Three stacked balls make a snowman; knocking it makes it wobble or fall (Windows)
- [ ] Fairy lights hang, twinkle and follow windows; each color and pattern looks right (Windows)
- [ ] Visitors appear, leave footprints, walk behind windows and react when hit (Windows)
- [ ] Clicks reach the apps underneath everywhere except right on a snowball (and on a visible layer of snow, on Windows); focus never jumps to the snow
- [ ] Windows: moving the mouse over snow does nothing; holding the left button down on the snow and sliding rolls a snowball that grows
- [ ] Pause, Clear snow and Quit work; Quit removes the tray/menu bar icon
- [ ] Locking the screen or opening a fullscreen video hides the snow, and it comes back afterwards (Windows)
- [ ] Started with `--reset-welcome`, the welcome window opens after a second; Esc, Enter, Close and "Let's go snow!" close it and the snow keeps going; it doesn't come back on the next start; How to Play opens it again

## Commit style

Small, focused commits with a message that says what changed for the person using the app.
