using System;

namespace MultiCoD_FoV_Changer
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
            base.Dispose(disposing);
        }

        #region Code

        /// <summary>
        /// Méthode requise pour la prise en charge du concepteur - ne modifiez pas
        /// le contenu de cette méthode avec l'éditeur de code.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.TimerHoldKey = new System.Windows.Forms.Timer(this.components);
            this.TimerUpdate = new System.Windows.Forms.Timer(this.components);
            this.lblFoV = new System.Windows.Forms.Label();
            this.TimerCheck = new System.Windows.Forms.Timer(this.components);
            this.chkBeep = new System.Windows.Forms.CheckBox();
            this.TimerVerif = new System.Windows.Forms.Timer(this.components);
            this.numFoV = new System.Windows.Forms.NumericUpDown();
            this.btnAbout = new System.Windows.Forms.Button();
            this.lblVersion = new System.Windows.Forms.Label();
            this.lblInstructions = new System.Windows.Forms.Label();
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
            this.ToolTip1 = new System.Windows.Forms.ToolTip(this.components);
            this.lblGameStatus = new System.Windows.Forms.Label();
            this.numFPS = new System.Windows.Forms.NumericUpDown();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.btnExit = new System.Windows.Forms.Button();
            this.lblFPS = new System.Windows.Forms.Label();
            this.numFoVScale = new System.Windows.Forms.NumericUpDown();
            this.lblFoVScale = new System.Windows.Forms.Label();
            this.lblEffectiveFoV = new System.Windows.Forms.Label();
            this.lblEffectiveFoVMax = new System.Windows.Forms.Label();
            this.lblEffectiveFoVNum = new System.Windows.Forms.Label();
            this.btnKeyOffOn = new System.Windows.Forms.Button();
            this.lblKeyOffOn = new System.Windows.Forms.Label();
            this.cbKeyOffOnMode = new System.Windows.Forms.ComboBox();
            ((System.ComponentModel.ISupportInitialize)(this.numFoV)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numFPS)).BeginInit();
            this.groupBox1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numFoVScale)).BeginInit();
            this.SuspendLayout();
            // 
            // TimerHoldKey
            // 
            this.TimerHoldKey.Interval = 350;
            this.TimerHoldKey.Tick += new System.EventHandler(this.TimerHoldKey_Tick);
            // 
            // TimerUpdate
            // 
            this.TimerUpdate.Interval = 250;
            this.TimerUpdate.Tick += new System.EventHandler(this.TimerUpdate_Tick);
            // 
            // lblFoV
            // 
            this.lblFoV.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblFoV.Location = new System.Drawing.Point(12, 60);
            this.lblFoV.Name = "lblFoV";
            this.lblFoV.Size = new System.Drawing.Size(131, 20);
            this.lblFoV.TabIndex = 0;
            this.lblFoV.Text = "Field of View";
            this.lblFoV.TextAlign = System.Drawing.ContentAlignment.TopRight;
            // 
            // TimerCheck
            // 
            this.TimerCheck.Interval = 3000;
            this.TimerCheck.Tick += new System.EventHandler(this.TimerCheck_Tick);
            // 
            // chkBeep
            // 
            this.chkBeep.AutoSize = true;
            this.chkBeep.Checked = true;
            this.chkBeep.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkBeep.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.chkBeep.Location = new System.Drawing.Point(29, 175);
            this.chkBeep.Name = "chkBeep";
            this.chkBeep.Size = new System.Drawing.Size(130, 20);
            this.chkBeep.TabIndex = 5;
            this.chkBeep.Text = "Beep on success";
            this.chkBeep.UseVisualStyleBackColor = true;
            this.chkBeep.CheckedChanged += new System.EventHandler(this.chkBeep_CheckedChanged);
            // 
            // TimerVerif
            // 
            this.TimerVerif.Interval = 3000;
            this.TimerVerif.Tick += new System.EventHandler(this.TimerVerif_Tick);
            // 
            // numFoV
            // 
            this.numFoV.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.numFoV.DecimalPlaces = 1;
            this.numFoV.Enabled = false;
            this.numFoV.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.numFoV.Location = new System.Drawing.Point(152, 61);
            this.numFoV.Maximum = new decimal(new int[] {
            999,
            0,
            0,
            0});
            this.numFoV.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.numFoV.MinimumSize = new System.Drawing.Size(58, 0);
            this.numFoV.Name = "numFoV";
            this.numFoV.Size = new System.Drawing.Size(64, 22);
            this.numFoV.TabIndex = 0;
            this.numFoV.Value = new decimal(new int[] {
            90,
            0,
            0,
            0});
            this.numFoV.ValueChanged += new System.EventHandler(this.numFoV_ValueChanged);
            // 
            // btnAbout
            // 
            this.btnAbout.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnAbout.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnAbout.Location = new System.Drawing.Point(132, 391);
            this.btnAbout.Name = "btnAbout";
            this.btnAbout.Size = new System.Drawing.Size(70, 30);
            this.btnAbout.TabIndex = 19;
            this.btnAbout.Text = "About";
            this.btnAbout.UseVisualStyleBackColor = true;
            this.btnAbout.Click += new System.EventHandler(this.btnAbout_Click);
            // 
            // lblVersion
            // 
            this.lblVersion.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.lblVersion.AutoSize = true;
            this.lblVersion.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblVersion.Location = new System.Drawing.Point(23, 398);
            this.lblVersion.Name = "lblVersion";
            this.lblVersion.Size = new System.Drawing.Size(60, 16);
            this.lblVersion.TabIndex = 0;
            this.lblVersion.Text = "v1.x.xxx.x";
            this.lblVersion.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.lblVersion.Visible = false;
            // 
            // lblInstructions
            // 
            this.lblInstructions.AutoSize = true;
            this.lblInstructions.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblInstructions.Location = new System.Drawing.Point(139, 247);
            this.lblInstructions.Name = "lblInstructions";
            this.lblInstructions.Size = new System.Drawing.Size(88, 16);
            this.lblInstructions.TabIndex = 8;
            this.lblInstructions.Text = "Zoom out (+1)";
            // 
            // lblLink
            // 
            this.lblLink.Anchor = System.Windows.Forms.AnchorStyles.Bottom;
            this.lblLink.AutoSize = true;
            this.lblLink.BackColor = System.Drawing.SystemColors.Control;
            this.lblLink.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblLink.LinkBehavior = System.Windows.Forms.LinkBehavior.HoverUnderline;
            this.lblLink.Location = new System.Drawing.Point(43, 367);
            this.lblLink.Margin = new System.Windows.Forms.Padding(0);
            this.lblLink.Name = "lblLink";
            this.lblLink.Size = new System.Drawing.Size(208, 13);
            this.lblLink.TabIndex = 18;
            this.lblLink.TabStop = true;
            this.lblLink.Text = "github.com/AgentRev/CoD-FoV-Changers";
            this.lblLink.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.lblLink_LinkClicked);
            // 
            // chkUpdate
            // 
            this.chkUpdate.AutoSize = true;
            this.chkUpdate.Checked = true;
            this.chkUpdate.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkUpdate.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.chkUpdate.Location = new System.Drawing.Point(29, 196);
            this.chkUpdate.Name = "chkUpdate";
            this.chkUpdate.Size = new System.Drawing.Size(198, 20);
            this.chkUpdate.TabIndex = 6;
            this.chkUpdate.Text = "Notify when update available";
            this.chkUpdate.UseVisualStyleBackColor = true;
            this.chkUpdate.CheckedChanged += new System.EventHandler(this.chkUpdate_CheckedChanged);
            // 
            // lblUpdateAvail
            // 
            this.lblUpdateAvail.Anchor = System.Windows.Forms.AnchorStyles.Bottom;
            this.lblUpdateAvail.AutoSize = true;
            this.lblUpdateAvail.BackColor = System.Drawing.SystemColors.Control;
            this.lblUpdateAvail.Enabled = false;
            this.lblUpdateAvail.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblUpdateAvail.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(180)))), ((int)(((byte)(0)))), ((int)(((byte)(0)))));
            this.lblUpdateAvail.Location = new System.Drawing.Point(75, 345);
            this.lblUpdateAvail.Name = "lblUpdateAvail";
            this.lblUpdateAvail.Size = new System.Drawing.Size(144, 13);
            this.lblUpdateAvail.TabIndex = 17;
            this.lblUpdateAvail.Text = "Update v1.x.xxx.x available";
            this.lblUpdateAvail.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
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
            this.btnKeyZoomOut.Location = new System.Drawing.Point(28, 243);
            this.btnKeyZoomOut.Name = "btnKeyZoomOut";
            this.btnKeyZoomOut.Size = new System.Drawing.Size(108, 23);
            this.btnKeyZoomOut.TabIndex = 10;
            this.btnKeyZoomOut.Text = "Numpad−";
            this.btnKeyZoomOut.UseVisualStyleBackColor = true;
            this.btnKeyZoomOut.Click += new System.EventHandler(this.btnKeyZoomOut_Click);
            // 
            // btnKeyZoomIn
            // 
            this.btnKeyZoomIn.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnKeyZoomIn.Location = new System.Drawing.Point(28, 266);
            this.btnKeyZoomIn.Name = "btnKeyZoomIn";
            this.btnKeyZoomIn.Size = new System.Drawing.Size(108, 23);
            this.btnKeyZoomIn.TabIndex = 11;
            this.btnKeyZoomIn.Text = "Numpad+";
            this.btnKeyZoomIn.UseVisualStyleBackColor = true;
            this.btnKeyZoomIn.Click += new System.EventHandler(this.btnKeyZoomIn_Click);
            // 
            // lblZoomIn
            // 
            this.lblZoomIn.AutoSize = true;
            this.lblZoomIn.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblZoomIn.Location = new System.Drawing.Point(139, 270);
            this.lblZoomIn.Name = "lblZoomIn";
            this.lblZoomIn.Size = new System.Drawing.Size(80, 16);
            this.lblZoomIn.TabIndex = 15;
            this.lblZoomIn.Text = "Zoom in (−1)";
            // 
            // lblResetDefault
            // 
            this.lblResetDefault.AutoSize = true;
            this.lblResetDefault.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblResetDefault.Location = new System.Drawing.Point(139, 293);
            this.lblResetDefault.Name = "lblResetDefault";
            this.lblResetDefault.Size = new System.Drawing.Size(125, 16);
            this.lblResetDefault.TabIndex = 16;
            this.lblResetDefault.Text = "Reset to default (65)";
            // 
            // btnKeyReset
            // 
            this.btnKeyReset.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnKeyReset.Location = new System.Drawing.Point(28, 289);
            this.btnKeyReset.Name = "btnKeyReset";
            this.btnKeyReset.Size = new System.Drawing.Size(108, 23);
            this.btnKeyReset.TabIndex = 12;
            this.btnKeyReset.Text = "Numpad*";
            this.btnKeyReset.UseVisualStyleBackColor = true;
            this.btnKeyReset.Click += new System.EventHandler(this.btnKeyReset_Click);
            // 
            // chkHotkeys
            // 
            this.chkHotkeys.AutoSize = true;
            this.chkHotkeys.Checked = true;
            this.chkHotkeys.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkHotkeys.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.chkHotkeys.Location = new System.Drawing.Point(29, 217);
            this.chkHotkeys.Name = "chkHotkeys";
            this.chkHotkeys.Size = new System.Drawing.Size(225, 20);
            this.chkHotkeys.TabIndex = 7;
            this.chkHotkeys.Text = "Enable keyboard/mouse hotkeys";
            this.chkHotkeys.UseVisualStyleBackColor = true;
            this.chkHotkeys.CheckedChanged += new System.EventHandler(this.chkHotkeys_CheckedChanged);
            // 
            // ToolTip1
            // 
            this.ToolTip1.AutoPopDelay = 10000;
            this.ToolTip1.InitialDelay = 500;
            this.ToolTip1.ReshowDelay = 100;
            // 
            // lblGameStatus
            // 
            this.lblGameStatus.Font = new System.Drawing.Font("Microsoft Sans Serif", 18F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblGameStatus.Location = new System.Drawing.Point(5, 11);
            this.lblGameStatus.Name = "lblGameStatus";
            this.lblGameStatus.Size = new System.Drawing.Size(260, 31);
            this.lblGameStatus.TabIndex = 21;
            this.lblGameStatus.Text = "Awaiting game...";
            this.lblGameStatus.TextAlign = System.Drawing.ContentAlignment.TopCenter;
            this.ToolTip1.SetToolTip(this.lblGameStatus, "Start a mission or join a match! Compatible with:\nMW1 \'07, WaW, MW2 \'09, BO1, MW3" +
        " \'11, BO2, Ghosts, AW");
            // 
            // numFPS
            // 
            this.numFPS.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.numFPS.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.numFPS.Location = new System.Drawing.Point(152, 145);
            this.numFPS.Maximum = new decimal(new int[] {
            1000,
            0,
            0,
            0});
            this.numFPS.MinimumSize = new System.Drawing.Size(58, 0);
            this.numFPS.Name = "numFPS";
            this.numFPS.Size = new System.Drawing.Size(64, 22);
            this.numFPS.TabIndex = 2;
            this.ToolTip1.SetToolTip(this.numFPS, "While you can set it to 0 (infinite), it is not recommended, as it can cause stut" +
        "ter.");
            this.numFPS.Value = new decimal(new int[] {
            200,
            0,
            0,
            0});
            this.numFPS.ValueChanged += new System.EventHandler(this.numFPS_ValueChanged);
            this.numFPS.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.numFPS_KeyPress);
            this.numFPS.Leave += new System.EventHandler(this.numFPS_Leave);
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.lblGameStatus);
            this.groupBox1.Location = new System.Drawing.Point(12, 4);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(270, 48);
            this.groupBox1.TabIndex = 22;
            this.groupBox1.TabStop = false;
            // 
            // btnExit
            // 
            this.btnExit.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnExit.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnExit.Location = new System.Drawing.Point(212, 391);
            this.btnExit.Name = "btnExit";
            this.btnExit.Size = new System.Drawing.Size(70, 30);
            this.btnExit.TabIndex = 20;
            this.btnExit.Text = "Exit";
            this.btnExit.UseVisualStyleBackColor = true;
            this.btnExit.Click += new System.EventHandler(this.btnExit_Click);
            // 
            // lblFPS
            // 
            this.lblFPS.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblFPS.Location = new System.Drawing.Point(12, 144);
            this.lblFPS.Name = "lblFPS";
            this.lblFPS.Size = new System.Drawing.Size(131, 20);
            this.lblFPS.TabIndex = 23;
            this.lblFPS.Text = "Max FPS";
            this.lblFPS.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // numFoVScale
            // 
            this.numFoVScale.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.numFoVScale.DecimalPlaces = 3;
            this.numFoVScale.Enabled = false;
            this.numFoVScale.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.numFoVScale.Increment = new decimal(new int[] {
            1,
            0,
            0,
            131072});
            this.numFoVScale.Location = new System.Drawing.Point(152, 89);
            this.numFoVScale.Maximum = new decimal(new int[] {
            999,
            0,
            0,
            131072});
            this.numFoVScale.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            131072});
            this.numFoVScale.MinimumSize = new System.Drawing.Size(58, 0);
            this.numFoVScale.Name = "numFoVScale";
            this.numFoVScale.Size = new System.Drawing.Size(64, 22);
            this.numFoVScale.TabIndex = 1;
            this.numFoVScale.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.numFoVScale.ValueChanged += new System.EventHandler(this.numFoVScale_ValueChanged);
            this.numFoVScale.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.numFoVScale_KeyPress);
            this.numFoVScale.Leave += new System.EventHandler(this.numFoVScale_Leave);
            // 
            // lblFoVScale
            // 
            this.lblFoVScale.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblFoVScale.Location = new System.Drawing.Point(12, 88);
            this.lblFoVScale.Name = "lblFoVScale";
            this.lblFoVScale.Size = new System.Drawing.Size(131, 20);
            this.lblFoVScale.TabIndex = 25;
            this.lblFoVScale.Text = "FoV Scale";
            this.lblFoVScale.TextAlign = System.Drawing.ContentAlignment.TopRight;
            // 
            // lblEffectiveFoV
            // 
            this.lblEffectiveFoV.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblEffectiveFoV.Location = new System.Drawing.Point(12, 117);
            this.lblEffectiveFoV.Name = "lblEffectiveFoV";
            this.lblEffectiveFoV.Size = new System.Drawing.Size(131, 20);
            this.lblEffectiveFoV.TabIndex = 26;
            this.lblEffectiveFoV.Text = "Effective FoV";
            this.lblEffectiveFoV.TextAlign = System.Drawing.ContentAlignment.TopRight;
            // 
            // lblEffectiveFoVMax
            // 
            this.lblEffectiveFoVMax.AutoSize = true;
            this.lblEffectiveFoVMax.Location = new System.Drawing.Point(220, 122);
            this.lblEffectiveFoVMax.Name = "lblEffectiveFoVMax";
            this.lblEffectiveFoVMax.Size = new System.Drawing.Size(53, 13);
            this.lblEffectiveFoVMax.TabIndex = 28;
            this.lblEffectiveFoVMax.Text = "(max 100)";
            // 
            // lblEffectiveFoVNum
            // 
            this.lblEffectiveFoVNum.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblEffectiveFoVNum.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblEffectiveFoVNum.Location = new System.Drawing.Point(152, 117);
            this.lblEffectiveFoVNum.Name = "lblEffectiveFoVNum";
            this.lblEffectiveFoVNum.Size = new System.Drawing.Size(64, 22);
            this.lblEffectiveFoVNum.TabIndex = 29;
            this.lblEffectiveFoVNum.Text = "90.0";
            this.lblEffectiveFoVNum.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // btnKeyOffOn
            // 
            this.btnKeyOffOn.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnKeyOffOn.Location = new System.Drawing.Point(28, 312);
            this.btnKeyOffOn.Name = "btnKeyOffOn";
            this.btnKeyOffOn.Size = new System.Drawing.Size(108, 23);
            this.btnKeyOffOn.TabIndex = 13;
            this.btnKeyOffOn.Text = "Numpad/";
            this.btnKeyOffOn.UseVisualStyleBackColor = true;
            this.btnKeyOffOn.Click += new System.EventHandler(this.btnKeyOffOn_Click);
            // 
            // lblKeyOffOn
            // 
            this.lblKeyOffOn.AutoSize = true;
            this.lblKeyOffOn.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblKeyOffOn.Location = new System.Drawing.Point(209, 316);
            this.lblKeyOffOn.Name = "lblKeyOffOn";
            this.lblKeyOffOn.Size = new System.Drawing.Size(50, 16);
            this.lblKeyOffOn.TabIndex = 30;
            this.lblKeyOffOn.Text = "Off / On";
            // 
            // cbKeyOffOnMode
            // 
            this.cbKeyOffOnMode.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbKeyOffOnMode.FormattingEnabled = true;
            this.cbKeyOffOnMode.Items.AddRange(new object[] {
            "Toggle",
            "Hold"});
            this.cbKeyOffOnMode.Location = new System.Drawing.Point(141, 313);
            this.cbKeyOffOnMode.Name = "cbKeyOffOnMode";
            this.cbKeyOffOnMode.Size = new System.Drawing.Size(64, 21);
            this.cbKeyOffOnMode.TabIndex = 14;
            this.cbKeyOffOnMode.SelectedIndexChanged += new System.EventHandler(this.cbKeyOffOnMode_SelectedIndexChanged);
            // 
            // MainForm
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.ClientSize = new System.Drawing.Size(294, 431);
            this.Controls.Add(this.cbKeyOffOnMode);
            this.Controls.Add(this.lblKeyOffOn);
            this.Controls.Add(this.btnKeyOffOn);
            this.Controls.Add(this.lblEffectiveFoVNum);
            this.Controls.Add(this.lblEffectiveFoVMax);
            this.Controls.Add(this.lblEffectiveFoV);
            this.Controls.Add(this.numFoVScale);
            this.Controls.Add(this.lblFoVScale);
            this.Controls.Add(this.btnExit);
            this.Controls.Add(this.btnAbout);
            this.Controls.Add(this.numFPS);
            this.Controls.Add(this.lblFPS);
            this.Controls.Add(this.chkHotkeys);
            this.Controls.Add(this.btnKeyReset);
            this.Controls.Add(this.lblResetDefault);
            this.Controls.Add(this.lblZoomIn);
            this.Controls.Add(this.btnKeyZoomIn);
            this.Controls.Add(this.btnKeyZoomOut);
            this.Controls.Add(this.lblUpdateAvail);
            this.Controls.Add(this.chkUpdate);
            this.Controls.Add(this.lblLink);
            this.Controls.Add(this.lblInstructions);
            this.Controls.Add(this.numFoV);
            this.Controls.Add(this.chkBeep);
            this.Controls.Add(this.lblFoV);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.lblVersion);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Icon = global::MultiCoD_FoV_Changer.Properties.Resources.multicodfovico;
            this.MaximizeBox = false;
            this.Name = "MainForm";
            this.Text = "MultiCoD FoV Changer";
            this.Deactivate += new System.EventHandler(this.MainForm_Deactivate);
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.MainForm_FormClosing);
            this.MouseClick += new System.Windows.Forms.MouseEventHandler(this.MainForm_MouseClick);
            ((System.ComponentModel.ISupportInitialize)(this.numFoV)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numFPS)).EndInit();
            this.groupBox1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.numFoVScale)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Timer TimerHoldKey;
        private System.Windows.Forms.Timer TimerUpdate;
        private System.Windows.Forms.Label lblFoV;
        private System.Windows.Forms.Timer TimerCheck;
        private System.Windows.Forms.CheckBox chkBeep;
        private System.Windows.Forms.Timer TimerVerif;
        private System.Windows.Forms.NumericUpDown numFoV;
        private System.Windows.Forms.Button btnAbout;
        private System.Windows.Forms.Label lblVersion;
        private System.Windows.Forms.Label lblInstructions;
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
        private System.Windows.Forms.ToolTip ToolTip1;
        private System.Windows.Forms.Label lblGameStatus;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Button btnExit;
        private System.Windows.Forms.NumericUpDown numFPS;
        private System.Windows.Forms.Label lblFPS;
        private System.Windows.Forms.NumericUpDown numFoVScale;
        private System.Windows.Forms.Label lblFoVScale;
        private System.Windows.Forms.Label lblEffectiveFoV;
        private System.Windows.Forms.Label lblEffectiveFoVMax;
        private System.Windows.Forms.Label lblEffectiveFoVNum;
        private System.Windows.Forms.Button btnKeyOffOn;
        private System.Windows.Forms.Label lblKeyOffOn;
        private System.Windows.Forms.ComboBox cbKeyOffOnMode;
    }
}

