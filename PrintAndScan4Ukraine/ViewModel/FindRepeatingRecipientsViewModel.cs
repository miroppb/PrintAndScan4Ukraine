using OfficeOpenXml;
using PrintAndScan4Ukraine.Command;
using PrintAndScan4Ukraine.Data;
using PrintAndScan4Ukraine.Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Data;

namespace PrintAndScan4Ukraine.ViewModel
{
    internal class FindRepeatingRecipientsViewModel : ClosableViewModel, INotifyPropertyChanged
    {
        public ObservableCollection<Export> RecentFiles { get; } =
        new ObservableCollection<Export>();

        public ObservableCollection<Package> OffendingPackages { get; } =
            new ObservableCollection<Package>();

        public ICollectionView GroupedPackages { get; private set; }

        private Export _SelectedFile = new();

        public Export SelectedFile
        {
            get => _SelectedFile;
            set
            {
                _SelectedFile = value;
                RaisePropertyChanged();
            }
        }

        public DelegateCommand LoadFilesCommand { get; }
        public DelegateCommand ProcessFileCommand { get; }

        private readonly IPackageDataProvider _packageDataProvider;

        public FindRepeatingRecipientsViewModel(IPackageDataProvider packageDataProvider)
        {
            _packageDataProvider = packageDataProvider;

            LoadFilesCommand = new DelegateCommand(async _ => await LoadFiles());
            ProcessFileCommand = new DelegateCommand(async _ => await ProcessFile());

            var cvs = new CollectionViewSource { Source = OffendingPackages };
            cvs.GroupDescriptions.Add(new PropertyGroupDescription("Sender_Name"));
            cvs.GroupDescriptions.Add(new PropertyGroupDescription("Recipient_Phone"));

            GroupedPackages = cvs.View;

            GroupedPackages.GroupDescriptions.Clear();
            GroupedPackages.GroupDescriptions.Add(new PropertyGroupDescription("Sender_Name"));
            GroupedPackages.GroupDescriptions.Add(new PropertyGroupDescription("Recipient_Phone"));

        }

        private async Task LoadFiles()
        {
            var files = await _packageDataProvider.FindRecentExports();

            RecentFiles.Clear();
            foreach (var f in files)
                RecentFiles.Add(f);
        }

        private async Task ProcessFile()
        {
            if (SelectedFile == null)
                return;

            var export = await _packageDataProvider.DownloadExportAsync(SelectedFile.Id);
            var ids = await ExtractPackageIdsFromExportAsync(export);

            var results = await _packageDataProvider.FindRepeatingRecipientsAsync(ids);

            OffendingPackages.Clear();
            foreach (var p in results)
                OffendingPackages.Add(p);
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void RaisePropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private async Task<List<string>> ExtractPackageIdsFromExportAsync(Export export)
        {
            // Step 2: Convert base64 to bytes
            byte[] fileBytes = Convert.FromBase64String(export.Content);

            ExcelPackage.License.SetNonCommercialOrganization("PrintAndScan4Ukraine");

            using var ms = new MemoryStream(fileBytes);
            using var package = new ExcelPackage(ms);

            var ws = package.Workbook.Worksheets[0]; // first worksheet

            var ids = new List<string>();
            int row = 7; // skip 5 rows + header row

            while (true)
            {
                var cellValue = ws.Cells[row, 1].Text?.Trim();

                if (string.IsNullOrWhiteSpace(cellValue))
                    break; // stop when we hit an empty row

                ids.Add(cellValue);
                row++;
            }

            return ids;

        }

    }
}
