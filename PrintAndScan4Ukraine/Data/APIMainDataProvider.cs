using CodingSeb.Localization;
using miroppb;
using Newtonsoft.Json;
using PrintAndScan4Ukraine.Model;
using System;
using System.Globalization;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace PrintAndScan4Ukraine.Data
{
    public class APIMainDataProvider(IApiService apiService) : IMainDataProvider
    {
        private readonly HttpClient _httpClient = apiService.Client;

        public async Task<Users> GetUserFromComputerNameAsync(string computerName, CancellationToken cancellationToken = default)
        {
            Users? user = new();
            try
            {
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
                var response = await _httpClient.PostAsync("/get-user", content, cancellationToken).ConfigureAwait(false);

                if (response.IsSuccessStatusCode)
                {
                    string json = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                    user = JsonConvert.DeserializeObject<Users>(json);

                    if (!string.IsNullOrWhiteSpace(user?.Api_key))
                    {
                        _ = new Secrets(user.Api_key); // set for use throughout app
                    }
                }
                else
                {
                    string error = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                    Libmiroppb.Log($"GetUser response error: {response.StatusCode} - {error}");
                    MessageBox.Show($"{Loc.Tr("PAS4U.API.VerifyAccess", "Could not verify access with PAS4U server.")}");
                }
            }
            catch (HttpRequestException ex)
            {
                Libmiroppb.Log(ex, "HTTP request failed in GetUser");
                MessageBox.Show($"{Loc.Tr("PAS4U.API.ConnectionFailed", "Failed to connect to PAS4U server.")}");
            }
            catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
            {
                Libmiroppb.Log(ex, "Request timed out in GetUser");
                MessageBox.Show($"{Loc.Tr("PAS4U.API.RequestTimedOut", "Request timed out. Please try again.")}");
            }
            catch (Exception ex)
            {
                Libmiroppb.Log(ex, "Exception in GetUser");
                MessageBox.Show($"{Loc.Tr("PAS4U.API.ConnectionFailed", "Failed to connect to PAS4U server.")}");
            }
            return user ?? new();
        }

        public async Task Heartbeat(Users user, CancellationToken cancellationToken = default)
        {
            try
            {
                var requestBody = new
                {
                    id = user.Id,
                    lastcheckin = DateTime.Now
                };

                var content = new StringContent(JsonConvert.SerializeObject(requestBody), Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync("/heartbeat", content, cancellationToken).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    string error = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                    Libmiroppb.Log($"Heartbeat failed: {response.StatusCode} - {error}");
                }
            }
            catch (Exception ex)
            {
                Libmiroppb.Log(ex, "Exception in Heartbeat");
            }
        }
    }
}