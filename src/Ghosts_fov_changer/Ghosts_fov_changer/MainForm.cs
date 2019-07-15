using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Media;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Web;
using System.Windows.Forms;
using Microsoft.Win32;


namespace Ghosts_FoV_Changer
{
#if WIN64
    using dword_ptr = Int64;
#else
    using dword_ptr = Int32;
#endif

    using DefaultGameMode = Multiplayer;

    #region struct KeyHook

    [StructLayout(LayoutKind.Sequential)]
    public struct KeyHook
    {
        public Keys Key;
        public int Code;
        public int flags;
        public int time;
        public IntPtr extra;
    }

    #endregion

    public partial class MainForm : Form
    {
        #region constants

        public const string c_toolVer = "3.15.2.0";

        public const float c_FoV = 65f;
        public const float c_FoV_lowerLimit = 65f;

#if ESL
        public const float c_FoV_upperLimit = 90f;
        public const string c_checkURL = "http://agentrevghostsfovesl.crabdance.com/";
#else
        public const float c_FoV_upperLimit = 100f;
        public const string c_checkURL = "http://agentrevghostsfov.crabdance.com/";
#endif

        public const byte c_pOffset = 0x10;

        public const bool c_doBeep = true;
        public const bool c_updateNotify = true;
        public const bool c_hotKeys = true;
        public static Keys[] c_catchKeys = { Keys.Subtract, Keys.Add, Keys.Multiply };

        #endregion

        #region settings

        static Type gameMode = typeof(Multiplayer);

        static string settingsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), DefaultGameMode.c_settingsDirName);
        static string settingsFile = Path.Combine(settingsPath, DefaultGameMode.c_settingsFileName);
        static string gameModeFile = Path.Combine(settingsPath, "gamemode.ini");

        // = gameMode.GetValue("c_FoV");

        //string toolVer = c_toolVer;
        dword_ptr pFoV; // = DefaultGameMode.c_pFoV;
        float fFoV; // = DefaultGameMode.c_FoV;
        bool doBeep; // = DefaultGameMode.c_doBeep;
        bool updateNotify; // = DefaultGameMode.c_updateNotify;
        bool hotKeys; // = DefaultGameMode.c_hotKeys;
        Keys[] catchKeys; // = DefaultGameMode.c_catchKeys;

        string exePath;
        bool gameFound = false;
        bool saveSettings = true;
        bool writeAllowed = false;
        bool currentlyReading = false;
        bool updateAvailable = false;
        bool firstTime = false;
        bool ignoreModeChanged = false;
        
        #endregion

        #region vars for hooks 'n other stuff
        //#####################################################################################################################

        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int id, LowLevelKeyboardProc callback, IntPtr hMod, uint dwThreadId);
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool UnhookWindowsHookEx(IntPtr hook);
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hook, int nCode, IntPtr wp, IntPtr lp);
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string name);
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern short GetAsyncKeyState(Keys key);
        [DllImport("kernel32", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern IntPtr LoadLibrary(string librayName);
        private LowLevelKeyboardProc objKeyboardProcess;
        private IntPtr ptrHook;
        private KeyHook theKey;

        private HttpWebRequest request;
        private bool requestSent;
        private Process proc = null;
        private Memory mem = null;

        //public delegate void Action();

        SoundPlayer sndGameFound = new SoundPlayer(Assembly.GetExecutingAssembly().GetManifestResourceStream(Assembly.GetExecutingAssembly().GetName().Name + ".Resources.gamefound.wav"));
        SoundPlayer sndGameLost = new SoundPlayer(Assembly.GetExecutingAssembly().GetManifestResourceStream(Assembly.GetExecutingAssembly().GetName().Name + ".Resources.gamelost.wav"));

        /*[DllImport("MW3_fov_lib.dll", EntryPoint = "SetVals", CallingConvention = CallingConvention.Cdecl)]
        public static extern void SetVals(uint nPid = 0, uint nBaseAddr = 0, byte nCheckRange = 0);
        [DllImport("MW3_fov_lib.dll", EntryPoint = "ReadFloat", CallingConvention = CallingConvention.Cdecl)]
        public static extern float ReadFloat(uint ptr);
        [DllImport("MW3_fov_lib.dll", EntryPoint = "WriteFloat", CallingConvention = CallingConvention.Cdecl)]
        public static extern void WriteFloat(uint ptr, float val);
        [DllImport("MW3_fov_lib.dll", EntryPoint = "FindFoVOffset", CallingConvention = CallingConvention.Cdecl)]
        public static extern bool FindFoVOffset(ref uint pFoV, ref byte step);*/

        //[DllImport("ntdll.dll", EntryPoint = "RtlAdjustPrivilege")]
        //public static extern long RtlAdjustPrivilege(long privilege, long bEnablePrivilege, long bCurrentThread, long OldState);

        bool isRunning(bool init)
        {
            Process[] spProcs = Process.GetProcessesByName(Singleplayer.c_exe);
            Process[] mpProcs = Process.GetProcessesByName(Multiplayer.c_exe);

            Process[] procs = new Process[mpProcs.Length + spProcs.Length];

            try
            {
                spProcs.CopyTo(procs, 0);
                mpProcs.CopyTo(procs, spProcs.Length);
            }
            catch { }

            if (procs.Length > 0)
            {
                if (proc == null && init)
                {
                    proc = procs[0];

                    if (proc.ProcessName == Singleplayer.c_exe && !rbSingleplayer.Checked)
                        rbSingleplayer.Checked = true;
                    else if (proc.ProcessName == Multiplayer.c_exe && !rbMultiplayer.Checked)
                        rbMultiplayer.Checked = true;

                    try
                    {
                        mem = new Memory((string)gameMode.GetValue("c_cVar"), proc.Id, (dword_ptr)gameMode.GetValue("c_baseAddr"), (byte)gameMode.GetValue("c_checkRange"), c_pOffset);
                    }
                    catch (Exception ex)
                    {
                        ErrMessage(ex);
                        Application.Exit();
                    }

                    TimerVerif.Start();
                }

                /*if (proc != null)
                {
                    proc.Refresh();
                    return !proc.HasExited;
                }*/
                
                return true;
            }
            else
            {
                if (proc != null && init)
                {
                    TimerVerif.Stop();
                    mem = null;
                    progStop();
                }
                return false;
            }
        }

        void TimerReset()
        {
            TimerHoldKey.Stop();
            TimerHoldKey.Interval = 350;
        }

        bool isOffsetWrong(dword_ptr ptr)
        {
            for (int i = 0x20; i < 0x30 /*(byte)gameMode.GetValue("c_checkRange")*/; i += 0x10)
            {
                //MessageBox.Show(ReadFloat(Increment(ptr, i)).ToString());
                try
                {
                    if (mem.ReadFloat(ptr + i) != c_FoV)
                        return true;
                }
                catch (Exception ex)
                {
                    ErrMessage(ex);
                    Application.Exit();
                }
            }

            return false;
        }

        public static string ProgramFilesx86()
        {
            if (IntPtr.Size == 8 || !String.IsNullOrEmpty(Environment.GetEnvironmentVariable("PROCESSOR_ARCHITEW6432")))
                return Environment.GetEnvironmentVariable("ProgramFiles(x86)");
            else
                return Environment.GetEnvironmentVariable("ProgramFiles");
        }

        private Keys currentKey;

        //#####################################################################################################################
        #endregion

        public MainForm()
        {
            InitializeComponent();

#if ESL
            this.Text = "ESL Ghosts FoV Changer";
#endif

            // string updtmess = HttpUtility.HtmlEncode(Regex.Escape("This is a test message\nhello\n123"));
            // MessageBox.Show(updtmess);
            // Clipboard.SetText(updtmess);

            ////this.numFoV.Maximum = Convert.ToDecimal(DefaultGameMode.c_FoV_upperLimit); //new decimal(new int[] { c_FoV_upperLimit, 0, 0, 0 });
            ////this.numFoV.Minimum = Convert.ToDecimal(DefaultGameMode.c_FoV_lowerLimit); //new decimal(new int[] { c_FoV_lowerLimit, 0, 0, 0 });

            //MessageBox.Show(pFoV.ToString("x8"));

            ////if (File.Exists(settingsFile)) ReadSettings();

            ////lblVersion.Text = "v" + c_toolVer;
            ////lblVersion.Visible = true;

            saveSettings = false;
            ReadGameMode();
            saveSettings = true;

            ProcessModule objCurrentModule = Process.GetCurrentProcess().MainModule;
            objKeyboardProcess = new LowLevelKeyboardProc(captureKey);
            ptrHook = SetWindowsHookEx(13, objKeyboardProcess, GetModuleHandle(objCurrentModule.ModuleName), 0);

            ////IsGameInstalled();

            //MessageBox.Show(Path.GetTempPath());
            //if (gameFound)
            //{
            /*string dirName = Path.Combine(Path.GetTempPath(), "MW3_fov_lib");
            if (!Directory.Exists(dirName))
                Directory.CreateDirectory(dirName);
            string dllPath = Path.Combine(dirName, "MW3_fov_lib.dll");

            using (Stream stm = Assembly.GetExecutingAssembly().GetManifestResourceStream(Assembly.GetExecutingAssembly().GetName().Name + ".Resources.MW3_fov_lib.dll"))
            {
                try
                {
                    using (Stream outFile = File.Create(dllPath))
                    {
                        const int sz = 4096;
                        byte[] buf = new byte[sz];
                        while (true)
                        {
                            int nRead = stm.Read(buf, 0, sz);
                            if (nRead < 1)
                                break;
                            outFile.Write(buf, 0, nRead);
                        }
                    }
                }
                catch { }
            }

            if (!debug)
            {
                IntPtr h = LoadLibrary(dllPath);
                if (h == IntPtr.Zero)
                {
                    MessageBox.Show("Unable to load library " + dllPath, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    Application.Exit();
                }
            }*/

            ////numFoV.Value = Convert.ToDecimal(fFoV);
            ////numFoV.Enabled = true;
            ////ToggleButton(!isRunning(false));

            TimerCheck.Start();

            //}
        }

        #region save/read settings

        void ReadSettings()
        {
            currentlyReading = true;
            StreamReader sr = null;
            string checkVer = c_toolVer;

            try
            {
                using (sr = new StreamReader(settingsFile))
                {
                    string line;
                    while ((line = sr.ReadLine()) != null)
                    {
                        try
                        {
                            int equalsign = line.IndexOf('=');
                            if (equalsign > 0)
                            {
                                string varName = line.Substring(0, equalsign);
                                string varValue = line.Substring(equalsign + 1);

                                if (varName == "ToolVersion" || varName == "GameVersion")
                                {
                                    checkVer = varValue;
                                }
                                else if (varName == "GameMode")
                                {
                                    rbSingleplayer.Checked = (varValue == "sp");
                                }
                                else if (varName == "Beep")
                                {
                                    chkBeep.Checked = bool.Parse(varValue);
                                }
                                else if (varName == "FoV")
                                {
                                    SetFoV(float.Parse(varValue));
                                }
                                else if (varName == "FoVOffset" || varName == "RelativeFoVOffset")
                                {
                                    dword_ptr tmp = dword_ptr.Parse(varValue, NumberStyles.AllowHexSpecifier);
                                    if (tmp > (dword_ptr)gameMode.GetValue("c_baseAddr"))
                                        pFoV = (varName == "RelativeFoVOffset" ? (dword_ptr)gameMode.GetValue("c_baseAddr") : 0) + tmp;
                                }
                                else if (varName == "UpdateNotify")
                                {
                                    chkUpdate.Checked = bool.Parse(varValue);
                                }
                                else if (varName == "DisableHotkeys")
                                {
                                    chkHotkeys.Checked = bool.Parse(varValue);
                                }
                                else if (varName == "HotkeyIncrease")
                                {
                                    catchKeys[0] = (Keys)int.Parse(varValue);
                                    btnKeyZoomOut.Text = VirtualKeyName(catchKeys[0]);
                                }
                                else if (varName == "HotkeyDecrease")
                                {
                                    catchKeys[1] = (Keys)int.Parse(varValue);
                                    btnKeyZoomIn.Text = VirtualKeyName(catchKeys[1]);
                                }
                                else if (varName == "HotkeyReset")
                                {
                                    catchKeys[2] = (Keys)int.Parse(varValue);
                                    btnKeyReset.Text = VirtualKeyName(catchKeys[2]);
                                }
                            }
                        }
                        catch { }
                    }
                }
            }
            finally
            {
                if (sr != null)
                    sr.Close();
            }

            if (checkVer != c_toolVer)
                pFoV = (dword_ptr)gameMode.GetValue("c_pFoV");

            UpdateCheck();

            currentlyReading = false;
        }

        void SaveSettings()
        {
            if (saveSettings && !currentlyReading)
            {
                try
                {
                    StreamWriter sw = null;

                    try
                    {
                        if (!Directory.Exists(settingsPath)) 
                            Directory.CreateDirectory(settingsPath);

                        using (sw = new StreamWriter(settingsFile))
                        {
                            sw.WriteLine("ToolVersion=" + c_toolVer);
                            sw.WriteLine("Beep=" + chkBeep.Checked);
                            sw.WriteLine("FoV=" + fFoV);
                            sw.WriteLine("FoVOffset=" + pFoV.ToString("x"));
                            sw.WriteLine("UpdateNotify=" + chkUpdate.Checked);
                            sw.WriteLine("DisableHotkeys=" + chkHotkeys.Checked);
                            sw.WriteLine("HotkeyIncrease=" + (int)catchKeys[0]);
                            sw.WriteLine("HotkeyDecrease=" + (int)catchKeys[1]);
                            sw.WriteLine("HotkeyReset=" + (int)catchKeys[2]);
                        }
                    }
                    catch
                    {
                        if (sw != null)
                            sw.Close();

                        File.Delete(settingsFile);
                        throw;
                    }

                    SaveGameMode();
                }
                catch
                {
                    saveSettings = false;
                }
            }
        }

        /*private void StringRead(string buffer, ref bool[] read, ref string checkVer)
        {
            if (!String.IsNullOrEmpty(buffer.Trim()))
            {
                if (buffer.StartsWith("ToolVersion=") || buffer.StartsWith("GameVersion="))
                {
                    checkVer = buffer.Substring(12);
                    read[0] = true;
                    //if (toolVer != c_toolVer) Checksum(settingsFile);
                }
                else if (buffer.StartsWith("Beep="))
                {
                    chkBeep.Checked = (buffer.Substring(5).ToLower() == "false") ? false : true;
                    read[1] = true;
                }
                else if (buffer.StartsWith("FoV="))
                {
                    SetFoV(float.Parse(buffer.Substring(4)));
                    read[2] = true;
                }
                else if (buffer.StartsWith("RelativeFoVOffset="))
                {
                    //MessageBox.Show((pFoV).ToString());

                    uint tmp = uint.Parse(buffer.Substring(18), NumberStyles.AllowHexSpecifier);
                    if (tmp > c_baseAddr && tmp < 0x40000000)
                        //MessageBox.Show(Increment(c_pFoV, -0x250000).ToString("x8"));
                        pFoV = c_baseAddr + tmp;
                    read[3] = true;

                    //MessageBox.Show((pFoV).ToString());
                }
                else if (buffer.StartsWith("FoVOffset="))
                {
                    uint tmp = uint.Parse(buffer.Substring(10), NumberStyles.AllowHexSpecifier);
                    if (tmp > c_baseAddr && tmp < 0x40000000)
                        pFoV = tmp;
                    read[3] = true;
                }
                else if (buffer.StartsWith("UpdatePopup=") || buffer.StartsWith("UpdateCheck="))
                {
                    chkUpdate.Checked = (buffer.Substring(12).ToLower() == "false") ? false : true;
                    read[4] = true;
                }
                else if (buffer.StartsWith("DisableHotkeys="))
                {
                    chkHotkeys.Checked = (buffer.Substring(15).ToLower() == "false") ? false : true;
                    read[5] = true;
                }
                else if (buffer.StartsWith("HotkeyIncrease="))
                {
                    catchKeys[0] = (Keys)int.Parse(buffer.Substring(15));
                    btnKeyZoomOut.Text = VirtualKeyName(catchKeys[0]);
                    read[6] = true;
                }
                else if (buffer.StartsWith("HotkeyDecrease="))
                {
                    catchKeys[1] = (Keys)int.Parse(buffer.Substring(15));
                    btnKeyZoomIn.Text = VirtualKeyName(catchKeys[1]);
                    read[7] = true;
                }
                else if (buffer.StartsWith("HotkeyReset="))
                {
                    catchKeys[2] = (Keys)int.Parse(buffer.Substring(12));
                    btnKeyReset.Text = VirtualKeyName(catchKeys[2]);
                    read[8] = true;
                }
                else throw new Exception("Invalid setting: " + buffer);
            }
        }*/

        void ReadGameMode()
        {
            saveSettings = false;
            ignoreModeChanged = true;

            try
            {
                if (!File.Exists(gameModeFile))
                {
                    firstTime = true;
                }

                using (StreamReader sr = new StreamReader(gameModeFile))
                {
                    string line = sr.ReadToEnd();

                    if (line.StartsWith("mp"))
                        rbMultiplayer.Checked = true;
                    else if (line.StartsWith("sp"))
                        rbSingleplayer.Checked = true;
                }
            }
            catch
            {
                rbMultiplayer.Checked = true;
            }

            GameModeChanged(false);

            ignoreModeChanged = false;
            saveSettings = true;
        }

        void SaveGameMode()
        {
            if (saveSettings && !currentlyReading)
            {
                try
                {
                    using (StreamWriter sw = new StreamWriter(gameModeFile))
                    {
                        if (rbMultiplayer.Checked)
                            sw.Write("mp");
                        else if (rbSingleplayer.Checked)
                            sw.Write("sp");
                    }
                }
                catch
                {
                    try
                    {
                        File.Delete(gameModeFile);
                    }
                    catch { }
                }
            }
        }

        #endregion

        #region methods

        void InitFovChanger()
        {
            settingsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), (string)gameMode.GetValue("c_settingsDirName"));
            settingsFile = Path.Combine(settingsPath, (string)gameMode.GetValue("c_settingsFileName"));
            gameModeFile = Path.Combine(settingsPath, "gamemode.ini");

            pFoV = (dword_ptr)gameMode.GetValue("c_pFoV");
            fFoV = c_FoV;
            doBeep = c_doBeep;
            updateNotify = c_updateNotify;
            hotKeys = c_hotKeys;
            catchKeys = (Keys[])c_catchKeys.Clone();

            this.numFoV.Maximum = Convert.ToDecimal(c_FoV_upperLimit);
            this.numFoV.Minimum = Convert.ToDecimal(c_FoV_lowerLimit);

            if (File.Exists(settingsFile)) ReadSettings();

            lblVersion.Text = "v" + c_toolVer;
            lblVersion.Visible = true;

            IsGameInstalled();

            numFoV.Value = Convert.ToDecimal(fFoV);
            numFoV.Enabled = true;
            ToggleButton(!isRunning(false));
        }

        void IsGameInstalled()
        {
            string lePath = Path.Combine(ProgramFilesx86(), @"Steam\" + gameMode.GetValue("c_exeDirectory") + @"\" + gameMode.GetValue("c_exe") + ".exe");
            TryPath(lePath);
            if (!gameFound)
            {
                RegistryKey steamSubKey = Registry.CurrentUser.OpenSubKey(@"Software\Valve\Steam\Apps\" + gameMode.GetValue("c_gameID"));
                if (steamSubKey != null)
                {
                    object installed = steamSubKey.GetValue("Installed");
                    if (installed != null && Convert.ToInt32(installed) > 0)
                    {
                        gameFound = true;
                    }
                }
            }
        }

        void GameModeChanged(bool save = true)
        {
            if (save)
                SaveSettings();

            if (rbMultiplayer.Checked)
                gameMode = typeof(Multiplayer);

            else if (rbSingleplayer.Checked)
                gameMode = typeof(Singleplayer);

            InitFovChanger();
            SaveSettings();

            //isRunning(false);
        }

        private void UpdateMem()
        {
            if (currentKey == catchKeys[0])
                SetFoV(fFoV + 1);
            else if (currentKey == catchKeys[1])
                SetFoV(fFoV - 1);
            else if (currentKey == catchKeys[2])
                SetFoV(c_FoV);
        }

        private void SetFoV(float val)
        {
            bool reset = val < 0 ? true : false;

            if (!reset)
            {
                if (val < c_FoV_lowerLimit)
                    fFoV = c_FoV_lowerLimit;
                else if (val > c_FoV_upperLimit)
                    fFoV = c_FoV_upperLimit;
                else
                    fFoV = val;
            }
            else
                SaveSettings();

            try
            {
                if (mem != null && isRunning(false) && writeAllowed)
                    mem.WriteFloat(pFoV, reset ? c_FoV : fFoV);
            }
            catch (Exception ex)
            {
                ErrMessage(ex);
                Application.Exit();
            }

            if (!reset)
            {
                numFoV.Value = Convert.ToDecimal(fFoV);
                SaveSettings();
            }
        }

        private void ToggleButton(bool state)
        {
            btnStartGame.Enabled = rbSingleplayer.Enabled = rbMultiplayer.Enabled = state;
            btnStartGame.Text = state ? "Start Game" : "Running";
        }

        private void UpdateNumBox()
        {
            SetFoV(Convert.ToSingle(numFoV.Value));
        }

        private void TryPath(string filePath)
        {
            if (File.Exists(filePath))
            {
                exePath = filePath;
                gameFound = true;
            }
        }

        private void progStart()
        {
            TimerVerif.Stop();
            writeAllowed = true;
            UpdateNumBox();
            TimerUpdate.Start();

            if (doBeep)
                sndGameFound.PlaySync();
        }

        private void progStop()
        {
            SetFoV(-1);

            if (proc != null && isRunning(false) && writeAllowed)
                mem = null;

            proc = null;

            if (writeAllowed && doBeep)
                sndGameLost.PlaySync();

            writeAllowed = false;
            TimerUpdate.Stop();

            if (!btnStartGame.Enabled)
                ToggleButton(true);
        }

        private long VersionNum(string data)
        {
            string[] separate = data.Split(new char[] { '.' }, 4);
            separate[1] = separate[1].PadLeft(4, '0');
            separate[2] = separate[2].PadLeft(4, '0');

            if (separate.Length == 4)
                separate[3] = separate[3].PadLeft(4, '0');
            else
                separate[2] = separate[2].PadRight(8, '0');

            long result;
            long.TryParse(separate[0] + separate[1] + separate[2] + (separate.Length == 4 ? separate[3] : ""), out result);

            return result;
        }

        public static string KeyName(Keys theKey)
        {
            switch (theKey)
            {
                case Keys.Back: return "Backspace";
                //case 9: return "Tab";
                //case 13: return "Enter";
                case Keys.CapsLock: return "CapsLock";
                case Keys.PageDown: return "PageDown";
                case Keys.D0: return "0";
                case Keys.D1: return "1";
                case Keys.D2: return "2";
                case Keys.D3: return "3";
                case Keys.D4: return "4";
                case Keys.D5: return "5";
                case Keys.D6: return "6";
                case Keys.D7: return "7";
                case Keys.D8: return "8";
                case Keys.D9: return "9";
                case Keys.Apps: return "Dropdown";
                case Keys.NumPad0: return "Numpad0";
                case Keys.NumPad1: return "Numpad1";
                case Keys.NumPad2: return "Numpad2";
                case Keys.NumPad3: return "Numpad3";
                case Keys.NumPad4: return "Numpad4";
                case Keys.NumPad5: return "Numpad5";
                case Keys.NumPad6: return "Numpad6";
                case Keys.NumPad7: return "Numpad7";
                case Keys.NumPad8: return "Numpad8";
                case Keys.NumPad9: return "Numpad9";
                case Keys.Multiply: return "Numpad*";
                case Keys.Add: return "Numpad+";
                case Keys.Subtract: return "Numpad−";
                case Keys.Decimal: return "Numpad.";
                case Keys.Divide: return "Numpad/";
                case Keys.LShiftKey: return "LShift";
                case Keys.RShiftKey: return "RShift";
                case Keys.LControlKey: return "LCtrl";
                case Keys.RControlKey: return "RCtrl";
                case Keys.LMenu: return "LAlt";
                case Keys.RMenu: return "RAlt";
                /*case 186: return ";";
                case 187: return "=";
                case 188: return ",";
                case 189: return "-";
                case 190: return ".";
                case 191: return "/";
                case 192: return "`";
                case 219: return "[";
                case 220: return @"\";
                case 221: return "]";
                case 222: return "'";*/
                default: return theKey.ToString();
            }
        }

        [DllImport("user32.dll")]
        public static extern uint MapVirtualKey(uint uCode, uint uMapType);

        public static string VirtualKeyName(Keys theKey)
        {
            uint nonVirtualKey = MapVirtualKey((uint)theKey, 2);

            if (nonVirtualKey > 0x80000000)
                nonVirtualKey -= 0x80000000; //UTF8 black magic

            if (nonVirtualKey > 0 && theKey > Keys.Enter && (theKey < Keys.NumPad0 || theKey > Keys.Divide))
                return Convert.ToChar(nonVirtualKey).ToString().ToUpper();

            else return KeyName(theKey);
        }

        private void ChgKey(uint key, string desc, Button btn)
        {
            ChangeKey chgKey = new ChangeKey(desc);

            if (chgKey.ShowDialog() == DialogResult.OK)
            {
                bool cancel = false;

                for (uint i = 0; i < catchKeys.Length; i++)
                {
                    if (i != key && catchKeys[i] == chgKey.PressedKey) cancel = true;
                }

                if (cancel)
                {
                    MessageBox.Show(this, "You cannot use a key that is already assigned.",
                                          "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else
                {
                    catchKeys[key] = chgKey.PressedKey;
                    btn.Text = chgKey.PressedKeyName;
                }
            }

            chgKey.Dispose();
            SaveSettings();
        }

        public void ErrMessage(Exception ex)
        {
            MessageBox.Show(this, "An unexpected error occured when attempting to access the game's process.\n\n" +
                                  "If the FoV changer has already worked for you before, please try to delete '" + gameMode.GetValue("c_settingsFileName") + "' located in " + settingsPath + "\n\n" +
                                  "Otherwise, please try to run the changer as an administator. If that doesn't work, " +
                                  "please contact me at agentrevo@gmail.com, and be sure to include a screenshot of this error:\n\n" + ex.ToString(),
                                  "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        #endregion

        private IntPtr captureKey(int nCode, IntPtr wp, IntPtr lp)
        {
            //using (StreamWriter sw = File.AppendText("keys.log"))
            //{
                //sw.WriteLine(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + "  nCode=" + nCode);

                if (nCode >= 0) //(proc != null && nCode >= 0)
                {
                    theKey = (KeyHook)Marshal.PtrToStructure(lp, typeof(KeyHook));
                    // Keyboard Hook Time /////////////////////////////////////////////////////////////////////////////////////////

                    //if (theKey.flags < 128) MessageBox.Show(theKey.key.ToString() + "\n" + ((int)theKey.key).ToString());

                    //sw.WriteLine(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + "  key=" + (int)theKey.Key + " code=" + theKey.Code + " flags=" + theKey.flags + " extra=" + theKey.extra);

                    if (hotKeys && ((IList<Keys>)catchKeys).Contains(theKey.Key))
                    {
                        if (theKey.flags < 128 && currentKey != theKey.Key)
                        {
                            TimerReset();
                            currentKey = theKey.Key;
                            UpdateMem();
                            TimerHoldKey.Start();
                        }
                        else if (theKey.flags >= 128)
                        {
                            TimerReset();
                            currentKey = Keys.None;
                            SaveSettings();
                        }
                    }

                    ///////////////////////////////////////////////////////////////////////////////////////////////////////////////
                }
                return CallNextHookEx(ptrHook, nCode, wp, lp);

            //}
        }

        #region timers

        private void TimerHoldKey_Tick(object sender, EventArgs e)
        {
            if (TimerHoldKey.Interval == 350) TimerHoldKey.Interval = 35;
            UpdateMem();
        }

        private void TimerUpdate_Tick(object sender, EventArgs e)
        {
            try
            {
                if (mem != null && isRunning(false))
                {
                    float readValue = mem.ReadFloat(pFoV);

                    if (readValue != fFoV && readValue >= c_FoV_lowerLimit)
                    {
                        mem.WriteFloat(pFoV, fFoV);
                    }
                }
            }
            catch (Exception ex)
            {
                ErrMessage(ex);
                Application.Exit();
            }
        }

        private void TimerCheck_Tick(object sender, EventArgs e)
        {
            isRunning(true);
        }

        private void TimerVerif_Tick(object sender, EventArgs e)
        {
            if (proc != null && mem != null && isRunning(false))
            {
                if (btnStartGame.Enabled) ToggleButton(false);
                proc.Refresh();

                try
                {
                    if (proc.PagedMemorySize64 > 0x2000000)
                    {
                        byte step = 0;

                        try
                        {
                            mem.FindFoVOffset(ref pFoV, ref step);

                            if (!isOffsetWrong(pFoV)) progStart();
                            else if (proc.PagedMemorySize64 > (dword_ptr)gameMode.GetValue("c_memSearchRange"))
                            {
                                TimerVerif.Stop();
                                TimerCheck.Stop();

                                //bool offsetFound = false;

                                //int ptrSize = IntPtr.Size * 4;
                                /*for (int i = -0x50000; i < 0x50000 && !offsetFound; i += 16)
                                {
                                    if (mem.ReadFloat(true, pFoV + i) == 65f && !isOffsetWrong(pFoV + i))
                                    {
                                        pFoV += i;
                                        offsetFound = true;
                                    }

                                    if (i % 50000 == 0)
                                    {
                                        label1.Text = i.ToString();
                                        Update();
                                    }
                                }*/

                                //Console.Beep(5000, 100);

                                //MessageBox.Show("find " + pFoV.ToString("X8"));
                                if (isRunning(false) && !mem.FindFoVOffset(ref pFoV, ref step))
                                {
                                    string memory = BitConverter.ToString(BitConverter.GetBytes(mem.ReadFloat(pFoV)));

                                    MessageBox.Show(this, "The memory research pattern wasn't able to find the FoV offset in your " + gameMode.GetValue("c_supportMessage") + ".\n" +
                                                          "Please look for an updated version of this FoV Changer tool.\n\n" +
                                                          "If you believe this might be a bug, please send me an email at agentrevo@gmail.com, and include a screenshot of this:\n" +
                                                          "\n" + c_toolVer +
                                                          "\nWorking Set: 0x" + proc.WorkingSet64.ToString("X8") +
                                                          "\nPaged Memory: 0x" + proc.PagedMemorySize64.ToString("X8") +
                                                          "\nVirtual Memory: 0x" + proc.VirtualMemorySize64.ToString("X8") +
                                                          "\nStep: " + step.ToString() +
                                                          "\nFoV pointer: 0x" + (pFoV - c_pOffset).ToString("X8") + " = " + memory,
                                                          "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);

                                    pFoV = (dword_ptr)gameMode.GetValue("c_pFoV");
                                    Application.Exit();
                                }
                                else
                                {
                                    //Console.Beep(5000, 100);
                                    SaveSettings();
                                    proc = null;
                                    TimerCheck.Start();
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            ErrMessage(ex);
                            Application.Exit();
                        }
                    }
                }
                catch (InvalidOperationException) { }
            }
        }

        private void UpdateResponse(IAsyncResult result)
        {
            try
            {
                HttpWebResponse response = (HttpWebResponse)request.EndGetResponse(result);
                Stream resStream = response.GetResponseStream();

                string tempString;
                int count;

                StringBuilder sb = new StringBuilder();
                byte[] buf = new byte[0x2000];

                do
                {
                    count = resStream.Read(buf, 0, buf.Length);
                    if (count != 0)
                    {
                        tempString = Encoding.ASCII.GetString(buf, 0, count);
                        sb.Append(tempString);
                    }
                }
                while (count > 0);

                string returnData = sb.ToString();

                string dataVer = Regex.Match(returnData, @"FoVChangerVer\[([0-9\.]*?)\]").Groups[1].Value;
                string dataSafe = Regex.Match(returnData, @"SafeToUse\[([A-Za-z]*?)\]").Groups[1].Value;
                string dataInfo = Regex.Unescape(HttpUtility.HtmlDecode(Regex.Match(returnData, @"UpdateInfo\[(.*?)\]").Groups[1].Value));
                string dataDownloadLink = Regex.Match(returnData, @"DownloadLink\[(.*?)\]").Groups[1].Value;
                string dataAnalytics = Regex.Match(returnData, @"GoogleAnalytics\[([A-Za-z\-0-9]*?)\]").Groups[1].Value;
                string dataIPService = Regex.Match(returnData, @"IPService\[(.*?)\]").Groups[1].Value;

                //MessageBox.Show(dataSafe);
                if (!String.IsNullOrEmpty(dataSafe) && dataSafe.ToLower() == "vacdetected")
                {
                    this.Invoke(new Action(() =>
                    {
                        DialogResult vacResult = MessageBox.Show(this, "It has been reported that this FoV Changer may cause anti-cheat software to trigger a ban. " +
                                                                       "For any information, please check github.com/AgentRev/CoD-FoV-Changers\n\n" +
                                                                       "Click 'OK' to exit the program, or 'Cancel' to continue using it AT YOUR OWN RISK.",
                                                                       "Detection alert", MessageBoxButtons.OKCancel, MessageBoxIcon.Error);

                        if (vacResult == DialogResult.OK)
                            Application.Exit();
                    }));
                }

                //MessageBox.Show(dataVer);
                if (!String.IsNullOrEmpty(dataVer) && VersionNum(dataVer) > VersionNum(c_toolVer))
                {
                    this.Invoke(new Action(() => 
                    {
                        updateAvailable = true;

                        if (chkUpdate.Checked && updateNotify)
                        {
                            MessageBox.Show(this, "Update v" + dataVer + " is available at MapModNews.com\nClicking the \"Help\" button below will take you to the download page." + (!String.IsNullOrEmpty(dataInfo) ? "\n\nInfos:\n" + dataInfo : ""),
                                                  "Update available", MessageBoxButtons.OK, MessageBoxIcon.Information,
                                                  MessageBoxDefaultButton.Button1, 0, (!String.IsNullOrEmpty(dataDownloadLink) ? dataDownloadLink : "http://ghostsfov.ftp.sh/"));

                            lblUpdateAvail.Text = "Update v" + dataVer + " available";
                            lblUpdateAvail.Enabled = true;
                            lblUpdateAvail.Visible = true;

                            TimerBlink.Start();
                        }
                        else
                        {
                            requestSent = false;
                        }
                    }));
                }

                if (!String.IsNullOrEmpty(dataAnalytics))
                {
                    GAnalytics.trackingID = dataAnalytics;
                }

                if (!String.IsNullOrEmpty(dataIPService))
                {
                    GAnalytics.ipService = dataIPService;
                }
            }
            catch {}

            try
            {
                GAnalytics.TriggerAnalytics((string)gameMode.GetValue("c_settingsDirName") + " v" + c_toolVer, firstTime);
            }
            catch {}
        }

        int n = 0;
        private void TimerBlink_Tick(object sender, EventArgs e)
        {
            if (n < 6)
            {
                lblUpdateAvail.Enabled = !lblUpdateAvail.Enabled;
                n++;
            }
            else TimerBlink.Stop();
        }

        #endregion

        #region events

        private void chkBeep_CheckedChanged(object sender, EventArgs e)
        {
            doBeep = chkBeep.Checked;
            SaveSettings();
        }

        private void numFoV_Leave(object sender, EventArgs e)
        {
            UpdateNumBox();
        }

        private void numFoV_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)Keys.Return)
            {
                UpdateNumBox();
                this.ActiveControl = null;
            }
        }

        private void MainForm_Deactivate(object sender, EventArgs e)
        {
            UpdateNumBox();
            this.ActiveControl = null;
        }

        private void MainForm_MouseClick(object sender, MouseEventArgs e)
        {
            if (this.ActiveControl != null)
            {
                UpdateNumBox();
                this.ActiveControl = null;
            }
        }

        private void btnStartGame_Click(object sender, EventArgs e)
        {
            if (gameFound)
            {
                try { Process.Start("steam://rungameid/" + (string)gameMode.GetValue("c_gameID")); }
                catch
                {
                    MessageBox.Show(this, (string)gameMode.GetValue("c_errorMessage"), "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            else
            {
                MessageBox.Show(this, (string)gameMode.GetValue("c_notFoundMessage"), "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void numFoV_ValueChanged(object sender, EventArgs e)
        {
            UpdateNumBox();
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (proc != null && isRunning(false)) progStop();
        }

        private void btnAbout_Click(object sender, EventArgs e)
        {
            MessageBox.Show(this, this.Text + " v" + c_toolVer + "\n" +
                                  "Made by AgentRev\n\n"+
                                  //"Approved by Infinity Ward and Valve\n\n" +
                                  "Contact email: agentrevo@gmail.com\n", //+
                                  //"Most emails are answered within 24h, or if you're lucky, 5 minutes.\n" +
                                  //"If your question is about any platform other than PC, the answer is 'no'.",
                                  "About", MessageBoxButtons.OK, MessageBoxIcon.Information, 
                                  MessageBoxDefaultButton.Button1,
                                  0, "http://www.callofduty.com/message/205674719");

        }

        private void btnExit_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void btnReset_Click(object sender, EventArgs e)
        {
            SetFoV(c_FoV);
        }

        private void UpdateCheck()
        {
#if !DEBUG
            try
            {
#endif
                if (!requestSent)
                {
                    request = (HttpWebRequest)WebRequest.Create(c_checkURL);
                    request.BeginGetResponse(new AsyncCallback(UpdateResponse), null);

                    requestSent = true;
                }
                else
                {
                    if (updateNotify && updateAvailable)
                    {
                        lblUpdateAvail.Visible = true;
                    }
                    else
                    {
                        lblUpdateAvail.Visible = false;
                    }
                }
#if !DEBUG
            }
            catch { }
#endif
        }

        private void chkUpdate_CheckedChanged(object sender, EventArgs e)
        {
            updateNotify = chkUpdate.Checked;
            UpdateCheck();
        }

        private void lblLink_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start("https://github.com/AgentRev/CoD-FoV-Changers");
        }

        private void chkHotkeys_CheckedChanged(object sender, EventArgs e)
        {
            hotKeys = !chkHotkeys.Checked;
            TimerReset();
            currentKey = Keys.None;
            SaveSettings();
        }

        private void btnKeyZoomOut_Click(object sender, EventArgs e)
        {
            ChgKey(0, "Zoom out", btnKeyZoomOut);
        }

        private void btnKeyZoomIn_Click(object sender, EventArgs e)
        {
            ChgKey(1, "Zoom in", btnKeyZoomIn);
        }

        private void btnKeyReset_Click(object sender, EventArgs e)
        {
            ChgKey(2, "Reset to default", btnKeyReset);
        }

        private void rbGameMode_CheckedChanged(object sender, EventArgs e)
        {
            if (!ignoreModeChanged)
                GameModeChanged();
        }

        #endregion
    }

    public static class TypeExtensionMethods
    {
        public static object GetValue(this Type gameType, string field)
        {
            return gameType.GetField(field, BindingFlags.Public | BindingFlags.Static).GetValue(null);
        }
    }
}
