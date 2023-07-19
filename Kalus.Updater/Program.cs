using System.Diagnostics;
using System.Globalization;
using System.IO.Compression;
using Octokit;

namespace Kalus.Updater
{
	class Program
	{
		private static readonly string owner = "KurwaClown";
		private static readonly string repo = "Kalus";

		private static readonly string pathToKalus = Path.Combine(Environment.CurrentDirectory, "Kalus.exe");
		private static readonly string tempDownloadPath = Path.Combine(Path.GetTempPath(), "KalusLatestRelease.zip");

		private static readonly GitHubClient client = new(new ProductHeaderValue("Kalus"));

		static void Main(string[] args)
		{
			CultureInfo.CurrentCulture = new CultureInfo(args[0]);
			CultureInfo.CurrentUICulture = new CultureInfo(args[0]);
			Console.WriteLine(Properties.Resources.FileDownload);

			bool isDownloadSuccesfull = DownloadLatestRelease().GetAwaiter().GetResult();
			if (!isDownloadSuccesfull)
				return;

			Console.WriteLine(Properties.Resources.DownloadSuccessful);

			bool fileReplaced = ReplaceFile();
			if (!fileReplaced)
				Console.WriteLine(Properties.Resources.UpdateFail);
			else Console.WriteLine(Properties.Resources.UpdateSuccess);

			Console.WriteLine(Properties.Resources.DeleteTempFile);
			File.Delete(tempDownloadPath);

			Console.WriteLine(Properties.Resources.OpenKalus);

			Thread.Sleep(3000);

			Process.Start(pathToKalus);
		}

		private async static Task<bool> DownloadLatestRelease()
		{
			string zipUrl = client.Repository.Release.GetAll(owner, repo).Result[0].Assets[0].BrowserDownloadUrl;

			try
			{
				using var httpClient = new HttpClient();
				httpClient.DefaultRequestHeaders.Add("user-agent", "KurwaClown");

				byte[] fileBytes = await httpClient.GetByteArrayAsync(zipUrl);

				File.WriteAllBytes(tempDownloadPath, fileBytes);
			}
			catch (Exception ex)
			{
				Console.WriteLine($"{Properties.Resources.DownloadError} {ex.Message}");
				return false;
			}

			return true;
		}


		private static bool ReplaceFile()
		{
			ZipArchive tempDownload = ZipFile.OpenRead(tempDownloadPath);

			List<ZipArchiveEntry> filesToReplace = AggregatingNewFilePaths(tempDownload);

			if (filesToReplace.Count == 0)
			{
				Console.WriteLine(Properties.Resources.NoNewFile);
				return false;
			}



			Console.WriteLine($"{filesToReplace.Count} {Properties.Resources.FilesFoundCount}");

			Console.WriteLine(Properties.Resources.ReplacingFiles);

			foreach (ZipArchiveEntry entry in filesToReplace)
			{
				string oldFilePath = Path.Combine(Environment.CurrentDirectory, entry.FullName);

				entry.ExtractToFile(oldFilePath, true);
			}

			tempDownload.Dispose();
			return true;
		}

		private static List<ZipArchiveEntry> AggregatingNewFilePaths(ZipArchive zipArchive)
		{
			List<ZipArchiveEntry> filePaths = new();
			foreach (ZipArchiveEntry entry in zipArchive.Entries)
			{
				if (entry.FullName.StartsWith("Kalus.Updater"))
					continue;

				if (entry.FullName.EndsWith("/"))
				{
					ProcessNestedEntries(zipArchive, entry.FullName, ref filePaths);
					continue;
				}

				filePaths.Add(entry);
			}

			return filePaths;
		}
		private static void ProcessNestedEntries(ZipArchive archive, string parentDirectory, ref List<ZipArchiveEntry> filePaths)
		{
			foreach (var entry in archive.Entries)
			{
				if (entry.FullName.StartsWith(parentDirectory))
				{
					if (entry.FullName.EndsWith("/"))
					{
						// Recursive call for nested directories
						ProcessNestedEntries(archive, entry.FullName, ref filePaths);
					}
					else
					{
						filePaths.Add(entry);
					}
				}
			}
		}
	}
}