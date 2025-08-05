using PrintAndScan4Ukraine.Data;
using PrintAndScan4Ukraine.ViewModel;
using System.Windows;

namespace PrintAndScan4Ukraine
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		private readonly MainViewModel _viewmodel;

		public MainWindow()
		{
			InitializeComponent();
			_viewmodel = new MainViewModel(new APIMainDataProvider());
			DataContext = _viewmodel;
		}
	}
}
