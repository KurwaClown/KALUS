using Kalus.Modules.Networking;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace Kalus.Modules
{
	internal static class DataCache
	{
		private static readonly string picksDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Picks");
		private static readonly string pickBanPath = Path.Combine(picksDirectory, "PickBan.json");
		private static readonly JObject pickBan = InitializePickBan();

		private static JArray? championsInformation;
		private static JArray? championsRunesRecommendation;
		private static JArray? runesStyleInformation;
		private static JArray? runesInformation;

		private static string? appRunePageId;

		private static byte[]? defaultChampionIcon;
		private static byte[]? defaultRuneIcon;
		private static byte[]? defaultMapIcon;

		private static byte[]? ingameClassicIcon;
		private static byte[]? champSelectClassicIcon;

		private static byte[]? ingameAramIcon;
		private static byte[]? champSelectAramIcon;

		#region Files

		private static JObject InitializePickBan()
		{
			try
			{
				if(!File.Exists(pickBanPath)) CreatePickBanFile();
			}
			catch (JsonReaderException)
			{
				MessageBox.Show("Error while getting picks and bans, try deleting the Picks folder to correct this error");
				//Forcing creation of new pickban file
				CreatePickBanFile();
			}
			return JObject.Parse(File.ReadAllText(pickBanPath));
		}

		private static void CreatePickBanFile()
		{
			if (!Directory.Exists(picksDirectory)) Directory.CreateDirectory(picksDirectory);
			if (!File.Exists(pickBanPath))
			{
				object template = new
				{
					Draft = new
					{
						Pick = new
						{
							TOP = Array.Empty<int>(),
							JUNGLE = Array.Empty<int>(),
							MIDDLE = Array.Empty<int>(),
							BOTTOM = Array.Empty<int>(),
							UTILITY = Array.Empty<int>()
						},
						Ban = new
						{
							TOP = Array.Empty<int>(),
							JUNGLE = Array.Empty<int>(),
							MIDDLE = Array.Empty<int>(),
							BOTTOM = Array.Empty<int>(),
							UTILITY = Array.Empty<int>()
						}
					},
					Blind = Array.Empty<int>(),
					Aram = Array.Empty<int>()
				};

				// Serialize the template object to JSON.
				string json = JsonConvert.SerializeObject(template, Formatting.Indented);

				// Write the JSON to the file.
				File.WriteAllText(pickBanPath, json);

			}

		}

		#region PickBan
		internal static void SavePickBan()
		{
			File.WriteAllText(pickBanPath, pickBan.ToString());
		}

		internal static int[]? GetDraftPick(string position)
		{
			var picks = pickBan.SelectToken($"Draft.Pick.{position}")?.Values<int>().ToArray();

			return picks;
		}

		internal static void SetDraftPick(string position, JArray pick)
		{
			pickBan["Draft"]!["Pick"]![position] = pick;

			SavePickBan();
		}

		internal static int[]? GetDraftBan(string position)
		{
			int[]? bans = pickBan.SelectToken($"Draft.Ban.{position}")?.Values<int>().ToArray();

			return bans;
		}

		internal static void SetDraftBan(string position, JArray ban)
		{
			pickBan["Draft"]!["Ban"]![position] = ban;

			SavePickBan();
		}

		internal static int[]? GetBlindPick()
		{
			var picks = pickBan["Blind"]?.Select(pick => (int)pick).ToArray();

			return picks;
		}

		internal static void SetBlindPick(JArray pick)
		{
			pickBan["Blind"] = pick;

			SavePickBan();
		}

		internal static int[] GetAramPick()
		{
			var picks = pickBan["Aram"]?.Select(pick => (int)pick).ToArray();
			if (picks == null) return Array.Empty<int>();
			return picks;
		}

		internal static void SetAramPick(JArray pick)
		{
			pickBan["Aram"] = pick;

			SavePickBan();
		}
		#endregion PickBan

		#endregion Files

		internal static async Task<JArray> GetChampionsInformations()
		{
			championsInformation ??= await ClientRequest.GetChampionsInfo();

			return championsInformation;
		}

		internal static async Task<JArray> GetChampionsRunesRecommendation()
		{
			championsRunesRecommendation ??= await ClientRequest.GetRecommendedRunes();

			return championsRunesRecommendation;
		}

		internal static async Task<JArray> GetRunesStyleInformation()
		{
			if(runesStyleInformation == null)
			{

				runesStyleInformation = await ClientRequest.GetRunesStylesInformation();


				for (int i = 0; i < runesStyleInformation.Count; i++)
				{
					JObject trimmedRune = new()
					{
						{ "id", runesStyleInformation[i].SelectToken("id") },
						{ "name", runesStyleInformation[i].SelectToken("name") },
						{ "iconPath", runesStyleInformation[i].SelectToken("iconPath") }
					};
					runesStyleInformation[i] = trimmedRune;
				}
			}

			return runesStyleInformation;
		}

		internal static async Task<JArray> GetRunesInformation()
		{
			if(runesInformation == null)
			{

				runesInformation = await ClientRequest.GetRunesInformation();

				for (int i = 0; i < runesInformation.Count; i++)
				{
					JObject trimmedRune = new()
					{
						{ "id", runesInformation[i].SelectToken("id") },
						{ "name", runesInformation[i].SelectToken("name") },
						{ "recommendationDescriptor", runesInformation[i].SelectToken("recommendationDescriptor") },
						{ "iconPath", runesInformation[i].SelectToken("iconPath") }
					};
					runesInformation[i] = trimmedRune;
				}

			}
			return runesInformation;
		}

		internal static async Task<string?> GetAppRunePageId()
		{
			if (appRunePageId == null)
			{
				//Get all pages
				var pages = await ClientRequest.GetRunePages();

				//Get the page containing the name KALUS
				var kurwappRunes = pages.Where(page => page["name"] != null && page["name"]?.ToString().ToLower().Contains("kalus") == true);

				//Assign the page id if there is any
				if (kurwappRunes.Any()) appRunePageId = kurwappRunes.Select(page => page.Value<int>("id")).First().ToString();
			}

			return appRunePageId;
		}

		internal static async Task<string?> GetChampionName(int championId)
		{
			var champion = await GetChampionsInformations();
			return champion.Where(champion => champion.Value<int>("id") == championId).Select(champion => champion["name"]?.ToString()).First();
		}

		internal static async Task<byte[]> GetDefaultChampionIcon()
		{
			defaultChampionIcon ??= await ClientRequest.GetChampionImageById(-1);

			return defaultChampionIcon;
		}

		internal static async Task<byte[]> GetDefaultRuneIcon()
		{
			defaultRuneIcon ??= await ClientRequest.GetDefaultRuneImage();

			return defaultRuneIcon;
		}

		internal static async Task<byte[]> GetDefaultMapIcon()
		{
			defaultMapIcon ??= await ClientRequest.GetDefaultMapImage();

			return defaultMapIcon;
		}

		internal static async Task<byte[]> GetAramMapIcon(bool isInGame)
		{
			if (isInGame)
			{
				ingameAramIcon ??= await ClientRequest.GetAramMapImage(inGame: true);
				return ingameAramIcon;
			}

			champSelectAramIcon ??= await ClientRequest.GetAramMapImage(inGame: false);
			return champSelectAramIcon;
		}

		internal static async Task<byte[]> GetClassicMapIcon(bool isInGame)
		{
			if (isInGame)
			{
				ingameClassicIcon ??= await ClientRequest.GetClassicMapImage(inGame: true);
				return ingameClassicIcon;
			}

			champSelectClassicIcon ??= await ClientRequest.GetClassicMapImage(inGame: false);
			return champSelectClassicIcon;
		}

		//Reset every cached variable
		internal static void ResetCachedData()
		{
			championsInformation = null;
			championsRunesRecommendation = null;
			runesStyleInformation = null;
			runesInformation = null;

			appRunePageId = null;

			defaultChampionIcon = null;
			defaultRuneIcon = null;
			defaultMapIcon = null;

			ingameClassicIcon = null;
			champSelectClassicIcon = null;

			ingameAramIcon = null;
			champSelectAramIcon = null;
		}
	}
}