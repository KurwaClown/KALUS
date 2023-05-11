using System;
using System.Threading.Tasks;

namespace KurwApp
{
	internal class Client_Request
	{
		internal static async Task<string> GetClientPhase()
		{
			var response = await Http_Request.GetRequest("/lol-gameflow/v1/gameflow-phase");
			string content = await response.Content.ReadAsStringAsync();
			string client_phase = content.Substring(1, content.Length - 2);
			return client_phase;
		}

		#region Current Summoner
		internal static async Task<string> GetSummonerAndAccountId()
		{
			var response = await Http_Request.GetRequest("/lol-summoner/v1/current-summoner/account-and-summoner-ids");
			return await response.Content.ReadAsStringAsync();
		}

		internal static async Task<string> GetCurrentSummonerInfo()
		{
			var response = await Http_Request.GetRequest("/lol-summoner/v1/current-summoner");
			return await response.Content.ReadAsStringAsync();
		} 
		#endregion

		#region Ready Check
		internal static async Task Accept()
		{
			await Http_Request.PostRequest("/lol-matchmaking/v1/ready-check/accept");
		}
		internal static async Task Decline()
		{
			await Http_Request.PostRequest("/lol-matchmaking/v1/ready-check/decline");
		}
		#endregion

		#region Champion Selection

		internal static async Task<string> GetAvailableChampions()
		{
			var response = await Http_Request.GetRequest("/lol-champ-select/v1/pickable-champion-ids");
			return await response.Content.ReadAsStringAsync();
		}

		internal static async Task<string> GetSessionInfo()
		{
			var response = await Http_Request.GetRequest("/lol-champ-select/v1/session");
			return await response.Content.ReadAsStringAsync();
		}

		internal static async Task<string> GetSessionTimer()
		{

			var response = await Http_Request.GetRequest("/lol-champ-select/v1/session/timer");
			if (!response.IsSuccessStatusCode) return "";
			return await response.Content.ReadAsStringAsync();
		}
		internal static async Task<string> GetCurrentChampionSkins()
		{
			var response = await Http_Request.GetRequest("/lol-champ-select/v1/skin-carousel-skins");
			return await response.Content.ReadAsStringAsync();
		}

		internal static async Task<string> ChangeSkinByID(int id)
		{
			var response = await Http_Request.PatchRequest("/lol-champ-select/v1/session/my-selection", $"{{\"selectedSkinId\": {id}}}");
			return response;
		}

		internal static async Task<string> SelectChampion(int actionId, int champId)
		{
			var response = await Http_Request.PatchRequest($"/lol-champ-select/v1/session/actions/{actionId}", $"{{\"championId\": {champId}}}");
			return response;
		}

		internal static async Task<string> ConfirmAction(int actionId)
		{
			var response = await Http_Request.PostRequest($"/lol-champ-select/v1/session/actions/{actionId}/complete");
			return response;
		}

		internal static async Task<string> AramBenchSwap(int championId)
		{
			var response = await Http_Request.PostRequest($"/lol-champ-select/v1/session/bench/swap/{championId}");
			return response;
		}

		internal static async Task<int> GetCurrentChampionId()
			{
				var response = await Http_Request.GetRequest("/lol-champ-select/v1/current-champion");
				return Int32.Parse(await response.Content.ReadAsStringAsync());
			}
		#endregion

		#region Runes
		internal static async Task<string> GetRecommendedRunes()
		{
			var response = await Http_Request.GetRequest($"https://raw.communitydragon.org/latest/plugins/rcp-be-lol-game-data/global/default/v1/champion-rune-recommendations.json");
			return await response.Content.ReadAsStringAsync();
		}

		internal static async Task<string> GetRunePages()
		{
			var response = await Http_Request.GetRequest("/lol-perks/v1/pages");
			return await response.Content.ReadAsStringAsync();
		}
		
		internal static async Task<string> GetRunesInventory()
		{
			var response = await Http_Request.GetRequest("/lol-perks/v1/inventory");
			return await response.Content.ReadAsStringAsync();
		}

		internal static async Task<string> CreateNewRunePage(string newRunesPage)
		{
			var response = await Http_Request.PostRequest("/lol-perks/v1/pages", newRunesPage);
			return response;
		}
		
		internal static async Task<string> EditRunePage(int runesPageId, string newRunesPage)
		{
			var response = await Http_Request.PutRequest($"/lol-perks/v1/pages/{runesPageId}", newRunesPage);
			return response;
		}
		#endregion

		internal static async Task<string> GetLobbyInfo()
		{
			var response = await Http_Request.GetRequest($"/lol-lobby/v2/lobby");
			return await response.Content.ReadAsStringAsync();
		}

		internal static async Task<string> GetChampionsInfo()
		{
			var response = await Http_Request.GetRequest($"https://raw.communitydragon.org/latest/plugins/rcp-be-lol-game-data/global/default/v1/champion-summary.json");
			return await response.Content.ReadAsStringAsync();
		}


		internal static async Task<byte[]> GetChampionImageById(int charId)
		{
			return await Http_Request.GetRequestImage($"https://raw.communitydragon.org/latest/plugins/rcp-be-lol-game-data/global/default/v1/champion-icons/{charId}.png");
		}

		internal static async Task RestartLCU()
		{
			await Http_Request.PostRequest("/riotclient/kill-and-restart-ux");
		}
	}
}
