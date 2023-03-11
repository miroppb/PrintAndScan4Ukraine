using Dapper;
using miroppb;
using MySqlConnector;
using Newtonsoft.Json;
using PrintAndScan4Ukraine.Model;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Z.Dapper.Plus;

namespace PrintAndScan4Ukraine.Data
{
	public interface IPackageDataProvider
	{
		Task<IEnumerable<Package>?> GetAllAsync();
		Task<IEnumerable<Package>?> GetByNameAsync(string SenderName);
		bool InsertRecord(Package package);
		bool UpdateRecord(List<Package> package);
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
			catch {
				libmiroppb.Log($"There is no connection to: {Secrets.GetMySQLUrl()}");
				MessageBox.Show($"There is no connection to: {Secrets.GetMySQLUrl()}");
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

		public bool UpdateRecord(List<Package> packages)
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
	}
}
