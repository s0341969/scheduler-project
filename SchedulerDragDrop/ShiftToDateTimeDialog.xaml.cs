using System.Windows;

namespace SchedulerDragDrop;

public partial class ShiftToDateTimeDialog : Window
{
    public DateTime? SelectedDateTime { get; private set; }

    public ShiftToDateTimeDialog(DateTime defaultDateTime)
    {
        InitializeComponent();

        TargetDatePicker.SelectedDate = defaultDateTime.Date;

        for (var h = 0; h < 24; h++)
            HourComboBox.Items.Add(h.ToString("00"));

        MinuteComboBox.Items.Add("00");
        MinuteComboBox.Items.Add("30");

        HourComboBox.SelectedItem = defaultDateTime.Hour.ToString("00");
        MinuteComboBox.SelectedItem = defaultDateTime.Minute >= 30 ? "30" : "00";
    }

    private void Ok_Click(object sender, RoutedEventArgs e)
    {
        if (TargetDatePicker.SelectedDate is null || HourComboBox.SelectedItem is null || MinuteComboBox.SelectedItem is null)
        {
            MessageBox.Show("請選擇日期與時間。", "輸入", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var date = TargetDatePicker.SelectedDate.Value;
        var hour = int.Parse(HourComboBox.SelectedItem.ToString()!);
        var minute = int.Parse(MinuteComboBox.SelectedItem.ToString()!);

        SelectedDateTime = new DateTime(date.Year, date.Month, date.Day, hour, minute, 0);
        DialogResult = true;
    }
}

