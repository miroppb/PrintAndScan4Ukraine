using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using MySqlConnector;
using PrintAndScan4Ukraine.Model;

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
				var temp = await db.QueryAsync<Users>("SELECT id, computername, access, comment FROM users WHERE computername = @computername", new { ComputerName });
				if (temp != null)
					access = (Access)temp.FirstOrDefault()!.Access;
			}
			return access;
		}
	}
}
