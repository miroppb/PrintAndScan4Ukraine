using PrintAndScan4Ukraine.Data;
using PrintAndScan4Ukraine.Model;
using PrintAndScan4Ukraine.ViewModel;
using System.Windows;

namespace PrintAndScan4Ukraine
{
	/// <summary>
	/// Interaction logic for MarkAsArrivedWindow.xaml
	/// </summary>
	public partial class MarkAsArrivedWindow : Window
	{
		private readonly PackagesViewModel _viewModel;

		public MarkAsArrivedWindow(Users user)
		{
			InitializeComponent();
			_viewModel = new PackagesViewModel(new APIPackageDataProvider(new ApiService(Secrets.ApiKey)), user);
			DataContext = _viewModel;
			PreviewKeyDown += _viewModel.PreviewKeyDownEvent;
			_viewModel.ClosingRequest += (sender, e) => Close();
		}
	}
}
