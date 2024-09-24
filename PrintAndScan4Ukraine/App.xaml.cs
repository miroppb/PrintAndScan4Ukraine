using miroppb;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace PrintAndScan4Ukraine
{
	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	public partial class App : Application
	{
		private static Mutex? _mutex;

		/// <summary> 
		/// Sets the foreground window. 
		/// </summary> 
		/// <param name="hWnd">Window handle to bring to front.</param> 
		/// <returns></returns> 
		[DllImport("user32.dll")]
		[return: MarshalAs(UnmanagedType.Bool)]
		static extern bool SetForegroundWindow(IntPtr hWnd);

		protected override void OnStartup(StartupEventArgs e)
		{
			const string appName = "PrintAndScan4Ukraine";
			bool createdNew;

			// Try to create a mutex to ensure only one instance of the app is running
			_mutex = new Mutex(true, appName, out createdNew);

			if (!createdNew)
			{
				// If the app is already running, prompt the user
				var result = MessageBox.Show("App is already running. Do you want to close it?", "", MessageBoxButton.YesNo);

				if (result == MessageBoxResult.Yes)
				{
					// Signal the running instance to close gracefully, don't forcefully kill
					NotifyExistingInstanceToShutdown(appName);
				}
				else
				{
					// Bring the existing instance to the foreground instead of launching a new one
					BringRunningInstanceToForeground();
				}

				// Exit the new instance since the app is already running
				Current.Shutdown();
				return;
			}

			MonitorForShutdownSignal();

			// Continue with normal startup if no other instance is running
			base.OnStartup(e);
		}

		private void NotifyExistingInstanceToShutdown(string appName)
		{
			// Set the shutdown event to signal the existing instance
			var shutdownEvent = new EventWaitHandle(false, EventResetMode.ManualReset, "PrintAndScan4Ukraine_ShutdownEvent");
			shutdownEvent.Set(); // Signal the running instance to shut down gracefully.
		}

		private void BringRunningInstanceToForeground()
		{
			// Find the running process and bring its main window to the front
			Process current = Process.GetCurrentProcess();
			foreach (Process process in Process.GetProcessesByName(current.ProcessName))
			{
				if (process.Id != current.Id)
				{
					SetForegroundWindow(process.MainWindowHandle);
					break;
				}
			}
		}
		public static void MonitorForShutdownSignal()
		{
			var shutdownEvent = new EventWaitHandle(false, EventResetMode.ManualReset, "PrintAndScan4Ukraine_ShutdownEvent");

			Task.Run(() =>
			{
				// Wait until the shutdown event is signaled
				shutdownEvent.WaitOne();

				// Gracefully shut down the application
				Current.Dispatcher.Invoke(() =>
				{
					Current.Shutdown();
				});
			});
		}
	}
}
