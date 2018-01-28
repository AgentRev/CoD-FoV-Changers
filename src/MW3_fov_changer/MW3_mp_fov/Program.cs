using System;
using System.Windows.Forms;

namespace MW3_fov_changer
{
    static class Program
    {
        /// <summary>
        /// Point d'entrée principal de l'application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            try
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new MainForm());
            }
            catch (Exception e)
            {
                MessageBox.Show(e.ToString() + "\n\n\nPlease take a screenshot of this error and send it to agentrevo@gmail.com\n\nThe application will now close.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
