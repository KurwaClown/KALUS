using Kalus.Modules.Networking;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kalus.Modules
{
	internal static class Runes
	{

		//Get the recommended runes for a champion
		internal static async Task<JArray?> GetRecommendedRunesById(int champId)
		{
			var runesRecommendation = await DataCache.GetChampionsRunesRecommendation();

			JArray? champRunes = runesRecommendation
						.Where(obj => obj.Value<int>("championId") == champId)
						.Select(obj => obj["runeRecommendations"] as JArray)
						.FirstOrDefault();


			return champRunes;
		}

		//Get the recommended champion runes for a champion depending on its position
		internal static async Task<JToken?> GetChampRunesByPosition(int champId, string position, int recommendationNumber)
		{
			var runesRecommendation = await GetRecommendedRunesById(champId);

			if (runesRecommendation == null)
				return null;

			var champRunesByPosition = runesRecommendation.Where(recommendation => recommendation["position"]?.ToString() == position.ToUpper()).Select(recommendation => recommendation).ToArray();
			if (!champRunesByPosition.Any())
			{
				champRunesByPosition = runesRecommendation.Where(recommendation => recommendation["position"]?.ToString() == "NONE").Select(recommendation => recommendation).ToArray();
			}
			if (recommendationNumber < champRunesByPosition.Length)
				return champRunesByPosition[recommendationNumber];
			return champRunesByPosition[0];
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

		//Get current page id
		internal static async Task<int> GetCurrentRunePageId()
		{
			//Get all pages
			var pages = await ClientRequest.GetRunePages();

			if (!pages.Any())
				return 0;
			//Get the page containing the name KALUS
			var kurwappRunes = pages.First(page => page.Value<bool>("current") == true);

			//Return the page id if there is a page else return 0
			return kurwappRunes.Any() ? int.Parse(kurwappRunes.ToString()) : 0;
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
		internal static async Task SetRunesPage(int champId, string position = "NONE", int recommendationNumber = 0)
		{
			var appPageId = await DataCache.GetAppRunePageId();

			var champions = await DataCache.GetChampionsInformations();

			string? championName = champions.Where(champion => champion.Value<int>("id") == champId).Select(champion => champion["name"]?.ToString()).First();

			if (championName == null)
				return;

			//Get the recommended rune page
			var runesRecommendation = await GetChampRunesByPosition(champId, position, recommendationNumber);
			if (runesRecommendation == null)
				return;

			string recommendedRunes = FormatChampRunes(runesRecommendation, championName, position);

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
			var runesStyles = await DataCache.GetRunesStyleInformation();

			if (runesStyles == null)
				return null;

			var currentRunes = await ClientRequest.GetActiveRunePage();

			if (currentRunes == null)
				return null;

			string? primaryRuneId = currentRunes["primaryStyleId"]?.ToString();
			string? subRuneId = currentRunes["subStyleId"]?.ToString();

			if (primaryRuneId == null || subRuneId == null)
				return null;

			var primaryRunes = runesStyles.FirstOrDefault(rune => rune["id"]?.ToString() == primaryRuneId)?.SelectToken("iconPath")?.ToString();
			var subRunes = runesStyles.FirstOrDefault(rune => rune["id"]?.ToString() == subRuneId)?.SelectToken("iconPath")?.ToString();

			if (primaryRunes == null || subRunes == null)
				return null;

			byte[] primaryRuneIcon = await RequestQueue.GetImage(primaryRunes);
			byte[] subRuneIcon = await RequestQueue.GetImage(subRunes);

			return Tuple.Create(primaryRuneIcon, subRuneIcon);
		}
	}
}
