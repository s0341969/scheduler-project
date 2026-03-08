using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Threading;

namespace SchedulerDragDrop;

public partial class App : Application
{
    private static readonly string LogDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "SchedulerDragDrop");

    private static readonly string LogFilePath = Path.Combine(LogDirectory, "startup_diagnostic.log");

    protected override void OnStartup(StartupEventArgs e)
    {
        AppDomain.CurrentDomain.UnhandledException += OnCurrentDomainUnhandledException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
        DispatcherUnhandledException += OnDispatcherUnhandledException;

        WriteDiagnostic("Application startup begin.");

        try
        {
            base.OnStartup(e);
            WriteDiagnostic("Application startup success.");
        }
        catch (Exception ex)
        {
            WriteDiagnostic("Application startup failed.", ex);
            ShowErrorWithLogPath(ex);
            Shutdown(-1);
        }
    }

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        WriteDiagnostic("Dispatcher unhandled exception.", e.Exception);
        ShowErrorWithLogPath(e.Exception);
        e.Handled = true;
    }

    private void OnCurrentDomainUnhandledException(object? sender, UnhandledExceptionEventArgs e)
    {
        var ex = e.ExceptionObject as Exception ?? new Exception(e.ExceptionObject?.ToString() ?? "Unknown unhandled exception.");
        WriteDiagnostic("AppDomain unhandled exception.", ex);
    }

    private void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        WriteDiagnostic("TaskScheduler unobserved task exception.", e.Exception);
        e.SetObserved();
    }

    private static void ShowErrorWithLogPath(Exception ex)
    {
        MessageBox.Show(
            $"{ex.Message}\n\n診斷日誌：{LogFilePath}",
            "啟動錯誤",
            MessageBoxButton.OK,
            MessageBoxImage.Error);
    }

    private static void WriteDiagnostic(string message, Exception? ex = null)
    {
        try
        {
            Directory.CreateDirectory(LogDirectory);
            var sb = new StringBuilder();
            sb.Append('[').Append(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")).Append("] ").AppendLine(message);

            if (ex is not null)
            {
                sb.AppendLine(ex.GetType().FullName ?? "Exception");
                sb.AppendLine(ex.Message);
                sb.AppendLine(ex.StackTrace ?? string.Empty);
                if (ex.InnerException is not null)
                {
                    sb.AppendLine("-- InnerException --");
                    sb.AppendLine(ex.InnerException.GetType().FullName ?? "Exception");
                    sb.AppendLine(ex.InnerException.Message);
                    sb.AppendLine(ex.InnerException.StackTrace ?? string.Empty);
                }
            }

            File.AppendAllText(LogFilePath, sb.ToString(), Encoding.UTF8);
        }
        catch
        {
            // Ignore logging failures to avoid recursive startup crashes.
        }
    }
}
