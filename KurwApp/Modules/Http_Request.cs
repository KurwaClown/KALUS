using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace League
{
	internal static class Http_Request
	{
		internal static HttpClient _httpClient;

		internal static void SetClient()
		{
			HttpClientHandler handler = new HttpClientHandler();
			handler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
			_httpClient = new HttpClient(handler);

			_httpClient.BaseAddress = new Uri($"https://127.0.0.1:{Auth.GetPort()}");

			_httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Auth.GetBasicAuth());
		}

		internal static async Task<HttpResponseMessage> GetRequest(string endpoint)
		{
			SetClient();
			HttpResponseMessage result = await _httpClient.GetAsync(endpoint);
			return result;
		}

		internal static async Task<byte[]> GetRequestImage(string endpoint)
		{
			SetClient();

			HttpResponseMessage result = await _httpClient.GetAsync(endpoint);
			return await result.Content.ReadAsByteArrayAsync();
		}

		internal static async Task<string> PostRequest(string endpoint, string body = "")
		{
			SetClient();

			_httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

			var content = new StringContent(body, Encoding.UTF8, "application/json");

			HttpResponseMessage result = await _httpClient.PostAsync(endpoint, content);

			return await result.Content.ReadAsStringAsync();
		}

		
		internal static async Task<string> PutRequest(string endpoint, string body)
		{
			SetClient();

			_httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

			var content = new StringContent(body, Encoding.UTF8, "application/json");

			HttpResponseMessage result = await _httpClient.PutAsync(endpoint, content);

			return await result.Content.ReadAsStringAsync();
		}

		internal static async Task<string> PatchRequest(string endpoint, string body) {
			SetClient();

			_httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

			var content = new StringContent(body, Encoding.UTF8, "application/json");

			HttpResponseMessage result = await _httpClient.PatchAsync(endpoint, content);

			return await result.Content.ReadAsStringAsync();
		}
	}
}
