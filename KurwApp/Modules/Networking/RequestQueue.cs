using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace KurwApp.Modules.Networking
{
    internal class RequestQueue
    {
        private static HttpClient _httpClient = new HttpClient();
        private static readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1);
        private static readonly Queue<Func<Task>> _requestQueue = new Queue<Func<Task>>();

        static RequestQueue()
        {
            ProcessNextRequest();
        }



        public static void Enqueue(Func<Task> request)
        {
            _requestQueue.Enqueue(request);
        }

        internal static void SetClient()
        {
            var certFilePath = "Certificates/riotgames.pem";
            var certCollection = new X509Certificate2Collection();
            certCollection.Import(certFilePath);
            var riotCert = certCollection[0];
            HttpClientHandler handler = new()
            {

                ClientCertificateOptions = ClientCertificateOption.Manual,
                SslProtocols = System.Security.Authentication.SslProtocols.Tls12 | System.Security.Authentication.SslProtocols.Tls11 | System.Security.Authentication.SslProtocols.Tls,
                ServerCertificateCustomValidationCallback =
                (httpRequestMessage, cert, cetChain, policyErrors) =>
                {
                    return true;
                }
            };
            handler.ClientCertificates.Add(riotCert);

            _httpClient = new HttpClient(handler)
            {
                BaseAddress = new Uri($"https://127.0.0.1:{Auth.GetPort()}")
            };

            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Auth.GetBasicAuth());
        }

        private static async Task ProcessNextRequest()
        {
            while (true)
            {
                await _semaphore.WaitAsync();

                if (_requestQueue.Count == 0)
                {
                    _semaphore.Release();
                    await Task.Delay(100);
                    continue;
                }

                var request = _requestQueue.Dequeue();
                _semaphore.Release();
                try
                {
                    await request();

                }
                catch (NullReferenceException)
                {
                    await Task.Delay(100);
                    continue;
                }
            }
        }

        internal static async Task<string> Request(HttpMethod httpMethod, string endpoint, string requestBody = "")
        {
            var request = async () =>
            {
                SetClient();
                var httpContent = new StringContent(requestBody, Encoding.UTF8, "application/json");

                var response = await _httpClient.SendAsync(new HttpRequestMessage(httpMethod, endpoint) { Content = httpContent });
                string responseString = string.Empty;

                if (response.IsSuccessStatusCode) responseString = await response.Content.ReadAsStringAsync();
                else
                {
                    Debug.WriteLine(response.StatusCode);
                    Debug.WriteLine(endpoint);
                }
                _httpClient.Dispose();
                return responseString;
            };

            var taskCompletionSource = new TaskCompletionSource<string>();

            Enqueue(async () =>
            {
                try
                {
                    var result = await request();
                    taskCompletionSource.SetResult(result);
                }
                catch (Exception ex)
                {
                    taskCompletionSource.SetException(ex);
                }
            });

            return await taskCompletionSource.Task;
        }

        internal static async Task<byte[]> GetImage(string endpoint)
        {
            var request = async () =>
            {
                SetClient();

                var response = await _httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Get, endpoint));
                byte[] responseBytes = new byte[0];
                if (response.IsSuccessStatusCode) responseBytes = await response.Content.ReadAsByteArrayAsync();
                _httpClient.Dispose();
                return responseBytes;
            };

            var taskCompletionSource = new TaskCompletionSource<byte[]>();

            Enqueue(async () =>
            {
                try
                {
                    var result = await request();
                    taskCompletionSource.SetResult(result);
                }
                catch (Exception ex)
                {
                    taskCompletionSource.SetException(ex);
                }
            });

            return await taskCompletionSource.Task;
        }
    }
}
