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
			AddMultipleCommand = new DelegateCommand(ExecuteAddMultiple, () => CanAddNew);
			ArriveCommand = new DelegateCommand(ShowArriveWindow, () => CanArrive);
			DeliverCommand = new DelegateCommand(ShowDeliverWindow, () => CanDeliver);
			ExportCommand = new DelegateCommand(ExecuteExport);
			DoneCommand = new DelegateCommand(ExecuteDoneCommand);
			GenerateReportCommand = new DelegateCommand(ExecuteGenerateReport);
			RadioDateChecked = new DelegateCommand(ExecuteRadioDateChecked);
			RadioStatusChecked = new DelegateCommand(ExecuteRadioStatusChecked);
			EditPackageIDCommand = new DelegateCommand(ExecuteEditPackageID, () => CanEditPackageID);
			ShowSearchCommand = new DelegateCommand(ExecuteShowSearch, () => AccessToSeePackages && AccessToSeeSender);
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
					SelectedPackage.NewPackageId = SelectedPackage.PackageId.ToLower();
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

		private string _barCode = string.Empty;

		public string BarCode
		{
			get => _barCode;
			set
			{
				_barCode = value;
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

		private bool _AddMultipleNew = false;

		public bool AddMultipleNew
		{
			get => _AddMultipleNew;
			set
			{
				_AddMultipleNew = value;
				RaisePropertyChanged();
			}
		}

		private string _AddMultipleButton = $"{Loc.Tr("PAS4U.ScanNewWindow.Multiple", "Add Multiple")}";

		public string AddMultipleButton
		{
			get => _AddMultipleButton;
			set
			{
				_AddMultipleButton = value;
				RaisePropertyChanged();
			}
		}

		private string _AddMultipleText = $"{Loc.Tr("PAS4U.ScanNewWindow.TopText", "Scan New Barcode to Add")}";

		public string AddMultipleText
		{
			get => _AddMultipleText;
			set
			{
				_AddMultipleText = value;
				RaisePropertyChanged();
			}
		}

		private Visibility _AddMultipleVisible = Visibility.Visible;

		public Visibility AddMultipleVisible
		{
			get => _AddMultipleVisible;
			set
			{
				_AddMultipleVisible = value;
				RaisePropertyChanged();
			}
		}

		private bool _IsEditingPackageID = false;

		public bool IsEditingPackageID
		{
			get => _IsEditingPackageID;
			set
			{
				_IsEditingPackageID = value;
				RaisePropertyChanged();
			}
		}

#if DEBUG
		public static string Header => "Scan Packages v. " + Assembly.GetExecutingAssembly().GetName().Version!.ToString() + " -Debug";
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
