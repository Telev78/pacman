# Pac-Man and Ms. Pac-Man (C# / SDL3)

[Français](README.md) | **English**

A faithful reproduction of the 1980 arcade Pac-Man and of Ms. Pac-Man (1982) in C# on SDL3: same mazes, same rules,
same ghost behaviors, and the whole arcade cabinet experience: game selection, credits, attract mode, demo,
2-player mode and intermissions. Runs on Windows and Linux.

Personal, non-commercial project. Pac-Man and Ms. Pac-Man are trademarks of Bandai Namco Entertainment.

## Requirements

To **play**: nothing to install, just unpack the archive for your platform (see "Publishing").

To **build**:

- .NET 10 SDK (`net10.0`)
- NuGet packages `SDL3-CS` (C# bindings for SDL3) and `SDL3-CS.Windows` / `SDL3-CS.Linux` / `SDL3-CS.MacOS`
  (native libraries), restored automatically at build time

Platforms: 64-bit Windows and 64-bit Linux (glibc 2.28 or newer, X11 or Wayland). The code is the same on
macOS (untested, not published).

## Build and run

```
dotnet build -c Release
dotnet run --project PACMAN.Sdl -c Release
```

The startup project is `PACMAN.Sdl`. The resulting executable is named `PACMAN.exe` (Windows) or `PACMAN` (Linux).

### Expected resources

Resources are embedded in the `PACMAN.Core` library. They must be located in the `PACMAN.Core\asset\` folder,
with one subfolder per game (`pacman\` and `mspacman\`):

| File (in the game's folder) | Purpose |
|---|---|
| `asset.png` | The game's sprite sheet (characters, text, fruits, intermissions…) |
| `maps\original_maze.txt` | Pac-Man maze layout |
| `maps\maze1.txt` to `maze6.txt` | Layouts of the six Ms. Pac-Man mazes |
| `sounds\*.wav` | Arcade sounds (16-bit PCM WAV, 48 kHz) |
| `demo.txt` | Key sequence for the demo (optional: without it, the demo is not played) |

The icon shared by both games is `asset\icon.png`. The program icon is `PACMAN.Sdl\Icon.ico`.

## Publishing

The `publier.ps1` script (PowerShell, run from the repository root) builds the archives to distribute:

```
powershell -ExecutionPolicy Bypass -File .\publier.ps1 -Version 1.0
```

| Archive (in `dist\`) | Contents |
|---|---|
| `PACMAN-1.0-windows-x64.zip` | `PACMAN\PACMAN.exe` + `LISEZMOI.txt` (French) + `README.txt` (English) |
| `PACMAN-1.0-linux-x64.tar.gz` | `PACMAN/PACMAN` (execute permission included) + `LISEZMOI.txt` + `README.txt` |

Each executable is a single self-contained file: the .NET runtime and the SDL3 library are included, nothing has to be
installed. The script uses the `Windows-x64` and `Linux-x64` publish profiles from
`PACMAN.Sdl\Properties\PublishProfiles\`, which can also be used on their own:

```
dotnet publish PACMAN.Sdl -p:PublishProfile=Windows-x64
dotnet publish PACMAN.Sdl -p:PublishProfile=Linux-x64
```

The notices shipped to users are in `packaging\`, in French (`LISEZMOI-*.txt`) and in English (`README-*.txt`).

## Game selection

At launch, a menu offers **Pac-Man** and **Ms. Pac-Man** (up / down arrows, then `1` or `2` to confirm). Each game has
its own sounds, sprites, mazes, attract mode and demo. `Esc` (when not in full screen) goes back to the menu:
switching games resets the credits and the high score, as if changing cabinets.

To start a game directly, without the menu: use the `--game=pacman` or `--game=mspacman` command-line option, or
`Game=PACMAN` / `Game=MSPACMAN` in `PACMAN.ini`.

## Controls

| Key | Action |
|---|---|
| `5` | Insert a coin (up to 99 credits) |
| `1` | Start a 1-player game (1 credit) |
| `2` | Start a 2-player game (2 credits) |
| Arrow keys | Steer Pac-Man / Ms. Pac-Man |
| `Alt` + `Enter` | Toggle full screen / windowed (`Esc` leaves full screen) |
| `Esc` | Back to the game selection menu (when not in full screen) |
| `F12` / `F11` | Volume up / down (10 % steps) |

As in the arcade, there is no pause. Keys `5`, `1` and `2` also work on the numeric keypad; they are recognized by
their position on the keyboard, so they also work on an AZERTY keyboard, without `Shift`.

**Gamepad**: D-pad or left stick to steer, `Select` (Back) to insert a coin, `Start` or `A` for a 1-player game,
`Y` for 2 players.

## Configuration: `PACMAN.ini`

The file is created automatically next to the executable on first launch. Changes take effect at the next launch;
an invalid value is ignored (the default is used). If the folder is read-only, settings only apply to the current session.

```ini
; Zoom: window size, from 1 to 8 (1 = 224x288, 3 = 672x864)
Zoom=3
; Fullscreen: true to start in full screen
Fullscreen=false
; Volume: from 0 to 100 (also set with F11 / F12 in game, and remembered)
Volume=100
; Version: JAP (Namco, Japanese names) or USA (Midway, American names)
Version=JAP
; Lives: number of starting lives, from 1 to 5
Lives=3
; BonusLife: score that awards a bonus life: 10000, 15000, 20000 or NONE
BonusLife=10000
; Game: ASK (selection menu at startup), PACMAN or MSPACMAN (starts that game directly)
Game=ASK
```

- **JAP / USA version** (Pac-Man only): changes the ghost names and nicknames (OIKAKE / "AKABEI"… or SHADOW / "BLINKY"…),
  Pac-Man's name (PUCKMAN / PAC-MAN) and the copyright line (namco logo or "© 1980 MIDWAY MFG.CO.").
- **Window and full screen**: the image is scaled by the largest integer multiple that fits the window or screen,
  centered, with black bars, to keep pixels sharp. The window is resizable.
- The high score is never saved: at each launch it starts again from zero, like a cabinet being powered on.

## What is reproduced

### Common to both games

- **Game loop** at a fixed 60 Hz, all timers in frames; rendering at the native 224×288 resolution, scaled up in one block.
- **Ghosts**: Blinky, Pinky, Inky and Clyde targets, Scatter / Chase alternation per level, Elroy, ghost house exit
  (individual counters and a global one after a death), frightened mode with deterministic randomness, tunnel slowdown.
- **Pac-Man**: anticipated turns (cornering), a pause on each dot (1 frame, 3 for an energizer),
  zones where upward turns are forbidden.
- **Levels**: speed and duration table per level, fruits (appearing at 70 and 170 dots, random duration of 9 to 10 s),
  level indicator.
- **Score**: a single bonus life per player and per game, shared high score.
- **Sound**: sirens according to the number of dots left, frightened mode, eyes, death, fruit, bonus life, intermissions,
  with priority between background sounds (eyes > panic > siren).
- **Screens**: attract mode (character presentation with the arcade timeline, chase animation),
  start screen (1 player / 1 or 2 players depending on credits), "GAME OVER" demo replayed from recorded keys,
  return to attract mode.
- **2-player mode**: maze, score, lives, level and fruit specific to each player, alternating after each lost life.
- **Intermissions**: after levels 2, 5, 9, 13 and 17 (acts 1, 2, 3, 3, 3), specific to each game.
- **Lives and bonus life**: the `Lives` and `BonusLife` settings apply to both games.

### Specific to Ms. Pac-Man

- **Mazes**: six mazes, chained by level (1-2, 3-5, 6-9, 10-13, then alternating color variants in blocks of
  4 levels), with their tunnels and their zones where upward turns are forbidden (maze 2 has none).
- **Ghosts**: a single 7-second Scatter phase at the start of the level, then Chase until the end; during that
  Scatter, Blinky and Pinky wander randomly (except Blinky in Elroy) while Inky and Sue head for their corners;
  Sue behaves like Clyde; when leaving the ghost house each ghost picks left or right according to its target;
  no upward overflow bug. Speeds, Elroy thresholds, frightened mode and ghost house counters are those of Pac-Man.
- **Fruits**: appearing at 64 and 176 dots; the fruit moves: it enters through a tunnel, goes to the ghost house,
  makes a clockwise lap around it, then leaves through another tunnel, bouncing along the way (0.5 px per frame).
  The second fruit does not appear if the first is still on screen. Seven fruits for levels 1 to 7, then a
  randomly chosen fruit (fixed for a given level).
- **Death**: a 900° spinning animation starting with the mouth pointing down, with the freeze and the sound timed
  to the arcade.
- **Screens**: attract mode, start screen and demo specific to Ms. Pac-Man; the intermissions ("They meet",
  "The chase", "Junior") are replayed frame by frame from arcade recordings.

Animation values (speeds, delays, positions, leg phases, scene durations) were measured frame by frame from
arcade recordings.

## Architecture

Two projects: `PACMAN.Core`, the game library with no platform dependency, and `PACMAN.Sdl`, the host
(window, keyboard, gamepad, audio output) built on SDL3.

```
PACMAN.Core/          Game library (net10.0)
  Game/               Game (state machine of a game), PlayerState, ScoreKeeper, FruitManager / MsFruitManager,
                      WaveScheduler, GameConfig, LevelSpecs, Pacman,
                      GameVariant (what differs between games): PacManVariant, MsPacManVariant, GameVariants
  Ghosts/             Ghost and the four ghosts, GhostHouse, GhostMode / GhostPhase
  Maze/               LevelMap, MazeLayout, Tiles, TileType, Direction
  Screens/            IScreen + ScreenManager (credits), GameSelectScreen (game choice), AttractScreen, StartScreen,
                      PlayScreen, DemoScreen, ChaseScene, Intermission1/2/3Scene (IIntermission), ArcadeTexts,
                      DemoRecording; Ms. Pac-Man counterparts: MsAttractScreen, MsStartScreen, MsIntermissionScene
  Graphics/           Canvas (in-memory image), Sprite, PngDecoder, GameRenderer (drawing), SpriteManager and
                      MsPacManSprites (sprite sheets with cache), HudLayout
  Audio/              IAudioEngine, MixerAudioEngine (mixer + volume), SoundDirector (playback rules),
                      SoundId, SoundVoice, WavDecoder
  Platform/           Settings (PACMAN.ini), DisplayConfig, InputKey, Assets (embedded resources)
  asset/              pacman/ and mspacman/: sprite sheet, mazes, sounds, demo
PACMAN.Sdl/           SDL3 host (executable PACMAN)
  Program.cs          Entry point, settings loading
  SdlHost.cs          Window, fixed 60 Hz step loop, display, keyboard, gamepad, full screen, volume
  SdlAudioOutput.cs   SDL audio output fed by MixerAudioEngine
packaging/            Notices shipped with the archives (French and English)
publier.ps1           Builds the Windows (.zip) and Linux (.tar.gz) archives
```

Principles:

- `PACMAN.Core` knows nothing about the window, the keyboard or the sound card: the keyboard and gamepad are
  translated into `InputKey` by the host, and screens (`IScreen`) only see that.
- `GameVariant` gathers everything that differs between games (mazes, ghosts, fruits, wave durations, death,
  sprites, screens, intermissions): the engine only knows this class, so a game can be added without touching the
  game loop.
- `ScreenManager` owns the credits, the sound and the session's high score, and chains the screens
  (attract → start → game → intermission → …).
- Everything is drawn into a native 224×288 `Canvas` (ARGB pixels in memory). The host uploads it to an SDL
  texture, which scales it up without smoothing.
- Sound is mixed in `MixerAudioEngine` (custom WAV decoding, voices, gapless intro-then-loop chaining); the host
  simply pulls the samples from it and sends them to the sound card.

## Development tools (Debug builds only)

- `F9`: records a demo. A real game is played and the keys are noted; the resulting `demo.txt` file
  is to be copied to `PACMAN.Core\asset\demo.txt` to be embedded. The demo is replayed with a fixed seed, sound off,
  and always with 3 lives, whatever the settings.
- The window title shows the player, the score, the lives and the dots remaining.

## Credits

Pac-Man is a creation of Namco and Ms. Pac-Man a creation of Midway (now Bandai Namco Entertainment); this project is a non-profit tribute.
The resources (sprites and sounds) are provided with the project, in the `PACMAN.Core\asset\` folder, for personal, non-commercial use.

- **Sprites**: sprite sheets uploaded by [Superjustinbros](https://www.spriters-resource.com/profile/superjustinbros/)
  on spriters-resource.com. The sheets used by the game are a personal adaptation of them.
- **Sounds**: packs uploaded by [125scratch](https://sounds.spriters-resource.com/profile/125scratch/) on
  sounds.spriters-resource.com, with [Brian Fan](https://sounds.spriters-resource.com/profile/brian+fan/) as contributor.
- **Window, input, audio**: [SDL3](https://libsdl.org) and its C# bindings [SDL3#](https://github.com/edwardgushchin/SDL3-CS)
  (zlib license).

## Out of scope

- No pause, no high score saving (as in the arcade).
- No level 256 "kill screen".
- No macOS package (the code is portable, but Apple signing and notarization are not handled).
