# Pac-Man and Ms. Pac-Man (C#, .NET 10, SDL3): UML and State Diagrams

This document describes the final architecture of the game through 31 diagrams: one package diagram, 7 class diagrams, 3 sequence diagrams and 20 state diagrams, all derived from the source code. The program offers two games, Pac-Man and Ms. Pac-Man, which share the same engine: everything that differs between the games is gathered in a `GameVariant` class. Chapters 1 to 4 describe what is common to both games; chapter 5 is devoted to the original Pac-Man and chapter 6 to the Ms. Pac-Man variant. The game is split into two projects: the `PACMAN.Core` library, independent of any platform (rules, software rendering, audio mixing), and the `PACMAN.Sdl` host, which opens the window, reads keyboard and gamepad and plays sound with SDL3 on Windows and Linux. The diagrams are written in [Mermaid](https://mermaid.js.org) and render directly in Markdown viewers that support it (GitHub, GitLab, VS Code with an extension, Obsidian, and so on); each one is also provided as SVG and PNG. In the PDF version, the large diagrams are placed on landscape pages.

## 1. Overview

The `PACMAN.Sdl` project (Program, SdlHost, SdlAudioOutput) is the only part that knows SDL3. It drives the `PACMAN.Core` library, whose seven folders form three levels: the platform (Platform) and the screens (Screens) drive the game (Game, Ghosts, Maze), which relies on rendering (Graphics) and audio (Audio). Game and Ghosts are tightly coupled: Game creates the four ghosts, and a ghost reads Pacman and LevelSpecs, defined in Game. Rendering and audio depend on no graphics or sound API: GameRenderer draws into a Canvas (a pixel array) and MixerAudioEngine produces samples that the host sends to the sound card. The two games (Pac-Man and Ms. Pac-Man) differ only through their `GameVariant`, which supplies the mazes, the ghosts, the fruit, the sprites, the sounds, the screens and the intermissions; the engine has no direct knowledge of any particular game.

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
      ScreenList["GameSelectScreen, AttractScreen, StartScreen, PlayScreen, DemoScreen, MsAttractScreen, MsStartScreen"]
      Scenes["ChaseScene, Intermission1/2/3Scene, MsIntermissionScene"]
      ArcadeTexts["ArcadeTexts"]
    end
    subgraph Game["Game"]
      GameC["Game, PlayerState, ScoreKeeper"]
      Rules["FruitManager, MsFruitManager, WaveScheduler, GameConfig, LevelSpecs"]
      PacmanC["Pacman"]
      Variantes["GameVariant, PacManVariant, MsPacManVariant, GameVariants"]
    end
    subgraph Ghosts["Ghosts"]
      GhostC["Ghost, Blinky, Pinky, Inky, Clyde"]
      GhostHouse["GhostHouse"]
    end
    subgraph Maze["Maze"]
      MazeC["LevelMap, MazeLayout, MazeDefinition, Tiles, Direction"]
    end
    subgraph Graphics["Graphics"]
      GameRenderer["GameRenderer, HudLayout"]
      SpriteManager["SpriteManager, MsPacManSprites"]
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
  GameC --> Variantes
  ScreenList --> Variantes
  Variantes -.->|creates the ghosts| GhostC
  Variantes -.->|creates the fruit| Regles
  Variantes -.->|creates the sprites| SpriteManager
  Variantes -.->|mazes| MazeC
  Variantes -.->|creates the screens| ScreenList
  ScreenManager -.->|loads the game sounds| AudioEngine
```

*Figure 1: package diagram: the PACMAN.Sdl host, the seven PACMAN.Core folders and their dependencies*  
Open full size: [SVG](diagrams/en/01_paquets.svg) · [PNG](diagrams/en/01_paquets.png)

| Folder | Role | Main classes |
| --- | --- | --- |
| PACMAN.Sdl (project) | SDL3 host: window, full screen, keyboard, gamepad, fixed-step loop, audio output | Program, SdlHost, SdlAudioOutput |
| Platform | `PACMAN.ini` settings and `--game` option, abstract keys, access to embedded resources | Settings, DisplayConfig, InputKey, Assets |
| Screens | Game selection, screen flow, credits, intermissions | ScreenManager, IScreen, GameSelectScreen, AttractScreen, StartScreen, PlayScreen, DemoScreen, Intermission1/2/3Scene, MsAttractScreen, MsStartScreen, MsIntermissionScene |
| Game | Rules and state of a game, differences between the games | Game, PlayerState, ScoreKeeper, FruitManager, MsFruitManager, WaveScheduler, Pacman, GameConfig, GameVariant, PacManVariant, MsPacManVariant, GameVariants |
| Ghosts | Ghost and ghost house behavior | Ghost, Blinky, Pinky, Inky, Clyde, GhostHouse |
| Maze | Mazes, tiles, directions | LevelMap, MazeLayout, MazeDefinition, Tiles, Direction |
| Graphics | Software drawing at arcade resolution (224×288) | GameRenderer, SpriteManager, MsPacManSprites, HudLayout, Canvas, Sprite, PngDecoder |
| Audio | Sounds, software mixing and volume | SoundDirector, IAudioEngine, MixerAudioEngine, SoundVoice, WavDecoder |

## 2. Class diagrams

The members shown are the most significant ones, not the full list. Relationships follow UML: hollow arrow for inheritance or interface implementation, filled diamond for composition, hollow diamond for aggregation, dashed line for a dependency.

### 2.1 Screens and navigation

Program creates the SdlHost, which owns a ScreenManager. The latter holds the current screen behind the IScreen interface. At startup the ScreenManager shows the game selection menu (GameSelectScreen), unless a game is forced by the `--game` option or by `Game=` in `PACMAN.ini`. The chosen game (GameVariant) then creates its own attract and start screens, behind the IAttractScreen and IStartScreen interfaces. Credits, the high score and sound cross screens: they live in the ScreenManager. SdlHost translates keyboard and gamepad input into InputKey, which keeps the screens independent of SDL.

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
    +string Game
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
    Menu
  }
  class IScreen {
    <<interface>>
    +Enter()
    +Update()
    +OnInput(InputKey)
    +Draw(GameRenderer, Canvas)
  }
  class IAttractScreen {
    <<interface>>
  }
  class IStartScreen {
    <<interface>>
  }
  class ScreenManager {
    +int MaxCredits = 99
    +int Credits
    +int HighScore
    +SoundDirector Sound
    +IScreen Current
    +GameVariant Variant
    +event VariantChanged
    +Update()
    +OnInput(InputKey)
    +ShowMenu()
    +SelectVariant(GameVariant)
    +ShowAttract()
    +ShowStart()
    +ShowDemo()
    +StartGame(int players)
    +OnGameEnded(int finalHighScore)
    -InsertCoin()
    -SwitchTo(IScreen)
  }
  class GameSelectScreen {
    -int selected
    -int tick
  }
  class GameVariants {
    <<static>>
    +IReadOnlyList~GameVariant~ All
    +FindById(string)$ GameVariant
  }
  class GameVariant {
    <<abstract>>
    +string Id
    +CreateAttractScreen(ScreenManager)* IAttractScreen
    +CreateStartScreen(ScreenManager)* IStartScreen
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
  class MsAttractScreen
  class MsStartScreen
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
  IScreen <|.. GameSelectScreen
  IScreen <|-- IAttractScreen
  IScreen <|-- IStartScreen
  IAttractScreen <|.. AttractScreen
  IAttractScreen <|.. MsAttractScreen
  IStartScreen <|.. StartScreen
  IStartScreen <|.. MsStartScreen
  IScreen <|.. PlayScreen
  IScreen <|.. DemoScreen
  ScreenManager o-- IScreen : current screen
  ScreenManager ..> InputKey
  AttractScreen --> ScreenManager
  StartScreen --> ScreenManager
  MsAttractScreen --> ScreenManager
  MsStartScreen --> ScreenManager
  GameSelectScreen --> ScreenManager
  PlayScreen --> ScreenManager
  DemoScreen --> ScreenManager
  AttractScreen *-- ChaseScene
  DemoScreen *-- DemoRecording
  DemoRecording *-- Entry
  AttractScreen ..> ArcadeTexts
  StartScreen ..> ArcadeTexts
  Program ..> Settings : --game option
  GameSelectScreen ..> GameVariants : lists the games
  ScreenManager o-- GameVariant : chosen game
  GameVariant ..> IAttractScreen : creates
  GameVariant ..> IStartScreen : creates
  MsAttractScreen ..> ArcadeTexts
```

*Figure 2: class diagram: screens and navigation*  
Open full size: [SVG](diagrams/en/02_classes_ecrans.svg) · [PNG](diagrams/en/02_classes_ecrans.png)

### 2.2 Intermissions

PlayScreen displays a single IIntermission at a time and pauses the Game meanwhile. The Game only knows the number of the act to play (1, 2 or 3); the GameVariant of the chosen game creates the matching scene. Pac-Man has three scene classes, one per act, drawn in code. Ms. Pac-Man has a single class, MsIntermissionScene, which replays the three acts from tracks of sprite positions measured frame by frame on the arcade (MsIntermissionData).

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
  class MsIntermissionScene {
    -int number
    -int level
    -int frame
    +bool Finished
    -Parse(string[])
  }
  class MsIntermissionData {
    <<internal static>>
    +int[][] Palette
    +string[][] Tracks
  }
  class GameVariant {
    <<abstract>>
    +CreateIntermission(int, int, SoundDirector)* IIntermission
  }
  class PacManVariant
  class MsPacManVariant
  class SoundDirector {
    +PlayIntermission()
    +PlayMsIntermission(int number)
    +PlayIntermissionBump()
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
  IIntermission <|.. MsIntermissionScene
  PlayScreen o-- IIntermission : one at a time
  PlayScreen --> Game : FinishIntermission()
  Intermission1Scene --> SoundDirector
  Intermission2Scene --> SoundDirector
  Intermission3Scene --> SoundDirector
  Intermission1Scene ..> GameRenderer
  Intermission2Scene ..> GameRenderer
  Intermission3Scene ..> GameRenderer
  GameRenderer --> SpriteManager
  PlayScreen --> GameVariant : CreateIntermission()
  GameVariant <|-- PacManVariant
  GameVariant <|-- MsPacManVariant
  PacManVariant ..> Intermission1Scene : creates (act 1)
  PacManVariant ..> Intermission2Scene : creates (act 2)
  PacManVariant ..> Intermission3Scene : creates (act 3)
  MsPacManVariant ..> MsIntermissionScene : creates (acts 1 to 3)
  MsIntermissionScene ..> MsIntermissionData : reads the tracks
  MsIntermissionScene --> SoundDirector
  MsIntermissionScene ..> GameRenderer
```

*Figure 3: class diagram: intermissions*  
Open full size: [SVG](diagrams/en/03_classes_intermissions.svg) · [PNG](diagrams/en/03_classes_intermissions.png)

### 2.3 Game engine

Game is the center of the engine. It chains the GameState states, delegates the rules to ScoreKeeper, FruitManager, WaveScheduler and GhostHouse, and keeps one PlayerState per player, each with its own maze. The rules that differ between the games (the level's maze, ghosts, fruit, wave durations, death animation) are requested from the GameVariant; FruitManager therefore has two versions, Pac-Man's fixed fruit and Ms. Pac-Man's moving fruit (MsFruitManager).

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
    #FirstFruitDots int
    #SecondFruitDots int
    #Spawn(level, LevelMap)
    #MoveFruit(LevelMap)
    +SaveProgress(PlayerState)
    +LoadProgress(PlayerState)
  }
  class WaveScheduler {
    +GhostMode CurrentMode
    +Reset()
    +Update(level, isFrozen, GameVariant) bool
  }
  class MsFruitManager {
    +FruitIndexForLevel(int)$ int
    #FirstFruitDots = 64
    #SecondFruitDots = 176
  }
  class GameVariant {
    <<abstract>>
    +GetMaze(int level)* MazeDefinition
    +CreateGhosts()* Ghost[]
    +CreateFruit(int?)* FruitManager
    +GetWaveDurationFrames(level, waveIndex) int
    +DeathAnimationFrames int
    +DeathFrameAt(int) int
  }
  class MazeDefinition {
    +string Name
    +int[] TunnelRows
    +Point[] NoUpTiles
    +IsTunnelRow(int) bool
    +IsInTunnelSlowZone(int, int) bool
    +IsNoUpTile(int, int) bool
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
  FruitManager <|-- MsFruitManager
  Game --> GameVariant : rules of the chosen game
  WaveScheduler ..> GameVariant : wave durations
  GameVariant ..> FruitManager : creates
  GameVariant ..> Ghost : creates
  GameVariant --> MazeDefinition : level maze
  LevelMap --> MazeDefinition : Definition
```

*Figure 4: class diagram: game engine*  
Open full size: [SVG](diagrams/en/04_classes_moteur.svg) · [PNG](diagrams/en/04_classes_moteur.png)

### 2.4 Ghosts

Ghost holds everything common: pixel-by-pixel movement, direction choice, speed, reversal. Each ghost only defines its chase target. Inky keeps a reference to Blinky to compute its own, and Blinky adds the Cruise Elroy state. Two options, enabled by the Ms. Pac-Man GameVariant, change the common behavior: `WandersInScatter` makes the ghost wander randomly during Scatter, and `ExitsTowardTarget` makes it choose its direction when leaving the house according to its target.

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
    +bool WandersInScatter
    +bool ExitsTowardTarget
    +int TileX
    +int TileY
    +Move(LevelMap, Pacman, LevelSpecs, GhostMode)
    +RequestReverse()
    +ResetRandom()$
    #GetChaseTargetTile(Pacman)* Point
    #GetCurrentTargetTile(Pacman) Point
    #GetNormalSpeed(LevelSpecs) float
    #ShouldWander() bool
    -MoveOnePixel(...)
    -ChooseNextDirection(...) Direction
    -ChooseFrightenedDirection(LevelMap) Direction
    -ChooseExitDirection(Pacman) Direction
  }
  class Blinky {
    +bool IsCruiseElroy1
    +bool IsCruiseElroy2
    +UpdateElroyStatus(dotsRemaining, LevelSpecs, enabled)
    #GetChaseTargetTile(Pacman) Point
    #GetNormalSpeed(LevelSpecs) float
    #ShouldWander() bool
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

GameRenderer draws everything into a native 224×288 Canvas (0xAARRGGBB pixels), then the host hands it to SDL, which scales it up by the nearest integer factor. A Sprite is just a rectangular view on a Canvas, with no pixel copy. PngDecoder decodes the `asset.png` sheet of the chosen game and SpriteManager slices it into cached sprites. Each game has its own sheet: Ms. Pac-Man uses MsPacManSprites, which inherits from SpriteManager and overrides the slicing of the characters, the mazes, the fruits and the death animation. The GameRenderer switches sprite sets when the ScreenManager announces a new game (SetVariant) and can also provide another game's set (SpritesFor), which the selection menu uses to draw its cursor.

```mermaid
classDiagram
  direction TB
  class GameRenderer {
    +SpriteManager Sprites
    +SetVariant(GameVariant)
    +SpritesFor(GameVariant) SpriteManager
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
  class MsPacManSprites {
    +GetPacmanSprite(int, Direction)
    +GetPacmanDeathSprite(int)
    +GetMazeLayer(LevelMap, MazeColor)
    +GetFruitSprite(int level)
    +GetLifeSprite()
  }
  class GameVariant {
    <<abstract>>
    +CreateSprites()* SpriteManager
    +string SheetResource
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
  class asset_png["asset.png (one sheet per game)"]

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
  SpriteManager <|-- MsPacManSprites
  GameVariant ..> SpriteManager : CreateSprites()
  GameRenderer ..> GameVariant : selects the sprite sheet
```

*Figure 6: class diagram: rendering*  
Open full size: [SVG](diagrams/en/06_classes_rendu.svg) · [PNG](diagrams/en/06_classes_rendu.png)

### 2.6 Audio

SoundDirector turns game events into sounds and guarantees that only one background sound plays at a time, through the IAudioEngine interface. MixerAudioEngine decodes the sounds of the chosen game (WavDecoder) when the ScreenManager calls LoadSoundPack, mixes them itself as 48 kHz stereo floats and applies the volume: 24 sounds for Pac-Man, plus the sounds specific to Ms. Pac-Man (fruit bounce, intermissions 2 and 3, the bump of intermission 1). It knows no audio output: SdlAudioOutput, on the host side, calls Read() from the SDL callback to fill the sound card buffer.

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
    +PlayFruitBounce()
    +PlayCredit()
    +PlayIntermission()
    +PlayMsIntermission(int number)
    +PlayIntermissionBump()
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
    +LoadSoundPack(string folder)
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
    +LoadSoundPack(string folder)
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
    Intermission2, Intermission3
    IntermissionBump
    FruitBounce
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
  MixerAudioEngine *-- "24..28" SoundData : decoded when the pack is loaded
  MixerAudioEngine o-- "0..*" SoundVoice : running voices
  MixerAudioEngine ..> WavDecoder : decodes
  MixerAudioEngine ..> Assets : opens the WAVs
  SoundVoice --> SoundData
  SoundData --> SoundId
  SdlAudioOutput --> MixerAudioEngine : Read()
```

*Figure 7: class diagram: audio*  
Open full size: [SVG](diagrams/en/07_classes_audio.svg) · [PNG](diagrams/en/07_classes_audio.png)

### 2.7 The games (variants)

GameVariant gathers everything that differs between the games; the engine only knows this abstract class. GameVariants lists the games offered by the menu (Pac-Man, then Ms. Pac-Man). PacManVariant describes the original game: a single maze, four classic ghosts, a fixed fruit. MsPacManVariant describes Ms. Pac-Man: six mazes chosen according to the level (MazeIndexForLevel), a single 7-second Scatter phase, ghosts that wander or choose their exit according to their target, the moving fruit MsFruitManager, its own death animation, and its own screens, sprites, sounds and intermissions. The common parameters (speeds, Elroy thresholds, frightened mode) remain those of GameConfig.

```mermaid
classDiagram
  direction TB
  class GameVariants {
    <<static>>
    +All
    +FindById(string)$ GameVariant
  }
  class GameVariant {
    <<abstract>>
    +string Id*
    +string DisplayName*
    +string AssetFolder*
    +string SheetResource
    +string SoundFolder
    +string DemoResource
    +GetMaze(int level)* MazeDefinition
    +int PacmanStartX*
    +int PacmanStartY*
    +CreateGhosts()* Ghost[]
    +GetSpecs(int level)* LevelSpecs
    +GetWaveDurationFrames(level, waveIndex) int
    +IntermissionAfterLevel(int)* int
    +CreateIntermission(int, int, SoundDirector)* IIntermission
    +CreateFruit(int?)* FruitManager
    +CreateSprites()* SpriteManager
    +CreateAttractScreen(ScreenManager)* IAttractScreen
    +CreateStartScreen(ScreenManager)* IStartScreen
    +DeathAnimationFrames int
    +DeathEndPauseFrames int
    +DeathFrameAt(int) int
    +DeathSoundDelayFrames int
  }
  class PacManVariant {
    +Folder = "asset.pacman."
    -MazeDefinition Original
  }
  class MsPacManVariant {
    +Folder = "asset.mspacman."
    -MazeDefinition[6] mazes
    +MazeIndexForLevel(int)$ int
    -ScatterSeconds = 7
  }
  class MazeDefinition {
    +string Name
    +string ResourcePath
    +int TileSetIndex
    +int DotTileId
    +int[] TunnelRows
    +Point[] NoUpTiles
    +Analyze(TileType[,])
  }
  class FruitManager
  class MsFruitManager
  class Ghost {
    <<abstract>>
  }
  class SpriteManager
  class MsPacManSprites
  class IIntermission {
    <<interface>>
  }
  class ScreenManager
  class Game

  GameVariants o-- "2" GameVariant
  GameVariant <|-- PacManVariant
  GameVariant <|-- MsPacManVariant
  PacManVariant --> "1" MazeDefinition : single maze
  MsPacManVariant --> "6" MazeDefinition : one maze per layout
  PacManVariant ..> FruitManager : creates
  MsPacManVariant ..> MsFruitManager : creates
  FruitManager <|-- MsFruitManager
  PacManVariant ..> SpriteManager : creates
  MsPacManVariant ..> MsPacManSprites : creates
  SpriteManager <|-- MsPacManSprites
  GameVariant ..> Ghost : CreateGhosts()
  GameVariant ..> IIntermission : CreateIntermission()
  ScreenManager --> GameVariant : chosen game
  Game --> GameVariant : game rules
```

*Figure 8: class diagram: the games (variants)*  
Open full size: [SVG](diagrams/en/08_classes_variantes.svg) · [PNG](diagrams/en/08_classes_variantes.png)

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
    G->>W: Update(level, frightened, game)
    G->>F: Update(level, dots, Pacman, map)
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

*Figure 9: sequence diagram: one game frame at 60 Hz*  
Open full size: [SVG](diagrams/en/09_sequence_image.svg) · [PNG](diagrams/en/09_sequence_image.png)

### 3.2 From a credit to the start of a game

A credit leads from the attract mode to the start screen; key 1 starts a game and consumes a credit. At the end of GAME OVER, the ScreenManager returns to the start screen if credits remain, otherwise to the attract mode. The screens involved are those of the chosen game (see 3.3 for the game selection): the diagram shows Pac-Man's, and Ms. Pac-Man's play the same role.

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
  P->>G: new Game(variant, sound, high score, player count)
  loop each frame
    S->>P: Update()
    P->>G: Update()
  end
  G-->>P: IsFinished
  P->>S: OnGameEnded(high score)
  S->>ST: ShowStart() if credits left
  S->>A: ShowAttract() otherwise
```

*Figure 10: sequence diagram: from a credit to the start of a game*  
Open full size: [SVG](diagrams/en/10_sequence_credit.svg) · [PNG](diagrams/en/10_sequence_credit.png)

### 3.3 From the menu to the attract mode

At startup, the ScreenManager shows the selection menu. Up and Down move the cursor, 1 or 2 confirms. SelectVariant loads the game's sound pack (only if the game changes), notifies the host, which replaces the GameRenderer's sprite sheet, then starts the chosen game's attract mode. Esc from the attract mode, the demo or the start screen goes back to the menu; credits and high score then restart from zero.

```mermaid
sequenceDiagram
  actor J as Player
  participant H as SdlHost
  participant S as ScreenManager
  participant M as GameSelectScreen
  participant V as GameVariant
  participant A as IAudioEngine
  participant R as GameRenderer
  participant AT as Attract (of the game)
  H->>S: new ScreenManager(audio, GameVariants.FindById(Settings.Game))
  S->>M: ShowMenu() then Enter()
  J->>H: down arrow
  H->>S: OnInput(Down)
  S->>M: OnInput(Down)
  M->>M: selected + 1
  J->>H: key 1
  H->>S: OnInput(Start1)
  S->>M: OnInput(Start1)
  M->>S: SelectVariant(chosen variant)
  S->>A: LoadSoundPack(variant.SoundFolder) if the game changes
  S-->>H: VariantChanged(variant)
  H->>R: SetVariant(variant)
  R->>V: CreateSprites()
  S->>V: CreateAttractScreen(S)
  V-->>S: the game's attract screen
  S->>AT: SwitchTo(), Enter()
  J->>H: Esc
  H->>S: OnInput(Menu)
  S->>M: ShowMenu(): credits and high score reset to zero
```

*Figure 11: sequence diagram: from the menu to the attract mode*  
Open full size: [SVG](diagrams/en/11_sequence_choix_jeu.svg) · [PNG](diagrams/en/11_sequence_choix_jeu.png)

## 4. State diagrams common to both games

Each diagram matches an enumeration or a timeline in the code. Durations are in frames at 60 Hz (60 frames = 1 second). This chapter gathers the behaviors shared by Pac-Man and Ms. Pac-Man; the diagrams specific to each game are in chapters 5 and 6.

### 4.1 Screen cycle

The ScreenManager chains the game selection menu, attract mode, demo, start screen and game. At startup the menu is shown, unless a game is forced by `--game` or `Game=` (the program then goes straight to the attract mode). A credit is accepted at any time: it leaves the attract mode or the demo for the start screen, and is simply added to the counter elsewhere. Esc goes back to the menu from the attract mode, the demo or the start screen, never during a game. Demo recording (F9) only exists in Debug builds.

```mermaid
stateDiagram-v2
  [*] --> Menu : startup
  [*] --> Attract : forced game (--game or Game=)
  state "AttractScreen" as Attract {
    [*] --> Presentation
    Presentation --> Chase : image 694
    Presentation : character presentation
    Chase : ChaseScene
  }
  state "DemoScreen (replayed demo)" as Demo
  state "StartScreen" as Start
  state "PlayScreen" as Play
  state "GameSelectScreen (game selection)" as Menu
  Menu --> Attract : game chosen (1 or 2)
  Attract --> Menu : Esc
  Demo --> Menu : Esc
  Start --> Menu : Esc
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
  note left of Menu
    Going back to the menu resets the
    credits and the high score.
  end note
```

*Figure 12: state diagram: screen cycle*  
Open full size: [SVG](diagrams/en/12_etats_ecrans.svg) · [PNG](diagrams/en/12_etats_ecrans.png)

### 4.2 Game states

Game goes through seven states (GameState). Dying and Victory are split into timed phases. The duration of the death animation and of the pause that follows depends on the game (GameVariant). In demo mode, a death ends after the animation and one second of pause, and a victory ends the demo immediately.

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
    DeathAnimation --> DeathPause : game animation (144 or 159 frames)
    DeathPause --> [*] : game pause (30 or 7 frames)
    DeathFreeze : everything frozen
    DeathAnimation : Pac-Man alone on screen
    DeathPause : empty screen
  }
  note left of Dying
    Pac-Man: 12 sprites of 12 frames,
    30-frame pause.
    Ms. Pac-Man: 30 frames mouth down,
    10 quarter turns of 7.5 frames, mouth
    up, then gone (159 frames);
    7-frame pause; the sound starts with the spin.
  end note
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

*Figure 13: state diagram: game states*  
Open full size: [SVG](diagrams/en/13_etats_partie.svg) · [PNG](diagrams/en/13_etats_partie.png)

### 4.3 Ghost life cycle

Four phases (GhostPhase). When the ghost is active, two dimensions evolve independently: the wave mode (Scatter or Chase) and the frightened state. When leaving the house, Pac-Man always sends the ghost to the left; Ms. Pac-Man makes it choose left or right according to its target.

```mermaid
stateDiagram-v2
  [*] --> InHouse : Pinky, Inky and Clyde (start of level or life)
  [*] --> Active : Blinky
  InHouse --> Exiting : dot counter reached, or 4 s of inactivity (3 s from level 5)
  Exiting --> Active : door crossed (Ms. Pac-Man: left or right depending on the target)
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
  note right of Active
    Ms. Pac-Man: during its single Scatter,
    Blinky and Pinky wander randomly;
    Inky and Sue keep their corner.
  end note
```

*Figure 14: state diagram: ghost life cycle*  
Open full size: [SVG](diagrams/en/14_etats_fantome.svg) · [PNG](diagrams/en/14_etats_fantome.png)

### 4.4 Background sound

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
  Eyes --> Siren : no more eyes, no more frightened ghost
  Frightened --> Siren : no more frightened ghost
  Siren --> None : death, victory or new level
  Frightened --> None : death, victory or new level
  Eyes --> None : death, victory or new level
  note right of Siren
    Thresholds are shifted by 0, 10, 20, 30 or 40 dots
    depending on the level (1, 2, 3 to 5, 6 to 20, 21 and up)
  end note
```

*Figure 15: state diagram: background sound*  
Open full size: [SVG](diagrams/en/15_etats_son.svg) · [PNG](diagrams/en/15_etats_son.png)

### 4.5 Two-player mode

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

*Figure 16: state diagram: two-player mode*  
Open full size: [SVG](diagrams/en/16_etats_deux_joueurs.svg) · [PNG](diagrams/en/16_etats_deux_joueurs.png)

## 5. Pac-Man (the original game)

This chapter describes what is specific to the 1980 Pac-Man, as supplied by `PacManVariant`: a single maze, the attract mode with the ghost presentation, eight Scatter / Chase waves, a fixed fruit, three intermissions drawn in code, and the classic behavior of the four ghosts.

### 5.1 Pac-Man attract mode

AttractScreen follows the arcade timeline: the table title, then the four ghosts one after another (the sprite, then its name 60 frames later, then its nickname 90 frames after the sprite), the points legend, the logo, and finally the chase scene (ChaseScene). When the fourth ghost is eaten, the demo starts.

```mermaid
stateDiagram-v2
  [*] --> Title : frame 2
  state "Title of the character table" as Title
  state "Ghost presentation" as Ghosts {
    [*] --> Blinky
    Blinky --> Pinky : frame 178
    Pinky --> Inky : frame 298
    Inky --> Clyde : frame 418
  }
  state "10 PTS and 50 PTS legend" as Legend
  state "Logo and left energizer" as Logo
  state "ChaseScene" as Chase {
    [*] --> ChasingLeft
    ChasingLeft --> Overshoot : energizer eaten
    Overshoot --> Pause : Pac-Man passes the energizer
    Pause --> ChasingRight : short pause
    ChasingRight --> [*] : fourth ghost eaten
    ChasingLeft : the ghosts chase Pac-Man to the left
    ChasingRight : Pac-Man chases the blue ghosts to the right
  }
  Title --> Ghosts : frame 58
  Ghosts --> Legend : frame 568
  Legend --> Logo : frame 628
  Logo --> Chase : frame 694
  Chase --> [*] : demo
  note right of Ghosts
    For each ghost: sprite, name 60 frames
    later, nickname 90 frames after the sprite.
    The energizers blink from frame 697.
  end note
```

*Figure 17: state diagram: Pac-Man attract mode*  
Open full size: [SVG](diagrams/en/17_etats_attract_pacman.svg) · [PNG](diagrams/en/17_etats_attract_pacman.png)

### 5.2 Pac-Man Scatter / Chase waves

Eight waves alternate; the last one (Chase) lasts until the end of the level. Durations depend on the level (`GameConfig.GetWaveDurationFrames`) and are detailed in 5.7.3.

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
  note right of C7
    Each mode change makes ghosts
    reverse direction.
    The timer is frozen in frightened mode.
  end note
```

*Figure 18: state diagram: Pac-Man Scatter and Chase waves*  
Open full size: [SVG](diagrams/en/18_etats_vagues_pacman.svg) · [PNG](diagrams/en/18_etats_vagues_pacman.png)

### 5.3 Pac-Man fruit

The Pac-Man fruit stays still, below the ghost house. It appears when 70 and then 170 dots have been eaten, stays between 9 and 10 seconds (random duration) and disappears if it is not eaten. Once eaten, it leaves its score displayed for 2 seconds. A lost life removes the visible fruit, but thresholds already passed are not replayed.

```mermaid
stateDiagram-v2
  direction LR
  [*] --> Absent
  Absent --> Visible : 70 or 170 dots eaten
  Visible --> Absent : 540 to 600 frames elapsed
  Visible --> Absent : life lost
  Visible --> Score : eaten by Pac-Man
  Score --> Absent : 120 frames
  Visible : fixed fruit below the ghost house
  Score : points shown in place of the fruit
```

*Figure 19: state diagram: Pac-Man fruit*  
Open full size: [SVG](diagrams/en/19_etats_fruit_pacman.svg) · [PNG](diagrams/en/19_etats_fruit_pacman.png)

### 5.4 Pac-Man intermission 1 (after level 2)

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

*Figure 20: state diagram: intermission 1*  
Open full size: [SVG](diagrams/en/20_etats_intermission1.svg) · [PNG](diagrams/en/20_etats_intermission1.png)

### 5.5 Pac-Man intermission 2 (after level 5)

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

*Figure 21: state diagram: intermission 2*  
Open full size: [SVG](diagrams/en/21_etats_intermission2.svg) · [PNG](diagrams/en/21_etats_intermission2.png)

### 5.6 Pac-Man intermission 3 (after levels 9, 13 and 17)

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

*Figure 22: state diagram: intermission 3*  
Open full size: [SVG](diagrams/en/22_etats_intermission3.svg) · [PNG](diagrams/en/22_etats_intermission3.png)

### 5.7 Analysis of ghost behavior

The four ghosts follow the same movement rule; only the tile they aim for changes, depending on their state and personality. Ms. Pac-Man keeps this mechanism and changes it in a few places, described in chapter 6.

#### 5.7.1 How a ghost chooses its direction

At each tile center, a ghost picks the exit whose next tile is closest to its target, using squared Euclidean distance. There is no pathfinding and no memory: the decision is local, which produces the predictable trajectories of the arcade.

- Candidate directions are tested in the order Up, Left, Down, Right; in case of a tie, the first one wins.
- Reversing is forbidden, except for a reversal imposed by a mode change.
- Walls and the ghost house door are excluded (only eyes may cross the door).
- In Pac-Man, at the four intersections (12,14), (15,14), (12,26) and (15,26), an active ghost cannot turn upward; Ms. Pac-Man has its own intersections, depending on the maze (6.1).

#### 5.7.2 Target by state

| State | Target | Note |
| --- | --- | --- |
| Scatter | The ghost's own corner | They loop around a block |
| Chase | Tile computed by each ghost (5.7.4) | The only difference in personality |
| Frightened | None: pseudo-random choice | Order Up, Right, Down, Left, fixed seed |
| Eaten (eyes) | Tile (13,14), in front of the house | Speed 2 px per frame |

#### 5.7.3 Alternation of Scatter and Chase

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

#### 5.7.4 What sets the four ghosts apart

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

#### 5.7.5 Cruise Elroy

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

#### 5.7.6 Leaving the ghost house

Ghosts do not leave together: each waits until Pac-Man has eaten enough dots. If Pac-Man stops eating, a timer forces the first waiting ghost out.

| Ghost | Level 1 | Level 2 | Level 3 and up |
| --- | --- | --- | --- |
| Blinky | Outside from the start | Outside | Outside |
| Pinky | 0 dots | 0 | 0 |
| Inky | 30 dots | 0 | 0 |
| Clyde | 60 dots | 50 | 0 |

After a death, a global counter replaces these thresholds (Pinky at 7, Inky at 17, Clyde at 32 dots). The forced-exit timer is 4 s on levels 1 to 4 and 3 s afterwards.

#### 5.7.7 Key takeaways

- The danger comes from the combination: Blinky pushes, Pinky cuts off, Inky closes the other side and Clyde eases the pressure up close.
- Scatter is not a break: it disperses the ghosts to break the pincer, and barely lasts at high levels.
- Direction reversals, not colors or sounds, announce a mode change.
- The "faulty" behaviors (Pinky's and Inky's upward shift, the no-Up intersections) are those of the original arcade and are kept.
- Ms. Pac-Man makes the ghosts less predictable: during its single Scatter, Blinky and Pinky wander randomly, and when leaving the house each one picks left or right according to its target.

## 6. Ms. Pac-Man (variant)

This chapter describes what is specific to Ms. Pac-Man, as supplied by `MsPacManVariant`. The engine, the speeds, the Elroy thresholds, the frightened duration and the ghost house counters are those of Pac-Man (chapters 4 and 5); the mazes, the waves, the ghosts in Scatter and when leaving the house, the fruit, the death, the attract mode and the intermissions are different.

### 6.1 What changes compared with Pac-Man

Ms. Pac-Man reuses Pac-Man's speeds, Elroy thresholds, frightened duration and ghost house counters. The differences concern the waves, the Scatter targets, leaving the house, the fruit and the mazes.

| Topic | Pac-Man | Ms. Pac-Man |
| --- | --- | --- |
| Waves | 8 waves, durations depending on the level | A single 7-second Scatter at the start of the level, then Chase until the end |
| Scatter targets | Each ghost's corner | Blinky and Pinky wander randomly (except Blinky in Elroy); Inky and Sue keep their corner |
| Fourth ghost | Clyde | Sue, same behavior as Clyde |
| Upward shift of Pinky and Inky | Yes (arcade bug) | No |
| Leaving the house | Always to the left (to the right if a reversal is pending) | Left or right according to the target (left if frightened) |
| Fruit | Fixed below the house, at 70 and 170 dots, 9 to 10 s | Moving, at 64 and 176 dots, enters through a tunnel, laps the house and leaves; the second is ignored if the first is present |
| Mazes | 1 | 6, depending on the level (1-2, 3-5, 6-9, 10-13, then color variants in blocks of 4 levels) |
| Intersections with no upward turn | (12,14), (15,14), (12,26), (15,26) | Mazes 1, 3 and 5: (9,14), (18,14), (9,26), (18,26); mazes 4 and 6: (12,14), (15,14), (12,23), (15,23); maze 2: none |
| Death | 144-frame animation, 30-frame pause | 159-frame animation (900° spin), 7-frame pause |

### 6.2 Mazes by level

Ms. Pac-Man has six mazes (`MazeIndexForLevel`). The first four follow one another on levels 1-2, 3-5, 6-9 and 10-13; then mazes 5 and 6, color variants of mazes 3 and 4, alternate in blocks of 4 levels. The tunnels of each maze are detected automatically when the map is loaded (`MazeDefinition.Analyze`), along with their slow zones.

```mermaid
stateDiagram-v2
  [*] --> L1 : level 1
  state "Maze 1 (levels 1 and 2)" as L1
  state "Maze 2 (levels 3 to 5)" as L2
  state "Maze 3 (levels 6 to 9)" as L3
  state "Maze 4 (levels 10 to 13)" as L4
  state "Maze 5 (layout of maze 3)" as L5
  state "Maze 6 (layout of maze 4)" as L6
  L1 --> L2 : level 3
  L2 --> L3 : level 6
  L3 --> L4 : level 10
  L4 --> L5 : level 14
  L5 --> L6 : 4 levels later
  L6 --> L5 : 4 levels later
```

*Figure 23: state diagram: Ms. Pac-Man mazes by level*  
Open full size: [SVG](diagrams/en/23_etats_labyrinthes_ms.svg) · [PNG](diagrams/en/23_etats_labyrinthes_ms.png)

| Maze | Levels | Intersections where an active ghost cannot turn upward |
| --- | --- | --- |
| 1 | 1 and 2 | (9,14), (18,14), (9,26), (18,26) |
| 2 | 3 to 5 | none |
| 3 | 6 to 9 | (9,14), (18,14), (9,26), (18,26) |
| 4 | 10 to 13 | (12,14), (15,14), (12,23), (15,23) |
| 5 | 14 to 17, 22 to 25, and so on | (9,14), (18,14), (9,26), (18,26) |
| 6 | 18 to 21, 26 to 29, and so on | (12,14), (15,14), (12,23), (15,23) |

### 6.3 Ms. Pac-Man attract mode

MsAttractScreen shows the title in a marquee (96 running lights), then presents Blinky, Pinky, Inky and Sue: the name appears, the ghost enters from the right below the marquee, goes all the way left, pauses 4 frames at the corner and climbs to stack up left of the marquee. Then come "STARRING MS PAC-MAN" and the entrance of Ms. Pac-Man, then the demo. The timeline is measured on the arcade.

```mermaid
stateDiagram-v2
  [*] --> Title : frame 5
  state "Title, marquee, Midway logo" as Title
  state "WITH" as With
  state "Ghost presentation" as Ghosts {
    [*] --> Blinky : name at frame 62
    Blinky --> Pinky : name at frame 334
    Pinky --> Inky : name at frame 590
    Inky --> Sue : name at frame 830
    Blinky : enters at frame 73
    Pinky : enters at frame 345
    Inky : enters at frame 601
    Sue : enters at frame 841
  }
  state "STARRING MS PAC-MAN" as Starring
  state "Ms. Pac-Man enters from the right and stops below the marquee" as Entrance
  Title --> With : frame 61
  With --> Ghosts : frame 62
  Ghosts --> Starring : frame 1054
  Starring --> Entrance : frame 1066
  Entrance --> [*] : frame 1304, demo
  note right of Ghosts
    Each ghost enters from the right below the marquee,
    goes left, stops 4 frames at the corner,
    then climbs to stack up. Its name stays on screen
    until the next name.
  end note
```

*Figure 24: state diagram: Ms. Pac-Man attract mode*  
Open full size: [SVG](diagrams/en/24_etats_attract_ms.svg) · [PNG](diagrams/en/24_etats_attract_ms.png)

### 6.4 Ms. Pac-Man Scatter / Chase waves

Ms. Pac-Man has a single Scatter phase, 7 seconds long, at the start of each level and each life; the ghosts then stay in Chase until the end of the level. `MsPacManVariant.GetWaveDurationFrames` returns 420 frames for wave 0 and an infinite duration for the next one; the WaveScheduler is the same as for Pac-Man.

```mermaid
stateDiagram-v2
  direction LR
  [*] --> S0 : start of level or life
  state "Scatter (wave 0)" as S0
  state "Chase (wave 1, infinite)" as C1
  S0 --> C1 : 420 frames (7 s), ghosts reverse
  C1 --> [*] : end of level or life lost
  note right of S0
    The timer is frozen in frightened mode.
  end note
```

*Figure 25: state diagram: Ms. Pac-Man Scatter and Chase waves*  
Open full size: [SVG](diagrams/en/25_etats_vagues_ms.svg) · [PNG](diagrams/en/25_etats_vagues_ms.png)

### 6.5 Ms. Pac-Man ghosts

The ghosts keep the common life cycle (4.3). Two settings applied by `MsPacManVariant.CreateGhosts` change their behavior. During the single Scatter, Blinky and Pinky pick their turns at random (`WandersInScatter`) instead of heading for their corner, except Blinky in Cruise Elroy, who chases Ms. Pac-Man; Inky and Sue aim at their corner. When leaving the house (`ExitsTowardTarget`), the ghost leaves on the side closest to its target, or at random if it is wandering; if it is frightened, it leaves as in Pac-Man. The eyes of an eaten ghost come back out as soon as they reach the door, without waiting for a counter.

```mermaid
stateDiagram-v2
  [*] --> InHouse : Pinky, Inky and Sue
  [*] --> Active : Blinky
  InHouse --> Exiting : dot counter or timer, as in Pac-Man
  Exiting --> Active : door crossed, direction chosen
  note left of Exiting
    Direction when leaving the house
    - frightened, left as in Pac-Man
    - Scatter and wandering ghost, left or right at random
    - otherwise, side closest to the target, left on a tie
  end note
  state Active {
    [*] --> Scatter
    state "Scatter (7 s)" as Scatter {
      Wander : Blinky and Pinky, random turns
      Corner : Inky and Sue, maze corner
      Elroy : Blinky in Cruise Elroy, chases Ms. Pac-Man
    }
    Scatter --> Chase : 7 s elapsed (reversal)
    Chase : Pac-Man chase targets, without the upward shift
  }
  Active --> Eaten : eaten while frightened
  Eaten --> Exiting : the eyes reach the door, comes back out at once
```

*Figure 26: state diagram: Ms. Pac-Man ghosts*  
Open full size: [SVG](diagrams/en/26_etats_fantome_ms.svg) · [PNG](diagrams/en/26_etats_fantome_ms.png)

### 6.6 Path of the Ms. Pac-Man fruit

The Ms. Pac-Man fruit does not stay still: it enters through a randomly chosen tunnel, reaches the rectangle of corridors around the ghost house (deduced from the house door, hence specific to each maze), makes a full clockwise lap around it without ever reversing, then leaves through another tunnel by the shortest path. It moves 0.5 pixel per frame (one tile every 16 frames) and bounces at the same rhythm; the whole trip takes about 14 s. It appears at 64 and 176 dots eaten; the second fruit is ignored if the first is still on screen. It never disappears on a timer: only when it leaves the maze or when Ms. Pac-Man eats it.

```mermaid
stateDiagram-v2
  [*] --> Approach : 64 or 176 dots eaten, no fruit on screen
  state "Approach: enters through a tunnel, reaches the loop around the house" as Approach
  state "Lap: one full lap, clockwise, no reversal" as Lap
  state "Leave: shortest path to another tunnel" as Leave
  state "Eaten: score shown for 2 s" as Eaten
  Approach --> Lap : reaches a corridor of the loop
  Approach --> Leave : no complete loop in this maze
  Lap --> Leave : lap finished, back at the arrival tile
  Leave --> [*] : leaves the screen through the tunnel
  Approach --> Eaten : touched by Ms. Pac-Man
  Lap --> Eaten : touched by Ms. Pac-Man
  Leave --> Eaten : touched by Ms. Pac-Man
  Eaten --> [*]
```

*Figure 27: state diagram: path of the Ms. Pac-Man fruit*  
Open full size: [SVG](diagrams/en/27_etats_fruit_ms.svg) · [PNG](diagrams/en/27_etats_fruit_ms.png)

### 6.7 Death of Ms. Pac-Man

On contact with a ghost, all sounds are cut and the game freezes for one second, as for Pac-Man. Ms. Pac-Man then stays 30 frames with her mouth pointing down, then spins clockwise: 10 quarter turns (900°), one every 7.5 frames, during which the death sound is played. She stops with her mouth pointing up, stays so for 62 frames, then disappears; the next life starts 7 frames later. The animation lasts 159 frames (`DeathAnimationFrames`), against 144 for Pac-Man.

```mermaid
stateDiagram-v2
  [*] --> Freeze : contact with a ghost, sounds cut
  state "Freeze, everything frozen" as Freeze
  state "Mouth down" as Down
  state "Clockwise spin, 10 quarter turns" as Spin
  state "Mouth up" as Up
  state "Gone" as Gone
  Freeze --> Down : 60 frames
  Down --> Spin : 30 frames, the death sound starts
  Spin --> Up : 67 frames (one quarter turn every 7.5 frames)
  Up --> Gone : 62 frames
  Gone --> [*] : 7 frames
```

*Figure 28: state diagram: death of Ms. Pac-Man*  
Open full size: [SVG](diagrams/en/28_etats_mort_ms.svg) · [PNG](diagrams/en/28_etats_mort_ms.png)

### 6.8 Ms. Pac-Man intermission 1: "They Meet" (after level 2)

The three Ms. Pac-Man intermissions are played by a single class, MsIntermissionScene: each character follows a track of positions recorded frame by frame on the arcade (MsIntermissionData). Each act starts with a clapperboard bearing its number (arm open, half closed at frame 47, closed at frame 53, open again at frame 59, clapperboard shown until frame 86) and with its title; the act's music starts at frame 1. In act 1, Pac-Man and Ms. Pac-Man first cross the screen in opposite directions, each chased by a ghost; they then come back toward each other on the same row, go up together at the last moment, and the two ghosts collide. A heart appears above the couple. The scene lasts 611 frames.

```mermaid
stateDiagram-v2
  [*] --> Clapper : frame 1, music of act 1
  state "Clapperboard 1 and title THEY MEET (title until frame 55)" as Clapper
  state "Crossing: Pac-Man at the top heading right, Ms. Pac-Man at the bottom heading left" as Crossing
  state "Inky chases Pac-Man (frame 145), Pinky chases Ms. Pac-Man (frame 149)" as Pursuit
  state "Empty screen" as Empty
  state "Meeting: Pac-Man enters from the right, Ms. Pac-Man from the left, same row" as Meeting
  state "Inky (frame 402) and Pinky (frame 405) enter behind them" as Ghosts
  state "Pac-Man and Ms. Pac-Man go up side by side" as Rise
  state "Inky and Pinky collide and bounce back (sound effect)" as Bump
  state "The ghosts are gone, a heart appears above the couple" as Heart
  Clapper --> Crossing : frame 101
  Crossing --> Pursuit : frame 145
  Pursuit --> Empty : frame 352, all gone
  Empty --> Meeting : frame 355
  Meeting --> Ghosts : frame 402
  Ghosts --> Rise : frame 459
  Rise --> Bump : frame 487
  Bump --> Heart : frame 514
  Heart --> [*] : frame 611, end
```

*Figure 29: state diagram: Ms. Pac-Man intermission 1 (They Meet)*  
Open full size: [SVG](diagrams/en/29_etats_intermission1_ms.svg) · [PNG](diagrams/en/29_etats_intermission1_ms.png)

### 6.9 Ms. Pac-Man intermission 2: "The Chase" (after level 5)

After the clapperboard, Pac-Man and Ms. Pac-Man chase each other across the screen: three crossings at different heights, changing direction and pursuer, then a final very fast round trip. The scene lasts 1284 frames, the longest of the three; the screen stays empty between crossings.

```mermaid
stateDiagram-v2
  [*] --> Clapper : frame 1, music of act 2
  state "Clapperboard 2 and title THE CHASE (title until frame 56)" as Clapper
  state "Crossing 1, top, heading right: Pac-Man in front, Ms. Pac-Man behind" as T1
  state "Crossing 2, bottom, heading left: Ms. Pac-Man in front, Pac-Man behind" as T2
  state "Crossing 3, middle, heading right: Pac-Man in front, Ms. Pac-Man behind" as T3
  state "Fast run to the left, top: Ms. Pac-Man in front, Pac-Man behind" as T4
  state "Fast run back to the right, bottom: Pac-Man in front, Ms. Pac-Man behind" as T5
  state "Empty screen until the music ends" as End
  Clapper --> T1 : frame 255
  T1 --> T2 : frame 511 (empty screen since frame 366)
  T2 --> T3 : frame 767 (empty screen since frame 620)
  T3 --> T4 : frame 1021 (empty screen since frame 873)
  T4 --> T5 : around frame 1070
  T5 --> End : frame 1125
  End --> [*] : frame 1284, end
```

*Figure 30: state diagram: Ms. Pac-Man intermission 2 (The Chase)*  
Open full size: [SVG](diagrams/en/30_etats_intermission2_ms.svg) · [PNG](diagrams/en/30_etats_intermission2_ms.png)

### 6.10 Ms. Pac-Man intermission 3: "Junior" (after levels 9, 13 and 17)

Pac-Man and Ms. Pac-Man wait at the bottom left. A stork crosses the screen from right to left carrying a bundle and drops it in flight; the bundle falls diagonally, bounces up to the couple and opens to reveal a baby Pac-Man. The bundle stays visible from its entrance until it changes. The scene lasts 504 frames.

```mermaid
stateDiagram-v2
  [*] --> Clapper : frame 1, music of act 3
  state "Clapperboard 3 and title JUNIOR (title until frame 55)" as Clapper
  state "Pac-Man and Ms. Pac-Man wait at the bottom left" as Wait
  state "The stork enters from the right (frame 102) with the bundle (frame 115)" as Stork
  state "The bundle is dropped, falls diagonally and bounces" as Fall
  state "The bundle comes to rest near the couple" as Rest
  state "The bundle opens: baby Pac-Man" as Baby
  state "Empty screen" as Empty
  Clapper --> Wait : frame 91
  Wait --> Stork : frame 102
  Stork --> Fall : frame 221
  Fall --> Rest : frame 378
  Rest --> Baby : frame 384
  Baby --> Empty : frame 446 (the stork left at frame 396)
  Empty --> [*] : frame 504, end
```

*Figure 31: state diagram: Ms. Pac-Man intermission 3 (Junior)*  
Open full size: [SVG](diagrams/en/31_etats_intermission3_ms.svg) · [PNG](diagrams/en/31_etats_intermission3_ms.png)
