﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Kalus.Modules.Networking
{
	internal class RequestQueue
	{
		private static HttpClient _httpClient = new();
		private static readonly SemaphoreSlim _semaphore = new(1);
		private static readonly Queue<Func<Task>> _requestQueue = new();

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
			var certFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Certificates/riotgames.pem");
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

		private static async void ProcessNextRequest()
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
				//if (_httpClient.BaseAddress?.ToString().Contains("127.0.0.1:0") == true) return string.Empty;
				var httpContent = new StringContent(requestBody, Encoding.UTF8, "application/json");


				try
				{
					var response = await _httpClient.SendAsync(new HttpRequestMessage(httpMethod, endpoint) { Content = httpContent });
					string responseString = string.Empty;

					if (!response.IsSuccessStatusCode) Debug.WriteLine($"{response.StatusCode} for endpoint : {endpoint}");
					responseString = await response.Content.ReadAsStringAsync();
					_httpClient.Dispose();
					return responseString;
				}
				catch (HttpRequestException)
				{
					_httpClient.Dispose();
					return string.Empty;
				}
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
				byte[] responseBytes = Array.Empty<byte>();
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