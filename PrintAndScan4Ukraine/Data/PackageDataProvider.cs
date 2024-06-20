using CodingSeb.Localization;
using Dapper;
using Dapper.Contrib.Extensions;
using miroppb;
using MySqlConnector;
using Newtonsoft.Json;
using PrintAndScan4Ukraine.Model;
using ServiceStack;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;

namespace PrintAndScan4Ukraine.Data
{
	public interface IPackageDataProvider
	{
		Task<IEnumerable<Package>?> GetAllAsync(bool initialLoad);
		Task<IEnumerable<Package>?> GetByNameAsync(string SenderName);
		IEnumerable<Package_Status>? GetAllStatuses(List<string> ids);
		IEnumerable<Package_Status>? GetStatusByPackage(string packageid);
		Task<IEnumerable<Package>?> GetPackagesByDateAndLastStatusAsync(DateTime start, DateTime end, int status);
		bool InsertRecord(Package package);
		bool VerifyIfExists(string packageid);
		Task<bool> ReloadPackagesAndUpdateIfChanged(ObservableCollection<Package> packages, Package CurrentlySelected);
		bool UpdateRecords(List<Package> package, int type = 0);
		bool InsertRecordStatus(List<Package_Status> package_statuses);
		IEnumerable<MissingPackages> FindMissingPackages(List<string> barcodesNotInPackages);
		IEnumerable<Users> GetUserIDsAndNames();
		Task<IEnumerable<Package>> GetPackageAsync(string packageid, bool useArchive); //returning list because we have duplicates :/
		List<Package_less> MapPackagesAndStatusesToLess(IEnumerable<Package> packages, IEnumerable<Package_Status> statuses);
		DateTime GetServerDate();
	}

	public class PackageDataProvider : IPackageDataProvider
	{
		public async Task<IEnumerable<Package>?> GetAllAsync(bool initialLoad)
		{
			libmiroppb.Log($"Get List of Packages{(initialLoad ? "" : " for a refresh")}");
			IEnumerable<Package> packages = new List<Package>();
			try
			{
				using MySqlConnection db = Secrets.GetConnectionString();
				var temp = await db.QueryAsync<Package>($"SELECT * FROM {Secrets.MySqlPackagesTable} WHERE removed = 0");
				packages = temp.ToList().Select(x =>
				{
					x.Recipient_Contents = x.Contents != null ? JsonConvert.DeserializeObject<List<Contents>>(x.Contents)! : new List<Contents>() { };
					return x;
				}).ToList();
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
				var temp = await db.QueryAsync<Package>($"SELECT * FROM {Secrets.MySqlPackagesTable} WHERE sender_name = @SenderName", new { SenderName });
				packages = temp.ToList().Select(x =>
				{
					x.Recipient_Contents = x.Contents != null ? JsonConvert.DeserializeObject<List<Contents>>(x.Contents)! : new List<Contents>() { };
					return x;
				}).ToList();
			}
			libmiroppb.Log(JsonConvert.SerializeObject(packages));
			return packages;
		}

		public IEnumerable<Package_Status>? GetAllStatuses(List<string> ids)
		{
			libmiroppb.Log("Get List of Packages Statuses");
			IEnumerable<Package_Status> statuses = new List<Package_Status>();
			try
			{
				using (MySqlConnection db = Secrets.GetConnectionString())
					statuses = db.Query<Package_Status>($"SELECT id, packageid, createddate, status FROM {Secrets.MySqlPackageStatusTable} WHERE packageid IN ({string.Join(",", ids.Select(x => $"'{x}'"))}) ORDER BY id");

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
			libmiroppb.Log($"Get List of Package Statuses for {packageid}");
			IEnumerable<Package_Status> statuses = new List<Package_Status>();
			try
			{
				using MySqlConnection db = Secrets.GetConnectionString();
				statuses = db.Query<Package_Status>($"SELECT id, packageid, createddate, status FROM {Secrets.MySqlPackageStatusTable} WHERE packageid = @packageid UNION SELECT id, packageid, createddate, status FROM {Secrets.MySqlPackageStatusArchiveTable} WHERE packageid = @packageid ORDER BY id", new { packageid });

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
			using MySqlConnection db = Secrets.GetConnectionString();
			libmiroppb.Log($"Inserting into Database: {JsonConvert.SerializeObject(package)}");
			db.Insert(package);
			return true;
		}

		public bool VerifyIfExists(string packageid)
		{
			using MySqlConnection db = Secrets.GetConnectionString();
			bool InNewList = db.Query<Package>($"SELECT id FROM {Secrets.MySqlPackagesTable} WHERE packageid = @packageid LIMIT 1", new { packageid }).FirstOrDefault() != null;
			bool InArchive = db.Query<Package>($"SELECT id FROM {Secrets.MySqlPackagesArchiveTable} WHERE packageid = @packageid LIMIT 1", new { packageid }).FirstOrDefault() != null;
			return InNewList || InArchive;
		}

		public async Task<bool> ReloadPackagesAndUpdateIfChanged(ObservableCollection<Package> packages, Package CurrentlySelected)
		{
			if (CurrentlySelected == null) return false;
			IEnumerable<Package>? RefreshedPackages = await GetAllAsync(false);
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
							if (package.Id != CurrentlySelected.Id) //Refresh only for non-current package. Otherwise could cause discrepencies
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

		public bool UpdateRecords(List<Package> packages, int type = 0)
		{
			using MySqlConnection db = Secrets.GetConnectionString();
			packages.ForEach(x => x.Contents = JsonConvert.SerializeObject(x.Recipient_Contents));
			if (type == -2) { libmiroppb.Log($"Saving Removed Records: {JsonConvert.SerializeObject(packages.Select(x => x.Id).ToList())}"); }
			else { libmiroppb.Log($"Saving {(type == -1 ? "Previous" : "Current")} Record: {JsonConvert.SerializeObject(packages)}"); }
			foreach (Package p in packages)
			{
				if (p.PackageIDModified)
				{
					p.NewPackageId = p.NewPackageId.ToLower();
					//check if in correct format
#if DEBUG
					Regex regex = new Regex("^cv\\d{7,9}us$");
#else
					Regex regex = new Regex("^cv\\d{7,9}us$");
#endif
					Match match = regex.Match(p.NewPackageId);
					bool ForceSave = false;
					if (!match.Success)
					{
						ForceSave = false;
						if (MessageBox.Show(string.Format(Loc.Tr("PAS4U.MainWindow.NewWrongPackageIDFormat", "New Package ID isn't in the correct format:\n\nScanned: {0}\n\nAppropriate: CV#########US\n\n Force save?"), p.NewPackageId), "Incorrect format", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
							ForceSave = true;
						else
							return false;
					}

					if (ForceSave)
					{
						libmiroppb.Log($"Package ID has been updated from {p.PackageId} to {p.NewPackageId}");
						db.Execute($"UPDATE {Secrets.MySqlPackageStatusTable} SET packageid = @NewPackageId WHERE packageid = @PackageId", new { p.PackageId, p.NewPackageId });
						p.PackageId = p.NewPackageId;
						p.PackageIDModified = false;
					}
				}
				db.Update(p);
			}
			return true;
		}

		public bool InsertRecordStatus(List<Package_Status> package_statuses)
		{
			using MySqlConnection db = Secrets.GetConnectionString();
			libmiroppb.Log($"Inserting Package Statuses: {JsonConvert.SerializeObject(package_statuses)}");
			db.Insert(package_statuses);
			return true;
		}

		public static string StatusToText(int status)
		{
			switch (status)
			{
				case 1:
					return Loc.Tr("PAS4U.Export.Status.1", "Scanned New");
				case 2:
					return Loc.Tr("PAS4U.Export.Status.2", "Scanned Shipped");
				case 3:
					return Loc.Tr("PAS4U.Export.Status.3", "Scanned Arrived");
				case 4:
					return Loc.Tr("PAS4U.Export.Status.4", "Scanned Delivered");
				default:
					return "";
			}
		}

		public async Task<IEnumerable<Package>?> GetPackagesByDateAndLastStatusAsync(DateTime start_date, DateTime end_date, int status_code)
		{
			using MySqlConnection db = Secrets.GetConnectionString();
			return await db.QueryAsync<Package>("Packages_between_dates_and_status", new { start_date, end_date, status_code }, commandType: System.Data.CommandType.StoredProcedure, commandTimeout: 300);
		}

		public IEnumerable<MissingPackages> FindMissingPackages(List<string> barcodesNotInPackages)
		{
			List<MissingPackages> results = new();
			//lets get a list of /all/ packages
			using MySqlConnection db = Secrets.GetConnectionString();
			List<Package> allPackages = db.Query<Package>($"SELECT packageid FROM {Secrets.MySqlPackagesTable}").ToList();
			//find packages that aren't in the db
			var notInDB = barcodesNotInPackages.Except(allPackages.Select(x => x.PackageId)).ToList();
			foreach (string item in notInDB)
			{
				results.Add(new() { Packageid = item, InPackages = false });
				barcodesNotInPackages.Remove(item);
			}
			//for packages that are in db, get list of statuses for each
			foreach (string barcode in barcodesNotInPackages)
			{
				var statuses = db.Query<Package_Status>($"SELECT * FROM {Secrets.MySqlPackageStatusTable} WHERE packageid = @barcode", new { barcode }).ToList();
				results.Add(new MissingPackages() { InPackages = true, Packageid = barcode, Statuses = statuses });
			}

			return results;
		}

		public IEnumerable<Users> GetUserIDsAndNames()
		{
			using MySqlConnection db = Secrets.GetConnectionString();
			return db.Query<Users>("SELECT id, SUBSTRING_INDEX(`comment`, ' ', 1) AS comment FROM users");
		}

		public async Task<IEnumerable<Package>> GetPackageAsync(string packageid, bool useArchive)
		{
			using MySqlConnection db = Secrets.GetConnectionString();
			string sql = $"SELECT * FROM {Secrets.MySqlPackagesTable} WHERE packageid = @packageid ";
			if (useArchive) sql += $"UNION SELECT * FROM {{Secrets.MySqlPackagesArchiveTable}} WHERE packageid = @packageid";
			return await db.QueryAsync<Package>(sql, new { packageid });
		}

		public List<Package_less> MapPackagesAndStatusesToLess(IEnumerable<Package> packages, IEnumerable<Package_Status> statuses)
		{
			List<Package_less> list = new();
			foreach (Package package in packages)
			{
				Package temp = package.CreateCopy();
				List<Contents> t = package.Recipient_Contents.CreateCopy();
				List<string> output = new();
				foreach (var item in t) { output.Add($"{item.Name}: {item.Amount}"); }

				var jsonParent = JsonConvert.SerializeObject(temp);
				Package_less c = JsonConvert.DeserializeObject<Package_less>(jsonParent)!;
				c.Contents = output.Join(Environment.NewLine);

				List<Package_Status> s = statuses.Where(s => s.PackageId == c.PackageId).ToList();
				List<string> so = new();
				s.ForEach(x => so.Add(x.ToString()));
				c.Statuses = so.Join(Environment.NewLine);

				list.Add(c);
			}
			return list;
		}

		public DateTime GetServerDate()
		{
			using MySqlConnection db = Secrets.GetConnectionString();
			return db.QuerySingle<DateTime>("SELECT NOW()");
		}
	}
}
