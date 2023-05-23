using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace KurwApp.Modules
{
	internal static class Client_Control
	{

		//Random variable used for getting random skins
		internal static Random random = new();

		//Ensure that the Authentication is set
		//Set it when client gets open or is open at startup
		//Reset it when it gets closed or is closed at startup
		internal static async void EnsureAuthentication(MainWindow mainWindow)
		{
			do
			{
				//Check if the authentication is set
				bool authenticated = Auth.IsAuthSet();

				//Check if the client is open
				bool isClientOpen = IsClientOpen();

				// When the client is closed
				if (!isClientOpen)
				{
					//Modify GroupBox style
					mainWindow.ShowLolState(false);
					//Reset cached data
					ClientDataCache.ResetCachedData();
					//If authenticated : reset the auth
					if (authenticated) Auth.ResetAuth();
					continue;
				}

				//When not authenticated
				if (!authenticated)
				{
					//If the client is open : set the authentication and player id
					if (isClientOpen)
					{
						Auth.SetBasicAuth(Process.GetProcessesByName("LeagueClientUx").First().MainModule.FileName);
						mainWindow.ShowLolState(true);
					}
				}
				Thread.Sleep(1000);
			} while (true);
		}

		//Returns if client is open
		internal static bool IsClientOpen()
		{
			//Checks if there is multiple UxRender to make sure the client is fully opened
			return Process.GetProcessesByName("LeagueClientUxRender").Length > 3;
		}

		internal static JToken GetPreference(string token)
		{
			var preferences = JObject.Parse(File.ReadAllText("Configurations/preferences.json"));

			var preference = preferences.SelectToken(token);

			return preference;
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
							Game? champSelect = await Game.CreateGame(mainWindow);
							if (champSelect == null) break;
							await champSelect.ChampSelectControl();
							break;
						case "GameStart":
						case "InProgress":
							string gameMode = mainWindow.GetGamemodeName();
							mainWindow.SetGameModeIcon(gameMode, true);
							mainWindow.EnableRandomSkinButton(false);
							break;
						//If not in any of the above game phase
						default:
							//Set the icon to default if it is not already
							if (!MainWindow.isStatusBoxDefault)
							{
								mainWindow.SetChampionIcon(await ClientDataCache.GetDefaultChampionIcon());

								mainWindow.SetDefaultIcons();
								mainWindow.SetDefaultLabels();

								MainWindow.isStatusBoxDefault = true;
							}
							break;
					}
				}
				Thread.Sleep(1000);
			}
		}

		//Returns a setting state by checking in the setting json
		internal static bool GetSettingState(string settingName)
		{
			var settings = JObject.Parse(File.ReadAllText("Configurations/settings.json"));

			return settings.Value<bool>(settingName);
		}


		#region Random Skin

		//Get the id of all available skins
		internal static async Task<int[]> GetAvailableSkinsID()
		{
			JArray currentChampionSkins = await Client_Request.GetCurrentChampionSkins();

			var availableSkinsId = currentChampionSkins.Where(skin => skin.Value<bool>("unlocked"))
														.Select(skin => skin.Value<int>("id"));
			if ((bool)GetPreference("randomSkin.addChromas"))
			{
				var availableChromasId = currentChampionSkins.SelectMany(skin => skin["childSkins"]
																		 .Where(childSkin => childSkin.Value<bool>("unlocked"))
																			.Select(childSkin => childSkin.Value<int>("id")));
				return availableSkinsId.Concat(availableChromasId).ToArray();
			};
			return availableSkinsId.ToArray();
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

		#endregion Random Skin

		//Returns if it's the player turn to act
		//If true output the id of the action and the type (e.g : pick or ban)
		internal static bool IsCurrentPlayerTurn(IEnumerable<JObject> actions, int cellId, out int actionId, out string type)
		{
			//Get the player actions that are in progress
			var currentPlayerAction = actions.Where(action => (int)action["actorCellId"] == cellId && (bool)action["isInProgress"] == true)
				.Select(action => action).ToArray();

			bool isCurrentPlayerTurn = currentPlayerAction.Any();

			actionId = isCurrentPlayerTurn ? currentPlayerAction.First().Value<int>("id") : 0;
			type = isCurrentPlayerTurn ? currentPlayerAction.First().Value<string>("type") : string.Empty;
			return isCurrentPlayerTurn;
		}

		//Retrieves the champion select ohase name
		internal static async Task<string> GetChampSelectPhase()
		{
			var session_timer = await Client_Request.GetSessionTimer();
			if (session_timer == "")
			{
				return "";
			}
			JObject champ_select_timer = JObject.Parse(session_timer);

			return champ_select_timer.Value<string>("phase").ToUpper();
		}

		internal static async Task<string> GetChampionDefaultPosition(int championId)
		{
			return (await ClientDataCache.GetChampionsRunesRecommendation())
																	.First(item => item.Value<int>("championId") == championId)
																	.Value<JArray>("runeRecommendations")
																	.First(recommendation => recommendation.Value<string>("position") != "NONE" && recommendation.Value<bool>("isDefaultPosition"))
																	.Value<string>("position");

		}

		internal static async Task<int[]> GetAllChampionForPosition(string position)
		{
			return (await ClientDataCache.GetChampionsRunesRecommendation())
																	.Where(item => item.Value<JArray>("runeRecommendations")
																		.Any(rune => rune.Value<string>("position") == position && rune.Value<bool>("isDefaultPosition")))
																	.Select(item => item.Value<int>("championId"))
																	.ToArray();
		}

		#region Runes

		//Get the recommended runes for a champion
		internal static async Task<JArray> GetRecommendedRunesById(int champId)
		{
			var runesRecommendation = await ClientDataCache.GetChampionsRunesRecommendation();

			JArray champRunes = runesRecommendation
				.Where(obj => obj.Value<int>("championId") == champId)
				.Select(obj => obj.Value<JArray>("runeRecommendations")).First();

			return champRunes;
		}

		//Get the recommended champion runes for a champion depending on its position
		internal static async Task<JToken> GetChampRunesByPosition(int champId, string position)
		{
			var runesRecommendation = await GetRecommendedRunesById(champId);

			var champRunesByPosition = runesRecommendation.Where(recommendation => recommendation.Value<string>("position") == position.ToUpper()).Select(recommendation => recommendation);
			if (!champRunesByPosition.Any())
			{
				champRunesByPosition = runesRecommendation.Where(recommendation => recommendation.Value<string>("position") == "NONE").Select(recommendation => recommendation);
			}
			return champRunesByPosition.FirstOrDefault();
		}

		//Get the recommended champion spells for a champion depending on its position and the game mode
		internal static async Task<JToken> GetSpellsRecommendationByPosition(int champId, string position)
		{
			var runesRecommendation = await GetRecommendedRunesById(champId);
			IEnumerable<JToken>? champRunesByPosition = null;

			if (position != "") champRunesByPosition = runesRecommendation.Where(recommendation => recommendation.Value<string>("position") == position);

			if (position == "" || !champRunesByPosition.Any()) champRunesByPosition = runesRecommendation.Where(recommendation => recommendation.Value<string>("position") != "NONE");

			return champRunesByPosition.First().Value<JToken>("summonerSpellIds");
		}

		//Format the champion runes for the rune request
		internal static string FormatChampRunes(JToken runes, string champion)
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


		//Get current page id
		internal static async Task<int> GetCurrentRunePageId()
		{
			//Get all pages
			var pages = await Client_Request.GetRunePages();

			//Get the page containing the name Kurwapp
			var kurwappRunes = pages.First(page => (bool)page["current"] == true);

			//Return the page id if there is a page else return 0
			return kurwappRunes.Any() ? int.Parse(kurwappRunes.ToString()) : 0;
		}

		//Check if we can create a new page
		internal static async Task<bool> CanCreateNewPage()
		{
			var inventory = await Client_Request.GetRunesInventory();
			return inventory.Value<bool>("canAddCustomPage");
		}

		//Set the recommended rune page
		internal static async Task SetRunesPage(int champId, string position = "NONE")
		{
			bool setActive = (bool)GetPreference("runes.notSetActive");

			var appPageId = await ClientDataCache.GetAppRunePageId();

			var champions = await ClientDataCache.GetChampionsInformations();

			string championName = champions.Where(champion => champion.Value<int>("id") == champId).Select(champion => champion.Value<string>("name")).First();
			//Get the recommended rune page
			var runesRecommendation = await GetChampRunesByPosition(champId, position);
			string recommendedRunes = FormatChampRunes(runesRecommendation, championName);

			if (appPageId != null)
			{
				await Client_Request.EditRunePage(appPageId, recommendedRunes);
			}
			else if (await CanCreateNewPage())
			{
				await Client_Request.CreateNewRunePage(recommendedRunes);
			}
			else if ((bool)GetPreference("runes.overridePage"))
			{
				await EditOldestRunePage(recommendedRunes);
			}
		}

		private static async Task EditOldestRunePage(string newRunesPage)
		{
			var runesPages = await Client_Request.GetRunePages();
			string oldestPageId = runesPages.OrderBy(page => page.Value<string>("lastModified")).First().Value<int>("id").ToString();

			await Client_Request.EditRunePage(oldestPageId, newRunesPage);
		}

		#endregion Runes

		internal static async void SetSummonerSpells(int[] recommendedSpells)
		{
			int flashPosition = (int)GetPreference("summoners.flashPosition");

			if (flashPosition != 2 && recommendedSpells.Contains(4))
			{
				if(Array.IndexOf(recommendedSpells, 4) != flashPosition) {
					(recommendedSpells[1], recommendedSpells[0]) = (recommendedSpells[0], recommendedSpells[1]);
				}
			}

			await Client_Request.ChangeSummonerSpells(recommendedSpells);
		}

		internal static async Task<Tuple<byte[], byte[]>?> GetRunesIcons()
		{
			var runesStyles = await ClientDataCache.GetRunesStyleInformation();

			var currentRunes = await Client_Request.GetActiveRunePage();

			if (currentRunes == null) return null;

			string primaryRuneId = currentRunes.Value<string>("primaryStyleId").ToString();
			string subRuneId = currentRunes.Value<string>("subStyleId").ToString();

			var primaryRunes = runesStyles.First(rune => rune.Value<string>("id") == primaryRuneId).Value<string>("iconPath");
			var subRunes = runesStyles.First(rune => rune.Value<string>("id") == subRuneId).Value<string>("iconPath");

			byte[] primaryRuneIcon = await RequestQueue.GetImage(primaryRunes);
			byte[] subRuneIcon = await RequestQueue.GetImage(subRunes);

			return Tuple.Create(primaryRuneIcon, subRuneIcon);
		}


	}
}