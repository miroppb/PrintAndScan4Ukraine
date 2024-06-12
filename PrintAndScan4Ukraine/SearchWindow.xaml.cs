using PrintAndScan4Ukraine.ViewModel;
using System.Windows;

namespace PrintAndScan4Ukraine
{
	/// <summary>
	/// Interaction logic for SearchWindow.xaml
	/// </summary>
	public partial class SearchWindow : Window
	{
		public SearchWindow(SearchViewModel _viewmodel)
		{
			InitializeComponent();
			DataContext = _viewmodel;
			_viewmodel.ClosingRequest += (sender, e) => Close();
		}
	}
}
