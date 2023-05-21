using KurwApp.Modules;
using Newtonsoft.Json.Linq;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace KurwApp
{
	internal class Client_Request
	{
		internal static async Task<string> GetClientPhase()
		{
			var response = await RequestQueue.Request(HttpMethod.Get, "/lol-gameflow/v1/gameflow-phase");

			string client_phase = response.Substring(1, response.Length - 2);

			return client_phase;
		}

		#region Current Summoner
		internal static async Task<JObject> GetSummonerAndAccountId()
		{
			var response = await RequestQueue.Request(HttpMethod.Get, "/lol-summoner/v1/current-summoner/account-and-summoner-ids");
			return JObject.Parse(response);
		}
		#endregion

		#region Ready Check
		internal static async Task Accept()
		{
			await RequestQueue.Request(HttpMethod.Post, "/lol-matchmaking/v1/ready-check/accept");
		}
		internal static async Task Decline()
		{
			await RequestQueue.Request(HttpMethod.Post, "/lol-matchmaking/v1/ready-check/decline");
		}
		#endregion

		#region Champion Selection


		internal static async Task<JObject?> GetSessionInfo()
		{
			var response = await RequestQueue.Request(HttpMethod.Get, "/lol-champ-select/v1/session");
			if (response == "") return null;
			return JObject.Parse(response);
		}

		internal static async Task<string> GetSessionTimer()
		{

			var response = await RequestQueue.Request(HttpMethod.Get, "/lol-champ-select/v1/session/timer");
			return response;
		}
		internal static async Task<JArray> GetCurrentChampionSkins()
		{
			var response = await RequestQueue.Request(HttpMethod.Get, "/lol-champ-select/v1/skin-carousel-skins");
			var currentChampionSkins = JArray.Parse(response);
			return currentChampionSkins;
		}

		internal static async Task<string> ChangeSkinByID(int id)
		{
			var response = await RequestQueue.Request(HttpMethod.Patch, "/lol-champ-select/v1/session/my-selection", $"{{\"selectedSkinId\": {id}}}");
			return response;
		}

		internal static async Task<string> ChangeSummonerSpells(int[] recommendedSpells)
		{
			var response = await RequestQueue.Request(HttpMethod.Patch, "/lol-champ-select/v1/session/my-selection", $"{{\"spell1Id\": {recommendedSpells[0]}, \"spell2Id\" : {recommendedSpells[1]}}}");
			return response;
		}

		internal static async Task<string> SelectChampion(int actionId, int champId)
		{
			var response = await RequestQueue.Request(HttpMethod.Patch, $"/lol-champ-select/v1/session/actions/{actionId}", $"{{\"championId\": {champId}}}");
			return response;
		}

		internal static async Task<string> ConfirmAction(int actionId)
		{
			var response = await RequestQueue.Request(HttpMethod.Post, $"/lol-champ-select/v1/session/actions/{actionId}/complete");
			return response;
		}

		internal static async Task<string> AramBenchSwap(int championId)
		{
			var response = await RequestQueue.Request(HttpMethod.Post, $"/lol-champ-select/v1/session/bench/swap/{championId}");
			return response;
		}

		internal static async Task<int> GetCurrentChampionId()
			{
				var response = await RequestQueue.Request(HttpMethod.Get, "/lol-champ-select/v1/current-champion");
				if (int.TryParse(response, out int id))return id;
				return 0;
			}
		#endregion

		#region Runes
		internal static async Task<JArray> GetRecommendedRunes()
		{
			var response = await RequestQueue.Request(HttpMethod.Get, $"/lol-game-data/assets/v1/champion-rune-recommendations.json");
			var recommendedRunes = JArray.Parse(response);
			return recommendedRunes;
		}

		internal static async Task<JObject?> GetActiveRunePage()
		{
			var response = await RequestQueue.Request(HttpMethod.Get, "/lol-perks/v1/currentpage");
			if(response == "") return null;
			var runePage = JObject.Parse(response);
			return runePage;
		}

		internal static async Task<string> SetActiveRunePage(string runesPageId)
		{
			var response = await RequestQueue.Request(HttpMethod.Put, "/lol-perks/v1/currentpage", runesPageId.ToString());
			return response;
		}

		internal static async Task<JArray> GetRunePages()
		{
			var response = await RequestQueue.Request(HttpMethod.Get, "/lol-perks/v1/pages");
			var runesPages = JArray.Parse(response);
			return runesPages;
		}

		internal static async Task<JObject> GetRunesInventory()
		{
			var response = await RequestQueue.Request(HttpMethod.Get, "/lol-perks/v1/inventory");
			var runesInventory = JObject.Parse(response);
			return runesInventory;
		}

		internal static async Task<string> CreateNewRunePage(string newRunesPage)
		{
			var response = await RequestQueue.Request(HttpMethod.Post, "/lol-perks/v1/pages", newRunesPage);
			return response;
		}

		internal static async Task<string> EditRunePage(string runesPageId, string newRunesPage)
		{
			var response = await RequestQueue.Request(HttpMethod.Put, $"/lol-perks/v1/pages/{runesPageId}", newRunesPage);
			return response;
		}
		#endregion

		internal static async Task<JObject?> GetLobbyInfo()
		{
			var response = await RequestQueue.Request(HttpMethod.Get, $"/lol-lobby/v2/lobby");
			if (response == "") return null;
			return JObject.Parse(response);
		}

		internal static async Task<JArray> GetChampionsInfo()
		{
			string request = Auth.IsAuthSet() ? "/lol-game-data/assets/v1/champion-summary.json" : "https://raw.communitydragon.org/latest/plugins/rcp-be-lol-game-data/global/default/v1/champion-summary.json";
			var response = await RequestQueue.Request(HttpMethod.Get, request);
			var champions = JArray.Parse(response);
			return champions;
		}

		internal static async Task<byte[]> GetChampionImageById(int charId)
		{
			string request = Auth.IsAuthSet() ? $"/lol-game-data/assets/v1/champion-icons/{charId}.png" : $"https://raw.communitydragon.org/latest/plugins/rcp-be-lol-game-data/global/default/v1/champion-icons/{charId}.png";
			return await RequestQueue.GetImage(request);
		}

		internal static async Task<byte[]> GetDefaultRuneImage()
		{
			string request = Auth.IsAuthSet() ? $"/lol-game-data/assets/v1/perk-images/styles/runesicon.png" : "https://raw.communitydragon.org/latest/plugins/rcp-be-lol-game-data/global/default/v1/perk-images/styles/runesicon.png";
			return await RequestQueue.GetImage(request);
		}

		internal static async Task<byte[]> GetDefaultMapImage()
		{
			string request = Auth.IsAuthSet() ? $"/lol-game-data/assets/content/src/leagueclient/gamemodeassets/classic_sru/img/game-select-icon-disabled.png" : "https://raw.communitydragon.org/latest/plugins/rcp-be-lol-game-data/global/default/content/src/leagueclient/gamemodeassets/classic_sru/img/game-select-icon-disabled.png";
			return await RequestQueue.GetImage(request);
		}

		internal static async Task<byte[]> GetAramMapImage(bool inGame)
		{
			string endpoint = "/lol-game-data/assets/content/src/leagueclient/gamemodeassets/aram/img/";
			endpoint += inGame ? "icon-victory.png" : "icon-hover.png";
			return await RequestQueue.GetImage(endpoint);
		}

		internal static async Task<byte[]> GetClassicMapImage(bool inGame)
		{
			string endpoint = "/lol-game-data/assets/content/src/leagueclient/gamemodeassets/classic_sru/img/";
			endpoint += inGame ? "icon-victory.png" : "icon-hover.png";
			return await RequestQueue.GetImage(endpoint);
		}



		internal static async Task<JArray> GetRunesStyles()
		{
			var request = Auth.IsAuthSet() ? $"/lol-game-data/assets/v1/perkstyles.json" : "https://raw.communitydragon.org/latest/plugins/rcp-be-lol-game-data/global/default/v1/perkstyles.json";
			var response = await RequestQueue.Request(HttpMethod.Get, request);
			var perksStyles = JObject.Parse(response);
			return JArray.Parse(perksStyles["styles"].ToString());
		}


		internal static async Task RestartLCU()
		{
			await RequestQueue.Request(HttpMethod.Post, "/riotclient/kill-and-restart-ux");
		}


	}
}
