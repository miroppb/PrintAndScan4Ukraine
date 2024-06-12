using PrintAndScan4Ukraine.ViewModel;
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
			PackagesViewModel.SearchSelectedPackage = viewModel.SelectedShipment!.PackageId;

			viewModel.ExecuteCloseCommand(new object());
		}
	}
}
