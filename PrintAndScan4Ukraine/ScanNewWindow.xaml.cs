using PrintAndScan4Ukraine.ViewModel;
using System.Windows;

namespace PrintAndScan4Ukraine
{
	/// <summary>
	/// Interaction logic for ScanNewWindow.xaml
	/// </summary>
	public partial class ScanNewWindow : Window
	{
		private PackagesViewModel _viewModel;

		public ScanNewWindow(PackagesViewModel vm)
		{
			InitializeComponent();
			_viewModel = vm;
			DataContext = _viewModel;
			PreviewKeyDown += _viewModel.NewPreviewKeyDownEvent;
			_viewModel.ClosingRequest += (sender, e) => Close();
		}
	}
}
