# Pac-Man (C#, .NET 10, SDL3): UML and State Diagrams

This document describes the final architecture of the game through 18 diagrams: one package diagram, 6 class diagrams, 2 sequence diagrams and 9 state diagrams, all derived from the source code. The game is split into two projects: the `PACMAN.Core` library, independent of any platform (rules, software rendering, audio mixing), and the `PACMAN.Sdl` host, which opens the window, reads keyboard and gamepad and plays sound with SDL3 on Windows and Linux. The diagrams are written in [Mermaid](https://mermaid.js.org) and render directly in Markdown viewers that support it (GitHub, GitLab, VS Code with an extension, Obsidian, and so on).

## 1. Overview

The `PACMAN.Sdl` project (Program, SdlHost, SdlAudioOutput) is the only part that knows SDL3. It drives the `PACMAN.Core` library, whose seven folders form three levels: the platform (Platform) and the screens (Screens) drive the game (Game, Ghosts, Maze), which relies on rendering (Graphics) and audio (Audio). Game and Ghosts are tightly coupled: Game creates the four ghosts, and a ghost reads Pacman and LevelSpecs, both defined in Game. Rendering and audio depend on no graphics or sound API: GameRenderer draws into a Canvas (a pixel array) and MixerAudioEngine produces samples that the host sends to the sound card.

```mermaid
flowchart TB
  subgraph Sdl["PACMAN.Sdl (host)"]
    Program["Program"]
    SdlHost["SdlHost"]
    SdlAudioOutput["SdlAudioOutput"]
  end
  subgraph Core["PACMAN.Core (library)"]
    subgraph Platform["Platform"]
      Settings["Settings / PACMAN.ini"]
      DisplayConfig["DisplayConfig"]
      InputKey["InputKey"]
      Assets["Assets"]
    end
    subgraph Screens["Screens"]
      ScreenManager["ScreenManager"]
      ScreenList["AttractScreen, StartScreen, PlayScreen, DemoScreen"]
      Scenes["ChaseScene, Intermission1/2/3Scene"]
      ArcadeTexts["ArcadeTexts"]
    end
    subgraph Game["Game"]
      GameC["Game, PlayerState, ScoreKeeper"]
      Rules["FruitManager, WaveScheduler, GameConfig, LevelSpecs"]
      PacmanC["Pacman"]
    end
    subgraph Ghosts["Ghosts"]
      GhostC["Ghost, Blinky, Pinky, Inky, Clyde"]
      GhostHouse["GhostHouse"]
    end
    subgraph Maze["Maze"]
      MazeC["LevelMap, MazeLayout, Tiles, Direction"]
    end
    subgraph Graphics["Graphics"]
      GameRenderer["GameRenderer, HudLayout"]
      SpriteManager["SpriteManager"]
      Pixels["Canvas, Sprite, PngDecoder"]
    end
    subgraph Audio["Audio"]
      SoundDirector["SoundDirector"]
      AudioEngine["IAudioEngine, MixerAudioEngine, SoundVoice, WavDecoder"]
    end
  end

  Program --> SdlHost
  SdlHost --> ScreenManager
  SdlHost --> GameRenderer
  SdlHost --> AudioEngine
  SdlHost --> SdlAudioOutput
  SdlHost --> Platform
  SdlAudioOutput -.->|pulls samples| AudioEngine
  ScreenManager --> ScreenList
  ScreenList --> Scenes
  ScreenList --> GameC
  ScreenList --> GameRenderer
  ScreenManager --> SoundDirector
  GameC --> Rules
  GameC --> PacmanC
  GameC --> GhostC
  GameC --> GhostHouse
  GameC --> SoundDirector
  GhostC --> MazeC
  PacmanC --> MazeC
  GameC --> MazeC
  GameRenderer --> SpriteManager
  GameRenderer --> Pixels
  SpriteManager --> Pixels
  GameRenderer -.->|reads state| GameC
  SoundDirector --> AudioEngine
  SpriteManager -.->|loads| Platform
  AudioEngine -.->|loads| Platform
  ArcadeTexts -.-> Platform
  GameC -.->|lives, bonus life| Platform
```

*Figure 1: package diagram: the PACMAN.Sdl host, the seven PACMAN.Core folders and their dependencies*  
Open full size: [SVG](diagrams/en/01_paquets.svg) · [PNG](diagrams/en/01_paquets.png)

| Folder | Role | Main classes |
| --- | --- | --- |
| PACMAN.Sdl (project) | SDL3 host: window, full screen, keyboard, gamepad, fixed-step loop, audio output | Program, SdlHost, SdlAudioOutput |
| Platform | `PACMAN.ini` settings, abstract keys, access to embedded resources | Settings, DisplayConfig, InputKey, Assets |
| Screens | Screen flow, credits, intermissions | ScreenManager, IScreen, AttractScreen, StartScreen, PlayScreen, DemoScreen, Intermission1/2/3Scene |
| Game | Rules and state of a game | Game, PlayerState, ScoreKeeper, FruitManager, WaveScheduler, Pacman, GameConfig |
| Ghosts | Ghost and ghost house behavior | Ghost, Blinky, Pinky, Inky, Clyde, GhostHouse |
| Maze | Maze, tiles, directions | LevelMap, MazeLayout, Tiles, Direction |
| Graphics | Software drawing at arcade resolution (224×288) | GameRenderer, SpriteManager, HudLayout, Canvas, Sprite, PngDecoder |
| Audio | Sounds, software mixing and volume | SoundDirector, IAudioEngine, MixerAudioEngine, SoundVoice, WavDecoder |

## 2. Class diagrams

The members shown are the most significant ones, not the full list. Relationships follow UML: hollow arrow for inheritance or interface implementation, filled diamond for composition, hollow diamond for aggregation, dashed line for a dependency.

### 2.1 Screens and navigation

Program creates the SdlHost, which owns a ScreenManager. The latter holds the current screen behind the IScreen interface. Credits, the high score and sound cross screens: they live in the ScreenManager. SdlHost translates keyboard and gamepad input into InputKey, which keeps the screens independent of SDL.

```mermaid
classDiagram
  direction TB
  class Program {
    <<static>>
    -Main() int
  }
  class SdlHost {
    <<internal>>
    -GameRenderer gameRenderer
    -MixerAudioEngine audio
    -ScreenManager screens
    -SdlAudioOutput audioOutput
    -bool fullscreen
    -int volumeFrames
    +Run()
    -Draw()
    -PumpEvents()
    -OnKeyDown(KeyboardEvent)
    -OnGamepadButton(GamepadButton)
    -OnGamepadAxis(GamepadAxis, short)
    -TryMapKey(Scancode, out InputKey)$ bool
    -SetFullscreen(bool)
    -ShowNotification(string, string)
  }
  class Settings {
    <<static>>
    +int Zoom
    +bool StartFullscreen
    +int Volume
    +GameVersion Version
    +int Lives
    +int BonusLife
    +Load()
    +SetVolume(int)
    +Save()
  }
  class InputKey {
    <<enumeration>>
    Up
    Down
    Left
    Right
    Coin
    Start1
    Start2
    Record
  }
  class IScreen {
    <<interface>>
    +Enter()
    +Update()
    +OnInput(InputKey)
    +Draw(GameRenderer, Canvas)
  }
  class ScreenManager {
    +int MaxCredits = 99
    +int Credits
    +int HighScore
    +SoundDirector Sound
    +IScreen Current
    +Update()
    +OnInput(InputKey)
    +ShowAttract()
    +ShowStart()
    +ShowDemo()
    +StartGame(int players)
    +OnGameEnded(int finalHighScore)
    -InsertCoin()
    -SwitchTo(IScreen)
  }
  class AttractScreen {
    -int frame
    -ChaseScene chase
  }
  class StartScreen
  class PlayScreen {
    -Game game
    -IIntermission intermission
    -int playerCount
    -CreateIntermission(int, int) IIntermission
  }
  class DemoScreen {
    -Game game
    -DemoRecording recording
    -bool recordMode
    -int frame
    -SaveRecording()
  }
  class ChaseScene {
    +bool EnergizerEaten
    +bool Finished
    +Update()
    +Draw(GameRenderer, Canvas)
  }
  class DemoRecording {
    +LoadEmbedded()$ DemoRecording
    +Add(int frame, Direction)
    +Save(string path)
  }
  class Entry {
    <<struct>>
    +int Frame
    +Direction Direction
  }
  class ArcadeTexts {
    <<static>>
    +bool IsUsa
    +string BonusLife
    +string[] GhostNames
    +string[] GhostNicknames
  }

  Program ..> SdlHost : creates and runs
  SdlHost *-- ScreenManager
  SdlHost ..> InputKey : maps keys
  SdlHost ..> Settings
  IScreen <|.. AttractScreen
  IScreen <|.. StartScreen
  IScreen <|.. PlayScreen
  IScreen <|.. DemoScreen
  ScreenManager o-- IScreen : current screen
  ScreenManager ..> InputKey
  AttractScreen --> ScreenManager
  StartScreen --> ScreenManager
  PlayScreen --> ScreenManager
  DemoScreen --> ScreenManager
  AttractScreen *-- ChaseScene
  DemoScreen *-- DemoRecording
  DemoRecording *-- Entry
  AttractScreen ..> ArcadeTexts
  StartScreen ..> ArcadeTexts
```

*Figure 2: class diagram: screens and navigation*  
Open full size: [SVG](diagrams/en/02_classes_ecrans.svg) · [PNG](diagrams/en/02_classes_ecrans.png)

### 2.2 Intermissions

PlayScreen displays a single IIntermission at a time and pauses the Game meanwhile. The Game only knows the number of the act to play (1, 2 or 3); PlayScreen creates the matching scene.

```mermaid
classDiagram
  direction TB
  class IIntermission {
    <<interface>>
    +bool Finished
    +Update()
    +Draw(GameRenderer, Canvas)
  }
  class PlayScreen {
    -Game game
    -IIntermission intermission
    +Update()
    +Draw(GameRenderer, Canvas)
    -CreateIntermission(int number, int level) IIntermission
  }
  class Game {
    +int PendingIntermission
    +GameState State
    +FinishIntermission()
  }
  class Intermission1Scene {
    -int frame
    -SoundDirector sound
    -int level
    +bool Finished
  }
  class Intermission2Scene {
    -int frame
    -SoundDirector sound
    -int level
    +bool Finished
    -DrawBlinky()
    -DrawAfterTear()
  }
  class Intermission3Scene {
    -int frame
    -SoundDirector sound
    -int level
    +bool Finished
  }
  class SoundDirector {
    +PlayIntermission()
  }
  class GameRenderer {
    +DrawLevelIndicator(Canvas, int level)
  }
  class SpriteManager {
    +GetGiantPacmanSprite(int)
    +GetSheetSprite(int, int)
    +GetWideSheetSprite(int, int, int, int)
  }

  IIntermission <|.. Intermission1Scene
  IIntermission <|.. Intermission2Scene
  IIntermission <|.. Intermission3Scene
  PlayScreen o-- IIntermission : one at a time
  PlayScreen --> Game : FinishIntermission()
  PlayScreen ..> Intermission1Scene : creates (act 1)
  PlayScreen ..> Intermission2Scene : creates (act 2)
  PlayScreen ..> Intermission3Scene : creates (act 3)
  Intermission1Scene --> SoundDirector
  Intermission2Scene --> SoundDirector
  Intermission3Scene --> SoundDirector
  Intermission1Scene ..> GameRenderer
  Intermission2Scene ..> GameRenderer
  Intermission3Scene ..> GameRenderer
  GameRenderer --> SpriteManager
```

*Figure 3: class diagram: intermissions*  
Open full size: [SVG](diagrams/en/03_classes_intermissions.svg) · [PNG](diagrams/en/03_classes_intermissions.png)

### 2.3 Game engine

Game is the center of the engine. It chains the GameState states, delegates the rules to ScoreKeeper, FruitManager, WaveScheduler and GhostHouse, and keeps one PlayerState per player, each with its own maze.

```mermaid
classDiagram
  direction TB
  class Game {
    +GameState State
    +int Score
    +int HighScore
    +int Lives
    +int CurrentLevel
    +int DotsRemaining
    +bool IsFrightenedMode
    +bool IsDemo
    +bool IsFinished
    +int PendingIntermission
    +HandleInput(Direction)
    +Update()
    +FinishIntermission()
    -CheckPelletCollision()
    -CheckGhostCollisions()
    -CheckLevelClear()
    -PrepareNextLifeOrGameOver()
    -SwitchPlayer(PlayerState)
    -StartNextLevel()
  }
  class GameState {
    <<enumeration>>
    Ready
    Playing
    GhostEaten
    Dying
    Victory
    GameOver
    Intermission
  }
  class PlayerState {
    +int Number
    +int Score
    +int Lives
    +int Level
    +bool BonusLifeAwarded
    +bool HasLostALife
    +int DotsRemaining
    +int[] HouseDotCounters
  }
  class ScoreKeeper {
    +int HighScore
    +Add(PlayerState, int points) bool
  }
  class FruitManager {
    +bool IsActive
    +int Value
    +Update(level, dotsRemaining, Pacman) int
    +StartLevel(int totalDots)
    +ResetForNewLife()
    +SaveProgress(PlayerState)
    +LoadProgress(PlayerState)
  }
  class WaveScheduler {
    +GhostMode CurrentMode
    +Reset()
    +Update(level, isFrozen) bool
  }
  class GhostHouse {
    +Reset(level, afterDeath)
    +OnDotEaten()
    +ReleaseByCounter()
    +Update(level)
    +SaveCounters(PlayerState)
    +LoadCounters(PlayerState)
  }
  class Pacman {
    +float X
    +float Y
    +Direction CurrentDirection
    +Direction NextDirection
    +PacmanState State
    +Move(LevelMap, LevelSpecs, bool)
    +Stall(int frames)
  }
  class PacmanState {
    <<enumeration>>
    Alive
    Dying
  }
  class LevelSpecs {
    +float PacmanNormal
    +float GhostNormal
    +float GhostTunnel
    +float FrightenedPacman
    +float FrightenedGhost
    +int FrightenedTimeSec
    +int FlashesCount
    +int Elroy1Dots
    +int Elroy2Dots
  }
  class GameConfig {
    <<static>>
    +GetSpecsForLevel(int) LevelSpecs
    +GetWaveDurationFrames(level, waveIndex) int
  }
  class LevelMap {
    +TileType[,] Grid
    +int[,] VisualGrid
    +LoadMapFromStream(Stream)
  }
  class TileType {
    <<enumeration>>
    Empty
    Wall
    Dot
    Energizer
    GhostDoor
  }
  class Ghost {
    <<abstract>>
  }
  class SoundDirector

  Game *-- "1..2" PlayerState : players
  Game o-- PlayerState : active player
  Game *-- ScoreKeeper
  Game *-- FruitManager
  Game *-- WaveScheduler
  Game *-- GhostHouse
  Game *-- Pacman
  Game *-- "4" Ghost
  Game --> GameState
  Game --> LevelSpecs : level specs
  Game --> SoundDirector
  GhostHouse o-- "4" Ghost
  PlayerState *-- LevelMap : one maze per player
  LevelMap --> TileType
  Pacman --> PacmanState
  Pacman ..> LevelMap
  GameConfig ..> LevelSpecs : fabrique
  ScoreKeeper ..> PlayerState
```

*Figure 4: class diagram: game engine*  
Open full size: [SVG](diagrams/en/04_classes_moteur.svg) · [PNG](diagrams/en/04_classes_moteur.png)

### 2.4 Ghosts

Ghost holds everything common: pixel-by-pixel movement, direction choice, speed, reversal. Each ghost only defines its chase target. Inky keeps a reference to Blinky to compute its own, and Blinky adds the Cruise Elroy state.

```mermaid
classDiagram
  direction LR
  class Ghost {
    <<abstract>>
    +float X
    +float Y
    +GhostType Type*
    +Direction CurrentDirection
    +GhostPhase Phase
    +GhostMode Mode
    +bool IsFrightened
    +int TileX
    +int TileY
    +Move(LevelMap, Pacman, LevelSpecs, GhostMode)
    +RequestReverse()
    +ResetRandom()$
    #GetChaseTargetTile(Pacman)* Point
    #GetCurrentTargetTile(Pacman) Point
    #GetNormalSpeed(LevelSpecs) float
    -MoveOnePixel(...)
    -ChooseNextDirection(...) Direction
    -ChooseFrightenedDirection(LevelMap) Direction
  }
  class Blinky {
    +bool IsCruiseElroy1
    +bool IsCruiseElroy2
    +UpdateElroyStatus(dotsRemaining, LevelSpecs, enabled)
    #GetChaseTargetTile(Pacman) Point
    #GetNormalSpeed(LevelSpecs) float
  }
  class Pinky {
    #GetChaseTargetTile(Pacman) Point
  }
  class Inky {
    -Ghost blinky
    #GetChaseTargetTile(Pacman) Point
  }
  class Clyde {
    #GetChaseTargetTile(Pacman) Point
  }
  class GhostType {
    <<enumeration>>
    Blinky
    Pinky
    Inky
    Clyde
  }
  class GhostPhase {
    <<enumeration>>
    InHouse
    Exiting
    Active
    Eaten
  }
  class GhostMode {
    <<enumeration>>
    Scatter
    Chase
  }

  Ghost <|-- Blinky
  Ghost <|-- Pinky
  Ghost <|-- Inky
  Ghost <|-- Clyde
  Inky --> Ghost : blinky (chase target)
  Ghost --> GhostType
  Ghost --> GhostPhase
  Ghost --> GhostMode
```

*Figure 5: class diagram: ghosts*  
Open full size: [SVG](diagrams/en/05_classes_fantomes.svg) · [PNG](diagrams/en/05_classes_fantomes.png)

### 2.5 Rendering

GameRenderer draws everything into a native 224×288 Canvas (0xAARRGGBB pixels), then the host hands it to SDL, which scales it up by the nearest integer factor. A Sprite is just a rectangular view on a Canvas, with no pixel copy. PngDecoder decodes the single sheet `asset.png` and SpriteManager slices it into cached sprites.

```mermaid
classDiagram
  direction TB
  class GameRenderer {
    +SpriteManager Sprites
    +Render(IScreen) Canvas
    +DrawGame(Canvas, Game)
    +DrawTopHud(...)
    +DrawCredits(Canvas, int)
    +DrawLevelIndicator(Canvas, int)
    +DrawText(Canvas, string, int, int, ArcadeColor)
    +DrawSprite(Canvas, Sprite, int, int)$
    +IsEnergizerOn(int)$ bool
  }
  class SpriteManager {
    +GetGhostSprite(GhostType, Direction, int)
    +GetGhostFrightenedSprite(int, bool)
    +GetGhostEyesSprite(Direction)
    +GetPacmanSprite(int, Direction)
    +GetPacmanDeathSprite(int)
    +GetGiantPacmanSprite(int)
    +GetMazeLayer(LevelMap, MazeColor)
    +GetFruitSprite(int level)
    +GetArcadeCharacter(char, ArcadeColor)
    +GetPtsSprite(ArcadeColor)
    +GetNamcoLogo(ArcadeColor)
    +GetSheetSprite(int, int)
    +GetWideSheetSprite(int, int, int, int)
  }
  class Canvas {
    +int Width
    +int Height
    +uint[] Pixels
    +Clear(uint)
    +GetPixel(int, int) uint
    +SetPixel(int, int, uint)
    +MakeTransparent(uint rgb)
    +Draw(Sprite, int, int)
    +DrawScaled(Sprite, int, int, int, int)
  }
  class Sprite {
    +Canvas Source
    +int X
    +int Y
    +int Width
    +int Height
    +Empty(int, int)$ Sprite
    +GetPixel(int, int) uint
  }
  class PngDecoder {
    <<static>>
    +Decode(Stream) Canvas
  }
  class Assets {
    <<static>>
    +Open(string) Stream
    +TryOpen(string) Stream
  }
  class ArcadeColor {
    <<enumeration>>
  }
  class MazeColor {
    <<enumeration>>
  }
  class HudLayout {
    <<static>>
    +MaxVisibleLives = 5
  }
  class DisplayConfig {
    <<static>>
    +NativeWidth = 224
    +NativeHeight = 288
  }
  class IScreen {
    <<interface>>
    +Draw(GameRenderer, Canvas)
  }
  class Game
  class asset_png["asset.png (single sheet)"]

  GameRenderer *-- SpriteManager
  GameRenderer *-- Canvas : native 224x288 drawing
  GameRenderer ..> IScreen : Render()
  GameRenderer ..> Game : DrawGame()
  GameRenderer ..> HudLayout
  GameRenderer ..> DisplayConfig
  Sprite --> Canvas : Source (view)
  SpriteManager ..> Sprite : returns views
  SpriteManager ..> PngDecoder : decodes the sheet
  SpriteManager ..> Assets : opens
  PngDecoder ..> Canvas : produces
  SpriteManager --> asset_png : slices and caches
  SpriteManager ..> ArcadeColor
  SpriteManager ..> MazeColor
```

*Figure 6: class diagram: rendering*  
Open full size: [SVG](diagrams/en/06_classes_rendu.svg) · [PNG](diagrams/en/06_classes_rendu.png)

### 2.6 Audio

SoundDirector turns game events into sounds and guarantees that only one background sound plays at a time, through the IAudioEngine interface. MixerAudioEngine decodes the 24 sounds at startup (WavDecoder), mixes them itself as 48 kHz stereo floats and applies the volume. It knows no audio output: SdlAudioOutput, on the host side, calls Read() from the SDL callback to fill the sound card buffer.

```mermaid
classDiagram
  direction TB
  class SoundDirector {
    +bool Muted
    +StopAll()
    +CleanUp()
    +ResetBackground()
    +PlayStartJingle()
    +PlayDot()
    +PlayFruitEaten()
    +PlayExtraLife()
    +PlayGhostEaten()
    +PlayDeath()
    +PlayCredit()
    +PlayIntermission()
    +UpdateBackground(level, dotsRemaining, eyesRunning, frightenedGhosts)
    -ComputeSirenIndex(level, dotsRemaining) int
  }
  class Background {
    <<enumeration>>
    None
    Siren
    Frightened
    Eyes
  }
  class IAudioEngine {
    <<interface>>
    +float Volume
    +Play(SoundId, bool loop)
    +PlaySequence(first, next, loopNext)
    +Stop(SoundId)
    +StopAll()
  }
  class MixerAudioEngine {
    +SampleRate = 48000$
    +Channels = 2$
    +float Volume
    +bool OutputAvailable
    +Read(Span~float~) int
    +Play(SoundId, bool loop)
    +PlaySequence(first, next, loopNext)
    +Stop(SoundId)
    +StopAll()
    +Dispose()
  }
  class WavDecoder {
    <<static>>
    +Decode(Stream, int targetSampleRate)$ float[]
  }
  class SoundVoice {
    <<internal>>
    +SoundId FirstId
    +SoundId ChainedId
    +bool IsFinished
    +Stop()
    +MixInto(Span~float~)
  }
  class SoundData {
    <<internal>>
    +SoundId Id
    +float[] Samples
  }
  class SoundId {
    <<enumeration>>
    Credit
    Start
    EatDot0, EatDot1
    EatFruit, EatGhost, Extend
    Death0, Death1
    Siren0 .. Siren4
    Siren0FirstLoop .. Siren4FirstLoop
    Fright, FrightFirstLoop
    Eyes, EyesFirstLoop
    Intermission
  }
  class Assets {
    <<static>>
    +TryOpen(string) Stream
  }
  class SdlAudioOutput {
    <<PACMAN.Sdl>>
    +Start() bool
    -Feed(...)
    +Dispose()
  }

  SoundDirector *-- IAudioEngine
  SoundDirector --> Background : one background sound
  IAudioEngine <|.. MixerAudioEngine
  MixerAudioEngine *-- "24" SoundData : decoded at startup
  MixerAudioEngine o-- "0..*" SoundVoice : running voices
  MixerAudioEngine ..> WavDecoder : decodes
  MixerAudioEngine ..> Assets : opens the WAVs
  SoundVoice --> SoundData
  SoundData --> SoundId
  SdlAudioOutput --> MixerAudioEngine : Read()
```

*Figure 7: class diagram: audio*  
Open full size: [SVG](diagrams/en/07_classes_audio.svg) · [PNG](diagrams/en/07_classes_audio.png)

## 3. Sequence diagrams

### 3.1 One game frame at 60 Hz

The SdlHost.Run loop accumulates the elapsed time and calls the logic update as many times as needed, at a fixed step of 1/60 s (catch-up capped at 100 ms), before redrawing. The Canvas produced by GameRenderer is then copied into an SDL texture and presented. Game speed therefore stays identical whatever the machine.

```mermaid
sequenceDiagram
  participant H as SdlHost
  participant S as ScreenManager
  participant P as PlayScreen
  participant G as Game
  participant W as WaveScheduler
  participant F as FruitManager
  participant D as SoundDirector
  participant GH as GhostHouse
  participant PM as Pacman
  participant GS as Ghost x4
  participant R as GameRenderer
  participant X as SDL3
  H->>H: Run(), PumpEvents(), accumulated time
  loop while accumulated time ≥ 16.67 ms
    H->>S: Update()
    S->>P: Update()
    P->>G: Update()
    G->>W: Update(level, frightened)
    G->>F: Update(level, dots, Pacman)
    G->>D: UpdateBackground(...)
    G->>GH: Update(level)
    G->>PM: Move(map, specs, frightened)
    G->>GS: Move(map, Pacman, specs, mode)
    G->>G: CheckPelletCollision()
    G->>G: CheckGhostCollisions()
  end
  H->>R: Render(current screen)
  R->>P: Draw(renderer, canvas)
  P->>R: DrawGame(canvas, game)
  R-->>H: 224×288 Canvas
  H->>X: UpdateTexture(), RenderPresent()
```

*Figure 17: sequence diagram: one game frame at 60 Hz*  
Open full size: [SVG](diagrams/en/17_sequence_image.svg) · [PNG](diagrams/en/17_sequence_image.png)

### 3.2 From a credit to the start of a game

A credit leads from the attract mode to the start screen; key 1 starts a game and consumes a credit. At the end of GAME OVER, the ScreenManager returns to the start screen if credits remain, otherwise to the attract mode.

```mermaid
sequenceDiagram
  actor J as Player
  participant M as SdlHost
  participant S as ScreenManager
  participant A as AttractScreen
  participant ST as StartScreen
  participant P as PlayScreen
  participant G as Game
  J->>M: key 5
  M->>M: OnKeyDown(), TryMapKey()
  M->>S: OnInput(Coin)
  S->>S: InsertCoin(), credit + 1
  S->>ST: ShowStart() via SwitchTo
  Note over S,ST: SwitchTo stops the sounds and calls Enter()
  J->>M: key 1
  M->>S: OnInput(Start1)
  S->>ST: OnInput(Start1)
  ST->>S: StartGame(1)
  S->>S: credits - number of players, SwitchTo(PlayScreen)
  S->>P: Enter()
  P->>G: new Game(sound, high score, player count)
  loop each frame
    S->>P: Update()
    P->>G: Update()
  end
  G-->>P: IsFinished
  P->>S: OnGameEnded(high score)
  S->>ST: ShowStart() if credits left
  S->>A: ShowAttract() otherwise
```

*Figure 18: sequence diagram: from a credit to the start of a game*  
Open full size: [SVG](diagrams/en/18_sequence_credit.svg) · [PNG](diagrams/en/18_sequence_credit.png)

## 4. State diagrams

Each diagram corresponds to an enumeration or a timeline in the code. Durations are in frames at 60 Hz (60 frames = 1 second).

### 4.1 Screen cycle

The ScreenManager chains attract mode, demo, start screen and game. A credit is accepted at any time: it leaves the attract mode or the demo for the start screen, and is simply added to the counter elsewhere. Demo recording (F9) only exists in Debug builds.

```mermaid
stateDiagram-v2
  [*] --> Attract : game start
  state "AttractScreen" as Attract {
    [*] --> Presentation
    Presentation --> Chase : image 694
    Presentation : character presentation
    Chase : ChaseScene
  }
  state "DemoScreen (replayed demo)" as Demo
  state "StartScreen" as Start
  state "PlayScreen" as Play
  Attract --> Demo : chase finished
  Demo --> Attract : demo finished
  Attract --> Start : coin inserted
  Demo --> Start : coin inserted
  Start --> Start : coin inserted, maximum 99
  Start --> Play : key 1 or 2
  Play --> Start : GAME OVER, credits left
  Play --> Attract : GAME OVER, no credit left
  note right of Start
    Key 1 costs 1 credit, key 2 costs 2.
    F9 (Debug only) starts recording
    the demo from the attract mode or the start screen.
  end note
```

*Figure 8: state diagram: screen cycle*  
Open full size: [SVG](diagrams/en/08_etats_ecrans.svg) · [PNG](diagrams/en/08_etats_ecrans.png)

### 4.2 Game states

Game goes through seven states (GameState). Dying and Victory are split into timed phases. In demo mode, a death ends after the animation and one second of pause, and a victory ends the demo immediately.

```mermaid
stateDiagram-v2
  [*] --> Ready : game start
  Ready --> Playing : countdown finished
  Playing --> GhostEaten : frightened ghost eaten
  GhostEaten --> Playing : 60-frame freeze
  Playing --> Dying : contact with an active ghost
  Playing --> Victory : last dot eaten
  state Dying {
    [*] --> DeathFreeze
    DeathFreeze --> DeathAnimation : 60 frames
    DeathAnimation --> DeathPause : 12 sprites of 12 frames
    DeathPause --> [*] : 30 frames
    DeathFreeze : everything frozen
    DeathAnimation : Pac-Man alone on screen
    DeathPause : empty screen
  }
  Dying --> Ready : life left (same or other player)
  Dying --> GameOver : no life left
  GameOver --> Ready : the other player still has lives
  GameOver --> [*] : end of game (180 frames)
  state Victory {
    [*] --> VictoryFreeze
    VictoryFreeze --> Flashing : 60 frames
    Flashing --> [*] : 4 flashes of 28 frames
    VictoryFreeze : ghosts visible
    Flashing : white and blue maze
  }
  Victory --> Intermission : after levels 2, 5, 9, 13 and 17
  Victory --> Ready : other levels (next level)
  Intermission --> Ready : intermission finished (next level)
  note right of Ready
    READY! lasts 240 frames at the start and after
    a player change, 120 frames after
    a death or a level
  end note
```

*Figure 9: state diagram: game states*  
Open full size: [SVG](diagrams/en/09_etats_partie.svg) · [PNG](diagrams/en/09_etats_partie.png)

### 4.3 Ghost life cycle

Four phases (GhostPhase). When the ghost is active, two dimensions evolve independently: the wave mode (Scatter or Chase) and the frightened state.

```mermaid
stateDiagram-v2
  [*] --> InHouse : Pinky, Inky and Clyde (start of level or life)
  [*] --> Active : Blinky
  InHouse --> Exiting : dot counter reached, or 4 s of inactivity (3 s from level 5)
  Exiting --> Active : door crossed
  state Active {
    [*] --> Scatter
    Scatter --> Chase : next wave (reversal)
    Chase --> Scatter : next wave (reversal)
    --
    [*] --> Normal
    Normal --> Frightened : energizer (reversal)
    Frightened --> Normal : time elapsed
  }
  Active --> Eaten : eaten while frightened
  Eaten --> Exiting : the eyes reach the house door
  note right of InHouse
    IsFrightened can also be true
    inside the house or while exiting
  end note
```

*Figure 10: state diagram: ghost life cycle*  
Open full size: [SVG](diagrams/en/10_etats_fantome.svg) · [PNG](diagrams/en/10_etats_fantome.png)

### 4.4 Scatter / Chase waves

Eight waves alternate; the last one (Chase) lasts until the end of the level. Durations depend on the level (`GameConfig.GetWaveDurationFrames`).

```mermaid
stateDiagram-v2
  [*] --> S0 : start of level or life
  state "Scatter (wave 0)" as S0
  state "Chase (wave 1)" as C1
  state "Scatter (wave 2)" as S2
  state "Chase (wave 3)" as C3
  state "Scatter (wave 4)" as S4
  state "Chase (wave 5)" as C5
  state "Scatter (wave 6)" as S6
  state "Chase (wave 7, infinite)" as C7
  S0 --> C1 : level duration
  C1 --> S2 : level duration
  S2 --> C3 : level duration
  C3 --> S4 : level duration
  S4 --> C5 : level duration
  C5 --> S6 : level duration
  S6 --> C7 : level duration
  note right of S0
    Each mode change makes ghosts
    reverse direction.
    The timer is frozen in frightened mode.
  end note
```

*Figure 11: state diagram: Scatter and Chase waves*  
Open full size: [SVG](diagrams/en/11_etats_vagues.svg) · [PNG](diagrams/en/11_etats_vagues.png)

### 4.5 Background sound

Only one background sound plays at a time, by priority: eyes, then panic, then siren. The siren speeds up in steps when few dots remain.

```mermaid
stateDiagram-v2
  [*] --> None
  None --> Siren : first game frame
  state Siren {
    [*] --> Siren0
    Siren0 --> Siren1 : 164 dots left or fewer
    Siren1 --> Siren2 : 144 or fewer
    Siren2 --> Siren3 : 124 or fewer
    Siren3 --> Siren4 : 104 or fewer
  }
  Siren --> Frightened : frightened ghost
  Siren --> Eyes : ghost as eyes
  Frightened --> Eyes : ghost as eyes
  Eyes --> Frightened : no more eyes, ghost still frightened
  Eyes --> Siren : plus d'yeux, plus de frightened ghost
  Frightened --> Siren : plus de frightened ghost
  Siren --> None : death, victory or new level
  Frightened --> None : death, victory or new level
  Eyes --> None : death, victory or new level
  note right of Siren
    Thresholds are shifted by 0, 10, 20, 30 or 40 dots
    depending on the level (1, 2, 3 to 5, 6 to 20, 21 and up)
  end note
```

*Figure 12: state diagram: background sound*  
Open full size: [SVG](diagrams/en/12_etats_son.svg) · [PNG](diagrams/en/12_etats_son.png)

### 4.6 Two-player mode

Players alternate after each lost life as long as the other player still has lives. A player with no lives left hands over to the other after their GAME OVER.

```mermaid
stateDiagram-v2
  [*] --> J1 : PLAYER ONE
  J1 --> J2 : life lost, J2 still has lives
  J2 --> J1 : life lost, J1 still has lives
  J1 --> J1 : life lost, the other player has no life left
  J2 --> J2 : life lost, the other player has no life left
  J1 --> GameOverJ1 : last life lost
  J2 --> GameOverJ2 : last life lost
  GameOverJ1 --> J2 : J2 still has lives
  GameOverJ2 --> J1 : J1 still has lives
  GameOverJ1 --> End : J2 has no life left
  GameOverJ2 --> End : J1 has no life left
  End --> [*] : back to the start screen or attract mode
  state "Player 1 active" as J1
  state "Player 2 active" as J2
  state "GAME OVER (player 1)" as GameOverJ1
  state "GAME OVER (player 2)" as GameOverJ2
  state "Game ended" as End
  note right of J2
    Each player keeps their own maze, score,
    lives, level, fruit and ghost house counters.
    The high score is shared.
  end note
```

*Figure 13: state diagram: two-player mode*  
Open full size: [SVG](diagrams/en/13_etats_deux_joueurs.svg) · [PNG](diagrams/en/13_etats_deux_joueurs.png)

### 4.7 Intermission 1 (after level 2)

Pac-Man crosses the screen to the left, gradually caught up by Blinky; then a blue Blinky goes back to the right, chased by a giant Pac-Man. The 5-second music, played twice, starts at frame 20.

```mermaid
stateDiagram-v2
  [*] --> PacmanAlone : frame 15, Pac-Man enters from the right
  PacmanAlone --> Chase : frame 34, Blinky enters, faster
  Chase --> FrightenedBlinky : frame 258, blue Blinky enters from the left
  FrightenedBlinky --> GiantPacman : frame 405, giant Pac-Man enters
  GiantPacman --> [*] : frame 648, end
  state "Pac-Man alone (heading left)" as PacmanAlone
  state "Blinky chases Pac-Man (heading left)" as Chase
  state "Blue Blinky (heading right)" as FrightenedBlinky
  state "Giant Pac-Man chases Blinky (heading right)" as GiantPacman
  note right of PacmanAlone
    The music starts at frame 20
    (5 s, played twice)
  end note
```

*Figure 14: state diagram: intermission 1*  
Open full size: [SVG](diagrams/en/14_etats_intermission1.svg) · [PNG](diagrams/en/14_etats_intermission1.png)

### 4.8 Intermission 2 (after level 5)

Blinky's cloth catches on a nail and stretches through three successive sprites before tearing. The music is cut off sharply at the end of the scene, after the black screen.

```mermaid
stateDiagram-v2
  [*] --> PacmanAlone : frame 16, Pac-Man enters from the right
  PacmanAlone --> Chase : frame 104, Blinky enters
  Chase --> Snagged : frame 190, the cloth catches on the nail
  Snagged --> Torn : frame 326, the cloth tears
  Torn --> LooksDown : frame 385, Blinky looks down
  LooksDown --> Black : frame 503, black screen
  Black --> [*] : frame 511, end
  state "Pac-Man alone" as PacmanAlone
  state "Blinky chases Pac-Man" as Chase
  state "Stretched cloth (3 sprites by step)" as Snagged
  state "Torn Blinky, eyes up" as Torn
  state "Torn Blinky, eyes down" as LooksDown
  state "Everything black" as Black
```

*Figure 15: state diagram: intermission 2*  
Open full size: [SVG](diagrams/en/15_etats_intermission2.svg) · [PNG](diagrams/en/15_etats_intermission2.png)

### 4.9 Intermission 3 (after levels 9, 13 and 17)

Pac-Man, chased by Blinky with his cloth sewn back, exits to the left; the screen stays empty for a moment, then Blinky, undressed, crawls to the right dragging his cloth.

```mermaid
stateDiagram-v2
  [*] --> PacmanAlone : frame 15, Pac-Man enters from the right
  PacmanAlone --> Chase : frame 56, Blinky enters, cloth sewn back
  Chase --> EmptyScreen : frame 235, both gone off to the left
  EmptyScreen --> Crawl : frame 307, Blinky enters from the left
  Crawl --> [*] : frame 533, end
  state "Pac-Man alone" as PacmanAlone
  state "Blinky chases Pac-Man (heading left)" as Chase
  state "Empty screen" as EmptyScreen
  state "Blinky crawls dragging his cloth (heading right)" as Crawl
```

*Figure 16: state diagram: intermission 3*  
Open full size: [SVG](diagrams/en/16_etats_intermission3.svg) · [PNG](diagrams/en/16_etats_intermission3.png)

## 5. Analysis of ghost behavior

The four ghosts follow the same movement rule; only the tile they aim for changes, depending on their state and personality.

### 5.1 How a ghost chooses its direction

At each tile center, a ghost picks the exit whose next tile is closest to its target, using squared Euclidean distance. There is no pathfinding and no memory: the decision is local, which produces the predictable trajectories of the arcade.

- Candidate directions are tested in the order Up, Left, Down, Right; in case of a tie, the first one wins.
- Reversing is forbidden, except for a reversal imposed by a mode change.
- Walls and the ghost house door are excluded (only eyes may cross the door).
- At the four intersections (12,14), (15,14), (12,26) and (15,26), an active ghost cannot turn upward.

### 5.2 Target by state

| State | Target | Note |
| --- | --- | --- |
| Scatter | The ghost's own corner | They loop around a block |
| Chase | Tile computed by each ghost (5.4) | The only difference in personality |
| Frightened | None: pseudo-random choice | Order Up, Right, Down, Left, fixed seed |
| Eaten (eyes) | Tile (13,14), in front of the house | Speed 2 px per frame |

### 5.3 Alternation of Scatter and Chase

A scheduler of 8 waves alternates Scatter (even waves) and Chase (odd waves), then stays in Chase forever at the 8th. The higher the level, the shorter the Scatter phase, until it becomes a mere direction reversal.

| Wave | Level 1 | Levels 2 to 4 | Levels 5 and up |
| --- | --- | --- | --- |
| 1 Scatter | 7 s | 7 s | 5 s |
| 2 Chase | 20 s | 20 s | 20 s |
| 3 Scatter | 7 s | 7 s | 5 s |
| 4 Chase | 20 s | 20 s | 20 s |
| 5 Scatter | 5 s | 5 s | 5 s |
| 6 Chase | 20 s | 1033 s | 1037 s |
| 7 Scatter | 5 s | 1 frame | 1 frame |
| 8 Chase | infinite | infinite | infinite |

Three rules complete this table:

- The timer is frozen while ghosts are frightened.
- At every wave change, all ghosts receive a request to reverse direction: this is the only visible signal of a mode change.
- A ghost leaving the house takes the current mode; those still inside keep the reversal request and therefore exit to the right instead of the left.

An energizer also requests the reversal, even when the level's frightened duration is zero (levels 17, 19, 20 and beyond).

### 5.4 What sets the four ghosts apart

Blinky aims at Pac-Man; the others aim at a computed point meant to cut off or avoid his path. This variety is what makes the pincer chase work.

| Ghost | Chase target | Scatter corner | Role |
| --- | --- | --- | --- |
| Blinky (red) | Pac-Man's tile | Top right, (25,0) | Direct pursuer, speeds up (Elroy) |
| Pinky (pink) | 4 tiles ahead of Pac-Man | Top left, (2,0) | Ambush |
| Inky (blue) | Mirror of Blinky through a point 2 tiles ahead of Pac-Man | Bottom right, (27,34) | Unpredictable, depends on Blinky |
| Clyde (orange) | Pac-Man if 8 or more tiles away, otherwise his own corner | Bottom left, (0,34) | Chases from afar, flees up close |

**Blinky** computes nothing: he aims at the tile Pac-Man occupies and follows him closely. He gets faster when few dots remain (Cruise Elroy, below) and does not go back to his corner while in Elroy.

**Pinky** aims 4 tiles ahead of Pac-Man in his direction. When Pac-Man goes up, the target is also shifted 4 tiles to the left: this is the arcade's overflow bug, deliberately reproduced. Turning around in front of Pinky often makes him head the other way.

**Inky** is the most complex: take the point 2 tiles ahead of Pac-Man (with the same 2-tile shift to the left when he goes up), draw the vector from Blinky to that point, then double it. Inky therefore ends up on the other side of Pac-Man from Blinky; alone he is harmless, but combined with Blinky he closes off the exits.

**Clyde** alternates: if he is 8 or more tiles from Pac-Man (squared distance of 64 or more), he targets him like Blinky; as soon as he gets close, he targets his bottom-left corner and moves away. He ends up circling that corner without ever cornering the player.

### 5.5 Cruise Elroy

When few dots remain, Blinky speeds up in two steps and chases Pac-Man even during Scatter phases. The threshold depends on the level; the second step is at half of the first.

| Levels | Dots remaining (step 1 / step 2) | Speed step 1 / step 2 |
| --- | --- | --- |
| 1 | 20 / 10 | 0.80 / 0.85 |
| 2 | 30 / 15 | 0.90 / 0.95 |
| 3 to 4 | 40 / 20 | 0.90 / 0.95 |
| 5 | 40 / 20 | 1.00 / 1.05 |
| 6 to 8 | 50 / 25 | 1.00 / 1.05 |
| 9 to 11 | 60 / 30 | 1.00 / 1.05 |
| 12 to 14 | 80 / 40 | 1.00 / 1.05 |
| 15 to 18 | 100 / 50 | 1.00 / 1.05 |
| 19 and up | 120 / 60 | 1.00 / 1.05 |

After Pac-Man dies, Elroy is suspended until Clyde leaves the house.

### 5.6 Leaving the ghost house

Ghosts do not leave together: each waits until Pac-Man has eaten enough dots. If Pac-Man stops eating, a timer forces the first waiting ghost out.

| Ghost | Level 1 | Level 2 | Level 3 and up |
| --- | --- | --- | --- |
| Blinky | Outside from the start | Outside | Outside |
| Pinky | 0 dots | 0 | 0 |
| Inky | 30 dots | 0 | 0 |
| Clyde | 60 dots | 50 | 0 |

After a death, a global counter replaces these thresholds (Pinky at 7, Inky at 17, Clyde at 32 dots). The forced-exit timer is 4 s on levels 1 to 4 and 3 s afterwards.

### 5.7 Key takeaways

- The danger comes from the combination: Blinky pushes, Pinky cuts off, Inky closes the other side and Clyde eases the pressure up close.
- Scatter is not a break: it disperses the ghosts to break the pincer, and barely lasts at high levels.
- Direction reversals, not colors or sounds, announce a mode change.
- The "faulty" behaviors (Pinky's and Inky's upward shift, the no-Up intersections) are those of the original arcade and are kept.
