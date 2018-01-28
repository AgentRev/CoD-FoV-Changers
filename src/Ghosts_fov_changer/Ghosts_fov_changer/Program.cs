using System;
using System.Windows.Forms;

namespace Ghosts_FoV_Changer
{
    static class Program
    {
        /// <summary>
        /// Point d'entrée principal de l'application.
        /// </summary>
        [STAThread]
        static void Main()
        {
#if !DEBUG
            try
            {
#endif
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new MainForm());
#if !DEBUG
            }
            catch (Exception e)
            {
                MessageBox.Show(e.ToString() + "\n\n\nPlease take a screenshot of this error and send it to agentrevo@gmail.com\n\nThe application will now close.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
#endif
        }
    }
}
