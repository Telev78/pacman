PAC-MAN (64-bit Linux)

Nothing to install: unpack the archive anywhere you like, then in a terminal:

    tar xzf PACMAN-*-linux-x64.tar.gz
    cd PACMAN
    ./PACMAN

(If the program refuses to start because of permissions: chmod +x PACMAN)

Nothing else is needed (the .NET runtime and SDL are included in the file).
Requirements: a recent distribution (glibc 2.28 or newer) with an X11 or Wayland display.

Sound: if "pw.conf ... client.conf" messages appear in the terminal, they come from PipeWire
and are harmless. To avoid them:  SDL_AUDIODRIVER=pulseaudio ./PACMAN

Settings (PACMAN.ini file, created on first launch next to the executable):
  Zoom        window size, from 1 to 8 (3 = 672x864)
  Fullscreen  true to start in full screen
  Volume      from 0 to 100
  Version     JAP (Namco) or USA (Midway)
  Lives       number of starting lives, from 1 to 5
  BonusLife   10000, 15000, 20000 or NONE

Controls:
  5 (or keypad 5)   insert a coin
  1 / 2             start a 1-player / 2-player game
  Arrow keys        steer Pac-Man
  F11 / F12         volume down / up
  Alt + Enter       full screen (Esc to leave it)
  Gamepad: D-pad or left stick = directions, Select = coin,
           Start or A = 1 player, Y = 2 players
