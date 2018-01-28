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
using System.Windows.Forms;
using Microsoft.Win32;

namespace MW2_mp_fov
{
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
        #region settings

        static string settingsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), c_settingsDirName);
        static string settingsFile = Path.Combine(settingsPath, c_settingsFileName);

        //string toolVer = c_toolVer;
        uint pFoV = c_pFoV;
        float fFoV = c_FoV;
        bool doBeep = c_doBeep;
        bool updateChk = c_updateChk;
        bool hotKeys = c_hotKeys;
        Keys[] catchKeys = c_catchKeys;

        string exePath;
        bool gameFound = false;

        bool saveSettings = true;
        bool writeAllowed = false;
        bool currentlyReading = false;

        #endregion

        #region (deprecated) sniper scopes offsets

        /*int[] pSnipersFoV = {0x24180C48,     // Barrett
                               0x24189A10,     // L118
                               0x2417BF54,     // Dragunov
                               0x2417E284,     // AS50
                               0x24179530,     // RSASS
                               0x24181B20};    // MSR

        float[] fSnipersFoV = {15,   // Barrett
                               15,   // L118
                               15,   // Dragunov
                               30,   // AS50
                               15,   // RSASS
                               15};  // MSR*/

        #endregion

        public MainForm()
        {
            InitializeComponent();

            this.numFoV.Maximum = new decimal(new int[] { c_FoV_upperLimit, 0, 0, 0 });
            this.numFoV.Minimum = new decimal(new int[] { c_FoV_lowerLimit, 0, 0, 0 });

            //MessageBox.Show(pFoV.ToString("x8"));

            if (File.Exists(settingsFile)) ReadData();

            lblVersion.Text = "v" + c_toolVer;
            lblVersion.Visible = true;

            ProcessModule objCurrentModule = Process.GetCurrentProcess().MainModule;
            objKeyboardProcess = new LowLevelKeyboardProc(captureKey);
            ptrHook = SetWindowsHookEx(13, objKeyboardProcess, GetModuleHandle(objCurrentModule.ModuleName), 0);

            string tryPath = Path.Combine(ProgramFilesx86(), @"Steam\" + c_exeDirectory + @"\" + c_exe + ".exe");
            TryPath(tryPath);
            if (!gameFound)
            {
                object steamPath = Registry.CurrentUser.OpenSubKey(@"Software\Valve\Steam").GetValue("SteamPath");
                if (steamPath != null && !String.IsNullOrEmpty(steamPath.ToString()))
                {
                    tryPath = Path.Combine(steamPath.ToString(), c_exeDirectory + @"\" + c_exe + ".exe");
                    TryPath(tryPath);
                }
            }

            //MessageBox.Show(Path.GetTempPath());
            //if (gameFound)
            //{
                /*string dirName = Path.Combine(Path.GetTempPath(), "MW3_fov_lib");
                if (!Directory.Exists(dirName))
                    Directory.CreateDirectory(dirName);
                string dllPath = Path.Combine(dirName, "MW3_fov_lib.dll");

                using (Stream stm = Assembly.GetExecutingAssembly().GetManifestResourceStream(c_projName + ".Resources.MW3_fov_lib.dll"))
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

                numFoV.Value = (decimal)fFoV;
                numFoV.Enabled = true;
                ToggleButton(!isRunning(false));

                TimerCheck.Start();
            //}
        }

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
        private extern static IntPtr LoadLibrary(string librayName);
        private LowLevelKeyboardProc objKeyboardProcess;
        private IntPtr ptrHook;
        private KeyHook theKey;

        private HttpWebRequest request;
        private bool requestSent;
        private Process proc = null;
        private Memory mem = null;

        SoundPlayer sndGameFound = new SoundPlayer(Assembly.GetExecutingAssembly().GetManifestResourceStream(c_projName + ".Resources.gamefound.wav"));
        SoundPlayer sndGameLost = new SoundPlayer(Assembly.GetExecutingAssembly().GetManifestResourceStream(c_projName + ".Resources.gamelost.wav"));

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
            Process[] procs = Process.GetProcessesByName(c_exe);

            if (procs.Length > 0)
            {
                if (proc == null && init)
                {
                    proc = procs[0];

                    try
                    {
                        mem = new Memory(c_cVar, proc.Id, c_baseAddr, c_checkRange);
                    }
                    catch (Exception err)
                    {
                        ErrMessage(err);
                        Application.Exit();
                    }

                    TimerVerif.Start();
                }
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
            TimerHoldKey.Interval = 500;
        }

        bool isOffsetWrong(uint ptr)
        {
            for (uint i = 0x20; i < c_checkRange; i += 0x10)
            {
                //MessageBox.Show(ReadFloat(Increment(ptr, i)).ToString());
                try
                {
                    if (mem.ReadFloat(ptr + i) != c_FoV)
                        return true;
                }
                catch (Exception err)
                {
                    ErrMessage(err);
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

        #region save/read settings

        private void SaveData()
        {
            if (saveSettings && !currentlyReading)
            {
                try
                {
                    StreamWriter sw = null;
                    try
                    {
                        if (!Directory.Exists(settingsPath)) Directory.CreateDirectory(settingsPath);
                        sw = new StreamWriter(Path.Combine(settingsPath, settingsFile));
                        sw.WriteLine("ToolVersion=" + c_toolVer);
                        sw.WriteLine("Beep=" + chkBeep.Checked.ToString().ToLower());
                        sw.WriteLine("FoV=" + ((int)fFoV).ToString());
                        sw.WriteLine("FoVOffset=" + pFoV.ToString("x"));
                        sw.WriteLine("UpdatePopup=" + chkUpdate.Checked.ToString().ToLower());
                        sw.WriteLine("DisableHotkeys=" + chkHotkeys.Checked.ToString().ToLower());
                        sw.WriteLine("HotkeyIncrease=" + (int)catchKeys[0]);
                        sw.WriteLine("HotkeyDecrease=" + (int)catchKeys[1]);
                        sw.WriteLine("HotkeyReset=" + (int)catchKeys[2]);
                    }
                    catch
                    {
                        //MessageBox.Show("catch");
                        if (sw != null) sw.Close();
                        File.Delete(settingsFile);
                        saveSettings = false;
                    }
                    finally
                    {
                        if (sw != null) sw.Close();
                    }
                }
                catch
                {
                    saveSettings = false;
                }
            }
        }

        private void StringRead(string buffer, ref bool[] read, ref string checkVer)
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
                    uint tmp = uint.Parse(buffer.Substring(0), NumberStyles.AllowHexSpecifier);
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
        }

        private void ReadData()
        {
            currentlyReading = true;
            StreamReader sr = null;
            string checkVer = c_toolVer;
            bool[] read = { false, false, false, false, false, false,
                            false, false, false };

            try
            {
                sr = new StreamReader(settingsFile);

                while (sr.Peek() > -1)
                {
                    StringRead(sr.ReadLine(), ref read, ref checkVer);
                }
            }
            catch
            {
                //if (!read[0]) toolVer = c_toolVer;

                /*if (!read[1]) pFoV = c_pFoV;
                if (!read[2]) fFoV = c_FoV;
                if (!read[3]) doBeep = c_doBeep;
                if (!read[4]) updateChk = c_updateChk;
                if (!read[5]) hotKeys = c_hotKeys;

                if (!read[6]) catchKeys[0] = c_catchKeys[0];
                if (!read[7]) catchKeys[1] = c_catchKeys[1];
                if (!read[8]) catchKeys[2] = c_catchKeys[2];*/
            }
            finally
            {
                if (sr != null) sr.Close();
            }

            if (checkVer != c_toolVer)
                pFoV = c_pFoV;

            if (!requestSent)
            {
                try
                {
                    request = (HttpWebRequest)WebRequest.Create(c_checkURL);
                    request.BeginGetResponse(new AsyncCallback(UpdateResponse), null);
                    requestSent = true;
                }
                catch { }
            }

            currentlyReading = false;
        }

        /* For most updates, only game files are modified, not the EXE itself
        private string Checksum(string fileName)
        {
            if (File.Exists(fileName))
            {
                FileStream file = new FileStream(fileName, FileMode.Open, FileAccess.Read);
                MD5 md5 = new MD5CryptoServiceProvider();
                byte[] retVal = md5.ComputeHash(file);
                file.Close();
                //MessageBox.Show(BitConverter.ToString(retVal) + "\n" + c_exeMD5);
                return BitConverter.ToString(retVal);
            }
            return "";
        }*/

        #endregion

        #region methods

        private void UpdateMem()
        {
            if (currentKey == catchKeys[0]) SetFoV(fFoV + 5);
            else if (currentKey == catchKeys[1]) SetFoV(fFoV - 5);
            else if (currentKey == catchKeys[2]) SetFoV(c_FoV);
        }

        private void SetFoV(float val)
        {
            bool reset = val < 0 ? true : false;

            if (!reset)
            {
                if (val < c_FoV_lowerLimit) fFoV = c_FoV_lowerLimit;
                else if (val > c_FoV_upperLimit) fFoV = c_FoV_upperLimit;
                else fFoV = val;
            }

            try
            {
                if (mem != null && isRunning(false) && writeAllowed)
                    mem.WriteFloat(pFoV, reset ? c_FoV : fFoV);
            }
            catch (Exception err)
            {
                ErrMessage(err);
                Application.Exit();
            }

            if (!reset) numFoV.Value = (decimal)fFoV;
            SaveData();
        }

        private void ToggleButton(bool state)
        {
            btnStartGame.Enabled = state;
            btnStartGame.Text = state ? "Start Game" : "Running";
        }

        private void UpdateNumBox()
        {
            SetFoV((float)numFoV.Value);
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

            if (doBeep) sndGameFound.PlaySync();
        }

        private void progStop()
        {
            SetFoV(-1);
            if (proc != null && isRunning(false) && writeAllowed) mem = null;
            proc = null;
            if (writeAllowed && doBeep) sndGameLost.PlaySync();
            writeAllowed = false;
            TimerUpdate.Stop();

            if (!btnStartGame.Enabled) ToggleButton(true);
        }

        private long VersionNum(string data)
        {
            string[] separate = data.Split(new char[] { '.' }, 4);
            separate[1] = separate[1].PadLeft(4, '0');
            separate[2] = separate[2].PadLeft(4, '0');
            if (separate.Length == 4) separate[3] = separate[3].PadLeft(4, '0');
            else separate[2] = separate[2].PadRight(8, '0');

            long result;
            long.TryParse(separate[0] + separate[1] + separate[2] + (separate.Length == 4 ? separate[3] : ""), out result);

            return result;
        }

        public static string KeyName(Keys theKey)
        {
            switch ((int)theKey)
            {
                case 8: return "Backspace";
                case 9: return "Tab";
                case 13: return "Enter";
                case 20: return "CapsLock";
                case 34: return "PageDown";
                case 48: return "0";
                case 49: return "1";
                case 50: return "2";
                case 51: return "3";
                case 52: return "4";
                case 53: return "5";
                case 54: return "6";
                case 55: return "7";
                case 56: return "8";
                case 57: return "9";
                case 93: return "Dropdown";
                case 96: return "Numpad0";
                case 97: return "Numpad1";
                case 98: return "Numpad2";
                case 99: return "Numpad3";
                case 100: return "Numpad4";
                case 101: return "Numpad5";
                case 102: return "Numpad6";
                case 103: return "Numpad7";
                case 104: return "Numpad8";
                case 105: return "Numpad9";
                case 106: return "Numpad*";
                case 107: return "Numpad+";
                case 109: return "Numpad−";
                case 110: return "Numpad.";
                case 111: return "Numpad/";
                case 160: return "LShift";
                case 161: return "RShift";
                case 162: return "LCtrl";
                case 163: return "RCtrl";
                case 164: return "LAlt";
                case 165: return "RAlt";
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

        [DllImport("user32.dll", EntryPoint = "MapVirtualKey")]
        static extern int MapVirtualKey(uint uCode, uint uMapType);

        public static string VirtualKeyName(Keys theKey)
        {
            int nonVirtualKey = MapVirtualKey((uint)theKey, 2);

            if (nonVirtualKey > 0 && theKey > Keys.Enter && (theKey < Keys.NumPad0 || theKey > Keys.Divide))
                return Convert.ToChar(nonVirtualKey).ToString().ToUpper();

            else if (nonVirtualKey < 0)
                return Convert.ToChar(nonVirtualKey + 0x80000000).ToString(); //UTF8 black magic

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
                    MessageBox.Show("You cannot use a key that is already assigned.",
                                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else
                {
                    catchKeys[key] = chgKey.PressedKey;
                    btn.Text = chgKey.PressedKeyName;
                }
            }

            chgKey.Dispose();
            SaveData();
        }

        public void ErrMessage(Exception err)
        {
            MessageBox.Show("An unexpected error occured when attempting to access the game's process.\n\n" +
                            "If the FoV changer has already worked for you before, please try to delete '" + c_settingsFileName + "' located in " + settingsPath + "\n\n" +
                            "Otherwise, please try to run the changer as an administator. If that doesn't work, " +
                            "please contact me at agentrevo@gmail.com, and be sure to include a screenshot of this error:\n\n" + err.Message,
                            "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        #endregion

        private IntPtr captureKey(int nCode, IntPtr wp, IntPtr lp)
        {
            if (proc != null && nCode >= 0)
            {
                theKey = (KeyHook)Marshal.PtrToStructure(lp, typeof(KeyHook));
                // Keyboard Hook Time /////////////////////////////////////////////////////////////////////////////////////////

                //if (theKey.flags < 128) MessageBox.Show(theKey.key.ToString() + "\n" + ((int)theKey.key).ToString());

                if (((IList<Keys>)catchKeys).Contains(theKey.Key) && hotKeys)
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
                        SaveData();
                    }
                }

                ///////////////////////////////////////////////////////////////////////////////////////////////////////////////
            }
            return CallNextHookEx(ptrHook, nCode, wp, lp);
        }

        #region timers

        private void TimerHoldKey_Tick(object sender, EventArgs e)
        {
            if (TimerHoldKey.Interval == 500) TimerHoldKey.Interval = 150;
            UpdateMem();
        }

        private void TimerUpdate_Tick(object sender, EventArgs e)
        {
            try
            {
                if (mem != null && isRunning(false)) mem.WriteFloat(pFoV, fFoV);
            }
            catch (Exception err)
            {
                ErrMessage(err);
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

                if (proc.PagedMemorySize64 > 0x2000000)
                {
                    byte step = 0;

                    try
                    {
                        mem.FindFoVOffset(ref pFoV, ref step);

                        if (!isOffsetWrong(pFoV)) progStart();
                        else if (proc.PagedMemorySize64 > memSearchRange)
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

                                MessageBox.Show("The memory research pattern wasn't able to find the FoV offset in your " + c_supportMessage + ".\n" +
                                                "Please look for an updated version of this FoV Changer tool.\n\n" +
                                                "If you believe this might be a bug, please send me an email at agentrevo@gmail.com, and include a screenshot of this:\n\n" + c_toolVer +
                                                "\n0x" + Convert.ToInt32(proc.WorkingSet64).ToString("X8") +
                                                "\n0x" + Convert.ToInt32(proc.PagedMemorySize64).ToString("X8") +
                                                "\n0x" + Convert.ToInt32(proc.VirtualMemorySize64).ToString("X8") +
                                                "\nStep = " + step.ToString() +
                                                "\n0x" + (pFoV - 0xC).ToString("X8") + " = " + memory,
                                                "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);

                                pFoV = c_pFoV;
                                Application.Exit();
                            }
                            else
                            {
                                //Console.Beep(5000, 100);
                                SaveData();
                                proc = null;
                                TimerCheck.Start();
                            }
                        }
                    }
                    catch (Exception err)
                    {
                        ErrMessage(err);
                        Application.Exit();
                    }
                }
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
                byte[] buf = new byte[0x1000];

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

                string dataVer = Regex.Match(sb.ToString(), @"FoVChangerVer\[([0-9\.]+)\]").Groups[1].Value;
                string dataSafe = Regex.Match(sb.ToString(), @"SafeToUse\[([A-Za-z]+)\]").Groups[1].Value;


                //MessageBox.Show(dataVer);
                if (!String.IsNullOrEmpty(dataSafe) && dataSafe.ToLower() == "vacdetected")
                {
                    DialogResult vacResult = MessageBox.Show("It has been reported that this FoV Changer may cause Valve Anti-Cheat to trigger a ban. " +
                                                             "For any information, please check MapModNews.com.\n\n" +
                                                             "Click 'OK' to exit the program, or 'Cancel' to continue using it AT YOUR OWN RISK.",
                                                             "Detection alert", MessageBoxButtons.OKCancel, MessageBoxIcon.Error);

                    if (vacResult == DialogResult.OK) Application.Exit();
                }

                //MessageBox.Show(dataVer);
                if (!String.IsNullOrEmpty(dataVer) && VersionNum(dataVer) > VersionNum(c_toolVer))
                {
                    lblUpdateAvail.Text = "└ Update v" + dataVer + " available";
                    lblUpdateAvail.Enabled = true;
                    lblUpdateAvail.Visible = true;

                    if (updateChk) MessageBox.Show("Update v" + dataVer + " for the FoV Changer is available at MapModNews.com",
                                                   "Update available", MessageBoxButtons.OK, MessageBoxIcon.Information);

                    TimerBlink.Start();
                }
            }
            catch { }

            requestSent = false;
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
            SaveData();
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
                try { Process.Start("steam://rungameid/" + c_gameID); }
                catch
                {
                    MessageBox.Show(c_errorMessage, "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            else
            {
                MessageBox.Show(c_notFoundMessage, "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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
            MessageBox.Show(this.Text + " v" + c_toolVer + "\n" +
                            "Made by AgentRev\n\n" +
                            "Contact/support email: agentrevo@gmail.com\n" +
                            "Most emails are answered within 24h, or if you're lucky, 5 minutes.\n" +
                            "If your question is about any platform other than PC, the answer is 'no'.",
                            "About", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void btnExit_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void btnReset_Click(object sender, EventArgs e)
        {
            SetFoV(c_FoV);
        }

        private void chkUpdate_CheckedChanged(object sender, EventArgs e)
        {
            updateChk = chkUpdate.Checked;
            if (updateChk && !requestSent)
            {
                request = (HttpWebRequest)WebRequest.Create(c_checkURL);
                request.BeginGetResponse(new AsyncCallback(UpdateResponse), null);
                requestSent = true;
            }
        }

        private void lblLink_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start("http://www.mapmodnews.com/");
        }

        private void chkHotkeys_CheckedChanged(object sender, EventArgs e)
        {
            hotKeys = !chkHotkeys.Checked;
            TimerReset();
            currentKey = Keys.None;
            SaveData();
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

        #endregion
    }
}