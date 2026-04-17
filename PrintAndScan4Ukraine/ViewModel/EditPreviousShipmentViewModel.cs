using PrintAndScan4Ukraine.Command;
using PrintAndScan4Ukraine.Data;
using PrintAndScan4Ukraine.Helpers;
using PrintAndScan4Ukraine.Model;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace PrintAndScan4Ukraine.ViewModel
{
    public class EditPreviousShipmentViewModel : ClosableViewModel, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void RaisePropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private readonly PackagesViewModel MainViewModel;
        private readonly IPackageDataProvider _packageDataProvider;

        public DelegateCommand LoadShipmentCommand { get; }
        public DelegateCommand CancelCommand { get; }


        public EditPreviousShipmentViewModel(PackagesViewModel packagesViewModel, IPackageDataProvider packageDataProvider)
        {
            MainViewModel = packagesViewModel;
            _packageDataProvider = packageDataProvider;
            LoadShipmentCommand = new DelegateCommand(async _ => await LoadShipment(), () => SelectedExport != null);
            CancelCommand = new DelegateCommand(Cancel, () => true);

            LoadAsync();
        }

        private async void LoadAsync()
        {
            var files = await _packageDataProvider.FindRecentExports();

            Exports.Clear();
            foreach (var f in files)
                Exports.Add(f);
        }

        private void Cancel()
        {
            OnClosingRequest();
        }

        private async Task LoadShipment()
        {
            if (MessageBox.Show("You're about to load a previous shipment. This will clear the current list of packages and replace it with the packages from the selected export. Make sure you have saved any unsaved work before proceeding.",
                "Confirm Load Previous Shipment", MessageBoxButton.OKCancel, MessageBoxImage.Warning) != MessageBoxResult.OK)
                return;

            MainViewModel.Packages.Clear();
            MainViewModel.EditingPreviousShipment = true;
            var TempPackages = await _packageDataProvider.GetPackagesAsync([.. PreviewPackageIds]);
            foreach (var item in TempPackages)
            {
                MainViewModel.Packages.Add(item);
            }
            OnClosingRequest();
        }

        public ObservableCollection<Export> Exports { get; } = new();

        private Export? _SelectedExport;

        public Export? SelectedExport
        {
            get => _SelectedExport;
            set
            {
                _SelectedExport = value;

                OnSelectedExportChanged().ConfigureAwait(false);

                RaisePropertyChanged();
            }
        }

        private async Task OnSelectedExportChanged()
        {
            if (SelectedExport == null)
                return;

            var fullExport = await _packageDataProvider.DownloadExportAsync(SelectedExport.Id);
            var ids = await ReadAndExtractExport.ExtractPackageIdsFromExportAsync(fullExport);
            PreviewPackageIds.Clear();
            foreach (var id in ids)
                PreviewPackageIds.Add(id);

            RaisePropertyChanged("PreviewPackageIds");
            RaisePropertyChanged("PreviewCount");
            LoadShipmentCommand.RaiseCanExecuteChanged();
        }

        public ObservableCollection<string> PreviewPackageIds { get; } = new();
        public int PreviewCount => PreviewPackageIds.Count;

    }
}
