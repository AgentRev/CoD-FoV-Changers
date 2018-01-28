using System.Windows.Forms;

namespace MW3_fov_changer
{
    public static class Multiplayer
    {
        //public static bool debug = false;

        public const string c_settingsDirName = "MW3 FoV Changer";
        public const string c_exe = "iw5mp";
        public const string c_exeDirectory = @"steamapps\common\call of duty modern warfare 3";
        public const string c_settingsFileName = "mp.ini";
        public const string c_gameID = "42690";
        public const string c_supportMessage = "MW3 MP executable (iw5mp.exe)";
        public const string c_manualMessage = "\nPlease start MW3 MP normally, and the FoV changer will enable itself.";
        public const string c_errorMessage = "An unexpected error occured while trying to start the game." + c_manualMessage;
        public const string c_notFoundMessage = "The game cannot be found." + c_manualMessage;
        

        public const string c_cVar = "cg_fov";
        public const int c_baseAddr = 0x400000;
        public const int c_pFoV = 0x05abd394;
        //public const byte c_pOffset = 0xC;
        public const byte c_checkRange = 0x40;
        public const long c_memSearchRange = 0x30000000;

        /*public const float c_FoV = 65f;
        public const float c_FoV_lowerLimit = 65f;
        public const float c_FoV_upperLimit = 100f;

        public const bool c_doBeep = true;
        public const bool c_updateChk = true;
        public const bool c_hotKeys = true;
        public static Keys[] c_catchKeys = { Keys.Subtract, Keys.Add, Keys.Multiply };*/
    }
}