using System;
using System.Windows.Forms;

namespace Ghosts_FoV_Changer
{
#if WIN64
    using dword_ptr = Int64;
#else
    using dword_ptr = Int32;
#endif

    public static class Constants
    {
        //public static bool debug = false;

        public const string c_settingsDirName = "Ghosts FoV Changer";
        public static readonly string[] c_exes = { "iw6mp64_ship", "iw6sp64_ship" };
        public const string c_settingsFileName = "fov.ini";

        public const string c_cVar = "cg_fov";
        public const dword_ptr c_memSearchRange = 0x90000000;
        public const dword_ptr c_memReadRange = 0x1000000;
        public const dword_ptr c_baseAddr =  0x0140000000;
        public const dword_ptr c_pFoV = 0x0145000000;
        public const byte c_checkRange = 0x40;
    }
}
