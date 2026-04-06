namespace BotExchangeRateWinForms.Forms
{
    partial class MainForm
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.Label lblSourceUrl;
        private System.Windows.Forms.TextBox txtSourceUrl;
        private System.Windows.Forms.Label lblConnectionString;
        private System.Windows.Forms.TextBox txtConnectionString;
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
            this.lblSourceUrl.Text = "來源網址";
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
            this.lblConnectionString.Text = "MSSQL 連線字串";
            // 
            // txtConnectionString
            // 
            this.txtConnectionString.Location = new System.Drawing.Point(108, 64);
            this.txtConnectionString.Name = "txtConnectionString";
            this.txtConnectionString.Size = new System.Drawing.Size(664, 22);
            this.txtConnectionString.TabIndex = 3;
            // 
            // lblInterval
            // 
            this.lblInterval.AutoSize = true;
            this.lblInterval.Location = new System.Drawing.Point(18, 105);
            this.lblInterval.Name = "lblInterval";
            this.lblInterval.Size = new System.Drawing.Size(53, 12);
            this.lblInterval.TabIndex = 4;
            this.lblInterval.Text = "抓取頻率";
            // 
            // numIntervalValue
            // 
            this.numIntervalValue.Location = new System.Drawing.Point(108, 101);
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
            this.numIntervalValue.TabIndex = 5;
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
            "分鐘",
            "小時"});
            this.cmbIntervalUnit.Location = new System.Drawing.Point(204, 101);
            this.cmbIntervalUnit.Name = "cmbIntervalUnit";
            this.cmbIntervalUnit.Size = new System.Drawing.Size(80, 20);
            this.cmbIntervalUnit.TabIndex = 6;
            // 
            // lblTimeout
            // 
            this.lblTimeout.AutoSize = true;
            this.lblTimeout.Location = new System.Drawing.Point(320, 105);
            this.lblTimeout.Name = "lblTimeout";
            this.lblTimeout.Size = new System.Drawing.Size(77, 12);
            this.lblTimeout.TabIndex = 7;
            this.lblTimeout.Text = "逾時秒數 (秒)";
            // 
            // numTimeoutSeconds
            // 
            this.numTimeoutSeconds.Location = new System.Drawing.Point(403, 101);
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
            this.numTimeoutSeconds.TabIndex = 8;
            this.numTimeoutSeconds.Value = new decimal(new int[] {
            30,
            0,
            0,
            0});
            // 
            // btnSaveSettings
            // 
            this.btnSaveSettings.Location = new System.Drawing.Point(20, 145);
            this.btnSaveSettings.Name = "btnSaveSettings";
            this.btnSaveSettings.Size = new System.Drawing.Size(116, 32);
            this.btnSaveSettings.TabIndex = 9;
            this.btnSaveSettings.Text = "儲存設定";
            this.btnSaveSettings.UseVisualStyleBackColor = true;
            this.btnSaveSettings.Click += new System.EventHandler(this.btnSaveSettings_Click);
            // 
            // btnInitializeDatabase
            // 
            this.btnInitializeDatabase.Location = new System.Drawing.Point(152, 145);
            this.btnInitializeDatabase.Name = "btnInitializeDatabase";
            this.btnInitializeDatabase.Size = new System.Drawing.Size(116, 32);
            this.btnInitializeDatabase.TabIndex = 10;
            this.btnInitializeDatabase.Text = "初始化資料庫";
            this.btnInitializeDatabase.UseVisualStyleBackColor = true;
            this.btnInitializeDatabase.Click += new System.EventHandler(this.btnInitializeDatabase_Click);
            // 
            // btnRunNow
            // 
            this.btnRunNow.Location = new System.Drawing.Point(284, 145);
            this.btnRunNow.Name = "btnRunNow";
            this.btnRunNow.Size = new System.Drawing.Size(116, 32);
            this.btnRunNow.TabIndex = 11;
            this.btnRunNow.Text = "立即抓取一次";
            this.btnRunNow.UseVisualStyleBackColor = true;
            this.btnRunNow.Click += new System.EventHandler(this.btnRunNow_Click);
            // 
            // btnStartTimer
            // 
            this.btnStartTimer.Location = new System.Drawing.Point(416, 145);
            this.btnStartTimer.Name = "btnStartTimer";
            this.btnStartTimer.Size = new System.Drawing.Size(116, 32);
            this.btnStartTimer.TabIndex = 12;
            this.btnStartTimer.Text = "啟動 Timer";
            this.btnStartTimer.UseVisualStyleBackColor = true;
            this.btnStartTimer.Click += new System.EventHandler(this.btnStartTimer_Click);
            // 
            // btnStopTimer
            // 
            this.btnStopTimer.Location = new System.Drawing.Point(548, 145);
            this.btnStopTimer.Name = "btnStopTimer";
            this.btnStopTimer.Size = new System.Drawing.Size(116, 32);
            this.btnStopTimer.TabIndex = 13;
            this.btnStopTimer.Text = "停止 Timer";
            this.btnStopTimer.UseVisualStyleBackColor = true;
            this.btnStopTimer.Click += new System.EventHandler(this.btnStopTimer_Click);
            // 
            // txtLog
            // 
            this.txtLog.Location = new System.Drawing.Point(20, 360);
            this.txtLog.Multiline = true;
            this.txtLog.Name = "txtLog";
            this.txtLog.ReadOnly = true;
            this.txtLog.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.txtLog.Size = new System.Drawing.Size(796, 214);
            this.txtLog.TabIndex = 14;
            // 
            // grpSettings
            // 
            this.grpSettings.Controls.Add(this.lblSourceUrl);
            this.grpSettings.Controls.Add(this.txtSourceUrl);
            this.grpSettings.Controls.Add(this.lblConnectionString);
            this.grpSettings.Controls.Add(this.txtConnectionString);
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
            this.grpSettings.Size = new System.Drawing.Size(796, 197);
            this.grpSettings.TabIndex = 15;
            this.grpSettings.TabStop = false;
            this.grpSettings.Text = "設定與控制";
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
            this.grpStatus.Location = new System.Drawing.Point(20, 228);
            this.grpStatus.Name = "grpStatus";
            this.grpStatus.Size = new System.Drawing.Size(796, 114);
            this.grpStatus.TabIndex = 16;
            this.grpStatus.TabStop = false;
            this.grpStatus.Text = "執行狀態";
            // 
            // lblStatus
            // 
            this.lblStatus.AutoSize = true;
            this.lblStatus.Location = new System.Drawing.Point(20, 29);
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Size = new System.Drawing.Size(53, 12);
            this.lblStatus.TabIndex = 0;
            this.lblStatus.Text = "目前狀態";
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
            this.lblTimerEnabled.Text = "Timer 狀態";
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
            this.lblNextRun.Text = "下次執行";
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
            this.lblLastRun.Text = "上次成功";
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
            this.Text = "臺灣銀行匯率自動抓取";
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
