using CodingSeb.Localization;
using CodingSeb.Localization.Loaders;
using PrintAndScan4Ukraine.Command;
using PrintAndScan4Ukraine.Connection;
using PrintAndScan4Ukraine.Data;
using PrintAndScan4Ukraine.Model;
using PrintAndScan4Ukraine.Properties;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;

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

		public MainViewModel(IMainDataProvider mainDataProvider)
		{
			LocalizationLoader.Instance.FileLanguageLoaders.Add(new JsonFileLoader());
			LocalizationLoader.Instance.AddDirectory(@"Language");

			Loc.Instance.CurrentLanguage = Settings.Default.Language;

			_mainDataProvider = mainDataProvider;
			PrintCommand = new DelegateCommand(ClickPrint, () => CanPrint);
			ScanCommand = new DelegateCommand(ClickScan, () => CanScan);

			IsOnline = InternetAvailability.IsInternetAvailable();
			_ = GetAccess();
		}

		private void ClickScan(object a)
		{
			isVisible = false;
			ScanWindow win = new ScanWindow();
			win.ShowDialog();
			isVisible = true;
		}

		private void ClickPrint(object a)
		{
			isVisible = false;
			PrintWindow win = new PrintWindow();
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


		private static Users? _CurrentUser = new Users() { Access = Access.None};

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

		public bool CanScan => true;

		public async Task GetAccess()
		{
			if (IsOnline)
				CurrentUser = await _mainDataProvider.GetUserFromComputerNameAsync(Environment.MachineName);
			else
				CurrentUser.Access = Access.None;
			if (CurrentUser.Access == Access.None)
				MessageBox.Show(Loc.Tr("PAS4U.NoAccess", "You don't have any access to this application, or you're offline. Please contact your administrator."));
		}

	}
}
