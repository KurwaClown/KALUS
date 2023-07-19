using Octokit;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Kalus.Modules.Networking
{
	internal class UpdateVerifier
	{
		private static readonly string owner = "KurwaClown";
		private static readonly string repo = "Kalus";

		private static readonly GitHubClient client = new(new ProductHeaderValue("Kalus"));

		private static readonly string kalusPath = Path.Combine(Environment.CurrentDirectory, "Kalus.exe");
		private static readonly string updaterPath = Path.Combine(Environment.CurrentDirectory, "Kalus.Updater.exe");

		internal static void CheckForUpdate()
		{

			if (!NewReleaseAvailable()) return;
			Debug.WriteLine(CultureInfo.CurrentCulture.Name);
			var updateRequest = MessageBox.Show("New KALUS Update available !\rDo you want to update now ?", "KALUS Update", MessageBoxButtons.YesNo);

			if(updateRequest == DialogResult.Yes)
			{
				Process.Start(updaterPath, CultureInfo.CurrentCulture.Name);
				Environment.Exit(0);
			}
		}

		private static bool NewReleaseAvailable()
		{

			Release latestRelease = client.Repository.Release.GetAll(owner, repo).Result[0];

			string? kalusVersion = FileVersionInfo.GetVersionInfo(kalusPath).ProductVersion;

			if (kalusVersion == null)
				return false;

			Version currentVersion = Assembly.GetExecutingAssembly().GetName().Version!;
			Version latestReleaseVersion = new(latestRelease.TagName.Trim('v'));

			return latestReleaseVersion.CompareTo(currentVersion) == 1;
		}
	}
}
