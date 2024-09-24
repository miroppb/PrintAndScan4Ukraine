using Dapper;
using Dapper.Contrib.Extensions;
using miroppb;
using MySqlConnector;
using PrintAndScan4Ukraine.Model;
using System;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;

namespace PrintAndScan4Ukraine.Data
{
	public interface IMainDataProvider
	{
		Task<Users> GetUserFromComputerNameAsync(string ComputerName);
		void Heartbeat(Users user);
	}

	public class MainDataProvider : IMainDataProvider
	{
		public async Task<Users> GetUserFromComputerNameAsync(string ComputerName)
		{
			Users user = new();
			using (MySqlConnection db = Secrets.GetConnectionString())
			{
				var temp = await db.QueryAsync<Users>($"SELECT id, computername, access, comment, lastconnectedversion, lang, lastcheckin FROM {Secrets.MySqlUserAccessTable} WHERE computername = @computername", new { ComputerName });
				if (temp != null && temp.Any())
				{
					user = temp.First();
					CultureInfo culture = CultureInfo.InstalledUICulture;

					await db.ExecuteAsync($"UPDATE {Secrets.MySqlUserAccessTable} SET lastconnectedversion = @lastconnectedversion, lang = @lang, lastcheckin = @Now WHERE id = @id",
						new { lastconnectedversion = Assembly.GetExecutingAssembly()
										   .GetName().Version!
										   .ToString(), id = temp.ToList()[0].Id, lang = culture.Name, DateTime.Now });
					user.Lang = culture.Name;
				}
				else
				{
					try
					{
						Libmiroppb.Log($"Inserting None User Access for: {Environment.MachineName}");
						db.Insert(new Users()
						{
							ComputerName = Environment.MachineName,
							Access = 0,
							Comment = "New",
							LastConnectedVersion = Assembly.GetExecutingAssembly().GetName().Version!.ToString(),
							LastCheckin = DateTime.Now
						});
					}
					catch (Exception ex)
					{
						Libmiroppb.Log($"Error while inserting new User Access: { ex.Message}");
						MessageBox.Show($"Error. Please contact your administrator: {ex.Message}");
					}
					
				}
			}
			return user;
		}

		public void Heartbeat(Users user)
		{
			using MySqlConnection db = Secrets.GetConnectionString();
			user.LastCheckin = DateTime.Now;
			db.Update(user);
		}
	}
}
