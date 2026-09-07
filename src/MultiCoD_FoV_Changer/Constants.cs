namespace MultiCoD_FoV_Changer
{
    public static class Constants
    {
        public const string c_settingsDirName = "MultiCoD FoV Changer";
        public const string c_settingsFileName = "fov.ini";
        public static readonly string[] c_exes = {
            "iw3sp", "iw3mp", // MW1
            "CoDWaW", "CoDWaWmp", // WaW
            "iw4sp", "iw4mp", // MW2
            "BlackOps", "BlackOpsMP", // BO1
            "iw5sp", "iw5mp", // MW3
            "t6sp", "t6mp", "t6zm", // BO2
            "iw6sp64_ship", "iw6mp64_ship", // Ghosts
            "s1_sp64_ship", "s1_mp64_ship", // AW ??? untested
        };

        public const string c_cVar = "cg_fov";
        public const int c_memReadRange = 0x10000000; // first 256MB RAM
    }
}
