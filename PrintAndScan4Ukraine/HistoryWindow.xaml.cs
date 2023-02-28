using PrintAndScan4Ukraine.Model;
using PrintAndScan4Ukraine.ViewModel;
using System;
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
		HistoryViewModel _viewmodel;

		public HistoryWindow(string NameOfSender, List<Package>? ListOfPreviousPackages)
		{
			InitializeComponent();
			_viewmodel = new HistoryViewModel();
			DataContext = _viewmodel;

			_viewmodel.SenderName = $"Select a previous shipment from: {NameOfSender}.{Environment.NewLine}Double-click to use for current package";
			ObservableCollection<Package> obp = new();
			if (ListOfPreviousPackages != null)
				ListOfPreviousPackages.ForEach(obp.Add);
			_viewmodel.PreviousShipments = obp;

			LstItems.MouseDoubleClick += LstItems_MouseDoubleClick;
		}

		private void LstItems_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
		{
			if (_viewmodel.SelectedShipment != null)
			{
				if (MessageBox.Show("Are you sure you want to overwrite the current package with these parameters?", "", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
				{
					if (MessageBox.Show("Contents too?", "", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No)
					{
						_viewmodel.SelectedShipment.Recipient_Contents = new();
					}
					if (MessageBox.Show("Weight?", "", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No)
					{
						_viewmodel.SelectedShipment.Weight = null;
					}
					if (MessageBox.Show("Value?", "", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No)
					{
						_viewmodel.SelectedShipment.Value = null;
					}
				}
				Close();
			}
		}

		public Package? SelectedPackageToUse => _viewmodel.SelectedShipment;
	}
}
