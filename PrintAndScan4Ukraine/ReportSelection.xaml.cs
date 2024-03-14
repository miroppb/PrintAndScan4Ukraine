using PrintAndScan4Ukraine.ViewModel;
using System.Windows;

namespace PrintAndScan4Ukraine
{
	/// <summary>
	/// Interaction logic for ReportSelection.xaml
	/// </summary>
	public partial class ReportSelection : Window
	{
		private readonly PackagesViewModel _viewModel;

		public ReportSelection(PackagesViewModel vm)
		{
			InitializeComponent();
			_viewModel = vm;
			DataContext = _viewModel;
			_viewModel.ClosingRequest += (sender, e) => Close();
		}
	}
}
