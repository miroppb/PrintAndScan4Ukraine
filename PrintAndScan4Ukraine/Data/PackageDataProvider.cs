using Dapper;
using miroppb;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using PrintAndScan4Ukraine.Model;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Z.Dapper.Plus;

namespace PrintAndScan4Ukraine.Data
{
	public interface IPackageDataProvider
	{
		Task<IEnumerable<Package>?> GetAllAsync();
		Task<bool> InsertRecordAsync(Package package);
		bool UpdateRecord(List<Package> package);
	}

	public class PackageDataProvider : IPackageDataProvider
	{
		public async Task<IEnumerable<Package>?> GetAllAsync()
		{
			libmiroppb.Log("Get List of Packages");
			IEnumerable<Package> packages = new List<Package>();
			using (MySqlConnection db = Secrets.GetConnectionString())
			{
				var temp = await db.QueryAsync<Package>($"SELECT * FROM {Secrets.MySqlTable} WHERE removed = 0");
				packages = temp.ToList().Select(x =>
				{
					x.Recipient_Contents = JsonConvert.DeserializeObject<List<Contents>>(x.Contents!)!;
					return x;
				}).ToList();
			}
			libmiroppb.Log(JsonConvert.SerializeObject(packages));
			return packages;
		}

		public async Task<bool> InsertRecordAsync(Package package)
		{
			using (MySqlConnection db = Secrets.GetConnectionString())
			{
				libmiroppb.Log($"Inserting into Database: {JsonConvert.SerializeObject(package)}");
				string sql = $"INSERT INTO {Secrets.MySqlTable}(id, packageid, cost, insurance, delivery, other, date_added) VALUES(NULL, @packageId, 0, 0, 0, 0, @date_added)";
				await db.ExecuteAsync(sql, new { package.PackageId, date_added = package.Date_Added.ToString("yyyy-MM-dd HH:mm:ss") });
			}
			return true;
		}

		public bool UpdateRecord(List<Package> packages)
		{
			using (MySqlConnection db = Secrets.GetConnectionString())
			{
				packages.ForEach(x => x.Contents = JsonConvert.SerializeObject(x.Recipient_Contents));
				libmiroppb.Log($"Updating Record: {JsonConvert.SerializeObject(packages)}");
				DapperPlusManager.Entity<Package>().Table(Secrets.MySqlTable).Identity(x => x.Id);
				db.BulkUpdate(packages);
			}
			return true;
		}
	}
}
