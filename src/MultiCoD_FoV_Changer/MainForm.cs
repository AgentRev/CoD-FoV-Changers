using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Media;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Windows.Forms;


namespace MultiCoD_FoV_Changer
{
    using dword_ptr = UInt64;

    public partial class MainForm : Form
    {
        #region constants

        public const string c_toolVer = "5.0.0.0";

        public const float c_FoV = 90f;
        public const float c_FoV_vanilla = 65f;
        public const float c_FoV_upperLimit = Constants.c_FoV_upperLimit;

        public const string c_checkURL = "http://agentrevcodfov.crabdance.com/";

        public static Keys[] c_catchKeys = { Keys.Subtract, Keys.Add, Keys.Multiply, Keys.Divide };

        #endregion

        #region settings

        static string settingsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), Constants.c_settingsDirName);
        static string settingsFile = Path.Combine(settingsPath, Constants.c_settingsFileName);

        dword_ptr pFoV = 0;
        float fFoV = c_FoV;
        float fFoVScale = 1;
        int maxFPS;
        Keys[] catchKeys; // 0 = Zoom out, 1 = Zoom in, 2 = Reset, 3 = On/Off

        bool saveSettings = true;
        bool writeAllowed = false;
        bool currentlyReading = false;
        bool updateAvailable = false;
        bool fovEnabled = true;

        #endregion

        #region vars for hooks 'n other stuff
        //#####################################################################################################################

        private readonly RawInputListener inputListener = new RawInputListener();

        private HttpWebRequest request;
        private bool requestSent;
        private Process proc = null;

        // Store instances at class level so the streams stay open
        SoundPlayer sndGameFound;
        SoundPlayer sndGameLost;

        private void InitAudio()
        {
            var assembly = GetType().Assembly;

            string foundPath = $"{GetType().Namespace}.Resources.gamefound.wav";
            string lostPath = $"{GetType().Namespace}.Resources.gamelost.wav";

            Stream streamFound = assembly.GetManifestResourceStream(foundPath);
            Stream streamLost = assembly.GetManifestResourceStream(lostPath);

            // If either resource fails to load, display an error message box listing all embedded manifest names
            if (streamFound == null || streamLost == null)
            {
                string[] resourceNames = assembly.GetManifestResourceNames();
                string manifestList = resourceNames.Length > 0
                    ? string.Join("\n", resourceNames)
                    : "No embedded resources found.";

                MessageBox.Show(
                    $"Failed to load embedded audio files.\n\n" +
                    $"Expected:\n- {foundPath}\n- {lostPath}\n\n" +
                    $"Available Embedded Resources:\n{manifestList}",
                    "Resource Loading Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning
                );
                return;
            }

            sndGameFound = new SoundPlayer(streamFound);
            sndGameFound.Load();

            sndGameLost = new SoundPlayer(streamLost);
            sndGameLost.Load();
        }

        private void UpdateKeyListener()
        {
            if (chkHotkeys.Checked)
                inputListener.Start();
            else
                inputListener.Stop();
        }

        private void OnKeyEvent(Keys key, bool pressed)
        {
            if (!catchKeys.Contains(key))
                return;

            if (pressed && currentKey != key)
            {
                TimerReset();
                currentKey = key;
                ProcessCurrentKey();
                TimerHoldKey.Start();
            }
            else if (!pressed && currentKey == key)
            {
                if (key == catchKeys[3] && cbKeyOffOnMode.SelectedIndex == 1) // "Hold" release
                    SetFoVEnabled(true);

                TimerReset();
                currentKey = Keys.None;

                //var iCatchKeys = Array.IndexOf(catchKeys, key);
                //if (iCatchKeys >= 0 && iCatchKeys <= 2)
                //    SaveSettings();
            }
        }

        bool CheckGameRunning(bool resetProc = false)
        {
            if (proc != null)
            {
                try
                {
                    //proc.Refresh();
                    if (!proc.HasExited)
                    {
                        return true;
                    }
                }
                catch { }

                if (resetProc)
                {
                    proc = null;
                }
            }

            return false;
        }

        string GetGameProcName()
        {
            if (proc != null)
            {
                try { return proc.MainModule.ModuleName; } catch { }
                try { return proc.ProcessName; } catch { }
            }

            return "";
        }

        static List<Process> FindGameProcesses()
        {
            var found = new List<Process>();

            foreach (string exe in Constants.c_exes)
            {
                Process[] procs = Process.GetProcessesByName(exe);
                if (procs.Length > 0)
                {
                    found.Add(procs[0]);
                }
            }

            return found;
        }

        // candidates: result of a previous FindGameProcesses call (e.g. run on a background thread)
        bool FindGame(bool init = false, IList<Process> candidates = null)
        {
            if (proc != null)
            {
                if (CheckGameRunning(true))
                {
                    return true;
                }

                progStop();
                return false;
            }

            foreach (Process candidate in candidates ?? FindGameProcesses())
            {
                if (init)
                {
                    proc = candidate;

                    if (!CheckGameRunning(true))
                    {
                        continue;
                    }

                    string procName = GetGameProcName();
                    if (procName == "")
                    {
                        continue;
                    }

                    lblGameStatus.Text = procName + " " + (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "⏰" : "↻");
                    lblGameStatus.Refresh();
                }

                return true;
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
            InitAudio();

            saveSettings = false;
            InitFovChanger();
            saveSettings = true;

            Shown += (s, e) =>
                this.ActiveControl = null;

            FormClosed += (s, e) =>
                SaveSettings();

            try // setup input listener
            {
                inputListener.KeyboardAction += OnKeyEvent;
                inputListener.MouseAction += OnKeyEvent;

                UpdateKeyListener();

                FormClosed += (s, e) =>
                    inputListener.Stop();
            }
            catch
            {
                chkHotkeys.Text = "Hotkeys unsupported by OS";
                chkHotkeys.Enabled = false;
                chkHotkeys.Checked = false;

                btnKeyZoomOut.Enabled = false;
                btnKeyZoomIn.Enabled = false;
                btnKeyReset.Enabled = false;
                btnKeyOffOn.Enabled = false;
            }

            TimerVerif.Start();
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
                                else if (varName == "FoVScale")
                                {
                                    SetFoVScale(float.Parse(varValue));
                                }
                                else if (varName == "FoVOffset")
                                {
                                    // unused, overwritten every time
                                    pFoV = dword_ptr.Parse(varValue, NumberStyles.AllowHexSpecifier);
                                }
                                else if (varName == "MaxFPS")
                                {
                                    SetFPS(int.Parse(varValue));
                                }
                                else if (varName == "UpdateNotify")
                                {
                                    chkUpdate.Checked = bool.Parse(varValue);
                                }
                                else if (varName == "EnableHotkeys")
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
                                else if (varName == "HotkeyOffOn")
                                {
                                    catchKeys[3] = (Keys)int.Parse(varValue);
                                    btnKeyOffOn.Text = VirtualKeyName(catchKeys[3]);
                                }
                                else if (varName == "OffOnMode")
                                {
                                    cbKeyOffOnMode.SelectedIndex = int.Parse(varValue);
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
                            sw.WriteLine("FoVScale=" + fFoVScale);
                            //sw.WriteLine("FoVOffset=" + pFoV.ToString("x"));
                            sw.WriteLine("MaxFPS=" + maxFPS);
                            sw.WriteLine("UpdateNotify=" + chkUpdate.Checked);
                            sw.WriteLine("EnableHotkeys=" + chkHotkeys.Checked);
                            sw.WriteLine("HotkeyIncrease=" + (int)catchKeys[0]);
                            sw.WriteLine("HotkeyDecrease=" + (int)catchKeys[1]);
                            sw.WriteLine("HotkeyReset=" + (int)catchKeys[2]);
                            sw.WriteLine("HotkeyOffOn=" + (int)catchKeys[3]);
                            sw.WriteLine("OffOnMode=" + cbKeyOffOnMode.SelectedIndex);
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
            maxFPS = Convert.ToInt32(numFPS.Value);
            catchKeys = (Keys[])c_catchKeys.Clone();

            this.cbKeyOffOnMode.SelectedIndex = 0; // "Toggle"

            if (File.Exists(settingsFile)) ReadSettings();

            lblVersion.Text = "v" + c_toolVer;
            lblVersion.Visible = true;

            numFoV.Value = Convert.ToDecimal(fFoV);
            numFoV.Enabled = true;
            numFoVScale.Value = Convert.ToDecimal(fFoVScale);
            numFoVScale.Enabled = true;
            UpdateEffectiveFoV();

            lblEffectiveFoVMax.Text = "(max " + c_FoV_upperLimit.ToString("0.#", CultureInfo.CurrentCulture) + ")";

            foreach (NumericUpDown num in new[] { numFoV, numFPS, numFoVScale })
            {
                num.KeyDown += (s, e) =>
                {
                    if (e.KeyCode == Keys.Enter || e.KeyCode == Keys.Escape)
                    {
                        e.SuppressKeyPress = true; // disable ding sound
                    }
                };
            }
        }

        private void SetFoVEnabled(bool enable)
        {
            if (fovEnabled == enable)
                return;

            fovEnabled = enable;

            if (!CheckGameRunning() || !writeAllowed)
                return;

            try
            {
                if (fovEnabled)
                {
                    UpdateFoV();
                }
                else
                {
                    Memory.ResetDvar("cg_fov");

                    if (Memory.dvarAddresses.ContainsKey("cg_fovScale"))
                        Memory.ResetDvar("cg_fovScale");
                }
            }
            catch (Exception ex)
            {
                ErrMessage(ex);
                Application.Exit();
            }
        }

        private void ProcessCurrentKey(bool isRepeat = false)
        {
            if (currentKey == catchKeys[0]) // "Zoom out"
                SetFoV(fFoV + 1);

            if (currentKey == catchKeys[1]) // "Zoom in"
                SetFoV(fFoV - 1);

            if (currentKey == catchKeys[2]) // "Reset to default"
                SetFoV(c_FoV_vanilla);

            if (currentKey == catchKeys[3] && !isRepeat) // "Off / On"
            {
                if (cbKeyOffOnMode.SelectedIndex == 1) // "Hold"
                    SetFoVEnabled(false);
                else // "Toggle"
                    SetFoVEnabled(!fovEnabled);
            }
        }

        private void WriteFoV(float val)
        {
            if (CheckGameRunning())
            {
                Memory.WriteFloat("cg_fov", val);
            }
        }

        private void SetFoV(float val)
        {
            var oldVal = fFoV;
            bool reset = val < 0;

            if (!reset)
            {
                if (val * fFoVScale > c_FoV_upperLimit)
                    val = c_FoV_upperLimit / fFoVScale;

                if (val >= Convert.ToSingle(numFoV.Minimum) && val <= Convert.ToSingle(numFoV.Maximum))
                    fFoV = val;
            }
#if !DEBUG
            try
            {
#endif
                if (CheckGameRunning() && writeAllowed && fovEnabled)
                {
                    WriteFoV(reset ? c_FoV_vanilla : fFoV);
                }
#if !DEBUG
            }
            catch (Exception ex)
            {
                ErrMessage(ex);
                Application.Exit();
            }
#endif
            if (!reset)
            {
                if (numFoV.Value != Convert.ToDecimal(fFoV))
                    numFoV.Value = Convert.ToDecimal(fFoV);
            }

            UpdateEffectiveFoV();
        }

        private void WriteFoVScale(float val)
        {
            if (CheckGameRunning() && Memory.dvarAddresses.ContainsKey("cg_fovScale"))
            {
                Memory.WriteFloat("cg_fovScale", val);
            }
        }

        private void SetFoVScale(float val)
        {
            var oldVal = fFoVScale;
            bool reset = val < 0;

            if (!reset)
            {
                if (val * fFoV > c_FoV_upperLimit)
                    val = c_FoV_upperLimit / fFoV;

                if (val >= Convert.ToSingle(numFoVScale.Minimum) && val <= Convert.ToSingle(numFoVScale.Maximum))
                    fFoVScale = val;
            }
#if !DEBUG
            try
            {
#endif
                if (CheckGameRunning() && writeAllowed && fovEnabled)
                {
                    WriteFoVScale(reset ? 1 : fFoVScale);
                }
#if !DEBUG
            }
            catch (Exception ex)
            {
                ErrMessage(ex);
                Application.Exit();
            }
#endif
            if (!reset)
            {
                if (numFoVScale.Value != Convert.ToDecimal(fFoVScale))
                    numFoVScale.Value = Convert.ToDecimal(fFoVScale);
            }

            UpdateEffectiveFoV();
        }

        private void WriteFPS(int val)
        {
            if (CheckGameRunning())
            {
                if (val >= 0)
                {
                    if (Memory.dvarAddresses.ContainsKey("data_validation_allow_drop"))
                        Memory.WriteInt("data_validation_allow_drop", 0); // needed for AW, otherwise the game crashes

                    Memory.WriteInt("com_maxfps", val);
                }
                else
                {
                    Memory.ResetDvar("com_maxfps");

                    if (Memory.dvarAddresses.ContainsKey("data_validation_allow_drop"))
                        Memory.WriteInt("data_validation_allow_drop", 1);
                }
            }
        }

        private void SetFPS(int val)
        {
            var oldVal = maxFPS;
            bool reset = val < 0;

            if (!reset)
            {
                if (val < numFPS.Minimum)
                    maxFPS = Convert.ToInt32(numFPS.Minimum);
                else if (val > numFPS.Maximum)
                    maxFPS = Convert.ToInt32(numFPS.Maximum);
                else
                    maxFPS = val;
            }
#if !DEBUG
            try
            {
#endif
                if (CheckGameRunning() && writeAllowed)
                {
                    WriteFPS(reset ? -1 : maxFPS);
                }
#if !DEBUG
            }
            catch (Exception ex)
            {
                ErrMessage(ex);
                Application.Exit();
            }
#endif

            if (!reset)
            {
                if (numFPS.Value != Convert.ToDecimal(maxFPS))
                    numFPS.Value = Convert.ToDecimal(maxFPS);
            }
        }

        private void UpdateNumBox()
        {
            SetFoV(Convert.ToSingle(numFoV.Value));
            SetFoVScale(Convert.ToSingle(numFoVScale.Value));
            SetFPS(Convert.ToInt32(numFPS.Value));
            //SaveSettings();
        }

        void UpdateEffectiveFoV()
        {
            lblEffectiveFoVNum.Text = (fFoV * fFoVScale).ToString("0.0");
        }

        private void progStart()
        {
            // Debug check to verify embedded resources under Mono
            #if DEBUG
            foreach (string name in GetType().Assembly.GetManifestResourceNames())
            {
                Console.WriteLine("Embedded resource: " + name);
            }
            #endif

            TimerVerif.Stop();
            writeAllowed = true;
            UpdateNumBox();
            TimerUpdate.Start();

            if (CheckGameRunning())
            {
                string procName = GetGameProcName();
                if (procName == "") return;

                lblGameStatus.Text = procName + " ✔";
                lblGameStatus.ForeColor = Color.ForestGreen;
                lblGameStatus.Refresh();

                if (chkBeep.Checked && sndGameFound != null)
                    sndGameFound.Play();
            }
        }

        private void progStop()
        {
            TimerUpdate.Stop();

            lblGameStatus.Text = "Stopping.";
            lblGameStatus.ForeColor = DefaultForeColor;
            lblGameStatus.Refresh();

            if (writeAllowed && chkBeep.Checked && sndGameLost != null)
            {
                try
                {
                    sndGameLost.PlaySync(); // Blocks execution until WAV playback completes
                }
                catch
                {
                    // Fallback gracefully if audio device fails under Wine/Mono
                }
            }

            //SetFoV(-1);
            //SetFPS(-1);

            writeAllowed = false;
            proc = null;
            Memory.Reset();

            lblGameStatus.Text = "Awaiting game...";
            TimerVerif.Start();
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
                case Keys.LButton: return "LClick";
                case Keys.RButton: return "RClick";
                case Keys.MButton: return "MClick";
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

        public static string VirtualKeyName(Keys key)
        {
            // Mode 2 = MAPVK_VK_TO_CHAR: Translates a virtual key into an unshifted character value
            const uint MAPVK_VK_TO_CHAR = 2;
            const uint DEAD_KEY_MASK = 0x80000000;

            uint rawCharacterValue = MapVirtualKey((uint)key, MAPVK_VK_TO_CHAR);

            // If the high bit is set, it's a dead key (e.g., an accent placeholder). 
            // Strip the mask bit to extract the actual character code.
            if (rawCharacterValue > DEAD_KEY_MASK)
            {
                rawCharacterValue -= DEAD_KEY_MASK;
            }

            // Verify a valid character mapping exists
            bool isMappedChar = rawCharacterValue > 0;

            // Ensure the key falls inside standard alpha-numeric zones, bypassing complex Numpad blocks
            bool isStandardTypingKey = key >= Keys.D0 && (key < Keys.NumPad0 || key > Keys.Divide);

            if (isMappedChar && isStandardTypingKey)
            {
                char character = (char)rawCharacterValue;
                return character.ToString().ToUpperInvariant();
            }

            // Fallback for special function keys (e.g., Escape, Control, Backspace)
            return KeyName(key);
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

        #region timers

        private void TimerHoldKey_Tick(object sender, EventArgs e)
        {
            if (TimerHoldKey.Interval == 350) TimerHoldKey.Interval = 35;
            ProcessCurrentKey(true);
        }

        private void UpdateFoV()
        {
#if !DEBUG
            try
            {
#endif
                if (FindGame())
                {
                    float readValue = Memory.ReadFloat("cg_fov");

                    if (fovEnabled && readValue != fFoV)
                    {
                        WriteFoV(fFoV);
                    }

                    if (fovEnabled && Memory.dvarAddresses.ContainsKey("cg_fovScale"))
                    {
                        float readFoVScale = Memory.ReadFloat("cg_fovScale");

                        if (readFoVScale != fFoVScale)
                        {
                            WriteFoVScale(fFoVScale);
                        }
                    }

                    int readFPS = Memory.ReadInt("com_maxfps");

                    if (readFPS != maxFPS)
                    {
                        WriteFPS(maxFPS);
                    }
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

        private void TimerUpdate_Tick(object sender, EventArgs e)
        {
            UpdateFoV();
        }

        private void TimerCheck_Tick(object sender, EventArgs e)
        {
            // noop
        }

        private async System.Threading.Tasks.Task CheckDvarAddresses()
        {
            TimerVerif.Stop();

            if (CheckGameRunning())
            {
                var p = proc;

#if !DEBUG
                try
                {
#endif
                    bool found = await System.Threading.Tasks.Task.Run(() =>
                    {
                        var memUsage = Memory.GetWorkingSet64(p);

                        if (memUsage > 512 * 0x100000 || memUsage == 0) // wait for 512 MB RAM usage before first scan (or proceed anyway if usage unavailable)
                        {
                            Memory.Init(p);
                            return Memory.FindDvarAddresses(new[] { "cg_fov", "com_maxfps" }, new[] { "cg_fovScale", "data_validation_allow_drop" });
                        }

                        return false;
                    });

                    if (found && CheckGameRunning())
                    {
                        pFoV = Memory.dvarAddresses["cg_fov"];
                        progStart();
                        return;
                    }
#if !DEBUG
                }
                catch (Exception ex)
                {
                    ErrMessage(ex);
                    Application.Exit();
                    return;
                }
#endif
            }

            TimerVerif.Start();
        }

        private bool verifBusy;

        private async void TimerVerif_Tick(object sender, EventArgs e)
        {
            if (verifBusy) return;
            verifBusy = true;

            try
            {
                // process enumeration is slow and would stall the hook thread (UI), freezing the mouse
                List<Process> candidates = proc == null
                    ? await System.Threading.Tasks.Task.Run(() => FindGameProcesses())
                    : null;

                if (FindGame(true, candidates))
                {
                    await CheckDvarAddresses();
                }
            }
            finally
            {
                verifBusy = false;
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

                        if (chkUpdate.Checked)
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
            //SaveSettings();
        }

        private void numFoV_Leave(object sender, EventArgs e)
        {
            UpdateNumBox();
        }

        private void numFoVScale_Leave(object sender, EventArgs e)
        {
            UpdateNumBox();
        }

        private void numFPS_Leave(object sender, EventArgs e)
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

        private void numFoVScale_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)Keys.Return)
            {
                UpdateNumBox();
                this.ActiveControl = null;
            }
        }

        private void numFPS_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)Keys.Return)
            {
                UpdateNumBox();
                this.ActiveControl = null;
            }
        }

        private void MainForm_Deactivate(object sender, EventArgs e)
        {
            //UpdateNumBox();
            this.ActiveControl = null;
        }

        private void MainForm_MouseClick(object sender, MouseEventArgs e)
        {
            if (this.ActiveControl != null)
            {
                //UpdateNumBox();
                this.ActiveControl = null;
            }
        }

        private void numFoV_ValueChanged(object sender, EventArgs e)
        {
            if (this.ContainsFocus)
                UpdateNumBox();
        }

        private void numFoVScale_ValueChanged(object sender, EventArgs e)
        {
            if (this.ContainsFocus)
                UpdateNumBox();
        }

        private void numFPS_ValueChanged(object sender, EventArgs e)
        {
            if (this.ContainsFocus)
                UpdateNumBox();
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (CheckGameRunning())
            {
                progStop();
            }
        }

        private void btnAbout_Click(object sender, EventArgs e)
        {
            string dvarList = "";

            if (Memory.dvarAddresses != null && Memory.dvarAddresses.Count > 0)
            {
                dvarList = $"\nCurrent dvar pointers:\nbase address = 0x{Memory.baseAddr:X}\n" + string.Join("\n", Memory.dvarAddresses.Select(kvp => $"{kvp.Key} = 0x{kvp.Value:X}")) + "\n";
            }

            MessageBox.Show(this, this.Text + " v" + c_toolVer + "\n" +
                                  "Made by AgentRev\n" +
                                  "agentrevo@gmail.com\n" +
                                  dvarList,
                                  "About", MessageBoxButtons.OK, MessageBoxIcon.Information,
                                  MessageBoxDefaultButton.Button1, 0,
                                  "https://github.com/AgentRev/CoD-FoV-Changers/issues");
        }

        private void btnExit_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void btnReset_Click(object sender, EventArgs e)
        {
            SetFoV(c_FoV_vanilla);
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
                    if (chkUpdate.Checked && updateAvailable)
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
            UpdateCheck();
        }

        private void lblLink_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            var url = "https://github.com/AgentRev/CoD-FoV-Changers";

            if (Environment.OSVersion.Platform == PlatformID.Unix)
            {
                // Mono on macOS returns PlatformID.Unix as well
                string launcher = Environment.OSVersion.VersionString.Contains("Darwin") ? "open" : "xdg-open";
                Process.Start(launcher, url);
            }
            else
            {
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            }
        }

        private void chkHotkeys_CheckedChanged(object sender, EventArgs e)
        {
            UpdateKeyListener();
            TimerReset();
            SetFoVEnabled(true);
            currentKey = Keys.None;
            //SaveSettings();
        }

        private void cbKeyOffOnMode_SelectedIndexChanged(object sender, EventArgs e)
        {
            SetFoVEnabled(true);
            //SaveSettings();
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

        private void btnKeyOffOn_Click(object sender, EventArgs e)
        {
            ChgKey(3, "Off / On", btnKeyOffOn);
        }

        #endregion
    }
}
