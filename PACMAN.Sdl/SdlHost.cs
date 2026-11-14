using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using PacmanWinForms;
using SDL3;

namespace PacmanSdl
{
    /// <summary>
    /// L'hôte SDL : fenêtre, boucle à 60 mises à jour par seconde, affichage de l'image du jeu (agrandie),
    /// clavier, manette et son. C'est l'équivalent de MainForm pour WinForms.
    /// </summary>
    internal sealed class SdlHost : IDisposable
    {
        // 60 mises à jour de jeu par seconde (16,67 ms, comme l'hôte WinForms), en nanosecondes
        private const double StepNs = 16670000.0;
        private const double MaxCatchUpNs = 100000000.0;   // Après une pause, on ne rattrape pas plus de 100 ms

        private const int AxisThreshold = 16000;           // Sur 32767 : seuil de la croix directionnelle analogique
        private const int VolumeShownFrames = 120;         // ≈ 2 secondes

        private readonly GameRenderer gameRenderer = new GameRenderer();
        private readonly MixerAudioEngine audio;
        private readonly ScreenManager screens;
        private readonly SdlAudioOutput audioOutput;

        private IntPtr window = IntPtr.Zero;
        private IntPtr sdlRenderer = IntPtr.Zero;
        private IntPtr texture = IntPtr.Zero;
        private IntPtr gamepad = IntPtr.Zero;

        private bool running = true;
        private bool fullscreen;
        private int volumeFrames;
        private string currentTitle = "Pac-Man";

        // Touches de service déjà enfoncées (pour ignorer la répétition automatique du clavier)
        private readonly HashSet<SDL.Scancode> heldServiceKeys = new HashSet<SDL.Scancode>();

        // Position actuelle du stick gauche, par axe : -1, 0 ou +1
        private int stickX, stickY;

        public SdlHost()
        {
            // Seule la vidéo est indispensable : sans carte son ni manette, le jeu tourne quand même
            if (!SDL.Init(SDL.InitFlags.Video))
                throw new InvalidOperationException("Initialisation de SDL impossible : " + SDL.GetError());
            SDL.InitSubSystem(SDL.InitFlags.Audio);
            SDL.InitSubSystem(SDL.InitFlags.Gamepad);

            int zoom = Settings.Zoom;
            window = SDL.CreateWindow("Pac-Man",
                DisplayConfig.NativeWidth * zoom, DisplayConfig.NativeHeight * zoom, SDL.WindowFlags.Resizable);
            if (window == IntPtr.Zero)
                throw new InvalidOperationException("Impossible de créer la fenêtre : " + SDL.GetError());

            // Sous Windows, le rendu OpenGL évite que les couleurs soient éclaircies sur un écran HDR
            // (les rendus Direct3D et Vulkan sont convertis différemment par Windows).
            // "software" sert de secours si OpenGL est indisponible.
            // Une variable d'environnement SDL_RENDER_DRIVER définie par l'utilisateur reste prioritaire.
            if (OperatingSystem.IsWindows())
            {
                SDL.SetHint("SDL_RENDER_DRIVER", "opengl,software");
            }

            sdlRenderer = SDL.CreateRenderer(window, null);
            if (sdlRenderer == IntPtr.Zero)
                throw new InvalidOperationException("Impossible de créer le moteur de rendu : " + SDL.GetError());

            // L'image du jeu (224x288) est envoyée dans une texture ; SDL l'agrandit d'un multiple entier
            // (plus proche voisin), centrée dans la fenêtre avec des bandes noires autour
            texture = SDL.CreateTexture(sdlRenderer, SDL.PixelFormat.ARGB8888, SDL.TextureAccess.Streaming,
                DisplayConfig.NativeWidth, DisplayConfig.NativeHeight);
            if (texture == IntPtr.Zero)
                throw new InvalidOperationException("Impossible de créer la texture : " + SDL.GetError());

            SetWindowIcon();

            SDL.SetTextureScaleMode(texture, SDL.ScaleMode.Nearest);
            SDL.SetRenderLogicalPresentation(sdlRenderer, DisplayConfig.NativeWidth, DisplayConfig.NativeHeight,
                SDL.RendererLogicalPresentation.IntegerScale);
            SDL.SetRenderVSync(sdlRenderer, 1);

            // Son
            audio = new MixerAudioEngine();
            audio.Volume = Settings.Volume / 100f;
            audioOutput = new SdlAudioOutput(audio);
            audio.OutputAvailable = audioOutput.Start();

            screens = new ScreenManager(audio, GameVariants.FindById(Settings.Game));
            screens.VariantChanged += gameRenderer.SetVariant;
            if (screens.Variant != null) gameRenderer.SetVariant(screens.Variant);
            screens.NotifyHandler = ShowNotification;

            if (Settings.StartFullscreen) SetFullscreen(true);
        }

        public void Run()
        {
            ulong last = SDL.GetTicksNS();
            double accumulated = 0;

            while (running)
            {
                PumpEvents();

                ulong now = SDL.GetTicksNS();
                double elapsed = now - last;
                last = now;

                if (elapsed > MaxCatchUpNs) elapsed = MaxCatchUpNs;
                accumulated += elapsed;

                while (accumulated >= StepNs)
                {
                    screens.Update();
                    accumulated -= StepNs;
                }

                if (volumeFrames > 0) volumeFrames--;

#if DEBUG
                UpdateTitle();
#endif
                Draw();
            }
        }

        // ------------------------------------------------------------------
        // Affichage
        // ------------------------------------------------------------------

        private void Draw()
        {
            Canvas frame = gameRenderer.Render(screens.Current);

            // Indicateur de volume, écrit avec la police de l'arcade, sous le bandeau des scores
            if (volumeFrames > 0)
            {
                string text = "VOLUME " + Settings.Volume;
                int column = (GameConfig.MapWidth - text.Length) / 2;
                gameRenderer.DrawText(frame, text, column, 2, SpriteManager.ArcadeColor.Yellow);
            }

            // Les pixels du Canvas sont en ARGB 32 bits : exactement le format de la texture
            SDL.UpdateTexture(texture, IntPtr.Zero, MemoryMarshal.AsBytes(frame.Pixels.AsSpan()), DisplayConfig.NativeWidth * 4);

            SDL.SetRenderDrawColor(sdlRenderer, 0, 0, 0, 255);
            SDL.RenderClear(sdlRenderer);
            SDL.RenderTexture(sdlRenderer, texture, IntPtr.Zero, IntPtr.Zero);
            SDL.RenderPresent(sdlRenderer);
        }

        // Icône de la fenêtre (Linux, macOS ; sous Windows l'icône de l'exécutable sert déjà) :
        // un Pac-Man tiré de la feuille de sprites, agrandi 4 fois
        private void SetWindowIcon()
        {
            try
            {
                const int scale = 4;
                const int size = 16 * scale;

                Sprite pacman = gameRenderer.Sprites.GetPacmanSprite(1, Direction.Right);
                uint[] pixels = new uint[size * size];
                for (int y = 0; y < size; y++)
                    for (int x = 0; x < size; x++)
                        pixels[y * size + x] = pacman.GetPixel(x / scale, y / scale);

                GCHandle handle = GCHandle.Alloc(pixels, GCHandleType.Pinned);
                try
                {
                    IntPtr surface = SDL.CreateSurfaceFrom(size, size, SDL.PixelFormat.ARGB8888, handle.AddrOfPinnedObject(), size * 4);
                    if (surface != IntPtr.Zero)
                    {
                        SDL.SetWindowIcon(window, surface);   // SDL en garde une copie
                        SDL.DestroySurface(surface);
                    }
                }
                finally
                {
                    handle.Free();
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Icône de fenêtre indisponible : " + ex.Message);
            }
        }

#if DEBUG
        // En Debug : les informations de partie dans la barre de titre, comme dans la version WinForms
        private void UpdateTitle()
        {
            string title;
            PlayScreen play = screens.Current as PlayScreen;

            if (play != null && play.Game != null)
            {
                Game game = play.Game;
                title = "Pac-Man Arcade (C#) - J" + game.ActivePlayer + " - SCORE: " + game.Score
                        + " - VIES: " + game.Lives + " - Gommes: " + game.DotsRemaining;
            }
            else if (screens.Current is DemoScreen demo && demo.IsRecording)
            {
                title = "Pac-Man Arcade (C#) - ENREGISTREMENT DE LA DÉMO";
            }
            else
            {
                title = "Pac-Man Arcade (C#) - CRÉDITS: " + screens.Credits;
            }

            if (title != currentTitle)
            {
                currentTitle = title;
                SDL.SetWindowTitle(window, title);
            }
        }
#endif

        private void SetFullscreen(bool value)
        {
            fullscreen = value;
            SDL.SetWindowFullscreen(window, value);
            if (value) SDL.HideCursor(); else SDL.ShowCursor();
        }

        private void ShowNotification(string title, string message)
        {
            // Bloque le jeu le temps que la boîte de dialogue est affichée
            SDL.ShowSimpleMessageBox(SDL.MessageBoxFlags.Information, title, message, window);
        }

        // ------------------------------------------------------------------
        // Événements : fenêtre, clavier, manette
        // ------------------------------------------------------------------

        private void PumpEvents()
        {
            SDL.Event e;
            while (SDL.PollEvent(out e))
            {
                switch ((SDL.EventType)e.Type)
                {
                    case SDL.EventType.Quit:
                    case SDL.EventType.WindowCloseRequested:
                        running = false;
                        break;

                    case SDL.EventType.KeyDown:
                        OnKeyDown(e.Key);
                        break;

                    case SDL.EventType.KeyUp:
                        heldServiceKeys.Remove(e.Key.Scancode);
                        break;

                    case SDL.EventType.GamepadAdded:
                        if (gamepad == IntPtr.Zero) gamepad = SDL.OpenGamepad(e.GDevice.Which);
                        break;

                    case SDL.EventType.GamepadRemoved:
                        if (gamepad != IntPtr.Zero && SDL.GetGamepadFromID(e.GDevice.Which) == gamepad)
                        {
                            SDL.CloseGamepad(gamepad);
                            gamepad = IntPtr.Zero;
                            stickX = 0;
                            stickY = 0;
                        }
                        break;

                    case SDL.EventType.GamepadButtonDown:
                        OnGamepadButton((SDL.GamepadButton)e.GButton.Button);
                        break;

                    case SDL.EventType.GamepadAxisMotion:
                        OnGamepadAxis((SDL.GamepadAxis)e.GAxis.Axis, e.GAxis.Value);
                        break;
                }
            }
        }

        private void OnKeyDown(SDL.KeyboardEvent key)
        {
            bool alt = (key.Mod & SDL.Keymod.Alt) != 0;

            if (alt && key.Scancode == SDL.Scancode.Return)
            {
                if (!key.Repeat) SetFullscreen(!fullscreen);
                return;
            }

            if (key.Scancode == SDL.Scancode.Escape)
            {
                if (fullscreen) SetFullscreen(false);
                else if (!key.Repeat) screens.OnInput(InputKey.Menu);
                return;
            }

            if (key.Scancode == SDL.Scancode.F11 || key.Scancode == SDL.Scancode.F12)
            {
                int volume = Settings.Volume + (key.Scancode == SDL.Scancode.F12 ? 10 : -10);
                Settings.SetVolume(volume);
                screens.Sound.Volume = Settings.Volume / 100f;
                volumeFrames = VolumeShownFrames;
                return;
            }

            InputKey input;
            if (!TryMapKey(key.Scancode, out input))
                return;

            // Les touches de service (crédit, start) ne se répètent pas quand on les maintient
            if (IsServiceKey(input) && !heldServiceKeys.Add(key.Scancode))
                return;

            screens.OnInput(input);
        }

        // Touches reconnues par leur position physique (Scancode) : "1", "2", "5" fonctionnent
        // donc aussi sur un clavier AZERTY, sans Maj
        private static bool TryMapKey(SDL.Scancode key, out InputKey input)
        {
            switch (key)
            {
                case SDL.Scancode.Up: input = InputKey.Up; return true;
                case SDL.Scancode.Down: input = InputKey.Down; return true;
                case SDL.Scancode.Left: input = InputKey.Left; return true;
                case SDL.Scancode.Right: input = InputKey.Right; return true;

                case SDL.Scancode.Alpha5:
                case SDL.Scancode.Kp5: input = InputKey.Coin; return true;

                case SDL.Scancode.Alpha1:
                case SDL.Scancode.Kp1: input = InputKey.Start1; return true;

                case SDL.Scancode.Alpha2:
                case SDL.Scancode.Kp2: input = InputKey.Start2; return true;
#if DEBUG
                case SDL.Scancode.F9: input = InputKey.Record; return true;
#endif
                default: input = InputKey.Coin; return false;
            }
        }

        private static bool IsServiceKey(InputKey input)
        {
            return input == InputKey.Coin || input == InputKey.Start1 || input == InputKey.Start2 || input == InputKey.Record;
        }

        private void OnGamepadButton(SDL.GamepadButton button)
        {
            switch (button)
            {
                case SDL.GamepadButton.DPadUp: screens.OnInput(InputKey.Up); break;
                case SDL.GamepadButton.DPadDown: screens.OnInput(InputKey.Down); break;
                case SDL.GamepadButton.DPadLeft: screens.OnInput(InputKey.Left); break;
                case SDL.GamepadButton.DPadRight: screens.OnInput(InputKey.Right); break;

                case SDL.GamepadButton.Back: screens.OnInput(InputKey.Coin); break;     // Select : insère un crédit
                case SDL.GamepadButton.Start:
                case SDL.GamepadButton.South: screens.OnInput(InputKey.Start1); break;  // Start ou A : 1 joueur
                case SDL.GamepadButton.North: screens.OnInput(InputKey.Start2); break;  // Y : 2 joueurs
            }
        }

        // Le stick gauche se comporte comme une croix : on envoie la direction quand il entre dans une zone
        private void OnGamepadAxis(SDL.GamepadAxis axis, short value)
        {
            if (axis == SDL.GamepadAxis.LeftX)
            {
                int zone = value < -AxisThreshold ? -1 : (value > AxisThreshold ? 1 : 0);
                if (zone != stickX && zone != 0) screens.OnInput(zone < 0 ? InputKey.Left : InputKey.Right);
                stickX = zone;
            }
            else if (axis == SDL.GamepadAxis.LeftY)
            {
                int zone = value < -AxisThreshold ? -1 : (value > AxisThreshold ? 1 : 0);
                if (zone != stickY && zone != 0) screens.OnInput(zone < 0 ? InputKey.Up : InputKey.Down);
                stickY = zone;
            }
        }

        public void Dispose()
        {
            audio.OutputAvailable = false;
            if (audioOutput != null) audioOutput.Dispose();
            if (screens != null) screens.Dispose();

            if (gamepad != IntPtr.Zero) SDL.CloseGamepad(gamepad);
            if (texture != IntPtr.Zero) SDL.DestroyTexture(texture);
            if (sdlRenderer != IntPtr.Zero) SDL.DestroyRenderer(sdlRenderer);
            if (window != IntPtr.Zero) SDL.DestroyWindow(window);
            SDL.Quit();
        }
    }
}
