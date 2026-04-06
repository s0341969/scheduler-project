using System;
using System.Collections.Generic;
using System.Windows.Forms;
using BotExchangeRateWinForms.Models;
using BotExchangeRateWinForms.Services;

namespace BotExchangeRateWinForms.Forms
{
    public partial class MainForm : Form
    {
        private readonly UserSettingsService _settingsService;
        private readonly ExchangeRateSqlRepository _repository;
        private readonly ExchangeRateJobRunner _jobRunner;
        private readonly BindingSource _rateBindingSource;
        private UserSettings _settings;
        private DateTime? _lastSuccessfulRunTime;

        public MainForm()
        {
            InitializeComponent();

            _settingsService = new UserSettingsService();
            _repository = new ExchangeRateSqlRepository();
            _jobRunner = new ExchangeRateJobRunner(new BotExchangeRateScraper(), _repository);
            _rateBindingSource = new BindingSource();

            InitializeRateGrid();
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            try
            {
                _settings = _settingsService.Load();
                ApplySettingsToUi(_settings);
                BindRatesToGrid(null);
                AppendLog("\u7a0b\u5f0f\u5df2\u555f\u52d5\u3002");
                UpdateTimerStatus();
            }
            catch (Exception ex)
            {
                HandleUiException(ex);
            }
        }

        private void btnSaveSettings_Click(object sender, EventArgs e)
        {
            try
            {
                SaveSettingsFromUi();
                AppendLog("\u8a2d\u5b9a\u5df2\u5132\u5b58\u3002");

                if (pollTimer.Enabled)
                {
                    ConfigureTimer();
                    AppendLog("Timer \u9593\u9694\u5df2\u91cd\u65b0\u5957\u7528\u3002");
                }
            }
            catch (Exception ex)
            {
                HandleUiException(ex);
            }
        }

        private async void btnInitializeDatabase_Click(object sender, EventArgs e)
        {
            await RunWithUiLockAsync(async delegate
            {
                SaveSettingsFromUi();
                var connectionString = _repository.ResolveConnectionString(_settings);
                await _repository.InitializeDatabaseAsync(connectionString).ConfigureAwait(true);
                AppendLog("\u8cc7\u6599\u8868\u521d\u59cb\u5316\u5b8c\u6210\u3002");
                lblStatusValue.Text = "\u8cc7\u6599\u8868\u521d\u59cb\u5316\u5b8c\u6210";
            });
        }

        private async void btnRunNow_Click(object sender, EventArgs e)
        {
            await ExecuteJobAsync("\u624b\u52d5\u57f7\u884c");
        }

        private void btnStartTimer_Click(object sender, EventArgs e)
        {
            try
            {
                SaveSettingsFromUi();
                ConfigureTimer();
                pollTimer.Start();
                AppendLog("\u81ea\u52d5\u6293\u53d6\u5df2\u555f\u52d5\u3002");
                lblStatusValue.Text = "Timer \u57f7\u884c\u4e2d";
                UpdateTimerStatus();
            }
            catch (Exception ex)
            {
                HandleUiException(ex);
            }
        }

        private void btnStopTimer_Click(object sender, EventArgs e)
        {
            pollTimer.Stop();
            AppendLog("\u81ea\u52d5\u6293\u53d6\u5df2\u505c\u6b62\u3002");
            lblStatusValue.Text = "Timer \u5df2\u505c\u6b62";
            UpdateTimerStatus();
        }

        private async void pollTimer_Tick(object sender, EventArgs e)
        {
            await ExecuteJobAsync("Timer \u81ea\u52d5\u57f7\u884c");
        }

        private async System.Threading.Tasks.Task ExecuteJobAsync(string triggerSource)
        {
            await RunWithUiLockAsync(async delegate
            {
                SaveSettingsFromUi();
                var result = await _jobRunner.ExecuteAsync(_settings).ConfigureAwait(true);
                if (result.IsSkipped)
                {
                    AppendLog(string.Format("{0}\uff1a{1}", triggerSource, result.Message));
                    lblStatusValue.Text = "\u7565\u904e";
                }
                else if (result.IsSuccess)
                {
                    _lastSuccessfulRunTime = DateTime.Now;
                    BindRatesToGrid(result.Records);

                    var sourceUpdatedAtText = result.SourceUpdatedAt.HasValue
                        ? result.SourceUpdatedAt.Value.ToString("yyyy/MM/dd HH:mm")
                        : "\u672a\u77e5";
                    AppendLog(string.Format(
                        "{0}\uff1a{1} \u4f86\u6e90\u639b\u724c\u6642\u9593 {2}",
                        triggerSource,
                        result.Message,
                        sourceUpdatedAtText));
                    lblStatusValue.Text = string.Format(
                        "\u6700\u8fd1\u4e00\u6b21\u57f7\u884c\u6210\u529f\uff08{0} \u7b46\uff09",
                        result.TotalRows);
                    lblLastRunValue.Text = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss");
                }
                else
                {
                    AppendLog(string.Format("{0}\uff1a\u5931\u6557\uff0c{1}", triggerSource, result.Message));
                    lblStatusValue.Text = "\u57f7\u884c\u5931\u6557";
                }

                UpdateTimerStatus();
            });
        }

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

        private void SaveSettingsFromUi()
        {
            _settings = BuildSettingsFromUi();
            _settingsService.Save(_settings);
        }

        private UserSettings BuildSettingsFromUi()
        {
            if (numIntervalValue.Value <= 0)
            {
                throw new InvalidOperationException("\u6293\u53d6\u983b\u7387\u5fc5\u9808\u5927\u65bc 0\u3002");
            }

            return new UserSettings
            {
                SourceUrl = txtSourceUrl.Text.Trim(),
                PollIntervalValue = Decimal.ToInt32(numIntervalValue.Value),
                PollIntervalUnit = cmbIntervalUnit.SelectedItem == null ? "\u5206\u9418" : cmbIntervalUnit.SelectedItem.ToString(),
                RequestTimeoutSeconds = Decimal.ToInt32(numTimeoutSeconds.Value),
                SqlConnectionString = txtConnectionString.Text.Trim(),
                WriteToDatabase = chkWriteChrname.Checked || chkWriteChrnameHistory.Checked,
                WriteChrname = chkWriteChrname.Checked,
                WriteChrnameHistory = chkWriteChrnameHistory.Checked
            };
        }

        private void ApplySettingsToUi(UserSettings settings)
        {
            txtSourceUrl.Text = settings.SourceUrl;
            txtConnectionString.Text = settings.SqlConnectionString;
            numIntervalValue.Value = settings.PollIntervalValue;
            numTimeoutSeconds.Value = settings.RequestTimeoutSeconds;
            chkWriteChrname.Checked = settings.WriteChrname;
            chkWriteChrnameHistory.Checked = settings.WriteChrnameHistory;

            var intervalUnit = string.IsNullOrWhiteSpace(settings.PollIntervalUnit) ? "\u5206\u9418" : settings.PollIntervalUnit;
            if (cmbIntervalUnit.Items.Contains(intervalUnit))
            {
                cmbIntervalUnit.SelectedItem = intervalUnit;
            }
            else
            {
                cmbIntervalUnit.SelectedIndex = 0;
            }
        }

        private void ConfigureTimer()
        {
            var settings = BuildSettingsFromUi();
            var interval = settings.PollIntervalUnit == "\u5c0f\u6642"
                ? TimeSpan.FromHours(settings.PollIntervalValue)
                : TimeSpan.FromMinutes(settings.PollIntervalValue);

            var intervalMilliseconds = Math.Max(1000.0, interval.TotalMilliseconds);
            if (intervalMilliseconds > int.MaxValue)
            {
                throw new InvalidOperationException("Timer \u9593\u9694\u904e\u5927\uff0c\u8acb\u964d\u4f4e\u8a2d\u5b9a\u503c\u3002");
            }

            pollTimer.Interval = (int)intervalMilliseconds;
        }

        private void UpdateTimerStatus()
        {
            lblTimerEnabledValue.Text = pollTimer.Enabled ? "\u555f\u7528\u4e2d" : "\u672a\u555f\u7528";

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
            dgvRates.Columns.Add(CreateTextColumn("CurrencyCode", "\u5e63\u5225\u4ee3\u78bc", 80));
            dgvRates.Columns.Add(CreateTextColumn("CurrencyName", "\u5e63\u5225\u540d\u7a31", 120));
            dgvRates.Columns.Add(CreateNumericColumn("CashBuy", "\u73fe\u91d1\u672c\u884c\u8cb7\u5165", 120));
            dgvRates.Columns.Add(CreateNumericColumn("CashSell", "\u73fe\u91d1\u672c\u884c\u8ce3\u51fa", 120));
            dgvRates.Columns.Add(CreateDateTimeColumn("SourceRateDate", "\u639b\u724c\u65e5\u671f", "yyyy/MM/dd", 95));
            dgvRates.Columns.Add(CreateDateTimeColumn("SourceUpdatedAt", "\u66f4\u65b0\u6642\u9593", "yyyy/MM/dd HH:mm", 130));
            dgvRates.DataSource = _rateBindingSource;
        }

        private void BindRatesToGrid(IList<ExchangeRateRecord> records)
        {
            _rateBindingSource.DataSource = records ?? new List<ExchangeRateRecord>();
            lblResultCountValue.Text = _rateBindingSource.Count.ToString();
        }

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

        private static DataGridViewTextBoxColumn CreateNumericColumn(string propertyName, string headerText, int minimumWidth)
        {
            var column = CreateTextColumn(propertyName, headerText, minimumWidth);
            column.DefaultCellStyle.Format = "N4";
            column.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            return column;
        }

        private static DataGridViewTextBoxColumn CreateDateTimeColumn(string propertyName, string headerText, string format, int minimumWidth)
        {
            var column = CreateTextColumn(propertyName, headerText, minimumWidth);
            column.DefaultCellStyle.Format = format;
            return column;
        }

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
            txtConnectionString.Enabled = enabled;
            chkWriteChrname.Enabled = enabled;
            chkWriteChrnameHistory.Enabled = enabled;
            UseWaitCursor = !enabled;
        }

        private void AppendLog(string message)
        {
            txtLog.AppendText(string.Format("[{0:yyyy/MM/dd HH:mm:ss}] {1}{2}", DateTime.Now, message, Environment.NewLine));
        }

        private void HandleUiException(Exception ex)
        {
            lblStatusValue.Text = "\u64cd\u4f5c\u5931\u6557";
            AppendLog(string.Format("\u932f\u8aa4\uff1a{0}", ex.Message));
            MessageBox.Show(this, ex.Message, "\u57f7\u884c\u5931\u6557", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
