# Pac-Man (C# / SDL3)

**Français** | [English](README.en.md)

Reproduction fidèle du Pac-Man d'arcade (1980) en C# sur SDL3 : même labyrinthe, mêmes règles, mêmes
comportements des fantômes, et tout l'habillage d'une borne : crédits, attract mode, démo, mode 2 joueurs
et les trois intermissions. Fonctionne sous Windows et Linux.

Projet personnel, non commercial. Pac-Man est une marque de Bandai Namco Entertainment.

## Prérequis

Pour **jouer** : rien à installer, il suffit de décompresser l'archive de votre plateforme (voir « Publication »).

Pour **compiler** :

- SDK .NET 10 (`net10.0`)
- Paquets NuGet `SDL3-CS` (liaisons C# de SDL3) et `SDL3-CS.Windows` / `SDL3-CS.Linux` / `SDL3-CS.MacOS`
  (bibliothèques natives), restaurés automatiquement à la compilation

Plateformes : Windows 64 bits et Linux 64 bits (glibc 2.28 ou plus, X11 ou Wayland). Le code est le même sur
macOS (non testé, non publié).

## Compilation et lancement

```
dotnet build -c Release
dotnet run --project PACMAN.Sdl -c Release
```

Le projet de départ est `PACMAN.Sdl`. L'exécutable produit s'appelle `PACMAN.exe` (Windows) ou `PACMAN` (Linux).

### Ressources attendues

Les ressources sont embarquées dans la bibliothèque `PACMAN.Core`. Elles doivent se trouver dans le dossier
`PACMAN.Core\asset\` :

| Fichier | Rôle |
|---|---|
| `asset\asset.png` | Feuille de sprites unique (personnages, texte, fruits, intermissions…) |
| `asset\maps\original_maze.txt` | Plan du labyrinthe |
| `asset\sounds\*.wav` | Sons de l'arcade (WAV PCM 16 bits, 48 kHz) |
| `asset\demo.txt` | Suite de touches de la démo (optionnel : sans lui, la démo n'est pas jouée) |

L'icône du programme est `PACMAN.Sdl\Icon.ico`.

## Publication

Le script `publier.ps1` (PowerShell, à lancer à la racine du dépôt) fabrique les archives à distribuer :

```
powershell -ExecutionPolicy Bypass -File .\publier.ps1 -Version 1.0
```

| Archive (dans `dist\`) | Contenu |
|---|---|
| `PACMAN-1.0-windows-x64.zip` | `PACMAN\PACMAN.exe` + `LISEZMOI.txt` (français) + `README.txt` (anglais) |
| `PACMAN-1.0-linux-x64.tar.gz` | `PACMAN/PACMAN` (droit d'exécution inclus) + `LISEZMOI.txt` + `README.txt` |

Chaque exécutable est un fichier unique autonome : le runtime .NET et la bibliothèque SDL3 sont inclus, il n'y a
rien à installer. Le script utilise les profils de publication `Windows-x64` et `Linux-x64` de
`PACMAN.Sdl\Properties\PublishProfiles\`, utilisables aussi seuls :

```
dotnet publish PACMAN.Sdl -p:PublishProfile=Windows-x64
dotnet publish PACMAN.Sdl -p:PublishProfile=Linux-x64
```

Les notices fournies aux utilisateurs sont dans `packaging\`, en français (`LISEZMOI-*.txt`) et en anglais (`README-*.txt`).

## Commandes

| Touche | Action |
|---|---|
| `5` | Insérer un crédit (jusqu'à 99) |
| `1` | Démarrer une partie à 1 joueur (1 crédit) |
| `2` | Démarrer une partie à 2 joueurs (2 crédits) |
| Flèches | Diriger Pac-Man |
| `Alt` + `Entrée` | Basculer plein écran / fenêtre (`Échap` pour quitter le plein écran) |
| `F12` / `F11` | Volume + / − (pas de 10 %) |

Comme à l'arcade, il n'y a pas de pause. Les touches `5`, `1` et `2` fonctionnent aussi au pavé numérique ; elles sont
reconnues par leur position sur le clavier, donc aussi sur un clavier AZERTY, sans `Maj`.

**Manette** : croix directionnelle ou stick gauche pour diriger, `Select` (Back) pour insérer un crédit,
`Start` ou `A` pour une partie à 1 joueur, `Y` pour 2 joueurs.

## Configuration : `PACMAN.ini`

Le fichier est créé automatiquement à côté de l'exécutable au premier lancement. Les modifications sont prises en compte
au lancement suivant ; une valeur invalide est ignorée (valeur par défaut). Si le dossier est en lecture seule, les
réglages valent pour la session seulement.

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
- **Fenêtre et plein écran** : l'image est agrandie par le plus grand multiple entier qui tient dans la fenêtre ou
  l'écran, centrée, avec des bandes noires, pour garder des pixels nets. La fenêtre est redimensionnable.
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

Deux projets : `PACMAN.Core`, la bibliothèque du jeu sans aucune dépendance à une plateforme, et `PACMAN.Sdl`, l'hôte
(fenêtre, clavier, manette, sortie audio) qui s'appuie sur SDL3.

```
PACMAN.Core/          Bibliothèque du jeu (net10.0)
  Game/               Game (machine à états d'une partie), PlayerState, ScoreKeeper, FruitManager, WaveScheduler,
                      GameConfig, LevelSpecs, Pacman
  Ghosts/             Ghost et les quatre fantômes, GhostHouse (nid), GhostMode / GhostPhase
  Maze/               LevelMap, MazeLayout, Tiles, TileType, Direction
  Screens/            IScreen + ScreenManager (crédits), AttractScreen, StartScreen, PlayScreen, DemoScreen,
                      ChaseScene, Intermission1/2/3Scene (IIntermission), ArcadeTexts, DemoRecording
  Graphics/           Canvas (image en mémoire), Sprite, PngDecoder, GameRenderer (dessin), SpriteManager
                      (feuille de sprites avec cache), HudLayout
  Audio/              IAudioEngine, MixerAudioEngine (mélangeur + volume), SoundDirector (règles de lecture),
                      SoundId, SoundVoice, WavDecoder
  Platform/           Settings (PACMAN.ini), DisplayConfig, InputKey, Assets (ressources embarquées)
  asset/              Feuille de sprites, labyrinthe, sons, démo
PACMAN.Sdl/           Hôte SDL3 (exécutable PACMAN)
  Program.cs          Point d'entrée, chargement des réglages
  SdlHost.cs          Fenêtre, boucle à pas fixe 60 Hz, affichage, clavier, manette, plein écran, volume
  SdlAudioOutput.cs   Sortie sonore SDL alimentée par MixerAudioEngine
packaging/            Notices fournies avec les archives (français et anglais)
publier.ps1           Fabrique les archives Windows (.zip) et Linux (.tar.gz)
```

Principes :

- `PACMAN.Core` ne connaît ni la fenêtre, ni le clavier, ni la carte son : le clavier et la manette sont traduits en
  `InputKey` par l'hôte, et les écrans (`IScreen`) ne voient que cela.
- `ScreenManager` possède les crédits, le son et le high score de la session, et enchaîne les écrans
  (attract → start → partie → intermission → …).
- Tout ce qui est dessiné l'est dans un `Canvas` natif de 224×288 (pixels ARGB en mémoire). L'hôte l'envoie dans une
  texture SDL qui l'agrandit sans lissage.
- Le son est mélangé dans `MixerAudioEngine` (décodage WAV maison, voix, enchaînement intro puis boucle sans trou) ;
  l'hôte vient simplement y chercher les échantillons pour les envoyer à la carte son.

## Outils de développement (build Debug uniquement)

- `F9` : enregistre une démo. Une vraie partie est jouée et les touches sont notées ; le fichier `demo.txt` obtenu est
  à copier dans `PACMAN.Core\asset\demo.txt` pour être embarqué. La démo est rejouée avec une graine fixe, le son coupé,
  et toujours avec 3 vies, quels que soient les réglages.
- Le titre de la fenêtre affiche le joueur, le score, les vies et les gommes restantes.

## Crédits

Pac-Man est une création de Namco (aujourd'hui Bandai Namco Entertainment) ; ce projet est un hommage sans but lucratif.
Les ressources (sprites et sons) sont fournies avec le projet, dans le dossier `PACMAN.Core\asset\`, pour un usage personnel et non commercial.

- **Sprites** : feuille de sprites mise en ligne par Superjustinbros sur spriters-resource.com (l'auteur original
  n'est pas connu avec certitude). La feuille utilisée par le jeu en est une adaptation personnelle.
- **Sons** : pack mis en ligne par Inthegrave sur sounds.spriters-resource.com, avec comme contributeurs crédités
  125scratch et NamelessSpriter.
- **Fenêtre, entrées, audio** : [SDL3](https://libsdl.org) et ses liaisons C# [SDL3#](https://github.com/edwardgushchin/SDL3-CS)
  (licence zlib).

## Hors périmètre

- Pas de pause, pas de sauvegarde du high score (comme à l'arcade).
- Pas de « kill screen » du niveau 256.
- Pas de paquet macOS (le code est portable, mais la signature et la notarisation d'Apple ne sont pas prises en charge).
