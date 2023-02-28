using AutoUpdaterDotNET;
using miroppb;
using PrintAndScan4Ukraine.Connection;
using PrintAndScan4Ukraine.Data;
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
		private PackagesViewModel _viewModel;

		public ScanWindow()
		{
			InitializeComponent();
			libmiroppb.Log("Welcome to Print And Scan 4 Ukraine. v" + Assembly.GetEntryAssembly()!.GetName().Version);
			_viewModel = new PackagesViewModel(new PackageDataProvider());
			DataContext = _viewModel;
			Loaded += ScanWindow_Loaded;
		}

		private async void ScanWindow_Loaded(object sender, RoutedEventArgs e)
		{
			_viewModel.IsOnline = InternetAvailability.IsInternetAvailable();
			await Task.Delay(20);
			await _viewModel.LoadAsync();
			SetupUpdater();
			SetupLogUploader();
			SetupSavingOften();
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
			int current = 0;
			try { current = (int)_viewModel.SelectedPackage.Id!; } catch { }
			MarkAsShippedWindow shippedWindow = new MarkAsShippedWindow(_viewModel);
			shippedWindow.ShowDialog();
			await _viewModel.LoadAsync();
			_viewModel.SelectedPackage = _viewModel.Packages.FirstOrDefault(x => x.Id == current)!;
		}

		private async void MnuNew_Click(object sender, RoutedEventArgs e)
		{
			ScanNewWindow scanNewWindow = new ScanNewWindow();
			scanNewWindow.ShowDialog();
			if (scanNewWindow.WasSomethingSet)
			{
				_viewModel.Save();
				await _viewModel.LoadAsync();
				try { _viewModel.SelectedPackage = _viewModel.Packages[_viewModel.Packages.Count - 1]; } catch { }
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
				if (_viewModel.Packages != null)
					_viewModel.SaveAll();
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
	}
}
