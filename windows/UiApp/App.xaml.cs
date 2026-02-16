using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Threading;

namespace UiApp;

public partial class App : Application
{
	protected override void OnStartup(StartupEventArgs e)
	{
		base.OnStartup(e);

		DispatcherUnhandledException += OnDispatcherUnhandledException;
		AppDomain.CurrentDomain.UnhandledException += OnDomainUnhandledException;
		TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
	}

	private static string CrashLogPath
		=> Path.Combine(Path.GetTempPath(), "tibiaAu", "uiapp_crash.log");

	private static void WriteCrashLog(string title, Exception ex)
	{
		try
		{
			var dir = Path.GetDirectoryName(CrashLogPath);
			if (!string.IsNullOrWhiteSpace(dir))
				Directory.CreateDirectory(dir);

			var sb = new StringBuilder();
			sb.AppendLine(DateTime.Now.ToString("O"));
			sb.AppendLine(title);
			sb.AppendLine(ex.ToString());
			sb.AppendLine("----");
			File.AppendAllText(CrashLogPath, sb.ToString());
		}
		catch
		{
			// last resort: ignore logging failures
		}
	}

	private static void ShowCrashMessage(string title, Exception ex)
	{
		try
		{
			MessageBox.Show(
				$"{title}\n\n{ex.Message}\n\nCrash log: {CrashLogPath}",
				"tibiaAu UI crash",
				MessageBoxButton.OK,
				MessageBoxImage.Error);
		}
		catch
		{
			// ignore UI failures
		}
	}

	private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
	{
		WriteCrashLog("DispatcherUnhandledException", e.Exception);
		ShowCrashMessage("UI thread exception", e.Exception);
		e.Handled = true;
		Shutdown(1);
	}

	private void OnDomainUnhandledException(object? sender, UnhandledExceptionEventArgs e)
	{
		var ex = e.ExceptionObject as Exception ?? new Exception("Non-Exception unhandled exception");
		WriteCrashLog("AppDomain.UnhandledException", ex);
		ShowCrashMessage("Unhandled exception", ex);
	}

	private void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
	{
		WriteCrashLog("TaskScheduler.UnobservedTaskException", e.Exception);
		e.SetObserved();
	}
}
