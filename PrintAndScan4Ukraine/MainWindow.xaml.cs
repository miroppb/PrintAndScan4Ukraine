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
			
			string? apiToken = !string.IsNullOrEmpty(Secrets.ApiKey) ? Secrets.ApiKey : null;
			var apiService = new ApiService(apiToken);
			var mainDataProvider = new APIMainDataProvider(apiService);
			_viewmodel = new MainViewModel(mainDataProvider, apiToken);
			DataContext = _viewmodel;
		}
	}
}
