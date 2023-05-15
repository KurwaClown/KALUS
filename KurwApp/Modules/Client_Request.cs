﻿using KurwApp.Modules;
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
		internal static async Task<string> GetSummonerAndAccountId()
		{
			var response = await RequestQueue.Request(HttpMethod.Get, "/lol-summoner/v1/current-summoner/account-and-summoner-ids");
			return response;
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

		internal static async Task<string> GetAvailableChampions()
		{
			var response = await RequestQueue.Request(HttpMethod.Get, "/lol-champ-select/v1/pickable-champion-ids");
			return response;
		}

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
		internal static async Task<string> GetCurrentChampionSkins()
		{
			var response = await RequestQueue.Request(HttpMethod.Get, "/lol-champ-select/v1/skin-carousel-skins");
			return response;
		}

		internal static async Task<string> ChangeSkinByID(int id)
		{
			var response = await RequestQueue.Request(HttpMethod.Patch, "/lol-champ-select/v1/session/my-selection", $"{{\"selectedSkinId\": {id}}}");
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
		internal static async Task<string> GetRecommendedRunes()
		{
			var response = await RequestQueue.Request(HttpMethod.Get, $"https://raw.communitydragon.org/latest/plugins/rcp-be-lol-game-data/global/default/v1/champion-rune-recommendations.json");
			return response;
		}

		internal static async Task<string> GetRunePages()
		{
			var response = await RequestQueue.Request(HttpMethod.Get, "/lol-perks/v1/pages");
			return response;
		}
		
		internal static async Task<string> GetRunesInventory()
		{
			var response = await RequestQueue.Request(HttpMethod.Get, "/lol-perks/v1/inventory");
			return response;
		}

		internal static async Task<string> CreateNewRunePage(string newRunesPage)
		{
			var response = await RequestQueue.Request(HttpMethod.Post, "/lol-perks/v1/pages", newRunesPage);
			return response;
		}
		
		internal static async Task<string> EditRunePage(int runesPageId, string newRunesPage)
		{
			var response = await RequestQueue.Request(HttpMethod.Put, $"/lol-perks/v1/pages/{runesPageId}", newRunesPage);
			return response;
		}
		#endregion

		internal static async Task<string> GetLobbyInfo()
		{
			var response = await RequestQueue.Request(HttpMethod.Get, $"/lol-lobby/v2/lobby");
			return response;
		}

		internal static async Task<string> GetChampionsInfo()
		{
			var response = await RequestQueue.Request(HttpMethod.Get, $"https://raw.communitydragon.org/latest/plugins/rcp-be-lol-game-data/global/default/v1/champion-summary.json");
			return response;
		}


		internal static async Task<byte[]> GetChampionImageById(int charId)
		{
			return await RequestQueue.GetImage($"https://raw.communitydragon.org/latest/plugins/rcp-be-lol-game-data/global/default/v1/champion-icons/{charId}.png");
		}

		internal static async Task RestartLCU()
		{
			await RequestQueue.Request(HttpMethod.Post, "/riotclient/kill-and-restart-ux");
		}
	}
}
