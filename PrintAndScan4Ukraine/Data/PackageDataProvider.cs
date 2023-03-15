using CodingSeb.Localization;
using Dapper;
using miroppb;
using MySqlConnector;
using Newtonsoft.Json;
using PrintAndScan4Ukraine.Model;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using Z.Dapper.Plus;

namespace PrintAndScan4Ukraine.Data
{
	public interface IPackageDataProvider
	{
		Task<IEnumerable<Package>?> GetAllAsync();
		Task<IEnumerable<Package>?> GetByNameAsync(string SenderName);
		IEnumerable<Package_Status>? GetAllStatuses();
		IEnumerable<Package_Status>? GetStatusByPackage(string packageid);
		bool InsertRecord(Package package);
		Task<bool> ReloadPackagesAndUpdateIfChanged(ObservableCollection<Package> packages, Package CurrentlySelected);
		bool UpdateRecords(List<Package> package);
		bool InsertRecordStatus(List<Package_Status> package_statuses);
	}

	public class PackageDataProvider : IPackageDataProvider
	{
		public async Task<IEnumerable<Package>?> GetAllAsync()
		{
			libmiroppb.Log("Get List of Packages");
			IEnumerable<Package> packages = new List<Package>();
			try
			{
				using (MySqlConnection db = Secrets.GetConnectionString())
				{
					var temp = await db.QueryAsync<Package>($"SELECT * FROM {Secrets.GetMySQLTable()} WHERE removed = 0");
					packages = temp.ToList().Select(x =>
					{
						x.Recipient_Contents = x.Contents != null ? JsonConvert.DeserializeObject<List<Contents>>(x.Contents)! : new List<Contents>() { };
						return x;
					}).ToList();
				}
				libmiroppb.Log(JsonConvert.SerializeObject(packages));
			}
			catch
			{
				libmiroppb.Log($"There is no connection to: {Secrets.GetMySQLUrl()}");
				MessageBox.Show($"{Loc.Tr("PAS4U.MainWindow.Offline", "There is no connection")}");
			}
			return packages;
		}

		public async Task<IEnumerable<Package>?> GetByNameAsync(string SenderName)
		{
			libmiroppb.Log($"Get List of Packages for {SenderName}");
			IEnumerable<Package> packages = new List<Package>();
			using (MySqlConnection db = Secrets.GetConnectionString())
			{
				var temp = await db.QueryAsync<Package>($"SELECT * FROM {Secrets.GetMySQLTable()} WHERE sender_name = @SenderName", new { SenderName });
				packages = temp.ToList().Select(x =>
				{
					x.Recipient_Contents = x.Contents != null ? JsonConvert.DeserializeObject<List<Contents>>(x.Contents)! : new List<Contents>() { };
					return x;
				}).ToList();
			}
			libmiroppb.Log(JsonConvert.SerializeObject(packages));
			return packages;
		}

		public IEnumerable<Package_Status>? GetAllStatuses()
		{
			libmiroppb.Log("Get List of Packages Statuses");
			IEnumerable<Package_Status> statuses = new List<Package_Status>();
			try
			{
				using (MySqlConnection db = Secrets.GetConnectionString())
					statuses = db.Query<Package_Status>($"SELECT id, packageid, createddate, status FROM {Secrets.GetMySQLPackageStatusTable()} ORDER BY id");

				libmiroppb.Log(JsonConvert.SerializeObject(statuses));
			}
			catch
			{
				libmiroppb.Log($"There is no connection to: {Secrets.GetMySQLUrl()}");
				MessageBox.Show($"{Loc.Tr("PAS4U.MainWindow.Offline", "There is no connection")}");
			}
			return statuses;
		}

		public IEnumerable<Package_Status>? GetStatusByPackage(string packageid)
		{
			libmiroppb.Log($"Get List of Packages Statuses for {packageid}");
			IEnumerable<Package_Status> statuses = new List<Package_Status>();
			try
			{
				using (MySqlConnection db = Secrets.GetConnectionString())
					statuses = db.Query<Package_Status>($"SELECT id, packageid, createddate, status FROM {Secrets.GetMySQLPackageStatusTable()} WHERE packageid = @packageid ORDER BY id", new { packageid });

				libmiroppb.Log(JsonConvert.SerializeObject(statuses));
			}
			catch
			{
				libmiroppb.Log($"There is no connection to: {Secrets.GetMySQLUrl()}");
				MessageBox.Show($"{Loc.Tr("PAS4U.MainWindow.Offline", "There is no connection")}");
			}
			return statuses;
		}

		public bool InsertRecord(Package package)
		{
			using (MySqlConnection db = Secrets.GetConnectionString())
			{
				libmiroppb.Log($"Inserting into Database: {JsonConvert.SerializeObject(package)}");
				DapperPlusManager.Entity<Package>().Table(Secrets.GetMySQLTable()).Identity(x => x.Id);
				db.BulkInsert(package);
				return true;
			}
		}

		public async Task<bool> ReloadPackagesAndUpdateIfChanged(ObservableCollection<Package> packages, Package CurrentlySelected)
		{
			IEnumerable<Package>? RefreshedPackages = await GetAllAsync();
			if (RefreshedPackages != null)
			{
				foreach (Package package in RefreshedPackages.ToList())
				{
					Package? FromPrevious = packages.FirstOrDefault(x => x.Id == package.Id);
					if (FromPrevious != null)
					{
						(bool, List<Variance>) Ret = ComparePackages(FromPrevious, package);
						if (!Ret.Item1)
						{
							if (package.Id == CurrentlySelected.Id)
							{
								if (MessageBox.Show(Loc.Tr("PAS4U.MainWindow.RefreshCurrentPackage", "Current Package was updated outside of the application. Reload it?"), "", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
									ReplaceProperties(FromPrevious, Ret.Item2);
							}
							else
								ReplaceProperties(FromPrevious, Ret.Item2);
						}
					}
				}
			}
			return true;
		}

		private static void ReplaceProperties(Package? FromPrevious, List<Variance> Variances)
		{
			foreach (Variance v in Variances)
			{
				PropertyInfo pi = FromPrevious!.GetType().GetProperty(v.Prop!)!;
				pi.SetValue(FromPrevious, v.ValB, null);
			}
		}

		private static (bool, List<Variance>) ComparePackages(Package p, Package n)
		{
			bool same = true;

			List<Variance> rt = p.Compare(n);
			foreach (Variance v in rt)
			{
				if (v.Prop == "Recipient_Contents") { } //Doesn't compare well the Recipient Contents for some reason
				else
					same = false;
			}

			return (same, rt);
		}

		public bool UpdateRecords(List<Package> packages)
		{
			using (MySqlConnection db = Secrets.GetConnectionString())
			{
				packages.ForEach(x => x.Contents = JsonConvert.SerializeObject(x.Recipient_Contents));
				libmiroppb.Log($"Updating Record: {JsonConvert.SerializeObject(packages)}");
				DapperPlusManager.Entity<Package>().Table(Secrets.GetMySQLTable()).Identity(x => x.Id);
				db.BulkUpdate(packages);
			}
			return true;
		}

		public bool InsertRecordStatus(List<Package_Status> package_statuses)
		{
			using (MySqlConnection db = Secrets.GetConnectionString())
			{
				libmiroppb.Log($"Inserting Package Statuses: {JsonConvert.SerializeObject(package_statuses)}");
				DapperPlusManager.Entity<Package_Status>().Table(Secrets.GetMySQLPackageStatusTable()).Identity(x => x.Id);
				db.BulkInsert(package_statuses);
			}
			return true;
		}
	}
}
