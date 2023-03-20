using CodingSeb.Localization.Loaders;
using CodingSeb.Localization;
using PrintAndScan4Ukraine.Connection;
using PrintAndScan4Ukraine.Data;
using PrintAndScan4Ukraine.ViewModel;
using System.Windows;
using PrintAndScan4Ukraine.Properties;

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
			_viewmodel = new MainViewModel(new MainDataProvider());
			DataContext = _viewmodel;
			Loaded += MainWindow_Loaded;

			LocalizationLoader.Instance.FileLanguageLoaders.Add(new JsonFileLoader());
			LocalizationLoader.Instance.AddDirectory(@"Language");

			Loc.Instance.CurrentLanguage = Settings.Default.Language;
		}

		private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
		{
			_viewmodel.IsOnline = InternetAvailability.IsInternetAvailable();
			await _viewmodel.GetAccess();
		}
	}
}
