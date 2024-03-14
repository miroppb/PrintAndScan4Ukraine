using CodingSeb.Localization;
using PrintAndScan4Ukraine.Command;
using PrintAndScan4Ukraine.Data;
using PrintAndScan4Ukraine.Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;

namespace PrintAndScan4Ukraine.ViewModel
{
	public partial class PackagesViewModel : ClosableViewModel, INotifyPropertyChanged
	{
		private readonly IPackageDataProvider _packageDataProvider;

		private string barCode = string.Empty;
		private List<string> barCodes = new List<string>();
		public bool WasSomethingSet = false;
		public string BarCodeThatWasSet = string.Empty;

		public event PropertyChangedEventHandler? PropertyChanged;
		protected virtual void RaisePropertyChanged([CallerMemberName] string? propertyName = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		public ObservableCollection<Package> Packages { get; } = new();

		private Visibility _IsSelectedPackageShowing = Visibility.Hidden;

		public Visibility IsSelectedPackageShowing
		{
			get => _IsSelectedPackageShowing;
			set
			{
				_IsSelectedPackageShowing = value;
				RaisePropertyChanged();
			}
		}

		public PackagesViewModel(IPackageDataProvider packageDataProvider, Users User)
		{
			_packageDataProvider = packageDataProvider;
			CurrentUser = User;
			SaveCommand = new DelegateCommand(ExecuteSave, () => CanSave);
			ShowHistoryCommand = new DelegateCommand(ShowHistory, () => CanShowHistory);
			SaveAllCommand = new DelegateCommand(SaveAll);
			ShipCommand = new DelegateCommand(ShowShipWindow, () => CanShip);
			AddNewCommand = new DelegateCommand(ShowAddNewWindow, () => CanAddNew);
			ArriveCommand = new DelegateCommand(ShowArriveWindow, () => CanArrive);
			DeliverCommand = new DelegateCommand(ShowDeliverWindow, () => CanDeliver);
			ExportCommand = new DelegateCommand(ExecuteExport);
			DoneCommand = new DelegateCommand(ExecuteDoneCommand);
			GenerateReportCommand = new DelegateCommand(ExecuteGenerateReport);
			RadioDateChecked = new DelegateCommand(ExecuteRadioDateChecked);
			RadioStatusChecked = new DelegateCommand(ExecuteRadioStatusChecked);
		}

		private Package? _selectedPackage;
		public Package SelectedPackage
		{
			get => _selectedPackage!;
			set
			{
				if (_selectedPackage != null && _selectedPackage.Modified) //if package was modified
					Save(_selectedPackage, -1); //save previous package before changing to new package

				_selectedPackage = value;
				RaisePropertyChanged();
				if (_selectedPackage != null)
				{
					IsSelectedPackageShowing = Visibility.Visible;
					SelectedPackageLastStatus = _packageDataProvider.GetStatusByPackage(_selectedPackage.PackageId)!.LastOrDefault()!;
				}
				else
					IsSelectedPackageShowing = Visibility.Hidden;
				SaveCommand.RaiseCanExecuteChanged();
				ShowHistoryCommand.RaiseCanExecuteChanged();
			}
		}

		private Package_Status _SelectedPackageStatus = new Package_Status();

		public Package_Status SelectedPackageLastStatus
		{
			get => _SelectedPackageStatus;
			set
			{
				_SelectedPackageStatus = value;
				RaisePropertyChanged();
			}
		}

		private Users _CurrentUser = new();

		public Users CurrentUser
		{
			get => _CurrentUser;
			set
			{
				_CurrentUser = value;
				RaisePropertyChanged();
				RaisePropertyChanged("AccessToSender");
				RaisePropertyChanged("AccessToEditSender");
				RaisePropertyChanged("AccessToEditReceipient");
			}
		}

		private bool _isOnline = true;

		public bool IsOnline
		{
			get => _isOnline;
			set
			{
				_isOnline = value;
				RaisePropertyChanged();
			}
		}

		private string _lastSaved = string.Empty;

		public string LastSaved
		{
			get => _lastSaved;
			set
			{
				_lastSaved = value;
				RaisePropertyChanged();
			}
		}

		private string _CodesScanned = $"{Loc.Tr("PAS4U.ScanShippedWindow.BarcodesScanned", "Barcodes Scanned")}: 0";

		public string CodesScanned
		{
			get => _CodesScanned;
			set
			{
				_CodesScanned = value;
				RaisePropertyChanged();
			}
		}


		private DateTime _ExportStartDate = DateTime.Now.AddDays(-1);

		public DateTime ExportStartDate
		{
			get => _ExportStartDate;
			set
			{
				_ExportStartDate = value;
				RaisePropertyChanged();
			}
		}


		private DateTime _ExportEndDate = DateTime.Now;

		public DateTime ExportEndDate
		{
			get => _ExportEndDate;
			set
			{
				_ExportEndDate = value;
				RaisePropertyChanged();
			}
		}

		private Visibility _SpinnerVisible = Visibility.Collapsed;

		public Visibility SpinnerVisible
		{
			get => _SpinnerVisible;
			set
			{
				_SpinnerVisible = value;
				RaisePropertyChanged();
			}
		}

		private bool _ExportButtonEnabled = false;

		public bool ExportButtonEnabled
		{
			get => _ExportButtonEnabled;
			set
			{
				_ExportButtonEnabled = value;
				RaisePropertyChanged();
			}
		}

		public bool ReportAll { get; set; } = false;

		public int ReportLastStatus { get; set; } = 2;

#if DEBUG
        public string Header => "Scan Packages v. " + Assembly.GetExecutingAssembly().GetName().Version!.ToString() + " -Debug";
#else
		public string Header => "Scan Packages v. " + Assembly.GetExecutingAssembly().GetName().Version!.ToString();
#endif

		private static char ToChar(Key key)
		{
			char c = '\0';
			if ((key >= Key.A) && (key <= Key.Z))
			{
				c = (char)('a' + (key - Key.A)); //Ignore letters
			}

			else if ((key >= Key.D0) && (key <= Key.D9))
			{
				c = (char)('0' + (key - Key.D0));
			}

			return c;
		}
	}
}
