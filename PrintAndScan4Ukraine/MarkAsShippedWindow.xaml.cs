using PrintAndScan4Ukraine.ViewModel;
using System.Windows;

namespace PrintAndScan4Ukraine
{
	/// <summary>
	/// Interaction logic for ScannerWindow.xaml
	/// </summary>
	public partial class MarkAsShippedWindow : Window
	{
		private readonly PackagesViewModel _viewModel;

		public MarkAsShippedWindow(PackagesViewModel vm)
		{
			InitializeComponent();
			_viewModel = vm;
			DataContext = _viewModel;
			PreviewKeyDown += _viewModel.PreviewKeyDownEvent;
			_viewModel.ClosingRequest += (sender, e) => Close();
		}
	}
}
