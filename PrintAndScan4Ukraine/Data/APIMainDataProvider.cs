using CodingSeb.Localization;
using miroppb;
using Newtonsoft.Json;
using PrintAndScan4Ukraine.Model;
using System;
using System.Globalization;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace PrintAndScan4Ukraine.Data
{
    public class APIMainDataProvider : IMainDataProvider
    {
        public async Task<Users> GetUserFromComputerNameAsync(string computerName)
        {
            Users? user = new();
            try
            {
                using HttpClient client = new();
                string url = $"{Secrets.ApiBaseUrl}/get-user";

                var requestBody = new
                {
                    computername = computerName,
                    version = Assembly.GetExecutingAssembly().GetName().Version!.ToString(),
                    lang = CultureInfo.InstalledUICulture.Name,
#if DEBUG
                    is_dev = true
#endif
                };

                var content = new StringContent(JsonConvert.SerializeObject(requestBody), Encoding.UTF8, "application/json");
                var response = await client.PostAsync(url, content);

                if (response.IsSuccessStatusCode)
                {
                    string json = await response.Content.ReadAsStringAsync();
                    user = JsonConvert.DeserializeObject<Users>(json);

                    if (!string.IsNullOrWhiteSpace(user?.Api_key))
                    {
                        _ = new Secrets(user.Api_key); // set for use throughout app
                    }
                }
                else
                {
                    MessageBox.Show($"{Loc.Tr("PAS4U.API.VerifyAccess", "Could not verify access with PAS4U server.")}");
                    Libmiroppb.Log($"GetUser response error: {await response.Content.ReadAsStringAsync()}");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"{Loc.Tr("PAS4U.API.ConnectionFailed", "Failed to connect to PAS4U server.")}");
                Libmiroppb.Log($"Exception in GetUser: {ex.Message}");
            }
            return user ?? new();
        }


        public async void Heartbeat(Users user)
        {
            using HttpClient client = new();
            string url = $"{Secrets.ApiBaseUrl}/heartbeat";

            var requestBody = new
            {
                id = user.Id,
            };

            var content = new StringContent(JsonConvert.SerializeObject(requestBody), Encoding.UTF8, "application/json");
            var response = await client.PostAsync(url, content);
        }
    }
}
