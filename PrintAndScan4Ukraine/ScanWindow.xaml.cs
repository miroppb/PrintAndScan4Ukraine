using AutoUpdaterDotNET;
using CodingSeb.Localization;
using CodingSeb.Localization.Loaders;
using miroppb;
using PrintAndScan4Ukraine.Connection;
using PrintAndScan4Ukraine.Data;
using PrintAndScan4Ukraine.Model;
using PrintAndScan4Ukraine.Properties;
using PrintAndScan4Ukraine.ViewModel;
using System;
using System.Linq;
using System.Reflection;
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

		public ScanWindow(Access UserAccess)
		{
			InitializeComponent();
			libmiroppb.Log($"Welcome to Print And (Scan) 4 Ukraine. v{Assembly.GetEntryAssembly()!.GetName().Version}");
			_viewModel = new PackagesViewModel(new PackageDataProvider(), UserAccess);
			DataContext = _viewModel;
			Loaded += ScanWindow_Loaded;

			MnuEnglish.IsChecked = Loc.Instance.CurrentLanguage == "en" ? true : false;
			MnuRussian.IsChecked = Loc.Instance.CurrentLanguage == "ru" ? true : false;
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
		}

		private void ScanWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
		{
			libmiroppb.Log("Application closing");
			UploadLogs(true);
		}

		private void ScanWindow_PreviewKeyDown(object sender, KeyEventArgs e)
		{
			ListViewItem? lvi = Keyboard.FocusedElement as ListViewItem;
			TextBox? tb = Keyboard.FocusedElement as TextBox;
			ScanWindow? sw = Keyboard.FocusedElement as ScanWindow;
			try
			{
				if (sw == null || lvi == null || !tb!.Name.Contains("Address"))
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

		private void SetupUpdater()
		{
			int minutes = 2;
			libmiroppb.Log($"Setting up the Updater for every {minutes} minutes");
			DispatcherTimer timer = new DispatcherTimer { Interval = TimeSpan.FromMinutes(minutes) };
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
			UploadLogs(true);
			Application.Current.Shutdown();
		}

		private void SetupLogUploader()
		{
			int minutes = 10;
			libmiroppb.Log($"Setting up uploading logs every {minutes} minutes");
			DispatcherTimer timer = new DispatcherTimer { Interval = TimeSpan.FromMinutes(minutes) };
			timer.Tick += delegate
			{
				UploadLogs(true);
			};
			timer.Start();
		}

		private void SetupSavingOften()
		{
			libmiroppb.Log("Setting up saving every 1 minute");
			DispatcherTimer timer = new DispatcherTimer { Interval = TimeSpan.FromMinutes(1) };
			timer.Tick += delegate
			{
				if (_viewModel.SelectedPackage != null) //only saving current package
					_viewModel.Save();
			};
			timer.Start();
		}

		private void SetupReloadingPackages()
		{
			int minutes = 2;
			libmiroppb.Log($"Setting up refreshing packages every {minutes} minute(s)");
			DispatcherTimer timer = new DispatcherTimer { Interval = TimeSpan.FromMinutes(minutes) };
			timer.Tick += delegate
			{
				_viewModel.ReloadPackagesAndUpdateIfChanged();
			};
			timer.Start();
		}

		private void SetupOnlineCheck()
		{
			int minutes = 1;
			libmiroppb.Log($"Setting up checking Internet every {minutes} minutes");
			DispatcherTimer timer = new DispatcherTimer { Interval = TimeSpan.FromMinutes(minutes) };
			timer.Tick += delegate
			{
				_viewModel.IsOnline = InternetAvailability.IsInternetAvailable();
			};
			timer.Start();
		}

		private void UploadLogs(bool deleteAfter)
		{
			libmiroppb.UploadLog(Secrets.GetConnectionString().ConnectionString, "logs", deleteAfter);
		}

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
