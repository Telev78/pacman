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
    }
}