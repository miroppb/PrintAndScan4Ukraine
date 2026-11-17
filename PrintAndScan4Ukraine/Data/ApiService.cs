using System;
using System.Net;
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

        public ApiService(string? token = null)
        {
            // Use SocketsHttpHandler for better connection pooling and reuse
            var handler = new SocketsHttpHandler
            {
                PooledConnectionLifetime = TimeSpan.FromMinutes(5),
                PooledConnectionIdleTimeout = TimeSpan.FromMinutes(2),
                MaxConnectionsPerServer = 10,
                EnableMultipleHttp2Connections = true,
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate | DecompressionMethods.Brotli
            };

            Client = new HttpClient(handler, disposeHandler: true)
            {
                BaseAddress = new Uri(Secrets.ApiBaseUrl.TrimEnd('/') + "/"),
                Timeout = TimeSpan.FromSeconds(30)
            };

            if (!string.IsNullOrEmpty(token))
            {
                Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
        }


    }
}