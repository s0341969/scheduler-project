using System.Collections.ObjectModel;
using System.Windows;

namespace SchedulerDragDrop;

public partial class ProcessGroupSelectionDialog : Window
{
    public ObservableCollection<ProcessGroupOption> Options { get; }

    public IReadOnlyCollection<string> SelectedNames =>
        Options.Where(x => x.IsSelected).Select(x => x.Name).ToList();

    public ProcessGroupSelectionDialog(IEnumerable<ProcessGroupOption> options)
    {
        InitializeComponent();
        Options = new ObservableCollection<ProcessGroupOption>(
            options.Select(x => new ProcessGroupOption { Name = x.Name, IsSelected = x.IsSelected }));
        DataContext = this;
    }

    private void SelectAll_Click(object sender, RoutedEventArgs e)
    {
        foreach (var option in Options)
            option.IsSelected = true;
    }

    private void ClearAll_Click(object sender, RoutedEventArgs e)
    {
        foreach (var option in Options)
            option.IsSelected = false;
    }

    private void Ok_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
    }
}
