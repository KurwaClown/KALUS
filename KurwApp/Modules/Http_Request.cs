using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace KurwApp
{
	internal static class Http_Request
	{
		//Create a new HTTP Client
		internal static HttpClient CreateClient()
		{
			var certFilePath = "riotgames.pem";
			var certCollection = new X509Certificate2Collection();
			certCollection.Import(certFilePath);
			var riotCert = certCollection[0];
			HttpClientHandler handler = new();
			handler.ClientCertificates.Add(riotCert);
			var httpClient = new HttpClient(handler)
			{
				BaseAddress = new Uri($"https://127.0.0.1:{Auth.GetPort()}")
			};

			httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Auth.GetBasicAuth());

			return httpClient;
		}

		//Default HTTP GET Request
		internal static async Task<HttpResponseMessage> GetRequest(string endpoint)
		{
			HttpClient client = CreateClient();
			HttpResponseMessage result = await client.GetAsync(endpoint);
			return result;
		}

		//Http Get Request for images
		internal static async Task<byte[]> GetRequestImage(string endpoint)
		{
			HttpClient client = CreateClient();

			HttpResponseMessage result = await client.GetAsync(endpoint);
			return await result.Content.ReadAsByteArrayAsync();
		}

		//Default HTTP POST Request
		internal static async Task<string> PostRequest(string endpoint, string body = "")
		{
			HttpClient client = CreateClient();

			client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

			var content = new StringContent(body, Encoding.UTF8, "application/json");

			HttpResponseMessage result = await client.PostAsync(endpoint, content);

			return await result.Content.ReadAsStringAsync();
		}

		//Default HTTP PUT Request
		internal static async Task<string> PutRequest(string endpoint, string body)
		{
			HttpClient client = CreateClient();

			client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

			var content = new StringContent(body, Encoding.UTF8, "application/json");

			HttpResponseMessage result = await client.PutAsync(endpoint, content);

			return await result.Content.ReadAsStringAsync();
		}

		//Default HTTP PATCH Request
		internal static async Task<string> PatchRequest(string endpoint, string body)
		{
			HttpClient client = CreateClient();

			client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

			var content = new StringContent(body, Encoding.UTF8, "application/json");

			HttpResponseMessage result = await client.PatchAsync(endpoint, content);

			return await result.Content.ReadAsStringAsync();
		}
	}
}
