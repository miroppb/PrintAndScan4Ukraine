using PrintAndScan4Ukraine.Model;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace PrintAndScan4Ukraine.ViewModel
{
	public class HistoryViewModel : INotifyPropertyChanged
	{
		public event PropertyChangedEventHandler? PropertyChanged;

		protected virtual void RaisePropertyChanged([CallerMemberName] string? propertyName = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		private string _senderName = string.Empty;

		public string SenderName
		{
			get => _senderName;
			set
			{
				_senderName = value;
				RaisePropertyChanged();
			}
		}

		private ObservableCollection<Package> _previousShipments = new();

		public ObservableCollection<Package> PreviousShipments
		{
			get => _previousShipments;
			set
			{
				_previousShipments = value;
				RaisePropertyChanged();
			}
		}

		private Package? _selectedShipment = null;

		public Package? SelectedShipment
		{
			get => _selectedShipment!;
			set
			{
				_selectedShipment = value;
				RaisePropertyChanged();
			}
		}

	}
}
