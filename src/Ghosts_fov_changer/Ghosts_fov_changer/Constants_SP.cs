using System;
using System.Windows.Forms;

namespace Ghosts_FoV_Changer
{
#if WIN64
    using dword_ptr = Int64;
#else
    using dword_ptr = Int32;
#endif

    public static class Singleplayer
    {
        //public static bool debug = false;

        public const string c_settingsDirName = "Ghosts FoV Changer";
        public const string c_exe = "iw6sp64_ship";
        public const string c_exeDirectory = @"steamapps\common\Call of Duty Ghosts";
        public const string c_settingsFileName = "sp.ini";
        public const string c_gameID = "209160";
        public const string c_supportMessage = "CoD:Ghosts SP executable (iw6sp64_ship.exe)";
        public const string c_manualMessage = "\nPlease start CoD:Ghosts SP normally, and the FoV changer will enable itself.";
        public const string c_errorMessage = "An unexpected error occured while trying to start the game." + c_manualMessage;
        public const string c_notFoundMessage = "The game cannot be found." + c_manualMessage;

        public const string c_cVar = "cg_fov";
        public const dword_ptr c_memSearchRange = 0xA0000000;
        public const dword_ptr c_baseAddr = 0x0140700000;
        public const dword_ptr c_pFoV = 0x01458EE6D0;
        public const byte c_checkRange = 0x30;
    }
}