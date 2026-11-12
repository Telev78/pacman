# Pac-Man (C# WinForms)

[Français](README.md) | **English**

A faithful reproduction of the 1980 arcade Pac-Man in C# / Windows Forms: same maze, same rules, same
ghost behaviors, and the whole arcade cabinet experience: credits, attract mode, demo, 2-player mode
and the three intermissions.

Personal, non-commercial project. Pac-Man is a trademark of Bandai Namco Entertainment.

## Requirements

- Windows
- .NET 10 SDK (`net10.0-windows`)
- `NAudio.WinMM` 3.3.0 NuGet package (restored automatically at build time)

## Build and run

```
dotnet build -c Release
dotnet run -c Release
```

The resulting executable is named `PACMAN.exe`.

### Expected resources

Resources are embedded in the executable. They must be located in the `asset\` folder:

| File | Purpose |
|---|---|
| `asset\asset.png` | Single sprite sheet (characters, text, fruits, intermissions…) |
| `asset\maps\original_maze.txt` | Maze layout |
| `asset\sounds\*.wav` | Arcade sounds |
| `asset\demo.txt` | Key sequence for the demo (optional: without it, the demo is not played) |

The program icon is `Icon.ico`, at the project root.

## Controls

| Key | Action |
|---|---|
| `5` | Insert a coin (up to 99 credits) |
| `1` | Start a 1-player game (1 credit) |
| `2` | Start a 2-player game (2 credits) |
| Arrow keys | Steer Pac-Man |
| `Alt` + `Enter` | Toggle full screen / windowed |
| `F12` / `F11` | Volume up / down (10 % steps) |

As in the arcade, there is no pause. Keys `5`, `1` and `2` also work on the numeric keypad.

## Configuration: `PACMAN.ini`

The file is created automatically next to the executable on first launch. Changes take effect at the next launch;
an invalid value is ignored (the default is used).

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
```

- **JAP / USA version**: changes the ghost names and nicknames (OIKAKE / "AKABEI"… or SHADOW / "BLINKY"…),
  Pac-Man's name (PUCKMAN / PAC-MAN) and the copyright line (namco logo or "© 1980 MIDWAY MFG.CO.").
- **Full screen**: the image is scaled by the largest integer multiple that fits the screen, centered, with black
  bars, to keep pixels sharp.
- The high score is never saved: at each launch it starts again from zero, like a cabinet being powered on.

## What is reproduced

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
- **Intermissions**: after levels 2, 5, 9, 13 and 17 (acts 1, 2, 3, 3, 3).

Animation values (speeds, delays, positions, leg phases, scene durations) were measured frame by frame from
arcade recordings.

## Architecture

```
Program.cs            Entry point, settings loading
UI/                   MainForm (window, keyboard, full screen, volume), Settings (PACMAN.ini), DisplayConfig, InputKey
Screens/              IScreen + ScreenManager (credits), AttractScreen, StartScreen, PlayScreen, DemoScreen,
                      ChaseScene, Intermission1/2/3Scene (IIntermission), ArcadeTexts, DemoRecording
Game/                 Game (state machine of a game), PlayerState, ScoreKeeper, FruitManager, WaveScheduler,
                      GameConfig, LevelSpecs, Pacman
Ghosts/               Ghost and the four ghosts, GhostHouse, GhostMode / GhostPhase
Maze/                 LevelMap, MazeLayout, Tiles, TileType, Direction
Graphics/             GameRenderer (drawing), SpriteManager (sprite sheet with cache), HudLayout
Audio/                AudioEngine (NAudio, mixer + volume), SoundDirector (playback rules), SoundId, SoundVoice
```

Principles:

- Screens (`IScreen`) know nothing about WinForms: the keyboard is translated into `InputKey` by `MainForm`.
- `ScreenManager` owns the credits, the sound and the session's high score, and chains the screens
  (attract → start → game → intermission → …).
- Everything is drawn into a native 224×288 `Bitmap`, scaled up without smoothing.

## Development tools (Debug builds only)

- `F9`: records a demo. A real game is played and the keys are noted; the resulting `demo.txt` file
  is to be copied to `asset\demo.txt` to be embedded. The demo is replayed with a fixed seed, sound off,
  and always with 3 lives, whatever the settings.
- The window title then shows the player, the score, the lives and the dots remaining.

## Credits

Pac-Man is a creation of Namco (now Bandai Namco Entertainment); this project is a non-profit tribute.
The resources (sprites and sounds) are provided with the project, in the `asset\` folder, for personal, non-commercial use.

- **Sprites**: sprite sheet uploaded by Superjustinbros on spriters-resource.com (the original author
  is not known with certainty). The sheet used by the game is a personal adaptation of it.
- **Sounds**: pack uploaded by Inthegrave on sounds.spriters-resource.com, with 125scratch and NamelessSpriter
  credited as contributors.
- **Audio**: [NAudio](https://github.com/naudio/NAudio).

## Out of scope

- No pause, no high score saving (as in the arcade).
- No level 256 "kill screen".
