using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Windows;

namespace PrintAndScan4Ukraine.Data
{
    public interface IApiService
    {
        HttpClient Client { get; }
    }

    public class ApiService : IApiService
    {
        public HttpClient Client { get; }

        public ApiService(string token)
        {
            if (token == null || string.IsNullOrEmpty(token))
                Application.Current.Shutdown();
            Client = new HttpClient
            {
                BaseAddress = new Uri(Secrets.ApiBaseUrl)
            };
            Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }
    }

}
