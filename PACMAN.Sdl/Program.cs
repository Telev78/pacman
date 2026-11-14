using System;
using System.Reflection;
using SDL3;

namespace PacmanSdl
{
    internal static class Program
    {
        private static int Main(string[] args)
        {
            // --version : uniquement sous Linux (sous Windows, la version est dans les propriétés du fichier)
            if (OperatingSystem.IsLinux() && args.Length > 0 && IsVersionOption(args[0]))
            {
                Console.WriteLine("Pac-Man " + GetVersion());
                return 0;
            }

            try
            {
                PacmanWinForms.Settings.Load();

                string game = GetGameOption(args);
                if (game != null) PacmanWinForms.Settings.SetGameOverride(game);

                using (SdlHost host = new SdlHost())
                {
                    host.Run();
                }
                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex);
                SDL.ShowSimpleMessageBox(SDL.MessageBoxFlags.Error, "Erreur critique", ex.Message, IntPtr.Zero);
                return 1;
            }
        }

        private static bool IsVersionOption(string arg)
        {
            return arg == "--version" || arg == "-v" || arg == "-V";
        }

        private static string GetVersion()
        {
            Assembly assembly = typeof(Program).Assembly;
            AssemblyInformationalVersionAttribute info = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>();
            if (info != null && !string.IsNullOrEmpty(info.InformationalVersion))
            {
                return info.InformationalVersion;
            }
            return assembly.GetName().Version.ToString(3);
        }

        private static string GetGameOption(string[] args)
        {
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "--game" && i + 1 < args.Length) return args[i + 1];
                if (args[i].StartsWith("--game=")) return args[i].Substring(7);
            }
            return null;
        }
    }
}