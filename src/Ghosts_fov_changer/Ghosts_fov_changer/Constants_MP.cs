using System;
using System.Windows.Forms;

namespace Ghosts_FoV_Changer
{
#if WIN64
    using dword_ptr = Int64;
#else
    using dword_ptr = Int32;
#endif

    public static class Multiplayer
    {
        //public static bool debug = false;

        public const string c_settingsDirName = "Ghosts FoV Changer";
        public const string c_exe = "iw6mp64_ship";
        public const string c_exeDirectory = @"steamapps\common\Call of Duty Ghosts";
        public const string c_settingsFileName = "mp.ini";
        public const string c_gameID = "209170";
        public const string c_supportMessage = "CoD:Ghosts MP executable (iw6mp64_ship.exe)";
        public const string c_manualMessage = "\nPlease start CoD:Ghosts MP normally, and the FoV changer will enable itself.";
        public const string c_errorMessage = "An unexpected error occured while trying to start the game." + c_manualMessage;
        public const string c_notFoundMessage = "The game cannot be found." + c_manualMessage;
        
        public const string c_cVar = "cg_fov";
        public const dword_ptr c_memSearchRange = 0x90000000;
        public const dword_ptr c_baseAddr = 0x0140800000;
        public const dword_ptr c_pFoV = 0x0147912FF0;
        public const byte c_checkRange = 0x40;
    }
}