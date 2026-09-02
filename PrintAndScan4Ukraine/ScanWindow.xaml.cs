using AutoUpdaterDotNET;
using CodingSeb.Localization;
using miroppb;
using PrintAndScan4Ukraine.Connection;
using PrintAndScan4Ukraine.Data;
using PrintAndScan4Ukraine.Properties;
using PrintAndScan4Ukraine.ViewModel;
using System;
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
		
		public ScanWindow()
		{
			InitializeComponent();

			Libmiroppb.Log($"Welcome to Print And (Scan) 4 Ukraine. v{Assembly.GetEntryAssembly()!.GetName().Version}");
			// Ensure appsettings.json exists and load language
			var lang = AppSettingsManager.GetLanguage();
			if (!string.IsNullOrWhiteSpace(lang))
				Loc.Instance.CurrentLanguage = lang;
			else
				AppSettingsManager.SetLanguage(Loc.Instance.CurrentLanguage ?? "en");
			_viewModel = new PackagesViewModel(new APIPackageDataProvider(new ApiService(Secrets.ApiKey)), MainViewModel.GetUser());
			DataContext = _viewModel;
			Loaded += ScanWindow_Loaded;
			_viewModel.ScrollListBox += ViewModel_ScrollListBox;

			MnuEnglish.IsChecked = Loc.Instance.CurrentLanguage == "en";
			MnuRussian.IsChecked = Loc.Instance.CurrentLanguage == "ru";

			_viewModel.HistoryShown += _viewModel_HistoryShown;
			TxtCost.GotFocus += Txt_Int_GotFocus;
			TxtWeight.GotFocus += Txt_Int_GotFocus;

			// Suppress autosave while the user is actively editing recipient fields
			TxtRecipientFName.GotFocus += (s, e) => _viewModel.IsUserEditingField = true;
			TxtRecipientFName.LostFocus += (s, e) => _viewModel.IsUserEditingField = false;
			TxtRecipientAddress.GotFocus += (s, e) => _viewModel.IsUserEditingField = true;
			TxtRecipientAddress.LostFocus += (s, e) => _viewModel.IsUserEditingField = false;
			TxtRecipientPhone.GotFocus += (s, e) => _viewModel.IsUserEditingField = true;
			TxtRecipientPhone.LostFocus += (s, e) => _viewModel.IsUserEditingField = false;
        }

        private void Txt_Int_GotFocus(object sender, RoutedEventArgs e)
        {
            ((TextBox)sender).SelectAll();
        }

        private void _viewModel_HistoryShown(object? sender, EventArgs e)
        {
            TxtSenderName.Focus();
			TxtSenderName.CaretIndex = TxtSenderName.Text.Length;
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
			await Task.Delay(1000); //delay to allow log to write before app closes, otherwise log may not show application closing
        }

        private async void ScanWindow_PreviewKeyDown(object sender, KeyEventArgs e)
		{
			ListViewItem? lvi = Keyboard.FocusedElement as ListViewItem;
			TextBox? tb = Keyboard.FocusedElement as TextBox;
			ScanWindow? sw = Keyboard.FocusedElement as ScanWindow;
			try
			{
				if (sw == null && lvi == null && !tb!.Name.Contains("Address") && !tb!.Name.Contains("PackageId") && !tb!.Name.Contains("Search") && !tb!.Name.Contains("TxtSenderName") && !tb!.Parent.ToString()!.Contains("DataGridCell"))
				{
					if (e.Key == Key.Enter)
					{
						//lets attempt to save current package before creating new one, if there is a problem with saving, we dont want to lose data by creating new package
						if (_viewModel.SelectedPackage != null)
						{
							if (await _viewModel.Save())
							{
								_viewModel.AddNewCommand.Execute(null);
								e.Handled = true;
							}
						}
					}
				}
				else if (sw == null && lvi == null && tb!.Name.Contains("PackageId"))
				{
					_viewModel.SelectedPackage.PackageIdValid = !Validation.GetHasError(TxtPackageIdEdit);
					_viewModel.SaveCommand.RaiseCanExecuteChanged();
                    if (e.Key == Key.Enter)
					{
						if (await _viewModel.Save())
							MessageBox.Show($"{Loc.Tr("PAS4U.MainWindow.PackageSaved", "Package has been saved manually")}", "");
						else
							MessageBox.Show($"{Loc.Tr("PAS4U.MainWindow.PackageNotSaved", "Error saving package. Please check the package details and try again.")}", "");
						e.Handled = true;
					}
				}
                else if (sw == null && lvi == null && tb!.Name.Contains("TxtSenderName"))
                {
                    if (e.Key == Key.Enter)
                    {
						_viewModel.ShowHistoryCommand.Execute(null);
                        e.Handled = true;
                    }
                }
			}
			catch { }
		}

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
			await Task.Delay(1000); //delay to allow log to write before app closes, otherwise log may not show update starting
            Environment.Exit(0); //Application.Current.Shutdown wasn't working for a customer
		}

		private void SetupSavingOften()
		{
			Libmiroppb.Log("Setting up saving every 1 minute");
			SavingOftenTimer = new DispatcherTimer { Interval = TimeSpan.FromMinutes(1) };
			SavingOftenTimer.Tick += delegate
			{
				// Only autosave when there is a modified package and the user is not editing recipient fields
				if (_viewModel.SelectedPackage != null && _viewModel.SelectedPackage.Modified && !_viewModel.IsUserEditingField) //only saving current package
					_ = _viewModel.Save();
			};
			SavingOftenTimer.Start();
		}

        private void SetupReloadingPackages()
        {
            int minutes = 2;
            Libmiroppb.Log($"Setting up refreshing packages every {minutes} minute(s)");
            ReloadingPackagesTimer = new()
            {
                Interval = TimeSpan.FromMinutes(minutes)
            };
            ReloadingPackagesTimer.Tick += async (_, _) =>
            {
                await _viewModel.ReloadPackagesAndUpdateIfChanged();
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

		private void MnuEnglish_Click(object sender, RoutedEventArgs e)
		{
			Loc.Instance.CurrentLanguage = "en";
			AppSettingsManager.SetLanguage("en");
			MnuEnglish.IsChecked = true;
			MnuRussian.IsChecked = false;
		}

		private void MnuRussian_Click(object sender, RoutedEventArgs e)
		{
			Loc.Instance.CurrentLanguage = "ru";
			AppSettingsManager.SetLanguage("ru");
			MnuEnglish.IsChecked = false;
			MnuRussian.IsChecked = true;
		}
	}
}
