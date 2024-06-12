using CodingSeb.Localization;
using PrintAndScan4Ukraine.Model;
using PrintAndScan4Ukraine.ViewModel;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;

namespace PrintAndScan4Ukraine
{
	/// <summary>
	/// Interaction logic for HistoryWindow.xaml
	/// </summary>
	public partial class HistoryWindow : Window
	{
		private readonly HistoryViewModel _viewmodel;

		public HistoryWindow(string NameOfSender, List<Package>? ListOfPreviousPackages)
		{
			InitializeComponent();
			_viewmodel = new HistoryViewModel();
			DataContext = _viewmodel;

			_viewmodel.SenderName = $"{string.Format(Loc.Tr("PAS4U.HistoryWindow.TopText", "Sender {0}"), NameOfSender)}";
			ObservableCollection<Package> obp = new();
			ListOfPreviousPackages?.ForEach(obp.Add);
			_viewmodel.PreviousShipments = obp;

			LstItems.MouseDoubleClick += LstItems_MouseDoubleClick;
		}

		private void LstItems_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
		{
			if (_viewmodel.SelectedShipment != null)
			{
				if (MessageBox.Show(Loc.Tr("PAS4U.HistoryWindow.AreYouSureText", "Are you sure?"), "", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
				{
					if (MessageBox.Show(Loc.Tr("PAS4U.HistoryWindow.ContentsText", "Contents?"), "", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No)
					{
						_viewmodel.SelectedShipment.Recipient_Contents = new();
					}
					if (MessageBox.Show(Loc.Tr("PAS4U.HistoryWindow.WeightText", "Weight?"), "", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No)
					{
						_viewmodel.SelectedShipment.Weight = null;
					}
					if (MessageBox.Show(Loc.Tr("PAS4U.HistoryWindow.ValueText", "Weight?"), "", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No)
					{
						_viewmodel.SelectedShipment.Value = null;
					}
				}
				DoubleClicked = true;
				Close();
			}
		}

		public bool DoubleClicked = false;

		public Package? SelectedPackageToUse => _viewmodel.SelectedShipment;
	}
}
