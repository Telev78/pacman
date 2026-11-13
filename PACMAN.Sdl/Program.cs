using System;
using SDL3;

namespace PacmanSdl
{
    internal static class Program
    {
        private static int Main()
        {
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
    }
}
