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

		/// <summary>
		/// Retrieves all the runes page recommendations for a single champion
		/// </summary>
		/// <param name="championId">The id of the champion</param>
		/// <returns>All the runes recommendation for a champion</returns>
		internal static async Task<JArray> GetAllRecommendedRunesForChampion(int championId)
		{
			JArray runesRecommendation = await DataCache.GetChampionsRunesRecommendation();

			JArray? championRunes = runesRecommendation
						.FirstOrDefault(obj => obj.Value<int>("championId") == championId)?["runeRecommendations"] as JArray;


			return championRunes ?? new JArray();
		}

		/// <summary>
		/// Converts a rune id to its name for UI purposes
		/// </summary>
		/// <param name="runeId">The id of the rune as a string</param>
		/// <returns>The rune name as a string</returns>
		private static async Task<string> RuneIdToName(string runeId)
		{
			var runesInformation = await DataCache.GetRunesInformation();

			return runesInformation.FirstOrDefault(runes => runes["id"]?.ToString() == runeId)?.SelectToken("name")?.ToString() ?? "Unknown Rune";
		}

		/// <summary>
		/// Converts a rune id to its descriptor for UI purposes
		/// </summary>
		/// <param name="runeId">The id of the rune as a string</param>
		/// <returns>The rune descriptor as a string</returns>
		private static async Task<string> RuneIdToDescriptor(string runeId)
		{
			var runesInformation = await DataCache.GetRunesInformation();

			return runesInformation.FirstOrDefault(runes => runes["id"]?.ToString() == runeId)?.SelectToken("recommendationDescriptor")?.ToString() ?? "Unknown Rune";
		}

		/// <summary>
		/// Converts a perk id to its name for UI purposes
		/// </summary>
		/// <param name="perkId">The id of the perk as a string</param>
		/// <returns>The perk name as a string</returns>
		private static async Task<string> PerkIdToName(string perkId)
		{
			JArray perksInformation = await DataCache.GetRunesStyleInformation();

			return perksInformation.FirstOrDefault(perk => perk["id"]?.ToString() == perkId)?.SelectToken("name")?.ToString() ?? "Unknown Perk";
		}

		/// <summary>
		/// Set the items for the combobox allowing user runes selection
		/// </summary>
		/// <param name="championId">The champion id</param>
		/// <param name="controlPanel">The control panel used inside the main window</param>
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

		/// <summary>
		/// Retrieves the recommended champion runes depending on a position
		/// </summary>
		/// <param name="championId">The champion Id</param>
		/// <param name="position">The position to look for in the recommendation</param>
		/// <returns>A JToken of the runes recommendation</returns>
		internal static async Task<JToken?> GetChampRunesByPosition(int championId, string position)
		{
			var runesRecommendation = await GetAllRecommendedRunesForChampion(championId);

			if (runesRecommendation == null)
				return null;

			var champRunesByPosition = runesRecommendation.Where(recommendation => recommendation?["position"]?.ToString() == position.ToUpper()).Select(recommendation => recommendation).ToArray();
			if (!champRunesByPosition.Any())
			{
				champRunesByPosition = runesRecommendation.Where(recommendation => recommendation?["position"]?.ToString() == "NONE").Select(recommendation => recommendation).ToArray();
			}
			return champRunesByPosition[0];
		}

		/// <summary>
		/// Retrieves the recommended runes for the current champion selected by the user
		/// </summary>
		/// <param name="championId">The champion ID</param>
		/// <param name="recommendationNumber">The index of the selected item (reflecting the index of the recommendation)</param>
		/// <returns>A JToken of the runes recommendation</returns>
		private static async Task<JToken?> GetChampRunesBySelectionIndex(int championId, int recommendationNumber)
		{
			return (await GetAllRecommendedRunesForChampion(championId))[recommendationNumber];
		}

		/// <summary>
		/// Format the runes recommendation into the content for the modify runes request
		/// </summary>
		/// <param name="runes">A Jtoken of the runes recommendation</param>
		/// <param name="championName">The name of the champion</param>
		/// <returns>A string of the formatted runes</returns>
		private static string FormatChampionRunes(JToken runes, string championName)
		{
			string position = runes["position"]?.ToString() ?? "NONE";

			if (position == "NONE") position = "ARAM";
			if (position == "UTILITY") position = "SUPPORT";

			//Create a template for the request body
			string runesTemplate = $"{{\"current\": true,\"name\": \"KALUS - {championName} - {position}\",\"primaryStyleId\": 0,\"subStyleId\": 0, \"selectedPerkIds\": []}}";
			JObject runesObject = JObject.Parse(runesTemplate);

			//Set the values from the recommended runes
			runesObject["primaryStyleId"] = runes["primaryPerkStyleId"];
			runesObject["subStyleId"] = runes["secondaryPerkStyleId"];
			runesObject["selectedPerkIds"] = runes["perkIds"];

			return runesObject.ToString();
		}

		/// <summary>
		/// Verifies if the there is an available slot to create a new runes page
		/// </summary>
		/// <returns>True if there is a page slot available, else false</returns>
		private static async Task<bool> CanCreateNewPage()
		{
			var inventory = await ClientRequest.GetRunesInventory();
			if (inventory == null)
				return false;
			return inventory.Value<bool>("canAddCustomPage");
		}

		/// <summary>
		/// Set the recommended runes page from a user selection
		/// </summary>
		/// <param name="championId">The current champion ID</param>
		/// <param name="position">The current player position</param>
		/// <param name="selectionIndex">The index of the selected runes recommendation</param>
		internal static async Task SetRunesPage(int championId, string position = "NONE", int selectionIndex = -1)
		{
			var appPageId = await DataCache.GetAppRunePageId();

			string? championName = await DataCache.GetChampionName(championId);

			if (championName == null)
				return;

			//Get the recommended runes page
			JToken? runesRecommendation = selectionIndex != -1 ? await GetChampRunesBySelectionIndex(championId, selectionIndex) : await GetChampRunesByPosition(championId, position);

			if (runesRecommendation == null)
				return;

			string formattedRunes = FormatChampionRunes(runesRecommendation, championName);

			await EditRunesPage(appPageId, formattedRunes);
		}

		/// <summary>
		/// Performs the change of the rune page either by modifying a page or creating a new one
		/// </summary>
		/// <param name="appPageId">The id of the page to modify</param>
		/// <param name="recommendedRunes">The content of the request for a runes page edit or creation</param>
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

		/// <summary>
		/// Find and edit the oldest runes page available
		/// </summary>
		/// <param name="recommendedRunes">The content of the request for a runes page edit</param>
		private static async Task EditOldestRunePage(string recommendedRunes)
		{
			var runesPages = await ClientRequest.GetRunePages();
			string? oldestPageId = runesPages.OrderBy(page => page["lastModified"]?.ToString()).First()["id"]?.ToString();

			if (oldestPageId == null)
				return;

			await ClientRequest.EditRunePage(oldestPageId, recommendedRunes);
		}

		/// <summary>
		/// Retrieves the icons for the runes of the rune page (primary rune and secondary perk)
		/// </summary>
		/// <returns>A tuple containing both icon as bytes array</returns>
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
