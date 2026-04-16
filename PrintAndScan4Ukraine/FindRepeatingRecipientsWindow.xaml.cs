using PrintAndScan4Ukraine.Data;
using PrintAndScan4Ukraine.ViewModel;
using System.Windows;

namespace PrintAndScan4Ukraine
{
    /// <summary>
    /// Interaction logic for FindRepeatingRecipientsWindow.xaml
    /// </summary>
    public partial class FindRepeatingRecipientsWindow : Window
    {
        private readonly FindRepeatingRecipientsViewModel _viewmodel;

        public FindRepeatingRecipientsWindow(IPackageDataProvider packageDataProvider)
        {
            InitializeComponent();
            _viewmodel = new FindRepeatingRecipientsViewModel(packageDataProvider);
            DataContext = _viewmodel;

            _viewmodel.ClosingRequest += (sender, e) => Close();
        }
    }
}
