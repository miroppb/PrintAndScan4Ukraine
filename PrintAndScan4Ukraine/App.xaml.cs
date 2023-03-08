using miroppb;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
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

			_mutex = new Mutex(true, appName, out createdNew);

			if (!createdNew)
			{
				//app is already running! Exiting the application
				MessageBox.Show("App is already running.");
				libmiroppb.Log("Trying to open already running application");
				Process current = Process.GetCurrentProcess();
				foreach (Process process in Process.GetProcessesByName(current.ProcessName))
				{
					if (process.Id != current.Id)
					{
						SetForegroundWindow(process.MainWindowHandle);
						break;
					}
				}
				Application.Current.Shutdown();
			}

			base.OnStartup(e);
		}
	}
}
