using System.Windows.Forms;

namespace MW3_fov_changer
{
    public static class Singleplayer
    {
        //public static bool debug = false;

        public const string c_settingsDirName = "MW3 FoV Changer";
        public const string c_exe = "iw5sp";
        public const string c_exeDirectory = @"steamapps\common\call of duty modern warfare 3";
        public const string c_settingsFileName = "sp.ini";
        public const string c_gameID = "42680";
        public const string c_supportMessage = "MW3 SP executable (iw5sp.exe)";
        public const string c_manualMessage = "\nPlease start MW3 SP normally, and the FoV changer will enable itself.";
        public const string c_crackMessage = "\n\nIf you are using a cracked version of the game, search for the FoV tool made by \"mgr.inz.Player\", as this one will not work.";
        public const string c_errorMessage = "An unexpected error occured while trying to start the game." + c_manualMessage + c_crackMessage;
        public const string c_notFoundMessage = "The game cannot be found." + c_manualMessage + c_crackMessage;

        public const string c_cVar = "cg_fov";
        public const int c_baseAddr = 0x400000;
        public const int c_pFoV = 0x01c55998;
        //public const byte c_pOffset = 0xC;
        public const byte c_checkRange = 0x30;
        public const long c_memSearchRange = 0x60000000;

        /*public const float c_FoV = 65f;
        public const float c_FoV_lowerLimit = 65f;
        public const float c_FoV_upperLimit = 90f;

        public const bool c_doBeep = true;
        public const bool c_updateChk = true;
        public const bool c_hotKeys = true;
        public static Keys[] c_catchKeys = { Keys.Subtract, Keys.Add, Keys.Multiply };*/
    }
}