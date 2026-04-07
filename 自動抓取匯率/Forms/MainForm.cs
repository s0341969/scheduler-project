using System;
using System.Collections.Generic;
using System.Windows.Forms;
using BotExchangeRateWinForms.Models;
using BotExchangeRateWinForms.Services;
using GonGinLibrary;

namespace BotExchangeRateWinForms.Forms
{
    /// <summary>
    /// 主操作畫面，提供設定、手動執行、Timer 控制與結果顯示。
    /// </summary>
    public partial class MainForm : Form
    {
        private readonly UserSettingsService _settingsService;
        private readonly ExchangeRateSqlRepository _repository;
        private readonly ExchangeRateJobRunner _jobRunner;
        private readonly BindingSource _rateBindingSource;
        private UserSettings _settings;
        private DateTime? _lastSuccessfulRunTime;

        /// <summary>
        /// 建立畫面並初始化服務與 Grid 設定。
        /// </summary>
        public MainForm()
        {
            InitializeComponent();

            _settingsService = new UserSettingsService();
            _repository = new ExchangeRateSqlRepository();
            _jobRunner = new ExchangeRateJobRunner(new BotExchangeRateScraper(), _repository);
            _rateBindingSource = new BindingSource();

            InitializeRateGrid();

            this.Text += "_" + GonGinVariable.SectionName;
            
            //btnStartTimer_Click(null, null);
        }

        /// <summary>
        /// 畫面載入時讀取設定並更新初始狀態。
        /// </summary>
        private void MainForm_Load(object sender, EventArgs e)
        {
            try
            {
                txtSourceUrl.Text = "https://rate.bot.com.tw/xrt?Lang=zh-tw&redirect=true";

                _settings = _settingsService.Load();
                ApplySettingsToUi(_settings);
                BindRatesToGrid(null);
                AppendLog("程式已啟動。");
                UpdateTimerStatus();

                btnStartTimer_Click(sender, e);
            }
            catch (Exception ex)
            {
                HandleUiException(ex);
            }
        }

        /// <summary>
        /// 儲存目前畫面設定，必要時同步重新套用 Timer 間隔。
        /// </summary>
        private void btnSaveSettings_Click(object sender, EventArgs e)
        {
            try
            {
                SaveSettingsFromUi();
                AppendLog("設定已儲存。");

                if (pollTimer.Enabled)
                {
                    ConfigureTimer();
                    AppendLog("Timer 間隔已重新套用。");
                }
            }
            catch (Exception ex)
            {
                HandleUiException(ex);
            }
        }

        /// <summary>
        /// 依目前連線字串初始化所需資料表。
        /// </summary>
        private async void btnInitializeDatabase_Click(object sender, EventArgs e)
        {
            await RunWithUiLockAsync(async delegate
            {
                SaveSettingsFromUi();
                var connectionString = GonGinVariable.SqlConnectString;

                //_repository.ResolveConnectionString(_settings);
                await _repository.InitializeDatabaseAsync(connectionString).ConfigureAwait(true);
                AppendLog("資料表初始化完成。");
                lblStatusValue.Text = "資料表初始化完成";
            });
        }

        /// <summary>
        /// 由使用者手動觸發一次抓取與寫入流程。
        /// </summary>
        private async void btnRunNow_Click(object sender, EventArgs e)
        {
            await ExecuteJobAsync("手動執行");
        }

        /// <summary>
        /// 啟動 Timer 並開始依設定週期自動抓取。
        /// </summary>
        private void btnStartTimer_Click(object sender, EventArgs e)
        {
            try
            {
                SaveSettingsFromUi();
                ConfigureTimer();
                pollTimer.Start();
                AppendLog("自動抓取已啟動。");
                lblStatusValue.Text = "Timer 執行中";
                UpdateTimerStatus();
            }
            catch (Exception ex)
            {
                HandleUiException(ex);
            }
        }

        /// <summary>
        /// 停止 Timer 自動抓取。
        /// </summary>
        private void btnStopTimer_Click(object sender, EventArgs e)
        {
            pollTimer.Stop();
            AppendLog("自動抓取已停止。");
            lblStatusValue.Text = "Timer 已停止";
            UpdateTimerStatus();
        }

        /// <summary>
        /// Timer 觸發時執行一次背景工作。
        /// </summary>
        private async void pollTimer_Tick(object sender, EventArgs e)
        {
            await ExecuteJobAsync("Timer 自動執行");
        }

        /// <summary>
        /// 統一處理單次抓取、寫入、Grid 更新與畫面狀態顯示。
        /// </summary>
        private async System.Threading.Tasks.Task ExecuteJobAsync(string triggerSource)
        {
            await RunWithUiLockAsync(async delegate
            {
                SaveSettingsFromUi();
                var result = await _jobRunner.ExecuteAsync(_settings).ConfigureAwait(true);
                if (result.IsSkipped)
                {
                    AppendLog(string.Format("{0}：{1}", triggerSource, result.Message));
                    lblStatusValue.Text = "略過";
                }
                else if (result.IsSuccess)
                {
                    _lastSuccessfulRunTime = DateTime.Now;
                    BindRatesToGrid(result.Records);

                    var sourceUpdatedAtText = result.SourceUpdatedAt.HasValue
                        ? result.SourceUpdatedAt.Value.ToString("yyyy/MM/dd HH:mm")
                        : "未知";
                    AppendLog(string.Format(
                        "{0}：{1} 來源掛牌時間 {2}",
                        triggerSource,
                        result.Message,
                        sourceUpdatedAtText));
                    lblStatusValue.Text = string.Format(
                        "最近一次執行成功（{0} 筆）",
                        result.TotalRows);
                    lblLastRunValue.Text = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss");
                }
                else
                {
                    AppendLog(string.Format("{0}：失敗，{1}", triggerSource, result.Message));
                    lblStatusValue.Text = "執行失敗";
                }

                UpdateTimerStatus();
            });
        }

        /// <summary>
        /// 執行非同步工作時鎖定畫面控制項，避免重複操作。
        /// </summary>
        private async System.Threading.Tasks.Task RunWithUiLockAsync(Func<System.Threading.Tasks.Task> action)
        {
            ToggleControls(false);
            try
            {
                await action().ConfigureAwait(true);
            }
            catch (Exception ex)
            {
                HandleUiException(ex);
            }
            finally
            {
                ToggleControls(true);
            }
        }

        /// <summary>
        /// 將畫面欄位轉成設定物件並立即保存。
        /// </summary>
        private void SaveSettingsFromUi()
        {
            _settings = BuildSettingsFromUi();
            _settingsService.Save(_settings);
        }

        /// <summary>
        /// 從畫面控制項組出新的使用者設定。
        /// </summary>
        private UserSettings BuildSettingsFromUi()
        {
            if (numIntervalValue.Value <= 0)
            {
                throw new InvalidOperationException("抓取頻率必須大於 0。");
            }

            return new UserSettings
            {
                SourceUrl = txtSourceUrl.Text.Trim(),
                PollIntervalValue = Decimal.ToInt32(numIntervalValue.Value),
                PollIntervalUnit = cmbIntervalUnit.SelectedItem == null ? "分鐘" : cmbIntervalUnit.SelectedItem.ToString(),
                RequestTimeoutSeconds = Decimal.ToInt32(numTimeoutSeconds.Value),
                //SqlConnectionString = txtConnectionString.Text.Trim(),
                WriteToDatabase = chkWriteChrname.Checked || chkWriteChrnameHistory.Checked,
                WriteChrname = chkWriteChrname.Checked,
                WriteChrnameHistory = chkWriteChrnameHistory.Checked
            };
        }

        /// <summary>
        /// 將已載入的設定套用回各個畫面控制項。
        /// </summary>
        private void ApplySettingsToUi(UserSettings settings)
        {
            txtSourceUrl.Text = settings.SourceUrl;
            //txtConnectionString.Text = settings.SqlConnectionString;
            numIntervalValue.Value = settings.PollIntervalValue;
            numTimeoutSeconds.Value = settings.RequestTimeoutSeconds;
            chkWriteChrname.Checked = settings.WriteChrname;
            chkWriteChrnameHistory.Checked = settings.WriteChrnameHistory;

            var intervalUnit = string.IsNullOrWhiteSpace(settings.PollIntervalUnit) ? "分鐘" : settings.PollIntervalUnit;
            if (cmbIntervalUnit.Items.Contains(intervalUnit))
            {
                cmbIntervalUnit.SelectedItem = intervalUnit;
            }
            else
            {
                cmbIntervalUnit.SelectedIndex = 0;
            }
        }

        /// <summary>
        /// 根據目前設定計算 Timer 間隔並套用到元件。
        /// </summary>
        private void ConfigureTimer()
        {
            var settings = BuildSettingsFromUi();
            var interval = settings.PollIntervalUnit == "小時"
                ? TimeSpan.FromHours(settings.PollIntervalValue)
                : TimeSpan.FromMinutes(settings.PollIntervalValue);

            var intervalMilliseconds = Math.Max(1000.0, interval.TotalMilliseconds);
            if (intervalMilliseconds > int.MaxValue)
            {
                throw new InvalidOperationException("Timer 間隔過大，請降低設定值。");
            }

            pollTimer.Interval = (int)intervalMilliseconds;
        }

        /// <summary>
        /// 更新狀態區塊中的 Timer、下次執行與上次成功時間。
        /// </summary>
        private void UpdateTimerStatus()
        {
            lblTimerEnabledValue.Text = pollTimer.Enabled ? "啟用中" : "未啟用";

            if (pollTimer.Enabled)
            {
                lblNextRunValue.Text = DateTime.Now.AddMilliseconds(pollTimer.Interval).ToString("yyyy/MM/dd HH:mm:ss");
            }
            else
            {
                lblNextRunValue.Text = "-";
            }

            if (_lastSuccessfulRunTime.HasValue)
            {
                lblLastRunValue.Text = _lastSuccessfulRunTime.Value.ToString("yyyy/MM/dd HH:mm:ss");
            }
            else if (string.IsNullOrWhiteSpace(lblLastRunValue.Text))
            {
                lblLastRunValue.Text = "-";
            }
        }

        /// <summary>
        /// 設定 DataGridView 欄位、格式與資料繫結模式。
        /// </summary>
        private void InitializeRateGrid()
        {
            dgvRates.AutoGenerateColumns = false;
            dgvRates.ReadOnly = true;
            dgvRates.AllowUserToAddRows = false;
            dgvRates.AllowUserToDeleteRows = false;
            dgvRates.AllowUserToResizeRows = false;
            dgvRates.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvRates.MultiSelect = false;
            dgvRates.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvRates.RowHeadersVisible = false;

            dgvRates.Columns.Clear();
            dgvRates.Columns.Add(CreateTextColumn("CurrencyCode", "幣別代碼", 80));
            dgvRates.Columns.Add(CreateTextColumn("CurrencyName", "幣別名稱", 120));
            dgvRates.Columns.Add(CreateNumericColumn("CashBuy", "現金本行買入", 120));
            dgvRates.Columns.Add(CreateNumericColumn("CashSell", "現金本行賣出", 120));
            dgvRates.Columns.Add(CreateDateTimeColumn("SourceRateDate", "掛牌日期", "yyyy/MM/dd", 95));
            dgvRates.Columns.Add(CreateDateTimeColumn("SourceUpdatedAt", "更新時間", "yyyy/MM/dd HH:mm", 130));
            dgvRates.DataSource = _rateBindingSource;
        }

        /// <summary>
        /// 將抓到的匯率清單繫結到畫面表格。
        /// </summary>
        private void BindRatesToGrid(IList<ExchangeRateRecord> records)
        {
            _rateBindingSource.DataSource = records ?? new List<ExchangeRateRecord>();
            lblResultCountValue.Text = _rateBindingSource.Count.ToString();
        }

        /// <summary>
        /// 建立一般文字欄位。
        /// </summary>
        private static DataGridViewTextBoxColumn CreateTextColumn(string propertyName, string headerText, int minimumWidth)
        {
            return new DataGridViewTextBoxColumn
            {
                DataPropertyName = propertyName,
                HeaderText = headerText,
                MinimumWidth = minimumWidth,
                SortMode = DataGridViewColumnSortMode.Automatic
            };
        }

        /// <summary>
        /// 建立數值欄位，並套用小數格式與靠右對齊。
        /// </summary>
        private static DataGridViewTextBoxColumn CreateNumericColumn(string propertyName, string headerText, int minimumWidth)
        {
            var column = CreateTextColumn(propertyName, headerText, minimumWidth);
            column.DefaultCellStyle.Format = "N4";
            column.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            return column;
        }

        /// <summary>
        /// 建立日期或時間欄位，並套用指定顯示格式。
        /// </summary>
        private static DataGridViewTextBoxColumn CreateDateTimeColumn(string propertyName, string headerText, string format, int minimumWidth)
        {
            var column = CreateTextColumn(propertyName, headerText, minimumWidth);
            column.DefaultCellStyle.Format = format;
            return column;
        }

        /// <summary>
        /// 在執行工作期間統一切換控制項是否可操作。
        /// </summary>
        private void ToggleControls(bool enabled)
        {
            btnSaveSettings.Enabled = enabled;
            btnInitializeDatabase.Enabled = enabled;
            btnRunNow.Enabled = enabled;
            btnStartTimer.Enabled = enabled;
            btnStopTimer.Enabled = enabled;
            numIntervalValue.Enabled = enabled;
            cmbIntervalUnit.Enabled = enabled;
            numTimeoutSeconds.Enabled = enabled;
            txtSourceUrl.Enabled = enabled;
            //txtConnectionString.Enabled = enabled;
            chkWriteChrname.Enabled = enabled;
            chkWriteChrnameHistory.Enabled = enabled;
            UseWaitCursor = !enabled;
        }

        /// <summary>
        /// 在畫面下方記錄帶時間戳的執行日誌。
        /// </summary>
        private void AppendLog(string message)
        {
            txtLog.AppendText(string.Format("[{0:yyyy/MM/dd HH:mm:ss}] {1}{2}", DateTime.Now, message, Environment.NewLine));
        }

        /// <summary>
        /// 統一處理 UI 錯誤訊息與訊息框顯示。
        /// </summary>
        private void HandleUiException(Exception ex)
        {
            lblStatusValue.Text = "操作失敗";
            AppendLog(string.Format("錯誤：{0}", ex.Message));
            MessageBox.Show(this, ex.Message, "執行失敗", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
