using miroppb;
using PrintAndScan4Ukraine.Command;
using PrintAndScan4Ukraine.Data;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace PrintAndScan4Ukraine.ViewModel
{
	class PrintViewModel : INotifyPropertyChanged
	{
		private readonly IPrintDataProvider _dataProvider;
		public event PropertyChangedEventHandler? PropertyChanged;

		protected virtual void RaisePropertyChanged([CallerMemberName] string? propertyName = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
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


		private int _starting = 3000001;
		public int Starting
		{
			get => _starting;
			set
			{
				_starting = value;
				RaisePropertyChanged();
			}
		}

		private int _ending = 3000002;
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

		private async void Print()
		{
			CanPrint = false;
			libmiroppb.Log($"Printing from {Starting} to {Ending}, {Copies} copies, to printer {SelectedPrinter}");
			_dataProvider.PrintBarcodes(Starting, Ending, Copies, SelectedPrinter);
			await Task.Delay(2000);
			CanPrint = true;
		}

		public PrintViewModel(IPrintDataProvider dataProvider)
		{
			_dataProvider = dataProvider;
			PrintCommand = new DelegateCommand(Print, () => CanPrint);
		}

		public void LoadPrinters()
		{
			foreach (string a in _dataProvider.LoadPrinters())
				Printers.Add(a);
		}
	}
}
