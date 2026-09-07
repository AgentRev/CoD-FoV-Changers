using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
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


namespace MultiCoD_FoV_Changer
{
#if WIN64
    using dword_ptr = UInt64;
#else
    using dword_ptr = UInt32;
#endif

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

        public const string c_toolVer = "4.00.17.1";

        public const float c_FoV = 65f;
        public const float c_FoV_lowerLimit = 65f;

        public const float c_FoV_upperLimit = 100f;
        public const string c_checkURL = "http://agentrevcodfov.crabdance.com/";

        public const bool c_doBeep = true;
        public const bool c_updateNotify = true;
        public const bool c_hotKeys = true;
        public static Keys[] c_catchKeys = { Keys.Subtract, Keys.Add, Keys.Multiply };

        #endregion

        #region settings

        static string settingsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), Constants.c_settingsDirName);
        static string settingsFile = Path.Combine(settingsPath, Constants.c_settingsFileName);

        dword_ptr pFoV = 0;
        float fFoV;
        bool doBeep;
        bool updateNotify;
        bool hotKeys;
        Keys[] catchKeys;

        bool saveSettings = true;
        bool writeAllowed = false;
        bool currentlyReading = false;
        bool updateAvailable = false;
        
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
        private LowLevelKeyboardProc objKeyboardProcess;
        private IntPtr ptrHook;
        private KeyHook theKey;

        private HttpWebRequest request;
        private bool requestSent;
        private Process proc = null;

        SoundPlayer sndGameFound => new SoundPlayer(GetType().Assembly.GetManifestResourceStream($"{GetType().Namespace}.Resources.gamefound.wav"));
        SoundPlayer sndGameLost => new SoundPlayer(GetType().Assembly.GetManifestResourceStream($"{GetType().Namespace}.Resources.gamelost.wav"));

        bool isRunning(bool init)
        {
            if (proc != null)
            {
                try
                {
                    proc.Refresh();
                    if (proc.HasExited)
                    {
                        progStop();
                        return false;
                    }
                    return true;
                }
                catch
                {
                    progStop();
                    return false;
                }
            }

            foreach (string exe in Constants.c_exes)
            {
                Process[] procs = Process.GetProcessesByName(exe);
                if (procs.Length > 0)
                {
                    if (init)
                    {
                        proc = procs[0];
                        lblGameStatus.Text = proc.ProcessName + ".exe";
#if !DEBUG
                        try
                        {
#endif
                            Memory.Init(proc.Id, (dword_ptr)proc.MainModule.BaseAddress);
#if !DEBUG
                        }
                        catch (Exception ex)
                        {
                            ErrMessage(ex);
                            Application.Exit();
                        }
#endif
                        TimerVerif.Start();
                    }
                    return true;
                }
            }

            return false;
        }

        void TimerReset()
        {
            TimerHoldKey.Stop();
            TimerHoldKey.Interval = 350;
        }

        private Keys currentKey;

        //#####################################################################################################################
#endregion

        public MainForm()
        {
            InitializeComponent();

            saveSettings = false;
            InitFovChanger();
            saveSettings = true;

            ProcessModule objCurrentModule = Process.GetCurrentProcess().MainModule;
            objKeyboardProcess = new LowLevelKeyboardProc(captureKey);
            ptrHook = SetWindowsHookEx(13, objKeyboardProcess, GetModuleHandle(objCurrentModule.ModuleName), 0);

            TimerCheck.Start();
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
                                else if (varName == "Beep")
                                {
                                    chkBeep.Checked = bool.Parse(varValue);
                                }
                                else if (varName == "FoV")
                                {
                                    SetFoV(float.Parse(varValue));
                                }
                                else if (varName == "FoVOffset")
                                {
                                    pFoV = dword_ptr.Parse(varValue, NumberStyles.AllowHexSpecifier);
                                }
                                else if (varName == "UpdateNotify")
                                {
                                    chkUpdate.Checked = bool.Parse(varValue);
                                }
                                else if (varName == "EnableHotkeys")
                                {
                                    chkHotkeys.Checked = bool.Parse(varValue);
                                }
                                else if (varName == "DisableHotkeys")
                                {
                                    chkHotkeys.Checked = !bool.Parse(varValue);
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
                            sw.WriteLine("EnableHotkeys=" + chkHotkeys.Checked);
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
                }
                catch
                {
                    saveSettings = false;
                }
            }
        }

        #endregion

        #region methods

        void InitFovChanger()
        {
            settingsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), Constants.c_settingsDirName);
            settingsFile = Path.Combine(settingsPath, Constants.c_settingsFileName);

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

            numFoV.Value = Convert.ToDecimal(fFoV);
            numFoV.Enabled = true;
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
                if (proc != null && isRunning(false) && writeAllowed)
                    Memory.WriteFloat(pFoV, reset ? c_FoV : fFoV);
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

        private void UpdateNumBox()
        {
            SetFoV(Convert.ToSingle(numFoV.Value));
        }

        private void progStart()
        {
            TimerVerif.Stop();
            writeAllowed = true;
            UpdateNumBox();
            TimerUpdate.Start();

            lblGameStatus.ForeColor = Color.ForestGreen;
            lblGameStatus.Refresh();

            if (doBeep)
                sndGameFound.PlaySync();
        }

        private void progStop()
        {
            TimerVerif.Stop();
            TimerUpdate.Stop();

            lblGameStatus.ForeColor = DefaultForeColor;
            lblGameStatus.Refresh();

            if (writeAllowed && doBeep)
                sndGameLost.PlaySync();

            writeAllowed = false;
            proc = null;
            Memory.Reset();

            SetFoV(-1);
            lblGameStatus.Text = "Awaiting game...";
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

        [DllImport("user32.dll", SetLastError = true)]
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
                                  "If the FoV changer has already worked for you before, please try to delete '" + Constants.c_settingsFileName + "' located in " + settingsPath + "\n\n" +
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
                if (proc != null && isRunning(false))
                {
                    float readValue = Memory.ReadFloat(pFoV);

                    if (readValue != fFoV && readValue >= c_FoV_lowerLimit)
                    {
                        Memory.WriteFloat(pFoV, fFoV);
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
            if (proc != null && isRunning(false))
            {
                proc.Refresh();
#if !DEBUG
                try
                {
#endif
                    if (proc.WorkingSet64 > Constants.c_memReadRange)
                    {
                        int step = 0;
#if !DEBUG
                        try
                        {
#endif
                        if (Memory.FindDvarAddress("cg_fov", out pFoV, out step))
                        {
                            progStart();
                        }
#if !DEBUG
                        }
                        catch (Exception ex)
                        {
                            ErrMessage(ex);
                            Application.Exit(); 
                        }
#endif
                    }
#if !DEBUG
                }
                catch (InvalidOperationException) { }
#endif
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
                            MessageBox.Show(this, "Update v" + dataVer + " is available on GitHub.\nClicking the \"Help\" button below will take you to the download page." + (!String.IsNullOrEmpty(dataInfo) ? "\n\nInfos:\n" + dataInfo : ""),
                                                  "Update available", MessageBoxButtons.OK, MessageBoxIcon.Information,
                                                  MessageBoxDefaultButton.Button1, 0, (!String.IsNullOrEmpty(dataDownloadLink) ? dataDownloadLink : "https://github.com/AgentRev/CoD-FoV-Changers/releases"));

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
                                  "Made by AgentRev\n\n" +
                                  "Compatible with:\n" +
                                  "MW1 '07, WaW, MW2 '09, BO1, MW3 '11, BO2, Ghosts, AW\n\n" +
                                  "Support email:\n" +
                                  "agentrevo@gmail.com\n",
                                  "About", MessageBoxButtons.OK, MessageBoxIcon.Information, 
                                  MessageBoxDefaultButton.Button1,
                                  0, "https://github.com/AgentRev/CoD-FoV-Changers/issues");

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
            hotKeys = chkHotkeys.Checked;
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

        #endregion

        private void MainForm_Load(object sender, EventArgs e)
        {

        }
    }
}
