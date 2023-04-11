using PrintAndScan4Ukraine.Command;
using System.ComponentModel;

namespace PrintAndScan4Ukraine.ViewModel
{
	public partial class PackagesViewModel : INotifyPropertyChanged
	{
		public DelegateCommand SaveCommand { get; }
		public DelegateCommand ShowHistoryCommand { get; }
		public DelegateCommand SaveAllCommand { get; }
		public DelegateCommand ShipCommand { get; }
		public DelegateCommand AddNewCommand { get; }
		public DelegateCommand ArriveCommand { get; }
		public DelegateCommand DeliverCommand { get; }
		public DelegateCommand ExportCommand { get; }
		public DelegateCommand ExportShippedNotArrivedCommand { get; }
		public DelegateCommand DoneCommand { get; }
	}
}
