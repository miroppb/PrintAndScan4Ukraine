using AutoUpdaterDotNET;
using CodingSeb.Localization;
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

			Libmiroppb.Log($"Welcome to Print And (Scan) 4 Ukraine. v{Assembly.GetEntryAssembly()!.GetName().Version}");
			_viewModel = new PackagesViewModel(new PackageDataProvider(), MainViewModel.GetUser());
			DataContext = _viewModel;
			Loaded += ScanWindow_Loaded;
			_viewModel.ScrollListBox += ViewModel_ScrollListBox;

			MnuEnglish.IsChecked = Loc.Instance.CurrentLanguage == "en";
			MnuRussian.IsChecked = Loc.Instance.CurrentLanguage == "ru";
		}

		private void ViewModel_ScrollListBox(object? sender, EventArgs e)
		{
			LstUPCAndNames.ScrollIntoView(_viewModel.SelectedPackage);
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

		private async void ScanWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
		{
			Libmiroppb.Log("Application closing");
			await UploadLogs(true);
		}

		private void ScanWindow_PreviewKeyDown(object sender, KeyEventArgs e)
		{
			ListViewItem? lvi = Keyboard.FocusedElement as ListViewItem;
			TextBox? tb = Keyboard.FocusedElement as TextBox;
			ScanWindow? sw = Keyboard.FocusedElement as ScanWindow;
			try
			{
				if (sw == null && lvi == null && !tb!.Name.Contains("Address") && !tb!.Name.Contains("PackageId") && !tb!.Parent.ToString()!.Contains("DataGridCell"))
				{
					if (e.Key == Key.Enter)
					{
						_viewModel.AddNewCommand.Execute(null);
						e.Handled = true;
					}
				}
				else if (sw == null && lvi == null && tb!.Name.Contains("PackageId"))
				{
					_viewModel.SelectedPackage.PackageIdValid = !Validation.GetHasError(TxtPackageId);
					if (e.Key == Key.Enter && _viewModel.SelectedPackage.PackageIdValid)
					{
						if (_viewModel.Save())
							MessageBox.Show($"{Loc.Tr("PAS4U.MainWindow.PackageSaved", "Package has been saved manually")}", "");
						else
							MessageBox.Show($"{Loc.Tr("PAS4U.MainWindow.PackageNotSaved", "Error saving package.")}", "");
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
			Libmiroppb.Log($"Setting up the Updater for every {minutes} minutes");
			DispatcherTimer timer = new() { Interval = TimeSpan.FromMinutes(minutes) };
			timer.Tick += delegate
			{
				Libmiroppb.Log("Checking for update...");
				AutoUpdater.Start(Secrets.GetUpdateURL());
			};
			timer.Start();

			Libmiroppb.Log("Checking for update...");
			AutoUpdater.Start(Secrets.GetUpdateURL()); //Checking for update on start
		}

		private async void AutoUpdater_ApplicationExitEvent()
		{
			Libmiroppb.Log("Update starting");
			await UploadLogs(true);
			Environment.Exit(0); //Application.Current.Shutdown wasn't working for a customer
		}

		private void SetupLogUploader()
		{
			int minutes = 10;
			Libmiroppb.Log($"Setting up uploading logs every {minutes} minutes");
			UploadLogsTimer = new() { Interval = TimeSpan.FromMinutes(minutes) };
			UploadLogsTimer.Tick += async delegate
			{
				await UploadLogs(true);
			};
			UploadLogsTimer.Start();
		}

		private void SetupSavingOften()
		{
			Libmiroppb.Log("Setting up saving every 1 minute");
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
			Libmiroppb.Log($"Setting up refreshing packages every {minutes} minute(s)");
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
			Libmiroppb.Log($"Setting up checking Internet every {minutes} minutes");
			DispatcherTimer timer = new() { Interval = TimeSpan.FromMinutes(minutes) };
			timer.Tick += delegate
			{
				_viewModel.IsOnline = InternetAvailability.IsInternetAvailable();
			};
			timer.Start();
		}

		private static async Task UploadLogs(bool deleteAfter) => await Libmiroppb.UploadLogAsync(Secrets.GetConnectionString().ConnectionString, deleteAfter);

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
