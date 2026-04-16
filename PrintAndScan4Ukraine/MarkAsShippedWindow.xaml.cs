using PrintAndScan4Ukraine.ViewModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Media.Animation;

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

            if (DataContext is INotifyPropertyChanged npc)
            {
                npc.PropertyChanged += (s, e) =>
                {
                    if (e.PropertyName == "CodesScanned")
                    {
                        Dispatcher.Invoke(() =>
                        {
                            var sb = (Storyboard)FindResource("FlashAnimation");
                            sb.Begin();
                        });
                    }
                };
            }
        }
	}
}
