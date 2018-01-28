using System;

namespace MW2_mp_fov
{
    partial class MainForm
    {
        /// <summary>
        /// Variable nécessaire au concepteur.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Nettoyage des ressources utilisées.
        /// </summary>
        /// <param name="disposing">true si les ressources managées doivent être supprimées ; sinon, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {

                components.Dispose();
            }
            if (ptrHook != IntPtr.Zero)
            {
                UnhookWindowsHookEx(ptrHook);
                ptrHook = IntPtr.Zero;
            }
            base.Dispose(disposing);
        }

        #region Code généré par le Concepteur Windows Form

        /// <summary>
        /// Méthode requise pour la prise en charge du concepteur - ne modifiez pas
        /// le contenu de cette méthode avec l'éditeur de code.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            this.TimerHoldKey = new System.Windows.Forms.Timer(this.components);
            this.TimerUpdate = new System.Windows.Forms.Timer(this.components);
            this.lblFoV = new System.Windows.Forms.Label();
            this.TimerCheck = new System.Windows.Forms.Timer(this.components);
            this.chkBeep = new System.Windows.Forms.CheckBox();
            this.lblGameState = new System.Windows.Forms.Label();
            this.TimerVerif = new System.Windows.Forms.Timer(this.components);
            this.btnStartGame = new System.Windows.Forms.Button();
            this.numFoV = new System.Windows.Forms.NumericUpDown();
            this.btnExit = new System.Windows.Forms.Button();
            this.btnAbout = new System.Windows.Forms.Button();
            this.btnReset = new System.Windows.Forms.Button();
            this.lblVersion = new System.Windows.Forms.Label();
            this.lblInstructions = new System.Windows.Forms.Label();
            this.lblMMN = new System.Windows.Forms.Label();
            this.lblLink = new System.Windows.Forms.LinkLabel();
            this.TimerHTTP = new System.Windows.Forms.Timer(this.components);
            this.chkUpdate = new System.Windows.Forms.CheckBox();
            this.lblUpdateAvail = new System.Windows.Forms.Label();
            this.TimerBlink = new System.Windows.Forms.Timer(this.components);
            this.btnKeyZoomOut = new System.Windows.Forms.Button();
            this.btnKeyZoomIn = new System.Windows.Forms.Button();
            this.lblZoomIn = new System.Windows.Forms.Label();
            this.lblResetDefault = new System.Windows.Forms.Label();
            this.btnKeyReset = new System.Windows.Forms.Button();
            this.chkHotkeys = new System.Windows.Forms.CheckBox();
            ((System.ComponentModel.ISupportInitialize)(this.numFoV)).BeginInit();
            this.SuspendLayout();
            // 
            // TimerHoldKey
            // 
            this.TimerHoldKey.Interval = 500;
            this.TimerHoldKey.Tick += new System.EventHandler(this.TimerHoldKey_Tick);
            // 
            // TimerUpdate
            // 
            this.TimerUpdate.Interval = 250;
            this.TimerUpdate.Tick += new System.EventHandler(this.TimerUpdate_Tick);
            // 
            // lblFoV
            // 
            this.lblFoV.AutoSize = true;
            this.lblFoV.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblFoV.Location = new System.Drawing.Point(45, 43);
            this.lblFoV.Name = "lblFoV";
            this.lblFoV.Size = new System.Drawing.Size(99, 20);
            this.lblFoV.TabIndex = 0;
            this.lblFoV.Text = "Field of View";
            // 
            // TimerCheck
            // 
            this.TimerCheck.Interval = 1000;
            this.TimerCheck.Tick += new System.EventHandler(this.TimerCheck_Tick);
            // 
            // chkBeep
            // 
            this.chkBeep.AutoSize = true;
            this.chkBeep.Checked = true;
            this.chkBeep.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkBeep.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.chkBeep.Location = new System.Drawing.Point(16, 94);
            this.chkBeep.Name = "chkBeep";
            this.chkBeep.RightToLeft = System.Windows.Forms.RightToLeft.Yes;
            this.chkBeep.Size = new System.Drawing.Size(231, 20);
            this.chkBeep.TabIndex = 4;
            this.chkBeep.Text = "Beep once FoV change succeeds";
            this.chkBeep.UseVisualStyleBackColor = true;
            this.chkBeep.CheckedChanged += new System.EventHandler(this.chkBeep_CheckedChanged);
            // 
            // lblGameState
            // 
            this.lblGameState.AutoSize = true;
            this.lblGameState.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblGameState.Location = new System.Drawing.Point(19, 12);
            this.lblGameState.Name = "lblGameState";
            this.lblGameState.Size = new System.Drawing.Size(125, 20);
            this.lblGameState.TabIndex = 0;
            this.lblGameState.Text = "MW2 Multiplayer";
            // 
            // TimerVerif
            // 
            this.TimerVerif.Interval = 1000;
            this.TimerVerif.Tick += new System.EventHandler(this.TimerVerif_Tick);
            // 
            // btnStartGame
            // 
            this.btnStartGame.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnStartGame.Location = new System.Drawing.Point(152, 11);
            this.btnStartGame.MinimumSize = new System.Drawing.Size(82, 24);
            this.btnStartGame.Name = "btnStartGame";
            this.btnStartGame.Size = new System.Drawing.Size(95, 24);
            this.btnStartGame.TabIndex = 1;
            this.btnStartGame.Text = "Start Game";
            this.btnStartGame.UseVisualStyleBackColor = true;
            this.btnStartGame.Click += new System.EventHandler(this.btnStartGame_Click);
            // 
            // numFoV
            // 
            this.numFoV.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.numFoV.DecimalPlaces = 2;
            this.numFoV.Enabled = false;
            this.numFoV.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.numFoV.Location = new System.Drawing.Point(153, 44);
            this.numFoV.Maximum = new decimal(new int[] {
            90,
            0,
            0,
            0});
            this.numFoV.Minimum = new decimal(new int[] {
            65,
            0,
            0,
            0});
            this.numFoV.MinimumSize = new System.Drawing.Size(58, 0);
            this.numFoV.Name = "numFoV";
            this.numFoV.Size = new System.Drawing.Size(70, 22);
            this.numFoV.TabIndex = 2;
            this.numFoV.Value = new decimal(new int[] {
            65,
            0,
            0,
            0});
            this.numFoV.ValueChanged += new System.EventHandler(this.numFoV_ValueChanged);
            // 
            // btnExit
            // 
            this.btnExit.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnExit.Location = new System.Drawing.Point(177, 257);
            this.btnExit.Name = "btnExit";
            this.btnExit.Size = new System.Drawing.Size(70, 30);
            this.btnExit.TabIndex = 8;
            this.btnExit.Text = "Exit";
            this.btnExit.UseVisualStyleBackColor = true;
            this.btnExit.Click += new System.EventHandler(this.btnExit_Click);
            // 
            // btnAbout
            // 
            this.btnAbout.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnAbout.Location = new System.Drawing.Point(97, 257);
            this.btnAbout.Name = "btnAbout";
            this.btnAbout.Size = new System.Drawing.Size(70, 30);
            this.btnAbout.TabIndex = 7;
            this.btnAbout.Text = "About";
            this.btnAbout.UseVisualStyleBackColor = true;
            this.btnAbout.Click += new System.EventHandler(this.btnAbout_Click);
            // 
            // btnReset
            // 
            this.btnReset.Font = new System.Drawing.Font("Microsoft Sans Serif", 15.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnReset.Location = new System.Drawing.Point(223, 43);
            this.btnReset.Name = "btnReset";
            this.btnReset.Size = new System.Drawing.Size(24, 24);
            this.btnReset.TabIndex = 3;
            this.btnReset.Text = "*";
            this.btnReset.UseVisualStyleBackColor = true;
            this.btnReset.Click += new System.EventHandler(this.btnReset_Click);
            // 
            // lblVersion
            // 
            this.lblVersion.AutoSize = true;
            this.lblVersion.Enabled = false;
            this.lblVersion.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblVersion.Location = new System.Drawing.Point(8, 264);
            this.lblVersion.MinimumSize = new System.Drawing.Size(82, 0);
            this.lblVersion.Name = "lblVersion";
            this.lblVersion.Size = new System.Drawing.Size(82, 16);
            this.lblVersion.TabIndex = 0;
            this.lblVersion.Text = "v1.x.xxx.x";
            this.lblVersion.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.lblVersion.Visible = false;
            // 
            // lblInstructions
            // 
            this.lblInstructions.AutoSize = true;
            this.lblInstructions.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblInstructions.Location = new System.Drawing.Point(121, 156);
            this.lblInstructions.Name = "lblInstructions";
            this.lblInstructions.Size = new System.Drawing.Size(89, 16);
            this.lblInstructions.TabIndex = 8;
            this.lblInstructions.Text = "Zoom out (+5)";
            // 
            // lblMMN
            // 
            this.lblMMN.AutoSize = true;
            this.lblMMN.Location = new System.Drawing.Point(8, 236);
            this.lblMMN.MinimumSize = new System.Drawing.Size(242, 0);
            this.lblMMN.Name = "lblMMN";
            this.lblMMN.Size = new System.Drawing.Size(242, 13);
            this.lblMMN.TabIndex = 9;
            this.lblMMN.Text = "Check MapModNews.com  for updates.";
            this.lblMMN.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // lblLink
            // 
            this.lblLink.AutoSize = true;
            this.lblLink.BackColor = System.Drawing.SystemColors.Control;
            this.lblLink.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblLink.LinkBehavior = System.Windows.Forms.LinkBehavior.HoverUnderline;
            this.lblLink.Location = new System.Drawing.Point(65, 236);
            this.lblLink.Margin = new System.Windows.Forms.Padding(0);
            this.lblLink.MaximumSize = new System.Drawing.Size(99, 0);
            this.lblLink.Name = "lblLink";
            this.lblLink.Size = new System.Drawing.Size(99, 13);
            this.lblLink.TabIndex = 6;
            this.lblLink.TabStop = true;
            this.lblLink.Text = "MapModNews.com";
            this.lblLink.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.lblLink_LinkClicked);
            // 
            // chkUpdate
            // 
            this.chkUpdate.AutoSize = true;
            this.chkUpdate.Checked = true;
            this.chkUpdate.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkUpdate.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.chkUpdate.Location = new System.Drawing.Point(7, 115);
            this.chkUpdate.Name = "chkUpdate";
            this.chkUpdate.RightToLeft = System.Windows.Forms.RightToLeft.Yes;
            this.chkUpdate.Size = new System.Drawing.Size(240, 20);
            this.chkUpdate.TabIndex = 5;
            this.chkUpdate.Text = "Alert popup if an update is available";
            this.chkUpdate.UseVisualStyleBackColor = true;
            this.chkUpdate.CheckedChanged += new System.EventHandler(this.chkUpdate_CheckedChanged);
            // 
            // lblUpdateAvail
            // 
            this.lblUpdateAvail.AutoSize = true;
            this.lblUpdateAvail.BackColor = System.Drawing.SystemColors.Control;
            this.lblUpdateAvail.Enabled = false;
            this.lblUpdateAvail.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblUpdateAvail.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(180)))), ((int)(((byte)(0)))), ((int)(((byte)(0)))));
            this.lblUpdateAvail.Location = new System.Drawing.Point(7, 132);
            this.lblUpdateAvail.Name = "lblUpdateAvail";
            this.lblUpdateAvail.Size = new System.Drawing.Size(159, 13);
            this.lblUpdateAvail.TabIndex = 12;
            this.lblUpdateAvail.Text = "└ Update v1.x.xxx.x available";
            this.lblUpdateAvail.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.lblUpdateAvail.Visible = false;
            // 
            // TimerBlink
            // 
            this.TimerBlink.Interval = 500;
            this.TimerBlink.Tick += new System.EventHandler(this.TimerBlink_Tick);
            // 
            // btnKeyZoomOut
            // 
            this.btnKeyZoomOut.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnKeyZoomOut.ForeColor = System.Drawing.Color.MidnightBlue;
            this.btnKeyZoomOut.Location = new System.Drawing.Point(10, 153);
            this.btnKeyZoomOut.Name = "btnKeyZoomOut";
            this.btnKeyZoomOut.Size = new System.Drawing.Size(108, 23);
            this.btnKeyZoomOut.TabIndex = 13;
            this.btnKeyZoomOut.Text = "Numpad−";
            this.btnKeyZoomOut.UseVisualStyleBackColor = true;
            this.btnKeyZoomOut.Click += new System.EventHandler(this.btnKeyZoomOut_Click);
            // 
            // btnKeyZoomIn
            // 
            this.btnKeyZoomIn.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnKeyZoomIn.ForeColor = System.Drawing.Color.MidnightBlue;
            this.btnKeyZoomIn.Location = new System.Drawing.Point(10, 175);
            this.btnKeyZoomIn.Name = "btnKeyZoomIn";
            this.btnKeyZoomIn.Size = new System.Drawing.Size(108, 23);
            this.btnKeyZoomIn.TabIndex = 14;
            this.btnKeyZoomIn.Text = "Numpad+";
            this.btnKeyZoomIn.UseVisualStyleBackColor = true;
            this.btnKeyZoomIn.Click += new System.EventHandler(this.btnKeyZoomIn_Click);
            // 
            // lblZoomIn
            // 
            this.lblZoomIn.AutoSize = true;
            this.lblZoomIn.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblZoomIn.Location = new System.Drawing.Point(121, 178);
            this.lblZoomIn.Name = "lblZoomIn";
            this.lblZoomIn.Size = new System.Drawing.Size(78, 16);
            this.lblZoomIn.TabIndex = 15;
            this.lblZoomIn.Text = "Zoom in (-5)";
            // 
            // lblResetDefault
            // 
            this.lblResetDefault.AutoSize = true;
            this.lblResetDefault.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblResetDefault.Location = new System.Drawing.Point(121, 200);
            this.lblResetDefault.Name = "lblResetDefault";
            this.lblResetDefault.Size = new System.Drawing.Size(126, 16);
            this.lblResetDefault.TabIndex = 16;
            this.lblResetDefault.Text = "Reset to default (65)";
            // 
            // btnKeyReset
            // 
            this.btnKeyReset.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnKeyReset.ForeColor = System.Drawing.Color.MidnightBlue;
            this.btnKeyReset.Location = new System.Drawing.Point(10, 197);
            this.btnKeyReset.Name = "btnKeyReset";
            this.btnKeyReset.Size = new System.Drawing.Size(108, 23);
            this.btnKeyReset.TabIndex = 17;
            this.btnKeyReset.Text = "Numpad*";
            this.btnKeyReset.UseVisualStyleBackColor = true;
            this.btnKeyReset.Click += new System.EventHandler(this.btnKeyReset_Click);
            // 
            // chkHotkeys
            // 
            this.chkHotkeys.AutoSize = true;
            this.chkHotkeys.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.chkHotkeys.Location = new System.Drawing.Point(123, 73);
            this.chkHotkeys.Name = "chkHotkeys";
            this.chkHotkeys.RightToLeft = System.Windows.Forms.RightToLeft.Yes;
            this.chkHotkeys.Size = new System.Drawing.Size(124, 20);
            this.chkHotkeys.TabIndex = 18;
            this.chkHotkeys.Text = "Disable hotkeys";
            this.chkHotkeys.UseVisualStyleBackColor = true;
            this.chkHotkeys.CheckedChanged += new System.EventHandler(this.chkHotkeys_CheckedChanged);
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(258, 298);
            this.Controls.Add(this.chkHotkeys);
            this.Controls.Add(this.btnKeyReset);
            this.Controls.Add(this.lblResetDefault);
            this.Controls.Add(this.lblZoomIn);
            this.Controls.Add(this.btnKeyZoomIn);
            this.Controls.Add(this.btnKeyZoomOut);
            this.Controls.Add(this.lblUpdateAvail);
            this.Controls.Add(this.chkUpdate);
            this.Controls.Add(this.lblLink);
            this.Controls.Add(this.lblMMN);
            this.Controls.Add(this.lblInstructions);
            this.Controls.Add(this.lblVersion);
            this.Controls.Add(this.btnReset);
            this.Controls.Add(this.btnAbout);
            this.Controls.Add(this.btnExit);
            this.Controls.Add(this.numFoV);
            this.Controls.Add(this.btnStartGame);
            this.Controls.Add(this.lblGameState);
            this.Controls.Add(this.chkBeep);
            this.Controls.Add(this.lblFoV);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.Name = "MainForm";
            this.Text = "MW2 MP FoV Changer";
            this.Deactivate += new System.EventHandler(this.MainForm_Deactivate);
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.MainForm_FormClosing);
            this.MouseClick += new System.Windows.Forms.MouseEventHandler(this.MainForm_MouseClick);
            ((System.ComponentModel.ISupportInitialize)(this.numFoV)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Timer TimerHoldKey;
        private System.Windows.Forms.Timer TimerUpdate;
        private System.Windows.Forms.Label lblFoV;
        private System.Windows.Forms.Timer TimerCheck;
        private System.Windows.Forms.CheckBox chkBeep;
        private System.Windows.Forms.Label lblGameState;
        private System.Windows.Forms.Timer TimerVerif;
        private System.Windows.Forms.Button btnStartGame;
        private System.Windows.Forms.NumericUpDown numFoV;
        private System.Windows.Forms.Button btnExit;
        private System.Windows.Forms.Button btnAbout;
        private System.Windows.Forms.Button btnReset;
        private System.Windows.Forms.Label lblVersion;
        private System.Windows.Forms.Label lblInstructions;
        private System.Windows.Forms.Label lblMMN;
        private System.Windows.Forms.LinkLabel lblLink;
        private System.Windows.Forms.Timer TimerHTTP;
        private System.Windows.Forms.CheckBox chkUpdate;
        private System.Windows.Forms.Label lblUpdateAvail;
        private System.Windows.Forms.Timer TimerBlink;
        private System.Windows.Forms.Button btnKeyZoomOut;
        private System.Windows.Forms.Button btnKeyZoomIn;
        private System.Windows.Forms.Label lblZoomIn;
        private System.Windows.Forms.Label lblResetDefault;
        private System.Windows.Forms.Button btnKeyReset;
        private System.Windows.Forms.CheckBox chkHotkeys;

    }
}

