# Features

Everything desktopsnowfall can do today, and on which platform.

✅ = available  ·  ➖ = not yet

## Snowfall

| Feature | macOS | Windows |
|---|:---:|:---:|
| Three layers of flakes (near, middle, far) drifting in a gentle breeze | ✅ | ✅ |
| Snow amount: Light / Medium / Heavy | ✅ | ✅ |
| Some of the smallest flakes fall in front of windows | ✅ | ✅ |
| Wind gusts that swell and die down every 25–70 s | ➖ | ✅ |
| Crystal flakes (turning six-armed crystals among the near flakes) | ➖ | ✅ |
| Sparkling flakes that glint now and then | ➖ | ✅ |

## Snow piles

| Feature | macOS | Windows |
|---|:---:|:---:|
| Snow piles up on top of windows (only where the top isn't covered by another window) | ✅ | ✅ |
| Snow piles up at the bottom of the screen | ✅ | ✅ |
| Snow on the Dock (needs the Accessibility permission) | ✅ | – |
| Snow on the taskbar (when it sits at the bottom) | – | ✅ |
| Drag a window and the snow flies off; the faster you drag, the farther it goes | ✅ | ✅ |
| Closing or minimizing a window lets its snow fall | ✅ | ✅ |
| Clear all snow | ✅ | ✅ |

## Snowballs

| Feature | macOS | Windows |
|---|:---:|:---:|
| Roll a snowball by sliding the mouse sideways over snow (no clicking); it grows as it rolls | ✅ | ✅ |
| Snowballs push each other; one pushed off the edge falls | ✅ | ✅ |
| Carry a snowball with the left mouse button | ✅ | ✅ |
| A snowball dropped from high up goes *poof* into a heap of snow | ✅ | ✅ |
| Setting a snowball down gently keeps it a snowball (a left click never breaks it) | ➖ | ✅ |
| Right-click a snowball to break it | ➖ | ✅ |
| Size modes: Normal (28 px), Large (42), Giant (70), Experimental (100 by default) | ➖ | ✅ |
| Size limit is kept within a small share of the screen | ➖ | ✅ |
| A soft ring and a puff of powder when a ball reaches full size | ➖ | ✅ |
| Maximum number of snowballs: 10 / 20 / 40 | ➖ (fixed at 20) | ✅ |
| Weight: big balls are slow to start and keep rolling after you let go | ➖ | ✅ |
| Real collisions: big balls knock small ones flying, a hit travels down a row | ➖ | ✅ |
| Powder bursts when snowballs collide | ➖ | ✅ |
| Rolling trails: fading dents in the snow | ➖ | ✅ |
| Throw a snowball: carry it, flick the mouse and let go (bigger balls fly slower) | ➖ | ✅ |
| Aim like a slingshot: hold Shift, pull back and let go | ➖ | ✅ |
| Dotted aiming guide showing the flight path and where it lands | ➖ | ✅ |
| Thrown snowballs burst on windows, snowballs, snowmen, the ground and visitors | ➖ | ✅ |
| A tip about throwing and Shift-aiming the first time you pick up a snowball | ➖ | ✅ |

## Snowmen

| Feature | macOS | Windows |
|---|:---:|:---:|
| Stack three snowballs to make a snowman | ✅ | ✅ |
| Eyes, carrot nose, smile, red scarf, buttons and stick arms | ✅ | ✅ |
| Snowman wobbles when a snowball hits it | ➖ | ✅ |
| A solid knock sends the carrot nose flying (it lies in the snow, then fades) | ➖ | ✅ |
| Bowling: a big fast snowball knocks the snowman over | ➖ | ✅ |
| Hit a snowman's head with a thrown snowball and it can topple | ➖ | ✅ |

## Decorations and visitors

| Feature | macOS | Windows |
|---|:---:|:---:|
| Fairy lights: colorful twinkling bulbs that swing into place along window tops | ✅ | ✅ |
| Fairy lights remembered between launches (off by default) | ✅ | ✅ |
| Glass bulbs with a bright center, soft bloom and a pool of colored light; bulbs light up one by one | ➖ | ✅ |
| Light colors: Classic Christmas, Warm White, Arctic Blue, Candy Colors, Rainbow, Golden Christmas | ➖ | ✅ |
| Light patterns: Sparkle on & off (with star glints), Gentle twinkle, Slow breathing, Cascade, Alternating, Festive sparkle | ➖ | ✅ |
| Lights sway in the wind and swing when a window moves | ➖ | ✅ |
| Snow settles on the light wires and shakes off | ➖ | ✅ |
| Visitors: penguins, snow foxes and rabbits; the first within ~15 s, then every 15–40 s, up to four at once | ➖ | ✅ |
| Visitors leave fading footprints and walk behind windows, never over them | ➖ | ✅ |
| Hit by a snowball, each reacts: the penguin tumbles, slides and shakes (and sometimes throws one back), the fox leaps aside and looks for you, the rabbit hops off to hide and comes back | ➖ | ✅ |
| Personalities: the penguin slips, sits down and kicks snowballs; the fox jumps over small snowballs and chases rolling ones; the rabbit hops, listens and bolts | ➖ | ✅ |
| Each visitor can be switched on or off; "Invite one now" | ➖ | ✅ |

## Controls and settings

| Feature | macOS | Windows |
|---|:---:|:---:|
| Menu: ❄︎ in the menu bar / ❄ in the system tray | ✅ | ✅ |
| Pause / resume snow | ✅ | ✅ |
| How to Play guide (shown once on first launch, and from the menu) | ✅ | ✅ |
| Clicks pass through to your apps, except right on a snowball | ✅ | ✅ |
| Grabbing a snowball never takes focus from the app you're using | ✅ | ✅ |
| Settings remembered between launches | Fairy lights only | All options (registry) |
| Stops drawing while the display sleeps or the screen is locked | ✅ | ➖ |

### Windows tray menu

- **Snow**: Light / Medium / Heavy, Wind gusts, Crystal and sparkly flakes
- **Snowballs**: Normal / Large / Giant / Experimental, at most 10 / 20 / 40, Rolling trails, Snowman's nose can fall off
- **Visitors 🐧**: Penguin, Snow fox, Rabbit, Invite one now
- **Fairy Lights ✨** (press `L` while the menu is open)
- **Light style**: the six colors, the six patterns, Snow on the wires
- **Clear snow**, **How to Play**, **Pause snow**, **Quit**

## Under the hood

- One source file per platform: `desktopsnowfall.swift` (SpriteKit) and `windows/Program.cs` (pure Win32 with its own small renderer). No extra packages.
- Windows draws a frame without allocating memory. Heavy snow takes about 4 ms per frame at 1920×1080.
- Windows has 124 automated checks (`cd windows/tests` then `dotnet run -c Release`) covering the simulation, physics, visitors and drawing.

## Known limits

- Only the main screen gets snow (both platforms).
- Windows: changing the display scaling needs a restart.
- The Dock or taskbar only gets snow when it sits at the bottom of the screen.
- Neither version starts automatically at login.
- The Experimental snowball size can only be changed in the registry (`HKCU\Software\desktopsnowfall\ExperimentalMaxRadius`).
