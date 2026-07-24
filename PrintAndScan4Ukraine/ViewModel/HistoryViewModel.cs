using PrintAndScan4Ukraine.Model;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Data;
using System.Globalization;

namespace PrintAndScan4Ukraine.ViewModel
{
	public class HistoryViewModel : INotifyPropertyChanged
	{
		public event PropertyChangedEventHandler? PropertyChanged;

		protected virtual void RaisePropertyChanged([CallerMemberName] string? propertyName = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		private bool FilterPreviousShipments(object? obj)
		{
			if (obj is not Package p)
				return false;
			if (string.IsNullOrWhiteSpace(SearchText))
				return true;
			var q = SearchText.Trim().ToLower(CultureInfo.InvariantCulture);
			return (p.Recipient_Name ?? string.Empty).ToLower(CultureInfo.InvariantCulture).Contains(q)
				|| (p.Recipient_Address ?? string.Empty).ToLower(CultureInfo.InvariantCulture).Contains(q)
				|| (p.Recipient_Phone ?? string.Empty).ToLower(CultureInfo.InvariantCulture).Contains(q);
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

		// The raw collection
		public ObservableCollection<Package> PreviousShipments
		{
			get => _previousShipments;
			set
			{
				_previousShipments = value ?? new ObservableCollection<Package>();
				RaisePropertyChanged();
				// recreate view when collection is replaced
				PreviousShipmentsView = CollectionViewSource.GetDefaultView(_previousShipments);
				if (PreviousShipmentsView != null)
				{
					PreviousShipmentsView.Filter = FilterPreviousShipments;
				}
			}
		}

		private ICollectionView? _previousShipmentsView;

		// The view that the UI should bind to (supports filtering)
		public ICollectionView? PreviousShipmentsView
		{
			get => _previousShipmentsView;
			private set
			{
				_previousShipmentsView = value;
				RaisePropertyChanged();
			}
		}

		private string _searchText = string.Empty;

		public string SearchText
		{
			get => _searchText;
			set
			{
				_searchText = value ?? string.Empty;
				RaisePropertyChanged();
				PreviousShipmentsView?.Refresh();
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
