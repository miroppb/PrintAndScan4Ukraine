using PrintAndScan4Ukraine.Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;

namespace PrintAndScan4Ukraine.Data
{
    public interface IPackageDataProvider
    {
        Task<IEnumerable<Package>?> GetAllAsync(bool initialLoad, CancellationToken cancellationToken = default);
  Task<PackageSyncPage> GetPackageChangesAsync(string? since, string? cursor, int limit = 500, CancellationToken cancellationToken = default);
  void ApplyPackageChanges(ObservableCollection<Package> packages, Package? currentlySelected, IEnumerable<Package> changed);
        Task<IEnumerable<Package>?> GetByNameAsync(string senderName, bool senderOnly = true, bool useArchive = false, CancellationToken cancellationToken = default);
        Task<IEnumerable<Package_Status>?> GetAllStatuses(List<string> ids, bool useArchive = false, CancellationToken cancellationToken = default);
        Task<IEnumerable<Package_Status>?> GetStatusByPackage(string packageid, CancellationToken cancellationToken = default);
        Task<IEnumerable<Package>?> GetPackagesByDateAndLastStatusAsync(DateTime start, DateTime end, int status, CancellationToken cancellationToken = default);
        Task<bool> InsertRecord(Package package, CancellationToken cancellationToken = default);
        Task<bool> VerifyIfExists(string packageid, CancellationToken cancellationToken = default);
        Task<bool> ReloadPackagesAndUpdateIfChanged(ObservableCollection<Package> packages, Package CurrentlySelected, CancellationToken cancellationToken = default);
        Task<bool> UpdateRecords(List<Package> package, int type = 0, CancellationToken cancellationToken = default);
        Task<bool> InsertRecordStatus(List<Package_Status> package_statuses, CancellationToken cancellationToken = default);
        Task<IEnumerable<MissingPackages>> FindMissingPackages(List<string> barcodesNotInPackages, CancellationToken cancellationToken = default);
        Task<IEnumerable<Users>> GetUserIDsAndNames(CancellationToken cancellationToken = default);
        Task<IEnumerable<Package>> GetPackageAsync(string packageid, bool useArchive, CancellationToken cancellationToken = default);
        Task<IEnumerable<Package>> GetPackagesAsync(List<string> packages, bool useArchive = false, CancellationToken cancellationToken = default);
        List<Package_less> MapPackagesAndStatusesToLess(IEnumerable<Package> packages, IEnumerable<Package_Status> statuses);
        Task<DateTime> GetServerDate(CancellationToken cancellationToken = default);
        Task<long> UploadExportedFile(string fileName, CancellationToken cancellationToken = default);
        Task<IEnumerable<Package>> FindRepeatingRecipientsAsync(List<string> package_ids, int max_allowed = 3, CancellationToken cancellationToken = default);
        Task<IEnumerable<Export>> FindRecentExports(CancellationToken cancellationToken = default);
        Task<Export> DownloadExportAsync(int id, CancellationToken cancellationToken = default);
    }
}