using System;
//using System.Collections.Generic;
using System.Windows.Forms;

namespace MW2_mp_fov
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
                MessageBox.Show(e.ToString() + "\n\n\nThe application will now close.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
