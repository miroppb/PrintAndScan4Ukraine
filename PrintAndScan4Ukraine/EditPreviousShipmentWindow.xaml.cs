using PrintAndScan4Ukraine.Data;
using PrintAndScan4Ukraine.ViewModel;
using System.Windows;

namespace PrintAndScan4Ukraine
{
    /// <summary>
    /// Interaction logic for EditPreviousShipmentWindow.xaml
    /// </summary>
    public partial class EditPreviousShipmentWindow : Window
    {
        readonly EditPreviousShipmentViewModel viewModel;
        public EditPreviousShipmentWindow(PackagesViewModel _packagesViewModel, IPackageDataProvider _packageDataProvider)
        {
            InitializeComponent();
            viewModel = new EditPreviousShipmentViewModel(_packagesViewModel, _packageDataProvider);
            DataContext = viewModel;
            viewModel.ClosingRequest += (sender, e) => Close();
        }
    }
}
