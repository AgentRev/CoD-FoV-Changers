using System.Windows.Forms;

namespace MW2_mp_fov
{
    public partial class MainForm
    {
        //public static bool debug = false;

        //Multiplayer
        public const string c_projName = "MW2_mp_fov";
        public const string c_settingsDirName = "MW2 FoV Changer";
        public const string c_toolVer = "1.2.211.0";
        public const string c_exe = "iw4mp";
        public const string c_exeDirectory = @"steamapps\common\call of duty modern warfare 2";
        public const string c_settingsFileName = "mp.ini";
        public const string c_gameID = "10190";
        public const string c_supportMessage = "MW2 MP executable (iw4mp.exe)";
        public const string c_checkURL = "http://agentrevmw2fov.crabdance.com/";
        public const string c_manualMessage = "\nPlease start MW2 MP normally, and the FoV changer will enable itself.";
        public const string c_errorMessage = "An unexpected error occured while trying to start the game." + c_manualMessage;
        public const string c_notFoundMessage = "The game cannot be found." + c_manualMessage;


        public const string c_cVar = "cg_fov";
        public const uint c_baseAddr = 0x400000;
        public const int c_pFoV = 0x06395E1C;
        public const byte c_checkRange = 0x40;
        public const uint memSearchRange = 0x30000000;

        public const float c_FoV = 65.0f;
        public const int c_FoV_lowerLimit = 65;
        public const int c_FoV_upperLimit = 90;

        public const bool c_doBeep = true;
        public const bool c_updateChk = true;
        public const bool c_hotKeys = true;
        public static Keys[] c_catchKeys = { Keys.Subtract, Keys.Add, Keys.Multiply };
    }
}