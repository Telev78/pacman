# Pac-Man (C# WinForms) : UML et diagrammes d'états

Ce document décrit l'architecture finale du jeu en 18 diagrammes : un diagramme de paquets, 6 diagrammes de classes, 2 diagrammes de séquence et 9 diagrammes d'états, tous établis à partir du code source. Les diagrammes sont écrits en [Mermaid](https://mermaid.js.org) et s'affichent directement dans les visionneuses Markdown qui le prennent en charge (GitHub, GitLab, VS Code avec extension, Obsidian…).

## 1. Vue d'ensemble

Les sept dossiers du projet forment trois niveaux : l'interface (UI) et les écrans (Screens) pilotent le jeu (Game, Ghosts, Maze), qui s'appuie sur le rendu (Graphics) et l'audio (Audio). Game et Ghosts sont étroitement liés : Game crée les quatre fantômes, et un fantôme lit Pacman et LevelSpecs, définis dans Game.

```mermaid
flowchart TB
  subgraph UI["UI"]
    MainForm["MainForm"]
    Settings["Settings / PACMAN.ini"]
    DisplayConfig["DisplayConfig"]
    InputKey["InputKey"]
  end
  subgraph Screens["Screens"]
    ScreenManager["ScreenManager"]
    Ecrans["AttractScreen, StartScreen, PlayScreen, DemoScreen"]
    Scenes["ChaseScene, Intermission1/2/3Scene"]
    ArcadeTexts["ArcadeTexts"]
  end
  subgraph Game["Game"]
    GameC["Game, PlayerState, ScoreKeeper"]
    Regles["FruitManager, WaveScheduler, GameConfig, LevelSpecs"]
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
  end
  subgraph Audio["Audio"]
    SoundDirector["SoundDirector"]
    AudioEngine["AudioEngine, SoundVoice"]
  end

  MainForm --> ScreenManager
  MainForm --> GameRenderer
  MainForm --> Settings
  ScreenManager --> Ecrans
  Ecrans --> Scenes
  Ecrans --> GameC
  Ecrans --> GameRenderer
  ScreenManager --> SoundDirector
  GameC --> Regles
  GameC --> PacmanC
  GameC --> GhostC
  GameC --> GhostHouse
  GameC --> SoundDirector
  GhostC --> MazeC
  PacmanC --> MazeC
  GameC --> MazeC
  GameRenderer --> SpriteManager
  GameRenderer -.->|lit l'état| GameC
  SoundDirector --> AudioEngine
  ArcadeTexts -.-> Settings
  AudioEngine -.->|volume| Settings
  GameC -.->|vies, vie bonus| Settings
```

*Figure 1 : diagramme de paquets : les sept dossiers et leurs dépendances*

| Dossier | Rôle | Classes principales |
| --- | --- | --- |
| UI | Fenêtre, clavier, plein écran, réglages `PACMAN.ini` | MainForm, Settings, DisplayConfig, InputKey |
| Screens | Enchaînement des écrans, crédits, intermissions | ScreenManager, IScreen, AttractScreen, StartScreen, PlayScreen, DemoScreen, Intermission1/2/3Scene |
| Game | Règles et état d'une partie | Game, PlayerState, ScoreKeeper, FruitManager, WaveScheduler, Pacman, GameConfig |
| Ghosts | Comportement des fantômes et du nid | Ghost, Blinky, Pinky, Inky, Clyde, GhostHouse |
| Maze | Labyrinthe, cases, directions | LevelMap, MazeLayout, Tiles, Direction |
| Graphics | Dessin à la résolution de l'arcade (224×288) | GameRenderer, SpriteManager, HudLayout |
| Audio | Sons, mixage et volume | SoundDirector, AudioEngine, SoundVoice |

## 2. Diagrammes de classes

Les membres affichés sont les plus significatifs, pas la liste complète. Les relations suivent l'UML : flèche creuse pour l'héritage ou l'implémentation d'une interface, losange plein pour la composition, losange creux pour l'agrégation, trait pointillé pour une dépendance.

### 2.1 Écrans et navigation

MainForm possède un ScreenManager, qui garde l'écran courant derrière l'interface IScreen. Les crédits, le high score et le son traversent les écrans : ils vivent dans le ScreenManager.

```mermaid
classDiagram
  direction TB
  class Form {
    <<WinForms>>
  }
  class MainForm {
    -GameRenderer renderer
    -ScreenManager screens
    -Timer gameTimer
    -bool fullscreen
    -int volumeShownFrames
    -OnKeyDown()
    -GameLoop()
    -OnPaint()
    -ToggleFullscreen()
    -ComputeDestination() Rectangle
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
    +Draw(GameRenderer, Graphics)
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
    +Draw(GameRenderer, Graphics)
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

  Form <|-- MainForm
  MainForm *-- ScreenManager
  MainForm ..> InputKey : traduit les touches
  MainForm ..> Settings
  IScreen <|.. AttractScreen
  IScreen <|.. StartScreen
  IScreen <|.. PlayScreen
  IScreen <|.. DemoScreen
  ScreenManager o-- IScreen : écran courant
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

*Figure 2 : diagramme de classes : écrans et navigation*

### 2.2 Intermissions

PlayScreen affiche une seule IIntermission à la fois et met le Game en pause pendant ce temps. Le Game ne connaît que le numéro de l'acte à jouer (1, 2 ou 3) ; c'est PlayScreen qui crée la scène correspondante.

```mermaid
classDiagram
  direction TB
  class IIntermission {
    <<interface>>
    +bool Finished
    +Update()
    +Draw(GameRenderer, Graphics)
  }
  class PlayScreen {
    -Game game
    -IIntermission intermission
    +Update()
    +Draw(GameRenderer, Graphics)
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
    +DrawLevelIndicator(Graphics, int level)
  }
  class SpriteManager {
    +GetGiantPacmanSprite(int)
    +GetSheetSprite(int, int)
    +GetWideSheetSprite(int, int, int, int)
  }

  IIntermission <|.. Intermission1Scene
  IIntermission <|.. Intermission2Scene
  IIntermission <|.. Intermission3Scene
  PlayScreen o-- IIntermission : une à la fois
  PlayScreen --> Game : FinishIntermission()
  PlayScreen ..> Intermission1Scene : crée (acte 1)
  PlayScreen ..> Intermission2Scene : crée (acte 2)
  PlayScreen ..> Intermission3Scene : crée (acte 3)
  Intermission1Scene --> SoundDirector
  Intermission2Scene --> SoundDirector
  Intermission3Scene --> SoundDirector
  Intermission1Scene ..> GameRenderer
  Intermission2Scene ..> GameRenderer
  Intermission3Scene ..> GameRenderer
  GameRenderer --> SpriteManager
```

*Figure 3 : diagramme de classes : intermissions*

### 2.3 Moteur de jeu

Game est le centre du moteur. Il enchaîne les états de GameState, délègue les règles à ScoreKeeper, FruitManager, WaveScheduler et GhostHouse, et garde un PlayerState par joueur, chacun avec son propre labyrinthe.

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
  Game o-- PlayerState : joueur actif
  Game *-- ScoreKeeper
  Game *-- FruitManager
  Game *-- WaveScheduler
  Game *-- GhostHouse
  Game *-- Pacman
  Game *-- "4" Ghost
  Game --> GameState
  Game --> LevelSpecs : specs du niveau
  Game --> SoundDirector
  GhostHouse o-- "4" Ghost
  PlayerState *-- LevelMap : un labyrinthe par joueur
  LevelMap --> TileType
  Pacman --> PacmanState
  Pacman ..> LevelMap
  GameConfig ..> LevelSpecs : fabrique
  ScoreKeeper ..> PlayerState
```

*Figure 4 : diagramme de classes : moteur de jeu*

### 2.4 Fantômes

Ghost contient tout ce qui est commun : déplacement pixel par pixel, choix de direction, vitesse, demi-tour. Chaque fantôme ne définit que sa cible de chasse. Inky garde une référence vers Blinky pour calculer la sienne, et Blinky ajoute l'état Cruise Elroy.

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
  Inky --> Ghost : blinky (cible de chasse)
  Ghost --> GhostType
  Ghost --> GhostPhase
  Ghost --> GhostMode
```

*Figure 5 : diagramme de classes : fantômes*

### 2.5 Rendu

GameRenderer dessine tout dans un bitmap natif de 224×288, puis l'agrandit d'un bloc dans le rectangle calculé par MainForm. SpriteManager découpe la feuille unique `asset.png` et garde chaque sprite en cache.

```mermaid
classDiagram
  direction TB
  class GameRenderer {
    +SpriteManager Sprites
    +Render(IScreen, Graphics, Rectangle)
    +DrawGame(Graphics, Game)
    +DrawTopHud(...)
    +DrawCredits(Graphics, int)
    +DrawLevelIndicator(Graphics, int)
    +DrawText(Graphics, string, int, int, ArcadeColor)
    +DrawSprite(Graphics, Bitmap, int, int)$
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
    +Dispose()
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
    +Draw(GameRenderer, Graphics)
  }
  class Game
  class asset_png["asset.png (feuille unique)"]
  class Bitmap {
    <<backBuffer 224x288>>
  }

  GameRenderer *-- SpriteManager
  GameRenderer *-- Bitmap : dessin natif puis agrandi
  GameRenderer ..> IScreen : Render()
  GameRenderer ..> Game : DrawGame()
  GameRenderer ..> HudLayout
  GameRenderer ..> DisplayConfig
  SpriteManager --> asset_png : découpe et met en cache
  SpriteManager ..> ArcadeColor
  SpriteManager ..> MazeColor
```

*Figure 6 : diagramme de classes : rendu*

### 2.6 Audio

SoundDirector traduit les événements du jeu en sons et garantit qu'un seul son de fond joue à la fois. AudioEngine décode les 24 sons au démarrage, les mixe avec NAudio, puis applique le volume avant la sortie WaveOut.

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
  class AudioEngine {
    +float Volume
    +Play(SoundId, bool loop)
    +PlaySequence(first, next, loopNext)
    +Stop(SoundId)
    +StopAll()
    +Dispose()
  }
  class SoundVoice {
    <<internal, ISampleProvider>>
    +SoundId FirstId
    +SoundId ChainedId
    +bool IsFinished
    +Stop()
    +Read(Span~float~) int
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
  class MixingSampleProvider {
    <<NAudio>>
  }
  class VolumeSampleProvider {
    <<NAudio>>
  }
  class WaveOut {
    <<NAudio>>
  }
  class Settings {
    <<static>>
    +int Volume
  }

  SoundDirector *-- AudioEngine
  SoundDirector --> Background : un seul son de fond
  AudioEngine *-- "24" SoundData : décodés au démarrage
  AudioEngine o-- "0..*" SoundVoice : voix en cours
  SoundVoice --> SoundData
  SoundData --> SoundId
  AudioEngine *-- MixingSampleProvider
  MixingSampleProvider --> VolumeSampleProvider : flux audio
  VolumeSampleProvider --> WaveOut : flux audio
  AudioEngine ..> Settings : volume initial
```

*Figure 7 : diagramme de classes : audio*

## 3. Diagrammes de séquence

### 3.1 Une image de jeu à 60 Hz

Le Timer de MainForm accumule le temps écoulé et appelle autant de fois que nécessaire la mise à jour logique, à pas fixe de 1/60 s, avant de redessiner. La vitesse du jeu reste ainsi identique quelle que soit la machine.

```mermaid
sequenceDiagram
  participant T as Timer (MainForm)
  participant S as ScreenManager
  participant P as PlayScreen
  participant G as Game
  participant W as WaveScheduler
  participant F as FruitManager
  participant D as SoundDirector
  participant H as GhostHouse
  participant PM as Pacman
  participant GH as Ghost x4
  participant R as GameRenderer
  T->>T: GameLoop, temps accumulé
  loop tant que le temps accumulé ≥ 16,67 ms
    T->>S: Update()
    S->>P: Update()
    P->>G: Update()
    G->>W: Update(niveau, effrayé)
    G->>F: Update(niveau, gommes, Pacman)
    G->>D: UpdateBackground(...)
    G->>H: Update(niveau)
    G->>PM: Move(carte, specs, effrayé)
    G->>GH: Move(carte, Pacman, specs, mode)
    G->>G: CheckPelletCollision()
    G->>G: CheckGhostCollisions()
  end
  T->>T: Invalidate()
  T->>R: Render(écran courant, Graphics, rectangle)
  R->>P: Draw(renderer, g)
  P->>R: DrawGame(g, game)
```

*Figure 17 : diagramme de séquence : une image de jeu à 60 Hz*

### 3.2 Du crédit au lancement d'une partie

Un crédit mène de l'attract mode à l'écran start ; la touche 1 démarre une partie et consomme un crédit. À la fin du GAME OVER, le ScreenManager revient à l'écran start s'il reste des crédits, sinon à l'attract mode.

```mermaid
sequenceDiagram
  actor J as Joueur
  participant M as MainForm
  participant S as ScreenManager
  participant A as AttractScreen
  participant ST as StartScreen
  participant P as PlayScreen
  participant G as Game
  J->>M: touche 5
  M->>S: OnInput(Coin)
  S->>S: InsertCoin(), crédit + 1
  S->>ST: ShowStart() via SwitchTo
  Note over S,ST: SwitchTo coupe les sons et appelle Enter()
  J->>M: touche 1
  M->>S: OnInput(Start1)
  S->>ST: OnInput(Start1)
  ST->>S: StartGame(1)
  S->>S: crédits - nombre de joueurs, SwitchTo(PlayScreen)
  S->>P: Enter()
  P->>G: new Game(son, high score, nombre de joueurs)
  loop chaque image
    S->>P: Update()
    P->>G: Update()
  end
  G-->>P: IsFinished
  P->>S: OnGameEnded(high score)
  S->>ST: ShowStart() si crédits restants
  S->>A: ShowAttract() sinon
```

*Figure 18 : diagramme de séquence : du crédit au lancement d'une partie*

## 4. Diagrammes d'états

Chaque diagramme correspond à une énumération ou à une chronologie du code. Les durées sont en images à 60 Hz (60 images = 1 seconde).

### 4.1 Cycle des écrans

Le ScreenManager enchaîne attract mode, démo, écran start et partie. Un crédit est accepté à tout moment : il fait quitter l'attract mode ou la démo pour l'écran start, et s'ajoute simplement au compteur ailleurs. L'enregistrement de la démo (F9) n'existe qu'en Debug.

```mermaid
stateDiagram-v2
  [*] --> Attract : lancement du jeu
  state "AttractScreen" as Attract {
    [*] --> Presentation
    Presentation --> Chasse : image 694
    Presentation : présentation des personnages
    Chasse : ChaseScene
  }
  state "DemoScreen (démo rejouée)" as Demo
  state "StartScreen" as Start
  state "PlayScreen" as Play
  Attract --> Demo : chasse terminée
  Demo --> Attract : démo terminée
  Attract --> Start : crédit inséré
  Demo --> Start : crédit inséré
  Start --> Start : crédit inséré, maximum 99
  Start --> Play : touche 1 ou 2
  Play --> Start : GAME OVER, crédits restants
  Play --> Attract : GAME OVER, plus de crédit
  note right of Start
    La touche 1 coûte 1 crédit, la touche 2 en coûte 2.
    F9 (Debug seulement) lance l'enregistrement
    de la démo depuis l'attract mode ou l'écran start.
  end note
```

*Figure 8 : diagramme d'états : cycle des écrans*

### 4.2 États d'une partie

Game passe par sept états (GameState). Dying et Victory sont découpés en phases chronométrées. En démo, la mort se termine après l'animation et une seconde de pause, et une victoire termine la démo aussitôt.

```mermaid
stateDiagram-v2
  [*] --> Ready : début de partie
  Ready --> Playing : compte à rebours terminé
  Playing --> GhostEaten : fantôme effrayé mangé
  GhostEaten --> Playing : gel de 60 images
  Playing --> Dying : contact avec un fantôme actif
  Playing --> Victory : dernière gomme mangée
  state Dying {
    [*] --> GelMort
    GelMort --> AnimationMort : 60 images
    AnimationMort --> PauseMort : 12 sprites de 12 images
    PauseMort --> [*] : 30 images
    GelMort : tout est figé
    AnimationMort : Pac-Man seul à l'écran
    PauseMort : écran vide
  }
  Dying --> Ready : vie restante (même joueur ou autre joueur)
  Dying --> GameOver : plus aucune vie
  GameOver --> Ready : l'autre joueur a encore des vies
  GameOver --> [*] : fin de la partie (180 images)
  state Victory {
    [*] --> GelVictoire
    GelVictoire --> Clignotement : 60 images
    Clignotement --> [*] : 4 clignotements de 28 images
    GelVictoire : fantômes visibles
    Clignotement : labyrinthe blanc et bleu
  }
  Victory --> Intermission : après les niveaux 2, 5, 9, 13 et 17
  Victory --> Ready : autres niveaux (niveau suivant)
  Intermission --> Ready : intermission terminée (niveau suivant)
  note right of Ready
    READY! dure 240 images au départ et après
    un changement de joueur, 120 après
    une mort ou un niveau
  end note
```

*Figure 9 : diagramme d'états : états d'une partie*

### 4.3 Cycle de vie d'un fantôme

Quatre phases (GhostPhase). Quand le fantôme est actif, deux dimensions évoluent indépendamment : le mode de vague (Scatter ou Chase) et l'état effrayé.

```mermaid
stateDiagram-v2
  [*] --> InHouse : Pinky, Inky et Clyde (début de niveau ou de vie)
  [*] --> Active : Blinky
  InHouse --> Exiting : compteur de gommes atteint, ou inactivité de 4 s (3 s dès le niveau 5)
  Exiting --> Active : porte franchie
  state Active {
    [*] --> Scatter
    Scatter --> Chase : vague suivante (demi-tour)
    Chase --> Scatter : vague suivante (demi-tour)
    --
    [*] --> Normal
    Normal --> Frightened : super-gomme (demi-tour)
    Frightened --> Normal : durée écoulée
  }
  Active --> Eaten : mangé alors qu'il est effrayé
  Eaten --> Exiting : les yeux atteignent la porte du nid
  note right of InHouse
    IsFrightened peut aussi être vrai
    dans le nid ou à la sortie
  end note
```

*Figure 10 : diagramme d'états : cycle de vie d'un fantôme*

### 4.4 Vagues Scatter / Chase

Huit vagues alternent ; la dernière (Chase) dure jusqu'à la fin du niveau. Les durées dépendent du niveau (`GameConfig.GetWaveDurationFrames`).

```mermaid
stateDiagram-v2
  [*] --> S0 : début de niveau ou de vie
  state "Scatter (vague 0)" as S0
  state "Chase (vague 1)" as C1
  state "Scatter (vague 2)" as S2
  state "Chase (vague 3)" as C3
  state "Scatter (vague 4)" as S4
  state "Chase (vague 5)" as C5
  state "Scatter (vague 6)" as S6
  state "Chase (vague 7, infinie)" as C7
  S0 --> C1 : durée du niveau
  C1 --> S2 : durée du niveau
  S2 --> C3 : durée du niveau
  C3 --> S4 : durée du niveau
  S4 --> C5 : durée du niveau
  C5 --> S6 : durée du niveau
  S6 --> C7 : durée du niveau
  note right of S0
    Chaque changement de mode fait faire
    demi-tour aux fantômes.
    Le compteur est figé en mode effrayé.
  end note
```

*Figure 11 : diagramme d'états : vagues Scatter et Chase*

### 4.5 Son de fond

Un seul son de fond joue à la fois, par priorité : yeux, puis panique, puis sirène. La sirène accélère par paliers quand il reste peu de gommes.

```mermaid
stateDiagram-v2
  [*] --> Aucun
  Aucun --> Sirene : première image de jeu
  state Sirene {
    [*] --> Sirene0
    Sirene0 --> Sirene1 : 164 gommes restantes ou moins
    Sirene1 --> Sirene2 : 144 ou moins
    Sirene2 --> Sirene3 : 124 ou moins
    Sirene3 --> Sirene4 : 104 ou moins
  }
  Sirene --> Panique : fantôme effrayé
  Sirene --> Yeux : fantôme en yeux
  Panique --> Yeux : fantôme en yeux
  Yeux --> Panique : plus d'yeux, fantôme encore effrayé
  Yeux --> Sirene : plus d'yeux, plus de fantôme effrayé
  Panique --> Sirene : plus de fantôme effrayé
  Sirene --> Aucun : mort, victoire ou nouveau niveau
  Panique --> Aucun : mort, victoire ou nouveau niveau
  Yeux --> Aucun : mort, victoire ou nouveau niveau
  note right of Sirene
    Les seuils sont décalés de 0, 10, 20, 30 ou 40 gommes
    selon le niveau (1, 2, 3 à 5, 6 à 20, 21 et plus)
  end note
```

*Figure 12 : diagramme d'états : son de fond*

### 4.6 Mode 2 joueurs

L'alternance se fait à chaque vie perdue tant que l'autre joueur a encore des vies. Un joueur sans vie laisse la main à l'autre après son GAME OVER.

```mermaid
stateDiagram-v2
  [*] --> J1 : PLAYER ONE
  J1 --> J2 : vie perdue, J2 a encore des vies
  J2 --> J1 : vie perdue, J1 a encore des vies
  J1 --> J1 : vie perdue, l'autre joueur n'a plus de vie
  J2 --> J2 : vie perdue, l'autre joueur n'a plus de vie
  J1 --> GameOverJ1 : dernière vie perdue
  J2 --> GameOverJ2 : dernière vie perdue
  GameOverJ1 --> J2 : J2 a encore des vies
  GameOverJ2 --> J1 : J1 a encore des vies
  GameOverJ1 --> Fin : J2 n'a plus de vie
  GameOverJ2 --> Fin : J1 n'a plus de vie
  Fin --> [*] : retour à l'écran start ou à l'attract
  state "Joueur 1 actif" as J1
  state "Joueur 2 actif" as J2
  state "GAME OVER (joueur 1)" as GameOverJ1
  state "GAME OVER (joueur 2)" as GameOverJ2
  state "Partie terminée" as Fin
  note right of J2
    Chaque joueur garde son labyrinthe, son score,
    ses vies, son niveau, son fruit et ses compteurs du nid.
    Le high score est partagé.
  end note
```

*Figure 13 : diagramme d'états : mode 2 joueurs*

### 4.7 Intermission 1 (après le niveau 2)

Pac-Man traverse l'écran vers la gauche, rattrapé peu à peu par Blinky ; puis un Blinky bleu repasse vers la droite, poursuivi par un Pac-Man géant. La musique de 5 s, jouée deux fois, démarre à l'image 20.

```mermaid
stateDiagram-v2
  [*] --> PacmanSeul : image 15, Pac-Man entre par la droite
  PacmanSeul --> Poursuite : image 34, Blinky entre, plus rapide
  Poursuite --> BlinkyEffraye : image 258, Blinky bleu entre par la gauche
  BlinkyEffraye --> PacmanGeant : image 405, Pac-Man géant entre
  PacmanGeant --> [*] : image 648, fin
  state "Pac-Man seul (vers la gauche)" as PacmanSeul
  state "Blinky poursuit Pac-Man (vers la gauche)" as Poursuite
  state "Blinky bleu (vers la droite)" as BlinkyEffraye
  state "Pac-Man géant poursuit Blinky (vers la droite)" as PacmanGeant
  note right of PacmanSeul
    La musique démarre à l'image 20
    (5 s jouées deux fois)
  end note
```

*Figure 14 : diagramme d'états : intermission 1*

### 4.8 Intermission 2 (après le niveau 5)

La tunique de Blinky s'accroche à un clou et s'étire avec trois sprites successifs avant de se déchirer. La musique est coupée net à la fin de la scène, après l'écran noir.

```mermaid
stateDiagram-v2
  [*] --> PacmanSeul : image 16, Pac-Man entre par la droite
  PacmanSeul --> Poursuite : image 104, Blinky entre
  Poursuite --> Accroche : image 190, la tunique s'accroche au clou
  Accroche --> Dechire : image 326, la tunique se déchire
  Dechire --> RegardeBas : image 385, Blinky baisse les yeux
  RegardeBas --> Noir : image 503, écran noir
  Noir --> [*] : image 511, fin
  state "Pac-Man seul" as PacmanSeul
  state "Blinky poursuit Pac-Man" as Poursuite
  state "Tunique étirée (3 sprites selon les pas)" as Accroche
  state "Blinky troué, yeux en haut" as Dechire
  state "Blinky troué, yeux en bas" as RegardeBas
  state "Tout est noir" as Noir
```

*Figure 15 : diagramme d'états : intermission 2*

### 4.9 Intermission 3 (après les niveaux 9, 13 et 17)

Pac-Man, poursuivi par Blinky à la tunique recousue, sort à gauche ; l'écran reste vide un instant, puis Blinky, déshabillé, rampe vers la droite en traînant sa tunique.

```mermaid
stateDiagram-v2
  [*] --> PacmanSeul : image 15, Pac-Man entre par la droite
  PacmanSeul --> Poursuite : image 56, Blinky entre, tunique recousue
  Poursuite --> EcranVide : image 235, tous deux sortis à gauche
  EcranVide --> Rampe : image 307, Blinky entre par la gauche
  Rampe --> [*] : image 533, fin
  state "Pac-Man seul" as PacmanSeul
  state "Blinky poursuit Pac-Man (vers la gauche)" as Poursuite
  state "Écran vide" as EcranVide
  state "Blinky rampe en traînant sa tunique (vers la droite)" as Rampe
```

*Figure 16 : diagramme d'états : intermission 3*

## 5. Analyse du comportement des fantômes

Les quatre fantômes suivent la même règle de déplacement ; seule la case qu'ils visent change, selon leur état et leur personnalité.

### 5.1 Comment un fantôme choisit sa direction

À chaque centre de case, un fantôme choisit la sortie dont la case suivante est la plus proche de sa cible, en distance euclidienne au carré. Il n'y a ni recherche de chemin ni mémoire : la décision est locale, ce qui produit les trajectoires prévisibles de l'arcade.

- Les directions candidates sont testées dans l'ordre Haut, Gauche, Bas, Droite ; en cas d'égalité, la première gagne.
- Le demi-tour est interdit, sauf inversion imposée par un changement de mode.
- Les murs et la porte du nid sont exclus (la porte n'est franchie que par les yeux).
- Aux quatre intersections (12,14), (15,14), (12,26) et (15,26), un fantôme actif ne peut pas tourner vers le haut.

### 5.2 Cible selon l'état

| État | Cible | Remarque |
| --- | --- | --- |
| Scatter | Coin propre au fantôme | Ils tournent en boucle autour d'un bloc |
| Chase | Case calculée par chaque fantôme (5.4) | Seule différence de personnalité |
| Effrayé | Aucune : choix pseudo-aléatoire | Ordre Haut, Droite, Bas, Gauche, graine fixe |
| Mangé (yeux) | Case (13,14), devant le nid | Vitesse 2 px par image |

### 5.3 Alternance Scatter et Chase

Un planificateur de 8 vagues alterne Scatter (vagues paires) et Chase (vagues impaires), puis reste en Chase pour toujours à la 8e. Plus le niveau monte, plus la phase Scatter raccourcit, jusqu'à devenir une simple inversion de direction.

| Vague | Niveau 1 | Niveaux 2 à 4 | Niveaux 5 et plus |
| --- | --- | --- | --- |
| 1 Scatter | 7 s | 7 s | 5 s |
| 2 Chase | 20 s | 20 s | 20 s |
| 3 Scatter | 7 s | 7 s | 5 s |
| 4 Chase | 20 s | 20 s | 20 s |
| 5 Scatter | 5 s | 5 s | 5 s |
| 6 Chase | 20 s | 1033 s | 1037 s |
| 7 Scatter | 5 s | 1 image | 1 image |
| 8 Chase | infini | infini | infini |

Trois règles complètent ce tableau :

- Le chronomètre est gelé tant que les fantômes sont effrayés.
- À chaque changement de vague, tous les fantômes reçoivent une demande d'inversion de direction : c'est le seul signal visible d'un changement de mode.
- Un fantôme qui sort du nid prend le mode courant ; ceux qui y sont encore gardent la demande d'inversion et sortent donc par la droite au lieu de la gauche.

Une super-gomme demande aussi l'inversion, même quand la durée d'effroi du niveau est nulle (niveaux 17, 19, 20 et suivants).

### 5.4 Ce qui distingue les quatre fantômes

Blinky vise Pac-Man, les autres visent un point calculé pour couper ou éviter sa route. C'est cette variété qui fait fonctionner la poursuite en tenaille.

| Fantôme | Cible en Chase | Coin Scatter | Rôle |
| --- | --- | --- | --- |
| Blinky (rouge) | La case de Pac-Man | Haut droite, (25,0) | Poursuivant direct, accélère (Elroy) |
| Pinky (rose) | 4 cases devant Pac-Man | Haut gauche, (2,0) | Embuscade |
| Inky (bleu) | Symétrique de Blinky par rapport à un point 2 cases devant Pac-Man | Bas droite, (27,34) | Imprévisible, dépend de Blinky |
| Clyde (orange) | Pac-Man s'il est à 8 cases ou plus, sinon son propre coin | Bas gauche, (0,34) | Poursuit de loin, fuit de près |

**Blinky** ne calcule rien : il vise la case occupée par Pac-Man et le suit à la trace. Il devient plus rapide quand il reste peu de gommes (Cruise Elroy, ci-dessous) et il ne retourne pas à son coin tant qu'il est en Elroy.

**Pinky** vise 4 cases devant Pac-Man dans sa direction. Quand Pac-Man monte, la cible est aussi décalée de 4 cases vers la gauche : c'est le dépassement de capacité de l'arcade, reproduit volontairement. Faire demi-tour face à Pinky le fait souvent repartir dans l'autre sens.

**Inky** est le plus complexe : on prend le point situé 2 cases devant Pac-Man (avec le même décalage de 2 cases à gauche quand il monte), on trace le vecteur de Blinky vers ce point, puis on le double. Inky se place donc de l'autre côté de Pac-Man par rapport à Blinky ; seul, il est inoffensif, mais combiné à Blinky il ferme les issues.

**Clyde** alterne : s'il est à 8 cases ou plus de Pac-Man (distance au carré de 64 ou plus), il le vise comme Blinky ; dès qu'il s'approche, il vise son coin du bas à gauche et s'éloigne. Il finit par tourner autour de ce coin sans jamais coincer le joueur.

### 5.5 Cruise Elroy

Quand il reste peu de gommes, Blinky accélère en deux paliers et poursuit Pac-Man même pendant les phases Scatter. Le seuil dépend du niveau ; le second palier est à la moitié du premier.

| Niveaux | Gommes restantes (palier 1 / palier 2) | Vitesse palier 1 / palier 2 |
| --- | --- | --- |
| 1 | 20 / 10 | 0,80 / 0,85 |
| 2 | 30 / 15 | 0,90 / 0,95 |
| 3 à 4 | 40 / 20 | 0,90 / 0,95 |
| 5 | 40 / 20 | 1,00 / 1,05 |
| 6 à 8 | 50 / 25 | 1,00 / 1,05 |
| 9 à 11 | 60 / 30 | 1,00 / 1,05 |
| 12 à 14 | 80 / 40 | 1,00 / 1,05 |
| 15 à 18 | 100 / 50 | 1,00 / 1,05 |
| 19 et plus | 120 / 60 | 1,00 / 1,05 |

Après une mort de Pac-Man, l'Elroy est suspendu jusqu'à la sortie de Clyde du nid.

### 5.6 Sortie du nid

Les fantômes ne sortent pas ensemble : chacun attend que Pac-Man ait mangé assez de gommes. Si Pac-Man cesse de manger, un minuteur force la sortie du premier fantôme en attente.

| Fantôme | Niveau 1 | Niveau 2 | Niveau 3 et plus |
| --- | --- | --- | --- |
| Blinky | Dehors dès le départ | Dehors | Dehors |
| Pinky | 0 gomme | 0 | 0 |
| Inky | 30 gommes | 0 | 0 |
| Clyde | 60 gommes | 50 | 0 |

Après une mort, un compteur global remplace ces seuils (Pinky à 7, Inky à 17, Clyde à 32 gommes). Le minuteur de sortie forcée vaut 4 s aux niveaux 1 à 4 et 3 s ensuite.

### 5.7 Points à retenir

- Le danger vient de la combinaison : Blinky pousse, Pinky coupe, Inky ferme l'autre côté et Clyde relâche la pression de près.
- Le Scatter n'est pas une pause : il disperse les fantômes pour casser la tenaille, et ne dure presque plus aux niveaux élevés.
- Les inversions de direction, et non les couleurs ou les sons, annoncent un changement de mode.
- Les comportements « fautifs » (décalage de Pinky et d'Inky vers le haut, impasses de Haut aux quatre intersections) sont ceux de l'arcade d'origine et sont conservés.
