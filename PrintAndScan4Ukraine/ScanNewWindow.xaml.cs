using PrintAndScan4Ukraine.ViewModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Media.Animation;

namespace PrintAndScan4Ukraine
{
	/// <summary>
	/// Interaction logic for ScanNewWindow.xaml
	/// </summary>
	public partial class ScanNewWindow : Window
	{
		private readonly PackagesViewModel _viewModel;

		public ScanNewWindow(PackagesViewModel vm)
		{
			InitializeComponent();
			_viewModel = vm;
			DataContext = _viewModel;
			PreviewKeyDown += _viewModel.NewPreviewKeyDownEvent;
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
