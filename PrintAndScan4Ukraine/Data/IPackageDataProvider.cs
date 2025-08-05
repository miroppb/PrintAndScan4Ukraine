using PrintAndScan4Ukraine.Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace PrintAndScan4Ukraine.Data
{
    public interface IPackageDataProvider
	{
		Task<IEnumerable<Package>?> GetAllAsync(bool initialLoad);
		Task<IEnumerable<Package>?> GetByNameAsync(string SenderName, bool useArchive = false);
		Task<IEnumerable<Package_Status>?> GetAllStatuses(List<string> ids, bool useArchive = false);
		Task<IEnumerable<Package_Status>?> GetStatusByPackage(string packageid);
		Task<IEnumerable<Package>?> GetPackagesByDateAndLastStatusAsync(DateTime start, DateTime end, int status);
		Task<bool> InsertRecord(Package package);
		Task<bool> VerifyIfExists(string packageid);
		Task<bool> ReloadPackagesAndUpdateIfChanged(ObservableCollection<Package> packages, Package CurrentlySelected);
		Task<bool> UpdateRecords(List<Package> package, int type = 0);
		Task<bool> InsertRecordStatus(List<Package_Status> package_statuses);
        Task<IEnumerable<MissingPackages>> FindMissingPackages(List<string> barcodesNotInPackages);
		Task<IEnumerable<Users>> GetUserIDsAndNames();
		Task<IEnumerable<Package>> GetPackageAsync(string packageid, bool useArchive); //returning list because we have duplicates :/
		Task<IEnumerable<Package>> GetPackagesAsync(List<string> packages, bool useArchive = false);
		List<Package_less> MapPackagesAndStatusesToLess(IEnumerable<Package> packages, IEnumerable<Package_Status> statuses);
		Task<DateTime> GetServerDate();
        Task<long> UploadExportedFile(string fileName);
	}
}
