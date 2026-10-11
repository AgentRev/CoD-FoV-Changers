using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace MultiCoD_FoV_Changer
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

        private readonly RawInputListener inputListener = new RawInputListener();

        public ChangeKey(string hotkey)
        {
            InitializeComponent();
            this.Text += '"' + hotkey + '"';

            try
            {
                inputListener.KeyboardAction += OnKeyboardEvent;
                inputListener.MouseAction += OnMouseEvent;
                inputListener.Start();
                FormClosed += (s, e) => inputListener.Dispose();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to set global keyboard hook: {ex.Message}\n\nKeybind shortcuts may not work.",
                                "Warning",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Warning);
            }
        }

        private void OnKeyboardEvent(Keys key, bool down)
        {
            if (down && key != Keys.Return)
                SetPressedKey(key);
        }

        private void OnMouseEvent(Keys key, bool down)
        {
            if (!down)
                return;

            var pt = Cursor.Position;

            if (btnAccept.RectangleToScreen(btnAccept.ClientRectangle).Contains(pt) ||
                btnCancel.RectangleToScreen(btnCancel.ClientRectangle).Contains(pt))
                return;

            SetPressedKey(key);
        }

        private void SetPressedKey(Keys key)
        {
            _pressedKey = key;
            _pressedKeyName = MainForm.VirtualKeyName(key);
            lblKeyName.Text = _pressedKeyName;
            btnAccept.Enabled = true;
            this.ActiveControl = btnAccept;
        }

        private void btnCancel_Enter(object sender, EventArgs e)
        {
            if(btnAccept.Enabled) this.ActiveControl = btnAccept;
        }
    }
}
