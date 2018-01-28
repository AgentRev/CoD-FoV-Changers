using System;
//using System.Collections.Generic;
//using System.ComponentModel;
//using System.Data;
//using System.Drawing;
//using System.Linq;
//using System.Text;
using System.Windows.Forms;

namespace MW2_mp_fov
{
    public partial class GameNotFound : Form
    {
        public GameNotFound()
        {
            InitializeComponent();
            if (Environment.OSVersion.Version.Major < 6) { button1.Text = "OK"; }
        }
    }
}
