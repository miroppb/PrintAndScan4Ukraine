using Dapper;
using Dapper.Contrib.Extensions;
using miroppb;
using MySqlConnector;
using PrintAndScan4Ukraine.Model;
using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace PrintAndScan4Ukraine.Data
{
	public interface IMainDataProvider
	{
		Task<Users> GetUserFromComputerNameAsync(string ComputerName);
	}

	public class MainDataProvider : IMainDataProvider
	{
		public async Task<Users> GetUserFromComputerNameAsync(string ComputerName)
		{
			Users user = new Users();
			using (MySqlConnection db = Secrets.GetConnectionString())
			{
				var temp = await db.QueryAsync<Users>($"SELECT id, access FROM {Secrets.MySqlUserAccessTable} WHERE computername = @computername", new { ComputerName });
				if (temp != null && temp.Any())
				{
					user = temp.First();
					await db.ExecuteAsync($"UPDATE {Secrets.MySqlUserAccessTable} SET lastconnectedversion = @lastconnectedversion WHERE id = @id",
						new { lastconnectedversion = Assembly.GetExecutingAssembly().GetName().Version!.ToString(), id = temp.ToList()[0].Id });
				}
				else
				{
					libmiroppb.Log($"Inserting None User Access for: {Environment.MachineName}");
					db.Insert(new Users() { ComputerName = Environment.MachineName, Access = 0, Comment = "New", LastConnectedVersion = Assembly.GetExecutingAssembly().GetName().Version!.ToString() });
				}
			}
			return user;
		}
	}
}
