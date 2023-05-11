﻿using System.Threading;
using System.Diagnostics;
using System.Linq;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Controls;

namespace KurwApp
{
	internal static class Client_Control
	{
		internal static string summonerId = string.Empty;

		//Random variable used for getting random skins
		internal static Random random = new();


		//Ensure that the Authentication is set
		//Set it when client gets open or is open at startup
		//Reset it when it gets closed or is closed at startup
		internal static void EnsureAuthentication(MainWindow mainWindow)
		{
			do
			{
				Thread.Sleep(1000);
				//Check if the authentication is set
				bool authenticated = Auth.IsAuthSet();

				//Check if the client is open
				bool isClientOpen = IsClientOpen();

				// When the client is closed
				if (!isClientOpen)
				{
					//Modify GroupBox style
					mainWindow.ShowLolState(false);
					//Reset player id
					SetSummonerId(isReset: true);
					//If authenticated : reset the auth
					if(authenticated) Auth.ResetAuth();
					continue;
				}

				//When not authenticated
				if (!authenticated)
				{
					//If the client is open : set the authentication and player id
					if (isClientOpen)
					{
						Auth.SetBasicAuth(Process.GetProcessesByName("LeagueClientUx").First().MainModule.FileName);
						SetSummonerId();
						mainWindow.ShowLolState(true);
					}
				}
			} while (true);
		}

		//Returns if client is open
		internal static bool IsClientOpen()
		{
			//Checks if there is multiple UxRender to make sure the client is fully opened
			return Process.GetProcessesByName("LeagueClientUxRender").Length > 3;
		}

		//Checks client phase every 5 seconds
		//Is used as a worker thread for the app thread
		internal static async void ClientPhase(MainWindow mainWindow)
		{
			
			while (true)
			{
				//Only act if the authentication is set
				if (Auth.IsAuthSet())
				{
					//Checks the game phase and perform action depending on it
					switch (await Client_Request.GetClientPhase())
					{
						//On ready check
						case "ReadyCheck":
							//If the setting to get automatically ready is on : accept the game
							if (GetSettingState("autoReady")) await Client_Request.Accept();
							break;
						//On champion selection : start and await the end of the champ select handler
						case "ChampSelect":
							await Task.Run(() => new Game(mainWindow).ChampSelectControl());
							break;
						//If not in any of the above game phase
						default:
							//Set the icon to default if it is not already
							if (!mainWindow.isIconDefault) mainWindow.SetDefaultIcon();
							break;
					}
				}
				Thread.Sleep(5000);
			}
		}

		//Returns a setting state by checking in the setting json
		internal static bool GetSettingState(string settingName)
		{
			var settings = JObject.Parse(File.ReadAllText("Configurations/settings.json"));

			return (bool)settings[settingName];
		}

		//Set or reset the player id
		internal static async void SetSummonerId(bool isReset = false)
		{

			if (isReset)
			{
				summonerId = string.Empty;
				return;
			}

			//Getting the current player info
			var summonerInfo = await Client_Request.GetSummonerAndAccountId();
			if (summonerInfo == "" || summonerInfo is null) return;

			//Set player id if the token is present
			if(summonerInfo.Contains("summonerId"))summonerId = JObject.Parse(summonerInfo)["summonerId"].ToString();
		}

		#region Random Skin
		//Get the id of all available skins
		internal static async Task<int[]> GetAvailableSkinsID()
		{
			JArray currentChampionSkins = JArray.Parse(await Client_Request.GetCurrentChampionSkins());

			//Select all current champion unlocked skins
			return currentChampionSkins.Where(j => (bool)j["unlocked"]).Select(j => (int)j["id"]).ToArray();
		}

		//Pick a random skin
		internal static async void PickRandomSkin()
		{
			//Get all available skins
			int[] skin_ids = await GetAvailableSkinsID();

			//If there is any skins available : select and change the next skin randomly
			if (skin_ids.Any())
			{
				int random_skin_index = random.Next(skin_ids.Length);
				await Client_Request.ChangeSkinByID(skin_ids[random_skin_index]);
			}
		} 
		#endregion

		//Returns the champion select phase name
		internal static async Task<string> GetChampSelectPhase()
		{
			JObject champ_select_timer = JObject.Parse(await Client_Request.GetSessionTimer());
			return champ_select_timer["phase"].ToString();
		}

		//Returns the player cellId, its position in the lobby
		internal static async Task<int> GetCellId()
		{
			JObject sessionInfo = JObject.Parse(await Client_Request.GetSessionInfo());
			return (int)sessionInfo["localPlayerCellId"];
		}

		//Returns if it's the player turn to act
		//If true output the id of the action and the type (e.g : pick or ban)
		internal static bool IsCurrentPlayerTurn(IEnumerable<JObject> actions, int cellId, out int actionId, out string type)
		{
			//Get the player actions that are in progress
			var currentPlayerAction = actions.Where(action => (int)action["actorCellId"] == cellId && (bool)action["isInProgress"] == true)
				.Select(action => action).ToArray();

			
			bool isCurrentPlayerTurn = currentPlayerAction.Any();

			actionId = isCurrentPlayerTurn ? (int)currentPlayerAction.First()["id"] : 0;
			type = isCurrentPlayerTurn ? currentPlayerAction.First()["type"].ToString() : string.Empty;
			return isCurrentPlayerTurn;
		}

		//Get the actions of the champion select
		internal static async Task<IEnumerable<JObject>> GetSessionActions(string sessionInfo)
		{
			return JObject.Parse(sessionInfo)["actions"].SelectMany(innerArray => innerArray).OfType<JObject>();
		}

		#region Runes
		//Get the recommended runes for a champion
		internal static async Task<JArray> GetRecommendedRunesById(int champId)
		{
			var runesRecommendation = JArray.Parse(await Client_Request.GetRecommendedRunes());

			JArray champRunes = runesRecommendation
				.Where(obj => (int)obj["championId"] == champId)
				.Select(obj => (JArray)obj["runeRecommendations"]).First();

			return champRunes;
		}

		//Get the recommended champion runes for a champion depending on its position
		internal static async Task<JToken> GetChampRunesByPosition(int champId, string position)
		{
			var runesRecommendation = await GetRecommendedRunesById(champId);

			var champRunesByPosition = runesRecommendation.Where(recommendation => (string)recommendation["position"] == position.ToUpper()).Select(recommendation => recommendation);
			if (!champRunesByPosition.Any())
			{
				champRunesByPosition = runesRecommendation.Where(recommendation => recommendation["position"].ToString() == "NONE").Select(recommendation => recommendation);
			}
			return champRunesByPosition.FirstOrDefault();
		}

		//Format the champion runes for the rune request
		internal static async Task<string> FormatChampRunes(JToken runes, string champion)
		{
			//Create a template for the request body
			string runesTemplate = $"{{\"current\": true,\"name\": \"Kurwapp - {champion}\",\"primaryStyleId\": 0,\"subStyleId\": 0, \"selectedPerkIds\": []}}";
			JObject runesObject = JObject.Parse(runesTemplate);

			//Set the values from the recommended runes
			runesObject["primaryStyleId"] = runes["primaryPerkStyleId"];
			runesObject["subStyleId"] = runes["secondaryPerkStyleId"];
			runesObject["selectedPerkIds"] = runes["perkIds"];

			return runesObject.ToString();
		}

		//Get the app rune page id if it's set
		internal static async Task<int> GetAppRunePageId()
		{
			//Get all pages
			var pages = await Client_Request.GetRunePages();

			//Get the page containing the name Kurwapp
			var kurwappRunes = JArray.Parse(pages).Where(page => page["name"].ToString().ToLower().Contains("kurwapp"));

			//Return the page id if there is a page else return 0
			return kurwappRunes.Any() ? kurwappRunes.Select(page => (int)page["id"]).First() : 0;
		}

		//Check if we can create a new page
		internal static async Task<bool> CanCreateNewPage()
		{
			var inventory = JObject.Parse(await Client_Request.GetRunesInventory());
			return (bool)inventory["canAddCustomPage"];
		}

		//Set the recommended rune page
		internal static async void SetRunesPage(int champId, string position = "NONE")
		{

			var appPageId = await GetAppRunePageId();

			var champions = JArray.Parse(await Client_Request.GetChampionsInfo());

			string championName = champions.Where(champion => (int)champion["id"] == champId).Select(champion => champion["name"].ToString()).First();
			//Get the recommended rune page
			string recommendedRunes = await FormatChampRunes(await GetChampRunesByPosition(champId, position), championName);

			
			if (appPageId != 0)
			{
				await Client_Request.EditRunePage(appPageId, recommendedRunes);
			}
			else if (await CanCreateNewPage())
			{
				await Client_Request.CreateNewRunePage(recommendedRunes);
			}
		} 
		#endregion
	}
}
