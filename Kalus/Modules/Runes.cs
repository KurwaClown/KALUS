using Kalus.Modules.Networking;
using Kalus.UI.Controls;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace Kalus.Modules
{
	internal static class Runes
	{

		private static async Task<JArray> GetAllRecommendedRunesForChampion(int championId)
		{
			JArray runesRecommendation = await DataCache.GetChampionsRunesRecommendation();

			JArray? championRunes = runesRecommendation
						.FirstOrDefault(obj => obj.Value<int>("championId") == championId)?["runeRecommendations"] as JArray;


			return championRunes ?? new JArray();
		}

		private static async Task<string> RuneIdToName(string runeId)
		{
			var runesInformation = await DataCache.GetRunesInformation();

			return runesInformation.FirstOrDefault(runes => runes["id"]?.ToString() == runeId)?.SelectToken("name")?.ToString() ?? "Unknown Rune";
		}

		private static async Task<string> RuneIdToDescriptor(string runeId)
		{
			var runesInformation = await DataCache.GetRunesInformation();

			return runesInformation.FirstOrDefault(runes => runes["id"]?.ToString() == runeId)?.SelectToken("recommendationDescriptor")?.ToString() ?? "Unknown Rune";
		}

		private static async Task<string> PerkIdToName(string perkId)
		{
			JArray perksInformation = await DataCache.GetRunesStyleInformation();

			return perksInformation.FirstOrDefault(perk => perk["id"]?.ToString() == perkId)?.SelectToken("name")?.ToString() ?? "Unknown Perk";
		}

		internal async static void SetControlPanelRunesSelection(int championId, ControlPanel controlPanel)
		{
			var championRunes = await GetAllRecommendedRunesForChampion(championId);

			foreach(var runes in championRunes)
			{
				string? mainRuneId = runes?["perkIds"]?.First?.ToString();
				string? subStyleId = runes?["secondaryPerkStyleId"]?.ToString();

				if(mainRuneId == null || subStyleId == null) continue;

				string mainRuneDescription = await RuneIdToDescriptor(mainRuneId);
				string mainRuneName = await RuneIdToName(mainRuneId);

				string subStyleName = await PerkIdToName(subStyleId);
				string position = runes?["position"]?.ToString()!;
				if (position == "NONE")
					position = "ARAM";
				App.Current.Dispatcher.Invoke(() =>
				{
					controlPanel.runesSelection.Items.Add(new ComboBoxItem
					{
						Content = $"{position} : {mainRuneDescription}",
						ToolTip = $"{mainRuneName} | {subStyleName}" 
					});
				});


			}
		}

		//Get the recommended runes for a champion
		internal static async Task<JArray?> GetRecommendedRunesById(int championId)
		{
			JArray championsRunes = await GetAllRecommendedRunesForChampion(championId);

			return championsRunes.First() as JArray;
		}

		//Get the recommended champion runes for a champion depending on its position
		internal static async Task<JToken?> GetChampRunesByPosition(int champId, string position)
		{
			var runesRecommendation = await GetAllRecommendedRunesForChampion(champId);

			if (runesRecommendation == null)
				return null;

			var champRunesByPosition = runesRecommendation.Where(recommendation => recommendation?["position"]?.ToString() == position.ToUpper()).Select(recommendation => recommendation).ToArray();
			if (!champRunesByPosition.Any())
			{
				champRunesByPosition = runesRecommendation.Where(recommendation => recommendation?["position"]?.ToString() == "NONE").Select(recommendation => recommendation).ToArray();
			}
			return champRunesByPosition[0];
		}

		internal static async Task<JToken?> GetChampRunesBySelectionIndex(int championId, int recommendationNumber)
		{
			return (await GetAllRecommendedRunesForChampion(championId))[recommendationNumber];
		}

		//Format the champion runes for the rune request
		internal static string FormatChampRunes(JToken runes, string champion, string position)
		{
			//Create a template for the request body
			string runesTemplate = $"{{\"current\": true,\"name\": \"KALUS - {champion} - {position}\",\"primaryStyleId\": 0,\"subStyleId\": 0, \"selectedPerkIds\": []}}";
			JObject runesObject = JObject.Parse(runesTemplate);

			//Set the values from the recommended runes
			runesObject["primaryStyleId"] = runes["primaryPerkStyleId"];
			runesObject["subStyleId"] = runes["secondaryPerkStyleId"];
			runesObject["selectedPerkIds"] = runes["perkIds"];

			return runesObject.ToString();
		}

		//Check if we can create a new page
		internal static async Task<bool> CanCreateNewPage()
		{
			var inventory = await ClientRequest.GetRunesInventory();
			if (inventory == null)
				return false;
			return inventory.Value<bool>("canAddCustomPage");
		}

		//Set the recommended rune page
		internal static async Task SetRunesPage(int championId, string position = "NONE", int recommendationNumber = -1)
		{
			var appPageId = await DataCache.GetAppRunePageId();

			string? championName = await DataCache.GetChampionName(championId);

			if (championName == null)
				return;

			//Get the recommended runes page
			JToken? runesRecommendation = recommendationNumber != -1 ? await GetChampRunesBySelectionIndex(championId, recommendationNumber) : await GetChampRunesByPosition(championId, position);

			if (runesRecommendation == null)
				return;

			string formattedRunes = FormatChampRunes(runesRecommendation, championName, position);

			await EditRunesPage(appPageId, formattedRunes);
		}

		private static async Task EditRunesPage(string? appPageId, string recommendedRunes)
		{
			if (appPageId != null)
			{
				await ClientRequest.EditRunePage(appPageId, recommendedRunes);
			}
			else if (await CanCreateNewPage())
			{
				await ClientRequest.CreateNewRunePage(recommendedRunes);
			}
			else if (Properties.Settings.Default.runesOverrideOldestPage)
			{
				await EditOldestRunePage(recommendedRunes);
			}
		}

		private static async Task EditOldestRunePage(string newRunesPage)
		{
			var runesPages = await ClientRequest.GetRunePages();
			string? oldestPageId = runesPages.OrderBy(page => page["lastModified"]?.ToString()).First()["id"]?.ToString();

			if (oldestPageId == null)
				return;

			await ClientRequest.EditRunePage(oldestPageId, newRunesPage);
		}

		internal static async Task<Tuple<byte[], byte[]>?> GetRunesIcons()
		{
			var runes = await DataCache.GetRunesInformation();
			var runesStyles = await DataCache.GetRunesStyleInformation();

			var currentRunes = await ClientRequest.GetActiveRunePage();

			if (currentRunes == null || runes == null || runesStyles == null)
				return null;

			string? primaryRuneId = currentRunes["selectedPerkIds"]?.First().ToString();
			string? subRuneId = currentRunes["subStyleId"]?.ToString();

			if (primaryRuneId == null || subRuneId == null)
				return null;

			var primaryRunes = runes.FirstOrDefault(rune => rune["id"]?.ToString() == primaryRuneId)?.SelectToken("iconPath")?.ToString();
			var subRunes = runesStyles.FirstOrDefault(rune => rune["id"]?.ToString() == subRuneId)?.SelectToken("iconPath")?.ToString();

			if (primaryRunes == null || subRunes == null)
				return null;

			byte[] primaryRuneIcon = await RequestQueue.GetImage(primaryRunes);
			byte[] subRuneIcon = await RequestQueue.GetImage(subRunes);

			return Tuple.Create(primaryRuneIcon, subRuneIcon);
		}
	}
}
