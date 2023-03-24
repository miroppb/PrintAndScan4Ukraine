using Dapper;
using miroppb;
using MySqlConnector;
using PrintAndScan4Ukraine.Model;
using System;
using System.Linq;
using System.Reflection;
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
				var temp = await db.QueryAsync<Users>($"SELECT id, computername, access, comment, lastconnectedversion FROM {Secrets.GetMySQLUserAccessTable()} WHERE computername = @computername", new { ComputerName });
				if (temp != null && temp.Count() > 0)
				{
					access = (Access)temp.FirstOrDefault()!.Access;
					DapperPlusManager.Entity<Users>().Table(Secrets.GetMySQLUserAccessTable()).Identity(x => x.Id);
					temp.ToList()[0].LastConnectedVersion = Assembly.GetExecutingAssembly().GetName().Version!.ToString();
					db.BulkUpdate(temp.ToList());
				}
				else
				{
					libmiroppb.Log($"Inserting None User Access for: {Environment.MachineName}");
					DapperPlusManager.Entity<Users>().Table(Secrets.GetMySQLUserAccessTable()).Identity(x => x.Id);
					db.BulkInsert(new Users() { ComputerName = Environment.MachineName, Access = 0, Comment = "New", LastConnectedVersion = Assembly.GetExecutingAssembly().GetName().Version!.ToString() });
				}
			}
			return access;
		}
	}
}
