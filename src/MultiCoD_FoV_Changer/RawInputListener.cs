using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace MultiCoD_FoV_Changer
{
    /// <summary>
    /// Receives global keyboard and mouse button events through WM_INPUT (Raw Input)
    /// using a hidden message-only window. Non-blocking, unlike low-level hooks.
    /// </summary>
    public sealed class RawInputListener : IDisposable
    {
        #region native

        const int WM_INPUT = 0x00FF;
        static readonly IntPtr HWND_MESSAGE = new IntPtr(-3);

        const uint RID_INPUT = 0x10000003;
        const uint RIDEV_REMOVE = 0x00000001;
        const uint RIDEV_INPUTSINK = 0x00000100;
        const uint RIM_TYPEMOUSE = 0;
        const uint RIM_TYPEKEYBOARD = 1;

        const ushort RI_KEY_BREAK = 0x01;
        const ushort RI_KEY_E0 = 0x02;

        const uint MAPVK_VSC_TO_VK_EX = 3;

        [StructLayout(LayoutKind.Sequential)]
        struct RAWINPUTDEVICE
        {
            public ushort usUsagePage;
            public ushort usUsage;
            public uint dwFlags;
            public IntPtr hwndTarget;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct RAWINPUTHEADER
        {
            public uint dwType;
            public uint dwSize;
            public IntPtr hDevice;
            public IntPtr wParam;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct RAWKEYBOARD
        {
            public ushort MakeCode;
            public ushort Flags;
            public ushort Reserved;
            public ushort VKey;
            public uint Message;
            public uint ExtraInformation;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct RAWMOUSE
        {
            public ushort usFlags;
            private ushort padding;
            public ushort usButtonFlags;
            public ushort usButtonData;
            public uint ulRawButtons;
            public int lLastX;
            public int lLastY;
            public uint ulExtraInformation;
        }

        [DllImport("user32.dll", SetLastError = true)]
        static extern bool RegisterRawInputDevices(RAWINPUTDEVICE[] devices, uint count, uint size);

        [DllImport("user32.dll")]
        static extern uint GetRawInputData(IntPtr hRawInput, uint command, IntPtr data, ref uint size, uint headerSize);

        [DllImport("user32.dll")]
        static extern uint MapVirtualKey(uint code, uint mapType);

        // Mouse button flag pairs: down flag, up flag, key
        static readonly ushort[] s_mouseDownFlags = { 0x0001, 0x0004, 0x0010, 0x0040, 0x0100 };
        static readonly ushort[] s_mouseUpFlags = { 0x0002, 0x0008, 0x0020, 0x0080, 0x0200 };
        static readonly Keys[] s_mouseKeys = { Keys.LButton, Keys.RButton, Keys.MButton, Keys.XButton1, Keys.XButton2 };

        #endregion

        /// <summary>Raised for keyboard events. Arguments: key, true if pressed / false if released.</summary>
        public event Action<Keys, bool> KeyboardAction;

        /// <summary>Raised for mouse button events. Arguments: key, true if pressed / false if released.</summary>
        public event Action<Keys, bool> MouseAction;

        private static readonly List<RawInputListener> s_listeners = new List<RawInputListener>();
        private static MessageWindow s_window;

        private bool started;

        public void Start()
        {
            if (started)
                return;

            if (s_listeners.Count == 0)
                RegisterDevices();

            s_listeners.Add(this);
            started = true;
        }

        public void Stop()
        {
            if (!started)
                return;

            started = false;
            s_listeners.Remove(this);

            if (s_listeners.Count == 0)
                UnregisterDevices();
        }

        public void Dispose()
        {
            Stop();
        }

        private static void RegisterDevices()
        {
            s_window = new MessageWindow();
            s_window.CreateHandle(new CreateParams { Parent = HWND_MESSAGE });

            var devices = new[]
            {
                new RAWINPUTDEVICE { usUsagePage = 1, usUsage = 6, dwFlags = RIDEV_INPUTSINK, hwndTarget = s_window.Handle }, // keyboard
                new RAWINPUTDEVICE { usUsagePage = 1, usUsage = 2, dwFlags = RIDEV_INPUTSINK, hwndTarget = s_window.Handle }  // mouse
            };

            if (!RegisterRawInputDevices(devices, (uint)devices.Length, (uint)Marshal.SizeOf(typeof(RAWINPUTDEVICE))))
            {
                int error = Marshal.GetLastWin32Error();
                s_window.DestroyHandle();
                s_window = null;
                throw new System.ComponentModel.Win32Exception(error, "Failed to register raw input devices.");
            }
        }

        private static void UnregisterDevices()
        {
            var devices = new[]
            {
                new RAWINPUTDEVICE { usUsagePage = 1, usUsage = 6, dwFlags = RIDEV_REMOVE, hwndTarget = IntPtr.Zero },
                new RAWINPUTDEVICE { usUsagePage = 1, usUsage = 2, dwFlags = RIDEV_REMOVE, hwndTarget = IntPtr.Zero }
            };

            RegisterRawInputDevices(devices, (uint)devices.Length, (uint)Marshal.SizeOf(typeof(RAWINPUTDEVICE)));

            if (s_window != null)
            {
                s_window.DestroyHandle();
                s_window = null;
            }
        }

        private sealed class MessageWindow : NativeWindow
        {
            protected override void WndProc(ref Message m)
            {
                if (m.Msg == WM_INPUT)
                {
                    foreach (var listener in s_listeners.ToArray())
                        listener.ProcessRawInput(m.LParam);
                }

                base.WndProc(ref m);
            }
        }

        private void ProcessRawInput(IntPtr hRawInput)
        {
            uint headerSize = (uint)Marshal.SizeOf(typeof(RAWINPUTHEADER));
            uint size = 0;

            if (GetRawInputData(hRawInput, RID_INPUT, IntPtr.Zero, ref size, headerSize) != 0 || size == 0)
                return;

            IntPtr buffer = Marshal.AllocHGlobal((int)size);
            try
            {
                if (GetRawInputData(hRawInput, RID_INPUT, buffer, ref size, headerSize) != size)
                    return;

                var header = (RAWINPUTHEADER)Marshal.PtrToStructure(buffer, typeof(RAWINPUTHEADER));
                IntPtr data = buffer + (int)headerSize;

                if (header.dwType == RIM_TYPEKEYBOARD)
                    ProcessKeyboard((RAWKEYBOARD)Marshal.PtrToStructure(data, typeof(RAWKEYBOARD)));
                else if (header.dwType == RIM_TYPEMOUSE)
                    ProcessMouse((RAWMOUSE)Marshal.PtrToStructure(data, typeof(RAWMOUSE)));
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }
        }

        private void ProcessKeyboard(RAWKEYBOARD kb)
        {
            if (kb.VKey == 0 || kb.VKey == 0xFF)
                return;

            bool extended = (kb.Flags & RI_KEY_E0) != 0;
            Keys key = (Keys)kb.VKey;

            // Raw input reports generic Shift/Ctrl/Alt; resolve to left/right variants
            switch (key)
            {
                case Keys.ShiftKey:
                    key = (Keys)MapVirtualKey(kb.MakeCode, MAPVK_VSC_TO_VK_EX);
                    break;
                case Keys.ControlKey:
                    key = extended ? Keys.RControlKey : Keys.LControlKey;
                    break;
                case Keys.Menu:
                    key = extended ? Keys.RMenu : Keys.LMenu;
                    break;
            }

            KeyboardAction?.Invoke(key, (kb.Flags & RI_KEY_BREAK) == 0);
        }

        private void ProcessMouse(RAWMOUSE mouse)
        {
            for (int i = 0; i < s_mouseKeys.Length; i++)
            {
                if ((mouse.usButtonFlags & s_mouseDownFlags[i]) != 0)
                    MouseAction?.Invoke(s_mouseKeys[i], true);
                if ((mouse.usButtonFlags & s_mouseUpFlags[i]) != 0)
                    MouseAction?.Invoke(s_mouseKeys[i], false);
            }
        }
    }
}
