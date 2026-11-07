using System;
using System.Windows.Forms;

namespace PacmanWinForms
{
    internal static class Program
    {
        /// <summary>
        /// Point d'entrée principal de l'application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            MainForm form;
            try
            {
                form = new MainForm();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Erreur critique", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            Application.Run(form);
        }
    }
}
