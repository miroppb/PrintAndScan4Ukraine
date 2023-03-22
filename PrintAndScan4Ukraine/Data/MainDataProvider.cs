using Dapper;
using miroppb;
using MySqlConnector;
using PrintAndScan4Ukraine.Model;
using System;
using System.Linq;
using System.Threading.Tasks;
using Z.Dapper.Plus;

namespace PrintAndScan4Ukraine.Data
{
	public interface IMainDataProvider
	{
		Task<Access> GetAccessFromComputerNameAsync(string ComputerName);
	}

	public class MainDataProvider : IMainDataProvider
	{
		public async Task<Access> GetAccessFromComputerNameAsync(string ComputerName)
		{
			Access access = Access.None;
			using (MySqlConnection db = Secrets.GetConnectionString())
			{
				var temp = await db.QueryAsync<Users>($"SELECT id, computername, access, comment FROM {Secrets.GetMySQLUserAccessTable()} WHERE computername = @computername", new { ComputerName });
				if (temp != null && temp.Count() > 0)
					access = (Access)temp.FirstOrDefault()!.Access;
				else
				{
					libmiroppb.Log($"Inserting None User Access for: {Environment.MachineName}");
					DapperPlusManager.Entity<Users>().Table(Secrets.GetMySQLUserAccessTable()).Identity(x => x.Id);
					db.BulkInsert(new Users() { ComputerName = Environment.MachineName, Access = 0, Comment = "New" });
				}
			}
			return access;
		}
	}
}
