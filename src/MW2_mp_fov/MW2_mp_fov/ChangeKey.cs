using System;
//using System.Collections.Generic;
//using System.ComponentModel;
//using System.Data;
using System.Diagnostics;
//using System.Drawing;
//using System.Text;
using System.Runtime.InteropServices;
using System.Windows.Forms;
//using Microsoft.Win32;

namespace MW2_mp_fov
{
    public partial class ChangeKey : Form
    {
        private Keys _pressedKey;
        private string _pressedKeyName;

        public Keys PressedKey
        {
            get { return _pressedKey; }
        }

        public string PressedKeyName
        {
            get { return _pressedKeyName; }
        }

        [DllImport("user32.dll")]
        static extern int MapVirtualKey(uint uCode, uint uMapType);

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
        private IntPtr ptrHook;
        private KeyHook theKey;
        private LowLevelKeyboardProc objKeyboardProcess;

        public ChangeKey(string hotkey)
        {
            InitializeComponent();
            this.Text += '"' + hotkey + '"';

            ProcessModule objCurrentModule = Process.GetCurrentProcess().MainModule;
            objKeyboardProcess = new LowLevelKeyboardProc(captureKey);
            ptrHook = SetWindowsHookEx(13, objKeyboardProcess, GetModuleHandle(objCurrentModule.ModuleName), 0);
        }

        private IntPtr captureKey(int nCode, IntPtr wp, IntPtr lp)
        {
            if (nCode >= 0)
            {
                theKey = (KeyHook)Marshal.PtrToStructure(lp, typeof(KeyHook));
                // Keyboard Hook Time /////////////////////////////////////////////////////////////////////////////////////////

                if (theKey.flags < 128 && (int)theKey.Key != (int)Keys.Return)
                {
                    _pressedKey = theKey.Key;

                    _pressedKeyName = MainForm.VirtualKeyName(_pressedKey);

                    lblKeyName.Text = _pressedKeyName; //+ " " + _pressedKey.ToString(); //+ " " + nonVirtualKey; //MainForm.KeyName(theKey.Key)
                    btnAccept.Enabled = true;
                    this.ActiveControl = btnAccept;
                }

                ///////////////////////////////////////////////////////////////////////////////////////////////////////////////
            }
            return CallNextHookEx(ptrHook, nCode, wp, lp);
        }

        private void btnCancel_Enter(object sender, EventArgs e)
        {
            if (btnAccept.Enabled) this.ActiveControl = btnAccept;
        }
    }
}
