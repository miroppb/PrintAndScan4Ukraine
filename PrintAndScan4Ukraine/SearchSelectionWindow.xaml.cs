using PrintAndScan4Ukraine.ViewModel;
using System.Windows;

namespace PrintAndScan4Ukraine
{
	/// <summary>
	/// Interaction logic for SearchSelectionWindow.xaml
	/// </summary>
	public partial class SearchSelectionWindow : Window
	{
		public readonly SearchViewModel _viewmodel;

		public SearchSelectionWindow()
		{
			InitializeComponent();
			_viewmodel = new SearchViewModel();
			DataContext = _viewmodel;
			_viewmodel.ClosingRequest += (sender, e) => Close();
		}
	}
}
