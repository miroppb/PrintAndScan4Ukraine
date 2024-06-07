using AutoUpdaterDotNET;
using CodingSeb.Localization;
using Microsoft.Win32;
using miroppb;
using PrintAndScan4Ukraine.Connection;
using PrintAndScan4Ukraine.Data;
using PrintAndScan4Ukraine.Properties;
using PrintAndScan4Ukraine.ViewModel;
using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace PrintAndScan4Ukraine
{
	/// <summary>
	/// Interaction logic for ScanWindow.xaml
	/// </summary>
	public partial class ScanWindow : Window
	{
		private readonly PackagesViewModel _viewModel;

		public ScanWindow()
		{
			InitializeComponent();

			libmiroppb.Log($"Welcome to Print And (Scan) 4 Ukraine. v{Assembly.GetEntryAssembly()!.GetName().Version}");
			_viewModel = new PackagesViewModel(new PackageDataProvider(), MainViewModel.GetUser());
			DataContext = _viewModel;
			Loaded += ScanWindow_Loaded;


			MnuEnglish.IsChecked = Loc.Instance.CurrentLanguage == "en";
			MnuRussian.IsChecked = Loc.Instance.CurrentLanguage == "ru";
		}

		private async void ScanWindow_Loaded(object sender, RoutedEventArgs e)
		{
			_viewModel.IsOnline = InternetAvailability.IsInternetAvailable();
			await Task.Delay(20);
			await _viewModel.LoadAsync();
			SetupUpdater();
			SetupLogUploader();
			SetupSavingOften();
			SetupReloadingPackages();
			SetupOnlineCheck();
			PreviewKeyDown += ScanWindow_PreviewKeyDown; //iffy

			AutoUpdater.ApplicationExitEvent += AutoUpdater_ApplicationExitEvent;
			Closing += ScanWindow_Closing;

			//resync time
			//ResyncTime.TryToResyncTime();

			SystemEvents.PowerModeChanged += SystemEvents_PowerModeChanged;
		}

		private void SystemEvents_PowerModeChanged(object sender, PowerModeChangedEventArgs e)
		{
			switch (e.Mode)
			{
				case PowerModes.Suspend:
					libmiroppb.Log("System is going to sleep");
					SavingOftenTimer.Stop();
					UploadLogsTimer.Stop();
					ReloadingPackagesTimer.Stop();
					UploadLogs(true);
					break;
				case PowerModes.Resume:
					libmiroppb.Log("System is awake");
					SavingOftenTimer.Start();
					UploadLogsTimer.Start();
					ReloadingPackagesTimer.Start();
					UploadLogs(true);
					break;
			}
		}

		private void ScanWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
		{
			libmiroppb.Log("Application closing");
			Thread uploadThread = new(() => UploadLogs(true));

			// Start the thread
			uploadThread.Start();
		}

		private void ScanWindow_PreviewKeyDown(object sender, KeyEventArgs e)
		{
			ListViewItem? lvi = Keyboard.FocusedElement as ListViewItem;
			TextBox? tb = Keyboard.FocusedElement as TextBox;
			ScanWindow? sw = Keyboard.FocusedElement as ScanWindow;
			try
			{
				if (sw == null && lvi == null && !tb!.Name.Contains("Address") && !tb!.Parent.ToString()!.Contains("DataGridCell"))
				{
					if (e.Key == Key.Enter)
					{
						_viewModel.AddNewCommand.Execute(null);
						e.Handled = true;
					}
				}
			}
			catch { }

		}

		DispatcherTimer UploadLogsTimer = new();
		DispatcherTimer SavingOftenTimer = new();
		DispatcherTimer ReloadingPackagesTimer = new();

		private static void SetupUpdater()
		{
			int minutes = 2;
			libmiroppb.Log($"Setting up the Updater for every {minutes} minutes");
			DispatcherTimer timer = new() { Interval = TimeSpan.FromMinutes(minutes) };
			timer.Tick += delegate
			{
				libmiroppb.Log("Checking for update...");
				AutoUpdater.Start(Secrets.GetUpdateURL());
			};
			timer.Start();

			libmiroppb.Log("Checking for update...");
			AutoUpdater.Start(Secrets.GetUpdateURL()); //Checking for update on start
		}

		private void AutoUpdater_ApplicationExitEvent()
		{
			libmiroppb.Log("Update starting");
			UploadLogs(true);
			Application.Current.Shutdown();
		}

		private void SetupLogUploader()
		{
			int minutes = 10;
			libmiroppb.Log($"Setting up uploading logs every {minutes} minutes");
			UploadLogsTimer = new() { Interval = TimeSpan.FromMinutes(minutes) };
			UploadLogsTimer.Tick += delegate
			{
				UploadLogs(true);
			};
			UploadLogsTimer.Start();
		}

		private void SetupSavingOften()
		{
			libmiroppb.Log("Setting up saving every 1 minute");
			SavingOftenTimer = new DispatcherTimer { Interval = TimeSpan.FromMinutes(1) };
			SavingOftenTimer.Tick += delegate
			{
				if (_viewModel.SelectedPackage != null && _viewModel.SelectedPackage.Modified) //only saving current package
					_viewModel.Save();
			};
			SavingOftenTimer.Start();
		}

		private void SetupReloadingPackages()
		{
			int minutes = 2;
			libmiroppb.Log($"Setting up refreshing packages every {minutes} minute(s)");
			ReloadingPackagesTimer = new() { Interval = TimeSpan.FromMinutes(minutes) };
			ReloadingPackagesTimer.Tick += delegate
			{
				_viewModel.ReloadPackagesAndUpdateIfChanged();
			};
			ReloadingPackagesTimer.Start();
		}

		private void SetupOnlineCheck()
		{
			int minutes = 1;
			libmiroppb.Log($"Setting up checking Internet every {minutes} minutes");
			DispatcherTimer timer = new() { Interval = TimeSpan.FromMinutes(minutes) };
			timer.Tick += delegate
			{
				_viewModel.IsOnline = InternetAvailability.IsInternetAvailable();
			};
			timer.Start();
		}

		private static void UploadLogs(bool deleteAfter) => libmiroppb.UploadLog(Secrets.GetConnectionString().ConnectionString, "logs", deleteAfter);

		private void MnuEnglish_Click(object sender, RoutedEventArgs e)
		{
			Loc.Instance.CurrentLanguage = "en";
			Settings.Default.Language = Loc.Instance.CurrentLanguage;
			Settings.Default.Save();
			MnuEnglish.IsChecked = true;
			MnuRussian.IsChecked = false;
		}

		private void MnuRussian_Click(object sender, RoutedEventArgs e)
		{
			Loc.Instance.CurrentLanguage = "ru";
			Settings.Default.Language = Loc.Instance.CurrentLanguage;
			Settings.Default.Save();
			MnuEnglish.IsChecked = false;
			MnuRussian.IsChecked = true;
		}
	}
}
