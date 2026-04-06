namespace BotExchangeRateWinForms.Forms
{
    partial class MainForm
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.Label lblSourceUrl;
        private System.Windows.Forms.TextBox txtSourceUrl;
        private System.Windows.Forms.Label lblConnectionString;
        private System.Windows.Forms.TextBox txtConnectionString;
        private System.Windows.Forms.CheckBox chkWriteToDatabase;
        private System.Windows.Forms.Label lblInterval;
        private System.Windows.Forms.NumericUpDown numIntervalValue;
        private System.Windows.Forms.ComboBox cmbIntervalUnit;
        private System.Windows.Forms.Label lblTimeout;
        private System.Windows.Forms.NumericUpDown numTimeoutSeconds;
        private System.Windows.Forms.Button btnSaveSettings;
        private System.Windows.Forms.Button btnInitializeDatabase;
        private System.Windows.Forms.Button btnRunNow;
        private System.Windows.Forms.Button btnStartTimer;
        private System.Windows.Forms.Button btnStopTimer;
        private System.Windows.Forms.TextBox txtLog;
        private System.Windows.Forms.GroupBox grpSettings;
        private System.Windows.Forms.GroupBox grpStatus;
        private System.Windows.Forms.Label lblStatus;
        private System.Windows.Forms.Label lblStatusValue;
        private System.Windows.Forms.Label lblTimerEnabled;
        private System.Windows.Forms.Label lblTimerEnabledValue;
        private System.Windows.Forms.Label lblNextRun;
        private System.Windows.Forms.Label lblNextRunValue;
        private System.Windows.Forms.Label lblLastRun;
        private System.Windows.Forms.Label lblLastRunValue;
        private System.Windows.Forms.Timer pollTimer;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }

            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.lblSourceUrl = new System.Windows.Forms.Label();
            this.txtSourceUrl = new System.Windows.Forms.TextBox();
            this.lblConnectionString = new System.Windows.Forms.Label();
            this.txtConnectionString = new System.Windows.Forms.TextBox();
            this.chkWriteToDatabase = new System.Windows.Forms.CheckBox();
            this.lblInterval = new System.Windows.Forms.Label();
            this.numIntervalValue = new System.Windows.Forms.NumericUpDown();
            this.cmbIntervalUnit = new System.Windows.Forms.ComboBox();
            this.lblTimeout = new System.Windows.Forms.Label();
            this.numTimeoutSeconds = new System.Windows.Forms.NumericUpDown();
            this.btnSaveSettings = new System.Windows.Forms.Button();
            this.btnInitializeDatabase = new System.Windows.Forms.Button();
            this.btnRunNow = new System.Windows.Forms.Button();
            this.btnStartTimer = new System.Windows.Forms.Button();
            this.btnStopTimer = new System.Windows.Forms.Button();
            this.txtLog = new System.Windows.Forms.TextBox();
            this.grpSettings = new System.Windows.Forms.GroupBox();
            this.grpStatus = new System.Windows.Forms.GroupBox();
            this.lblStatus = new System.Windows.Forms.Label();
            this.lblStatusValue = new System.Windows.Forms.Label();
            this.lblTimerEnabled = new System.Windows.Forms.Label();
            this.lblTimerEnabledValue = new System.Windows.Forms.Label();
            this.lblNextRun = new System.Windows.Forms.Label();
            this.lblNextRunValue = new System.Windows.Forms.Label();
            this.lblLastRun = new System.Windows.Forms.Label();
            this.lblLastRunValue = new System.Windows.Forms.Label();
            this.pollTimer = new System.Windows.Forms.Timer(this.components);
            ((System.ComponentModel.ISupportInitialize)(this.numIntervalValue)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numTimeoutSeconds)).BeginInit();
            this.grpSettings.SuspendLayout();
            this.grpStatus.SuspendLayout();
            this.SuspendLayout();
            // 
            // lblSourceUrl
            // 
            this.lblSourceUrl.AutoSize = true;
            this.lblSourceUrl.Location = new System.Drawing.Point(18, 32);
            this.lblSourceUrl.Name = "lblSourceUrl";
            this.lblSourceUrl.Size = new System.Drawing.Size(53, 12);
            this.lblSourceUrl.TabIndex = 0;
            this.lblSourceUrl.Text = "\u4f86\u6e90\u7db2\u5740";
            // 
            // txtSourceUrl
            // 
            this.txtSourceUrl.Location = new System.Drawing.Point(108, 28);
            this.txtSourceUrl.Name = "txtSourceUrl";
            this.txtSourceUrl.Size = new System.Drawing.Size(664, 22);
            this.txtSourceUrl.TabIndex = 1;
            // 
            // lblConnectionString
            // 
            this.lblConnectionString.AutoSize = true;
            this.lblConnectionString.Location = new System.Drawing.Point(18, 68);
            this.lblConnectionString.Name = "lblConnectionString";
            this.lblConnectionString.Size = new System.Drawing.Size(83, 12);
            this.lblConnectionString.TabIndex = 2;
            this.lblConnectionString.Text = "MSSQL \u9023\u7dda\u5b57\u4e32";
            // 
            // txtConnectionString
            // 
            this.txtConnectionString.Location = new System.Drawing.Point(108, 64);
            this.txtConnectionString.Name = "txtConnectionString";
            this.txtConnectionString.Size = new System.Drawing.Size(664, 22);
            this.txtConnectionString.TabIndex = 3;
            // 
            // chkWriteToDatabase
            // 
            this.chkWriteToDatabase.AutoSize = true;
            this.chkWriteToDatabase.Location = new System.Drawing.Point(108, 98);
            this.chkWriteToDatabase.Name = "chkWriteToDatabase";
            this.chkWriteToDatabase.Size = new System.Drawing.Size(132, 16);
            this.chkWriteToDatabase.TabIndex = 4;
            this.chkWriteToDatabase.Text = "\u5beb\u5165 MSSQL \u8cc7\u6599\u5eab";
            this.chkWriteToDatabase.UseVisualStyleBackColor = true;
            // 
            // lblInterval
            // 
            this.lblInterval.AutoSize = true;
            this.lblInterval.Location = new System.Drawing.Point(18, 131);
            this.lblInterval.Name = "lblInterval";
            this.lblInterval.Size = new System.Drawing.Size(53, 12);
            this.lblInterval.TabIndex = 5;
            this.lblInterval.Text = "\u6293\u53d6\u983b\u7387";
            // 
            // numIntervalValue
            // 
            this.numIntervalValue.Location = new System.Drawing.Point(108, 127);
            this.numIntervalValue.Maximum = new decimal(new int[] {
            9999,
            0,
            0,
            0});
            this.numIntervalValue.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.numIntervalValue.Name = "numIntervalValue";
            this.numIntervalValue.Size = new System.Drawing.Size(82, 22);
            this.numIntervalValue.TabIndex = 6;
            this.numIntervalValue.Value = new decimal(new int[] {
            30,
            0,
            0,
            0});
            // 
            // cmbIntervalUnit
            // 
            this.cmbIntervalUnit.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbIntervalUnit.FormattingEnabled = true;
            this.cmbIntervalUnit.Items.AddRange(new object[] {
            "\u5206\u9418",
            "\u5c0f\u6642"});
            this.cmbIntervalUnit.Location = new System.Drawing.Point(204, 127);
            this.cmbIntervalUnit.Name = "cmbIntervalUnit";
            this.cmbIntervalUnit.Size = new System.Drawing.Size(80, 20);
            this.cmbIntervalUnit.TabIndex = 7;
            // 
            // lblTimeout
            // 
            this.lblTimeout.AutoSize = true;
            this.lblTimeout.Location = new System.Drawing.Point(320, 131);
            this.lblTimeout.Name = "lblTimeout";
            this.lblTimeout.Size = new System.Drawing.Size(77, 12);
            this.lblTimeout.TabIndex = 8;
            this.lblTimeout.Text = "\u903e\u6642\u79d2\u6578 (\u79d2)";
            // 
            // numTimeoutSeconds
            // 
            this.numTimeoutSeconds.Location = new System.Drawing.Point(403, 127);
            this.numTimeoutSeconds.Maximum = new decimal(new int[] {
            300,
            0,
            0,
            0});
            this.numTimeoutSeconds.Minimum = new decimal(new int[] {
            5,
            0,
            0,
            0});
            this.numTimeoutSeconds.Name = "numTimeoutSeconds";
            this.numTimeoutSeconds.Size = new System.Drawing.Size(82, 22);
            this.numTimeoutSeconds.TabIndex = 9;
            this.numTimeoutSeconds.Value = new decimal(new int[] {
            30,
            0,
            0,
            0});
            // 
            // btnSaveSettings
            // 
            this.btnSaveSettings.Location = new System.Drawing.Point(20, 171);
            this.btnSaveSettings.Name = "btnSaveSettings";
            this.btnSaveSettings.Size = new System.Drawing.Size(116, 32);
            this.btnSaveSettings.TabIndex = 10;
            this.btnSaveSettings.Text = "\u5132\u5b58\u8a2d\u5b9a";
            this.btnSaveSettings.UseVisualStyleBackColor = true;
            this.btnSaveSettings.Click += new System.EventHandler(this.btnSaveSettings_Click);
            // 
            // btnInitializeDatabase
            // 
            this.btnInitializeDatabase.Location = new System.Drawing.Point(152, 171);
            this.btnInitializeDatabase.Name = "btnInitializeDatabase";
            this.btnInitializeDatabase.Size = new System.Drawing.Size(116, 32);
            this.btnInitializeDatabase.TabIndex = 11;
            this.btnInitializeDatabase.Text = "\u521d\u59cb\u5316\u8cc7\u6599\u5eab";
            this.btnInitializeDatabase.UseVisualStyleBackColor = true;
            this.btnInitializeDatabase.Click += new System.EventHandler(this.btnInitializeDatabase_Click);
            // 
            // btnRunNow
            // 
            this.btnRunNow.Location = new System.Drawing.Point(284, 171);
            this.btnRunNow.Name = "btnRunNow";
            this.btnRunNow.Size = new System.Drawing.Size(116, 32);
            this.btnRunNow.TabIndex = 12;
            this.btnRunNow.Text = "\u7acb\u5373\u6293\u53d6\u4e00\u6b21";
            this.btnRunNow.UseVisualStyleBackColor = true;
            this.btnRunNow.Click += new System.EventHandler(this.btnRunNow_Click);
            // 
            // btnStartTimer
            // 
            this.btnStartTimer.Location = new System.Drawing.Point(416, 171);
            this.btnStartTimer.Name = "btnStartTimer";
            this.btnStartTimer.Size = new System.Drawing.Size(116, 32);
            this.btnStartTimer.TabIndex = 13;
            this.btnStartTimer.Text = "\u555f\u52d5 Timer";
            this.btnStartTimer.UseVisualStyleBackColor = true;
            this.btnStartTimer.Click += new System.EventHandler(this.btnStartTimer_Click);
            // 
            // btnStopTimer
            // 
            this.btnStopTimer.Location = new System.Drawing.Point(548, 171);
            this.btnStopTimer.Name = "btnStopTimer";
            this.btnStopTimer.Size = new System.Drawing.Size(116, 32);
            this.btnStopTimer.TabIndex = 14;
            this.btnStopTimer.Text = "\u505c\u6b62 Timer";
            this.btnStopTimer.UseVisualStyleBackColor = true;
            this.btnStopTimer.Click += new System.EventHandler(this.btnStopTimer_Click);
            // 
            // txtLog
            // 
            this.txtLog.Location = new System.Drawing.Point(20, 386);
            this.txtLog.Multiline = true;
            this.txtLog.Name = "txtLog";
            this.txtLog.ReadOnly = true;
            this.txtLog.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.txtLog.Size = new System.Drawing.Size(796, 188);
            this.txtLog.TabIndex = 14;
            // 
            // grpSettings
            // 
            this.grpSettings.Controls.Add(this.lblSourceUrl);
            this.grpSettings.Controls.Add(this.txtSourceUrl);
            this.grpSettings.Controls.Add(this.lblConnectionString);
            this.grpSettings.Controls.Add(this.txtConnectionString);
            this.grpSettings.Controls.Add(this.chkWriteToDatabase);
            this.grpSettings.Controls.Add(this.lblInterval);
            this.grpSettings.Controls.Add(this.numIntervalValue);
            this.grpSettings.Controls.Add(this.cmbIntervalUnit);
            this.grpSettings.Controls.Add(this.lblTimeout);
            this.grpSettings.Controls.Add(this.numTimeoutSeconds);
            this.grpSettings.Controls.Add(this.btnSaveSettings);
            this.grpSettings.Controls.Add(this.btnInitializeDatabase);
            this.grpSettings.Controls.Add(this.btnRunNow);
            this.grpSettings.Controls.Add(this.btnStartTimer);
            this.grpSettings.Controls.Add(this.btnStopTimer);
            this.grpSettings.Location = new System.Drawing.Point(20, 18);
            this.grpSettings.Name = "grpSettings";
            this.grpSettings.Size = new System.Drawing.Size(796, 223);
            this.grpSettings.TabIndex = 15;
            this.grpSettings.TabStop = false;
            this.grpSettings.Text = "\u8a2d\u5b9a\u8207\u63a7\u5236";
            // 
            // grpStatus
            // 
            this.grpStatus.Controls.Add(this.lblStatus);
            this.grpStatus.Controls.Add(this.lblStatusValue);
            this.grpStatus.Controls.Add(this.lblTimerEnabled);
            this.grpStatus.Controls.Add(this.lblTimerEnabledValue);
            this.grpStatus.Controls.Add(this.lblNextRun);
            this.grpStatus.Controls.Add(this.lblNextRunValue);
            this.grpStatus.Controls.Add(this.lblLastRun);
            this.grpStatus.Controls.Add(this.lblLastRunValue);
            this.grpStatus.Location = new System.Drawing.Point(20, 254);
            this.grpStatus.Name = "grpStatus";
            this.grpStatus.Size = new System.Drawing.Size(796, 114);
            this.grpStatus.TabIndex = 16;
            this.grpStatus.TabStop = false;
            this.grpStatus.Text = "\u57f7\u884c\u72c0\u614b";
            // 
            // lblStatus
            // 
            this.lblStatus.AutoSize = true;
            this.lblStatus.Location = new System.Drawing.Point(20, 29);
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Size = new System.Drawing.Size(53, 12);
            this.lblStatus.TabIndex = 0;
            this.lblStatus.Text = "\u76ee\u524d\u72c0\u614b";
            // 
            // lblStatusValue
            // 
            this.lblStatusValue.AutoSize = true;
            this.lblStatusValue.Location = new System.Drawing.Point(108, 29);
            this.lblStatusValue.Name = "lblStatusValue";
            this.lblStatusValue.Size = new System.Drawing.Size(9, 12);
            this.lblStatusValue.TabIndex = 1;
            this.lblStatusValue.Text = "-";
            // 
            // lblTimerEnabled
            // 
            this.lblTimerEnabled.AutoSize = true;
            this.lblTimerEnabled.Location = new System.Drawing.Point(20, 57);
            this.lblTimerEnabled.Name = "lblTimerEnabled";
            this.lblTimerEnabled.Size = new System.Drawing.Size(59, 12);
            this.lblTimerEnabled.TabIndex = 2;
            this.lblTimerEnabled.Text = "Timer \u72c0\u614b";
            // 
            // lblTimerEnabledValue
            // 
            this.lblTimerEnabledValue.AutoSize = true;
            this.lblTimerEnabledValue.Location = new System.Drawing.Point(108, 57);
            this.lblTimerEnabledValue.Name = "lblTimerEnabledValue";
            this.lblTimerEnabledValue.Size = new System.Drawing.Size(9, 12);
            this.lblTimerEnabledValue.TabIndex = 3;
            this.lblTimerEnabledValue.Text = "-";
            // 
            // lblNextRun
            // 
            this.lblNextRun.AutoSize = true;
            this.lblNextRun.Location = new System.Drawing.Point(410, 29);
            this.lblNextRun.Name = "lblNextRun";
            this.lblNextRun.Size = new System.Drawing.Size(53, 12);
            this.lblNextRun.TabIndex = 4;
            this.lblNextRun.Text = "\u4e0b\u6b21\u57f7\u884c";
            // 
            // lblNextRunValue
            // 
            this.lblNextRunValue.AutoSize = true;
            this.lblNextRunValue.Location = new System.Drawing.Point(498, 29);
            this.lblNextRunValue.Name = "lblNextRunValue";
            this.lblNextRunValue.Size = new System.Drawing.Size(9, 12);
            this.lblNextRunValue.TabIndex = 5;
            this.lblNextRunValue.Text = "-";
            // 
            // lblLastRun
            // 
            this.lblLastRun.AutoSize = true;
            this.lblLastRun.Location = new System.Drawing.Point(410, 57);
            this.lblLastRun.Name = "lblLastRun";
            this.lblLastRun.Size = new System.Drawing.Size(53, 12);
            this.lblLastRun.TabIndex = 6;
            this.lblLastRun.Text = "\u4e0a\u6b21\u6210\u529f";
            // 
            // lblLastRunValue
            // 
            this.lblLastRunValue.AutoSize = true;
            this.lblLastRunValue.Location = new System.Drawing.Point(498, 57);
            this.lblLastRunValue.Name = "lblLastRunValue";
            this.lblLastRunValue.Size = new System.Drawing.Size(9, 12);
            this.lblLastRunValue.TabIndex = 7;
            this.lblLastRunValue.Text = "-";
            // 
            // pollTimer
            // 
            this.pollTimer.Tick += new System.EventHandler(this.pollTimer_Tick);
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(838, 595);
            this.Controls.Add(this.grpStatus);
            this.Controls.Add(this.grpSettings);
            this.Controls.Add(this.txtLog);
            this.Font = new System.Drawing.Font("Microsoft JhengHei UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(136)));
            this.Name = "MainForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "\u81fa\u7063\u9280\u884c\u532f\u7387\u81ea\u52d5\u6293\u53d6";
            this.Load += new System.EventHandler(this.MainForm_Load);
            ((System.ComponentModel.ISupportInitialize)(this.numIntervalValue)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numTimeoutSeconds)).EndInit();
            this.grpSettings.ResumeLayout(false);
            this.grpSettings.PerformLayout();
            this.grpStatus.ResumeLayout(false);
            this.grpStatus.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();
        }
    }
}
