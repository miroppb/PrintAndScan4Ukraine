using CodingSeb.Localization;
using CodingSeb.Localization.Loaders;
using IWshRuntimeLibrary;
using miroppb;
using PrintAndScan4Ukraine.Command;
using PrintAndScan4Ukraine.Connection;
using PrintAndScan4Ukraine.Data;
using PrintAndScan4Ukraine.Model;
using PrintAndScan4Ukraine.Properties;
using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace PrintAndScan4Ukraine.ViewModel
{
	public class MainViewModel : INotifyPropertyChanged
	{
		public event PropertyChangedEventHandler? PropertyChanged;

		protected virtual void RaisePropertyChanged([CallerMemberName] string? propertyName = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}


		private readonly IMainDataProvider _mainDataProvider;

		public DelegateCommand PrintCommand { get; }
		public DelegateCommand ScanCommand { get; }

		DispatcherTimer CheckinTimer = new();

		public MainViewModel(IMainDataProvider mainDataProvider)
		{
			LocalizationLoader.Instance.FileLanguageLoaders.Add(new JsonFileLoader());
			LocalizationLoader.Instance.AddDirectory(@"Language");

			_mainDataProvider = mainDataProvider;
			PrintCommand = new DelegateCommand(ClickPrint, () => CanPrint);
			ScanCommand = new DelegateCommand(ClickScan, () => CanScan);

			IsOnline = InternetAvailability.IsInternetAvailable();
			Task<int> done = Task.Run(GetAccess);
			int d = done.Result;

			Loc.Instance.CurrentLanguage = Settings.Default.Language;
			if (!CurrentUser.Lang.Contains("en") && Loc.Instance.CurrentLanguage == "en")
			{
				if (MessageBox.Show("Мы обнаружили что ваш язык не английский. Поменять на русский?", "", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
				{
					Settings.Default.Language = Loc.Instance.CurrentLanguage = "ru";
					Settings.Default.Save();
				}
			}

			//CheckShortcut(); //Not going to use for now. Maybe later if a need arises
			var localTimeZone = TimeZoneInfo.Local;
			libmiroppb.Log($"Current timezone is: {localTimeZone.DisplayName} and time is: {DateTime.Now}");

			SetupHeartbeatTimer();
		}

		private void SetupHeartbeatTimer()
		{
			libmiroppb.Log("Setting up saving every 1 minute");
			CheckinTimer = new DispatcherTimer { Interval = TimeSpan.FromMinutes(1) };
			CheckinTimer.Tick += delegate
			{
				_mainDataProvider.Heartbeat(CurrentUser);
			};
			CheckinTimer.Start();
		}

		private void CheckShortcut()
		{
			string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
			string shortcutName = "PrintAndScan 4 Ukraine";
			string shortcutfile = $"{desktopPath}\\{shortcutName}.lnk";
			if (!System.IO.File.Exists(shortcutfile))
			{
				if (MessageBox.Show($"{Loc.Tr("PAS4U.MainWindow.ShortcutDoesNotExist", "Shortcut doesn't exist. Create it?")}", "", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
				{
					string shortcutLocation = Path.Combine(desktopPath, "PrintAndScan 4 Ukraine" + ".lnk");
					WshShell shell = new WshShell();
					IWshShortcut shortcut = (IWshShortcut)shell.CreateShortcut(shortcutLocation);

					shortcut.Description = "PrintAndScan 4 Ukraine";   // The description of the shortcut
					shortcut.IconLocation = AppDomain.CurrentDomain.BaseDirectory + "barcode.ico";           // The icon of the shortcut
					shortcut.TargetPath = $"{AppDomain.CurrentDomain.BaseDirectory}\\{AppDomain.CurrentDomain.FriendlyName}.exe";           // The path of the file that will launch when the shortcut is run
					shortcut.WorkingDirectory = AppDomain.CurrentDomain.BaseDirectory;
					shortcut.Save();                                    // Save the shortcut
				}
			}
		}

		private void ClickScan(object a)
		{
			isVisible = false;
			ScanWindow win = new();
			win.ShowDialog();
			isVisible = true;
		}

		private void ClickPrint(object a)
		{
			isVisible = false;
			PrintWindow win = new();
			win.ShowDialog();
			isVisible = true;
		}

		private bool _IsOnline = true;
		public bool IsOnline
		{
			get => _IsOnline;
			set
			{
				_IsOnline = value;
				RaisePropertyChanged();
			}
		}


		private bool _isVisible = true;

		public bool isVisible
		{
			get => _isVisible;
			set
			{
				_isVisible = value;
				RaisePropertyChanged();
			}
		}


		private static Users? _CurrentUser = new() { Access = Access.None };

		public Users CurrentUser
		{
			get => _CurrentUser!;
			set
			{
				_CurrentUser = value;
				PrintCommand.RaiseCanExecuteChanged();
				ScanCommand.RaiseCanExecuteChanged();
				RaisePropertyChanged();
			}
		}
		public static Users GetUser() => _CurrentUser!;

		public bool CanPrint => CurrentUser.Access.HasFlag(Access.Print);

		public static bool CanScan => true;

		public async Task<int> GetAccess()
		{
			if (IsOnline)
				CurrentUser = await _mainDataProvider.GetUserFromComputerNameAsync(Environment.MachineName);
			else
				CurrentUser.Access = Access.None;
			if (CurrentUser.Access == Access.None)
				MessageBox.Show(Loc.Tr("PAS4U.NoAccess", "You don't have any access to this application, or you're offline. Please contact your administrator."));
			return 0;
		}

	}
}
