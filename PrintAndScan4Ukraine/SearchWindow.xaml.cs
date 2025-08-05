using CodingSeb.Localization;
using PrintAndScan4Ukraine.ViewModel;
using System.Linq;
using System.Windows;

namespace PrintAndScan4Ukraine
{
	/// <summary>
	/// Interaction logic for SearchWindow.xaml
	/// </summary>
	public partial class SearchWindow : Window
	{
		private readonly SearchViewModel viewModel;
		public SearchWindow(SearchViewModel _viewmodel)
		{
			InitializeComponent();
			viewModel = _viewmodel;
			DataContext = _viewmodel;
			_viewmodel.ClosingRequest += (sender, e) => Close();

			LstItems.MouseDoubleClick += LstItems_MouseDoubleClick;
		}

		private void LstItems_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
		{
			var IsAvailable = viewModel.PackagesOnList.FirstOrDefault(x => x == viewModel.SelectedShipment!.PackageId);
			if (IsAvailable == null)
			{
                MessageBox.Show(Loc.Tr($"PAS4U.SearchSelectionWindow.PackageNotOnList", "This package isn't on the list"), "Selected package isn't on the list, and can't be displayed");
            }
			else
			{
				PackagesViewModel.SearchSelectedPackage = viewModel.SelectedShipment!.PackageId;

				viewModel.ExecuteCloseCommand(new object());
			}
		}
	}
}
