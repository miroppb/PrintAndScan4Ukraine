using miroppb;
using PrintAndScan4Ukraine.Command;
using PrintAndScan4Ukraine.Data;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace PrintAndScan4Ukraine.ViewModel
{
	class PrintViewModel : ClosableViewModel, INotifyPropertyChanged, IDataErrorInfo
	{
		private readonly IPrintDataProvider _dataProvider;
		public event PropertyChangedEventHandler? PropertyChanged;

		protected virtual void RaisePropertyChanged([CallerMemberName] string? propertyName = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		public string Error => string.Empty;

		public string this[string columnName]
		{
			get
			{
				if (columnName == nameof(Starting) || columnName == nameof(Ending))
				{
					if (Starting < 100000000 || Starting > 999999999 || Ending < 100000000 || Ending > 999999999)
					{
						CanPrint = false;
						return "7 digits please";
					}
					else if (_dataProvider.FindPackagesBetweenRange(Starting, Ending))
					{
						CanPrint = false;
						return "Packages within these ranges already exist.";
					}
				}
				CanPrint = true;
				return string.Empty;
			}
		}

		public PrintViewModel(IPrintDataProvider dataProvider)
		{
			_dataProvider = dataProvider;
			PrintCommand = new DelegateCommand(Print, () => CanPrint);
			CancelCommand = new DelegateCommand(CancelPrinting, () => CanCancel);
		}

		public ObservableCollection<string> Printers { get; } = new();
		private string _selectedPrinter = string.Empty;

		public string SelectedPrinter
		{
			get => _selectedPrinter;
			set
			{
				_selectedPrinter = value;
				RaisePropertyChanged();
			}
		}


		private int _starting = 300000001;
		public int Starting
		{
			get => _starting;
			set
			{
				_starting = value;
				RaisePropertyChanged();
			}
		}

		private int _ending = 300000002;
		public int Ending
		{
			get => _ending;
			set
			{
				_ending = value;
				RaisePropertyChanged();
			}
		}

		private int _copies = 1;
		public int Copies
		{
			get => _copies;
			set
			{
				_copies = value;
				RaisePropertyChanged();
			}
		}

		public DelegateCommand PrintCommand { get; }
		public DelegateCommand CancelCommand { get; }

		private bool _canPrint = true;
		public bool CanPrint
		{
			get => _canPrint;
			set
			{
				_canPrint = value;
				PrintCommand.RaiseCanExecuteChanged();
			}
		}

		private bool _canCancel = false;
		public bool CanCancel
		{
			get => _canCancel;
			set
			{
				_canCancel = value;
				CancelCommand.RaiseCanExecuteChanged();
			}
		}

		private async void Print(object a)
		{
			CanPrint = false;
			Libmiroppb.Log($"Printing from {Starting} to {Ending}, {Copies} copies, to printer {SelectedPrinter}");
			_dataProvider.PrintBarcodes(Starting, Ending, Copies, SelectedPrinter);
			await Task.Delay(2000);
			CanPrint = true;
		}

		private void CancelPrinting(object obj)
		{
			Libmiroppb.Log("Trying to cancel any print jobs...");
			CanCancel = !_dataProvider.CancelPrinting(SelectedPrinter);
		}

		public void LoadPrinters()
		{
			foreach (string a in _dataProvider.LoadPrinters())
				Printers.Add(a);
		}
	}
}
