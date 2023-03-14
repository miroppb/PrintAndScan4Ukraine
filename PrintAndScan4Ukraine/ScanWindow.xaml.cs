using AutoUpdaterDotNET;
using CodingSeb.Localization;
using CodingSeb.Localization.Loaders;
using Microsoft.Win32;
using miroppb;
using PrintAndScan4Ukraine.Connection;
using PrintAndScan4Ukraine.Data;
using PrintAndScan4Ukraine.Model;
using PrintAndScan4Ukraine.Properties;
using PrintAndScan4Ukraine.ViewModel;
using System;
using System.Collections.Generic;
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
		private PackagesViewModel _viewModel;

		public ScanWindow()
		{
			InitializeComponent();
			libmiroppb.Log($"Welcome to Print And (Scan) 4 Ukraine. v{Assembly.GetEntryAssembly()!.GetName().Version}");
			_viewModel = new PackagesViewModel(new PackageDataProvider());
			DataContext = _viewModel;
			Loaded += ScanWindow_Loaded;

			LocalizationLoader.Instance.FileLanguageLoaders.Add(new JsonFileLoader());
			LocalizationLoader.Instance.AddDirectory(@"Language");

			Loc.Instance.CurrentLanguage = Settings.Default.Language;
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

			LstUPCAndNames.SelectionChanged += LstUPCAndNames_SelectionChanged;
		}

		private void LstUPCAndNames_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (LstUPCAndNames.SelectedItem != null)
			{
				TxtTotal.Visibility = Visibility.Visible;
				TxtDateAdded.Visibility = Visibility.Visible;
				if (((Package)LstUPCAndNames.SelectedItem).Date_Shipped != null)
					TxtDateShipped.Visibility = Visibility.Visible;
			}
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
			try
			{
				if (lvi == null && !tb!.Name.Contains("Address"))
				{
					if (e.Key == Key.Enter)
					{
						MnuNew_Click(sender, e);
						e.Handled = true;
					}
				}
			}
			catch { }

		}

		private async void MnuShipped_Click(object sender, RoutedEventArgs e)
		{
			int current = (_viewModel.SelectedPackage != null ? (int)_viewModel.SelectedPackage.Id! : 0);
			MarkAsShippedWindow shippedWindow = new MarkAsShippedWindow(_viewModel);
			shippedWindow.ShowDialog();
			await _viewModel.LoadAsync();
			_viewModel.SelectedPackage = _viewModel.Packages.FirstOrDefault(x => x.Id == current)!;
		}

		private async void MnuNew_Click(object sender, RoutedEventArgs e)
		{
			ScanNewWindow scanNewWindow = new ScanNewWindow(_viewModel.Packages.Select(x => x.PackageId).ToList());
			scanNewWindow.ShowDialog();
			if (scanNewWindow.WasSomethingSet)
			{
				_viewModel.Save();
				await _viewModel.LoadAsync();
			}
			if (scanNewWindow.BarCodeThatWasSet != string.Empty)
			{
				try
				{
					_viewModel.SelectedPackage = _viewModel.Packages.FirstOrDefault(x => x.PackageId == scanNewWindow.BarCodeThatWasSet)!;
				}
				catch { }
			}
		}

		private void MnuExport_Click(object sender, RoutedEventArgs e)
		{
			_viewModel.Export(_viewModel.Packages);
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
				ReloadPackagesAndUpdateIfChanged();
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
        }

		private void MnuRussian_Click(object sender, RoutedEventArgs e)
		{
			Loc.Instance.CurrentLanguage = "ru";
			Settings.Default.Language = Loc.Instance.CurrentLanguage;
			Settings.Default.Save();
		}

		private void ReloadPackagesAndUpdateIfChanged()
		{
			_viewModel.ReloadPackagesAndUpdateIfChanged();
		}
    }
}
