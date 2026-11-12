# Pac-Man (C# WinForms)

**Français** | [English](README.en.md)

Reproduction fidèle du Pac-Man d'arcade (1980) en C# / Windows Forms : même labyrinthe, mêmes règles, mêmes
comportements des fantômes, et tout l'habillage d'une borne : crédits, attract mode, démo, mode 2 joueurs
et les trois intermissions.

Projet personnel, non commercial. Pac-Man est une marque de Bandai Namco Entertainment.

## Prérequis

- Windows
- SDK .NET 10 (`net10.0-windows`)
- Paquet NuGet `NAudio.WinMM` 3.3.0 (restauré automatiquement à la compilation)

## Compilation et lancement

```
dotnet build -c Release
dotnet run -c Release
```

L'exécutable produit s'appelle `PACMAN.exe`.

### Ressources attendues

Les ressources sont embarquées dans l'exécutable. Elles doivent se trouver dans le dossier `asset\` :

| Fichier | Rôle |
|---|---|
| `asset\asset.png` | Feuille de sprites unique (personnages, texte, fruits, intermissions…) |
| `asset\maps\original_maze.txt` | Plan du labyrinthe |
| `asset\sounds\*.wav` | Sons de l'arcade |
| `asset\demo.txt` | Suite de touches de la démo (optionnel : sans lui, la démo n'est pas jouée) |

L'icône du programme est `Icon.ico`, à la racine du projet.

## Commandes

| Touche | Action |
|---|---|
| `5` | Insérer un crédit (jusqu'à 99) |
| `1` | Démarrer une partie à 1 joueur (1 crédit) |
| `2` | Démarrer une partie à 2 joueurs (2 crédits) |
| Flèches | Diriger Pac-Man |
| `Alt` + `Entrée` | Basculer plein écran / fenêtre |
| `F12` / `F11` | Volume + / − (pas de 10 %) |

Comme à l'arcade, il n'y a pas de pause. Les touches `5`, `1` et `2` fonctionnent aussi au pavé numérique.

## Configuration : `PACMAN.ini`

Le fichier est créé automatiquement à côté de l'exécutable au premier lancement. Les modifications sont prises en compte
au lancement suivant ; une valeur invalide est ignorée (valeur par défaut).

```ini
; Zoom : taille de la fenêtre, de 1 à 8 (1 = 224x288, 3 = 672x864)
Zoom=3
; Fullscreen : true pour démarrer en plein écran
Fullscreen=false
; Volume : de 0 à 100 (réglé aussi par F11 / F12 en jeu, et mémorisé)
Volume=100
; Version : JAP (Namco, noms japonais) ou USA (Midway, noms américains)
Version=JAP
; Lives : nombre de vies au départ, de 1 à 5
Lives=3
; BonusLife : score donnant une vie bonus : 10000, 15000, 20000 ou NONE
BonusLife=10000
```

- **Version JAP / USA** : change les noms et surnoms des fantômes (OIKAKE / « AKABEI »… ou SHADOW / « BLINKY »…),
  le nom de Pac-Man (PUCKMAN / PAC-MAN) et la ligne de copyright (logo namco ou « © 1980 MIDWAY MFG.CO. »).
- **Plein écran** : l'image est agrandie par le plus grand multiple entier qui tient dans l'écran, centrée, avec des
  bandes noires, pour garder des pixels nets.
- Le high score n'est jamais enregistré : à chaque lancement, il repart de zéro, comme à l'allumage d'une borne.

## Ce qui est reproduit

- **Boucle de jeu** à 60 Hz fixe, tous les timers en images ; rendu à la résolution native 224×288, agrandi d'un bloc.
- **Fantômes** : cibles de Blinky, Pinky, Inky et Clyde, alternance Scatter / Chase par niveau, Elroy, sortie du nid
  (compteurs individuels et global après une mort), mode effrayé avec hasard déterministe, ralentissement dans le tunnel.
- **Pac-Man** : virages anticipés (cornering), pause à chaque gomme (1 image, 3 pour une super-gomme),
  zones interdites aux virages vers le haut.
- **Niveaux** : tableau de vitesses et de durées par niveau, fruits (apparition à 70 et 170 gommes, durée aléatoire de 9 à 10 s),
  indicateur de niveau.
- **Score** : vie bonus unique par joueur et par partie, high score partagé.
- **Son** : sirènes selon le nombre de gommes restantes, mode effrayé, yeux, mort, fruit, vie bonus, intermissions,
  avec priorité entre les sons de fond (yeux > panique > sirène).
- **Écrans** : attract mode (présentation des personnages avec la chronologie de l'arcade, animation de chasse),
  écran start (1 joueur / 1 ou 2 joueurs selon les crédits), démo « GAME OVER » rejouée à partir de touches enregistrées,
  retour à l'attract mode.
- **Mode 2 joueurs** : labyrinthe, score, vies, niveau et fruit propres à chaque joueur, alternance à chaque vie perdue.
- **Intermissions** : après les niveaux 2, 5, 9, 13 et 17 (actes 1, 2, 3, 3, 3).

Les valeurs d'animation (vitesses, délais, positions, phases des jambes, durées de scène) ont été mesurées image par image sur
des enregistrements de l'arcade.

## Architecture

```
Program.cs            Point d'entrée, chargement des réglages
UI/                   MainForm (fenêtre, clavier, plein écran, volume), Settings (PACMAN.ini), DisplayConfig, InputKey
Screens/              IScreen + ScreenManager (crédits), AttractScreen, StartScreen, PlayScreen, DemoScreen,
                      ChaseScene, Intermission1/2/3Scene (IIntermission), ArcadeTexts, DemoRecording
Game/                 Game (machine à états d'une partie), PlayerState, ScoreKeeper, FruitManager, WaveScheduler,
                      GameConfig, LevelSpecs, Pacman
Ghosts/               Ghost et les quatre fantômes, GhostHouse (nid), GhostMode / GhostPhase
Maze/                 LevelMap, MazeLayout, Tiles, TileType, Direction
Graphics/             GameRenderer (dessin), SpriteManager (feuille de sprites avec cache), HudLayout
Audio/                AudioEngine (NAudio, mélangeur + volume), SoundDirector (règles de lecture), SoundId, SoundVoice
```

Principes :

- Les écrans (`IScreen`) ne connaissent pas WinForms : le clavier est traduit en `InputKey` par `MainForm`.
- `ScreenManager` possède les crédits, le son et le high score de la session, et enchaîne les écrans
  (attract → start → partie → intermission → …).
- Tout ce qui est dessiné l'est dans un `Bitmap` natif de 224×288, agrandi sans lissage.

## Outils de développement (build Debug uniquement)

- `F9` : enregistre une démo. Une vraie partie est jouée et les touches sont notées ; le fichier `demo.txt` obtenu est
  à copier dans `asset\demo.txt` pour être embarqué. La démo est rejouée avec une graine fixe, le son coupé,
  et toujours avec 3 vies, quels que soient les réglages.
- Le titre de la fenêtre affiche alors le joueur, le score, les vies et les gommes restantes.

## Crédits

Pac-Man est une création de Namco (aujourd'hui Bandai Namco Entertainment) ; ce projet est un hommage sans but lucratif.
Les ressources (sprites et sons) sont fournies avec le projet, dans le dossier `asset\`, pour un usage personnel et non commercial.

- **Sprites** : feuille de sprites mise en ligne par Superjustinbros sur spriters-resource.com (l'auteur original
  n'est pas connu avec certitude). La feuille utilisée par le jeu en est une adaptation personnelle.
- **Sons** : pack mis en ligne par Inthegrave sur sounds.spriters-resource.com, avec comme contributeurs crédités
  125scratch et NamelessSpriter.
- **Audio** : [NAudio](https://github.com/naudio/NAudio).

## Hors périmètre

- Pas de pause, pas de sauvegarde du high score (comme à l'arcade).
- Pas de « kill screen » du niveau 256.