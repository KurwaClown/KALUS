using Kalus.Modules.Games;
using Kalus.Modules.Networking;
using Kalus.UI.Controls.Tabs.Console;
using Kalus.UI.Windows;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace Kalus.Modules
{
	internal static class ClientControl
	{
		//Random variable used for getting random skins
		private static readonly Random random = new();
		internal static string gamePhase = "";

		internal static ClientState state = ClientState.NOCLIENT;
		//Ensure that the Authentication is set
		//Set it when client gets open or is open at startup
		//Reset it when it gets closed or is closed at startup
		internal static void EnsureAuthentication(MainWindow mainWindow)
		{
			do
			{
				//Check if the authentication is set
				bool authenticated = Auth.IsAuthSet();

				//Check if the client is open
				bool isClientOpen = IsClientOpen();

				// When the client is closed
				if (!isClientOpen && authenticated)
				{
					state = ClientState.NOCLIENT;
					mainWindow.consoleTab.AddLog("Client has been closed", Utility.CLIENT, LogLevel.WARN);

					if ((bool)Properties.Settings.Default["closeWithClient"])
					{
						Application.Current.Dispatcher.Invoke(() =>
						{
							Application.Current.Shutdown();
							Environment.Exit(0);
						});
					}
					//Modify GroupBox style
					mainWindow.controlPanel.ShowLolState(false);
					//Reset cached data
					DataCache.ResetCachedData();
					//If authenticated : reset the auth
					Auth.ResetAuth();

					continue;
				}

				//When not authenticated
				if (!authenticated)
				{
					//If the client is open : set the authentication and player id
					if (isClientOpen)
					{
						state = ClientState.NONE;
						mainWindow.consoleTab.AddLog("Client Found", Utility.CLIENT, LogLevel.INFO);

						if ((bool)Properties.Settings.Default["openWithClient"])
						{
							Application.Current.Dispatcher.Invoke(() =>
							{
								mainWindow.Show();
								mainWindow.Activate();
							});
						}

						ProcessModule? leagueProcess = Process.GetProcessesByName("LeagueClientUx").First().MainModule;
						if (leagueProcess == null) return;

						string? filename = leagueProcess.FileName;
						if (filename == null) return;

						Auth.SetBasicAuth(filename);
						mainWindow.controlPanel.ShowLolState(true);

						mainWindow.consoleTab.AddLog("KALUS is ready", Utility.KALUS, LogLevel.INFO);
					}
				}
				Thread.Sleep((int)Properties.Settings.Default["checkInterval"]);
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

					gamePhase = await ClientRequest.GetClientPhase();
					//Checks the game phase and perform action depending on it
					switch (gamePhase)
					{
						case "":
							continue;
						case "None":
							state = ClientState.NONE;
							break;
						case "Lobby":
							state = ClientState.LOBBY;
							break;
						case "Matchmaking":
							state = ClientState.MATCHMAKING;
							break;
						//On ready check
						case "ReadyCheck":
							state = ClientState.READYCHECK;
							//If the setting to get automatically ready is on : accept the game
							if ((bool)Properties.Settings.Default["utilityReadyCheck"])
							{
								await ClientRequest.Accept();
								mainWindow.consoleTab.AddLog("Accepting Ready Check", Utility.READY, LogLevel.INFO);
							}
							//Prevent being auto-ready multiple times
							while (await ClientRequest.GetClientPhase() == "ReadyCheck")
							{
								Thread.Sleep((int)Properties.Settings.Default["checkInterval"]);
							}
							break;
						//On champion selection : start and await the end of the champ select handler
						case "ChampSelect":
							state = ClientState.CHAMPSELECT;
							Game? champSelect = await Game.CreateGame(mainWindow);

							if (champSelect == null) break;

							await champSelect.ChampSelectControl();
							break;

						case "GameStart":
						case "InProgress":
							state = ClientState.GAMESTART;
							string gameMode = mainWindow.GetGamemodeName();
							mainWindow.controlPanel.SetGameModeIcon(gameMode, true);
							mainWindow.controlPanel.EnableRandomSkinButton(false);
							mainWindow.controlPanel.EnableChangeRuneButtons(false);
							break;
						//If not in any of the above game phase
						default:
							//Set the icon to default if it is not already
							if (!MainWindow.isStatusBoxDefault)
							{
								mainWindow.controlPanel.SetChampionIcon(await DataCache.GetDefaultChampionIcon());

								mainWindow.controlPanel.SetDefaultIcons();
								mainWindow.controlPanel.SetDefaultLabels();

								MainWindow.isStatusBoxDefault = true;
							}

							mainWindow.controlPanel.EnableRandomSkinButton(false);
							mainWindow.controlPanel.EnableChangeRuneButtons(false);
							break;
					}
				}
				Thread.Sleep((int)Properties.Settings.Default["checkInterval"]);
			}
		}


		#region Random Skin

		//Get the id of all available skins
		internal static async Task<int[]?> GetAvailableSkinsID()
		{
			JArray currentChampionSkins = await ClientRequest.GetCurrentChampionSkins();

			var availableSkinsId = currentChampionSkins.Where(skin => skin.Value<bool>("unlocked"))
														.Select(skin => skin.Value<int>("id"));


			if ((bool)Properties.Settings.Default["randomSkinAddChromas"])
			{
				var availableChromasId = currentChampionSkins.SelectMany(skin =>
				{
					var childSkins = skin["childSkins"];
					if (childSkins != null)
					{
						return childSkins.Where(childSkin => childSkin.Value<bool>("unlocked"))
										 .Select(childSkin => childSkin.Value<int>("id"));
					}
					return Enumerable.Empty<int>(); // Return an empty collection if "childSkins" is null
				});
				return availableSkinsId.Concat(availableChromasId).ToArray();
			};
			return availableSkinsId.ToArray();
		}

		//Pick a random skin
		internal static async void PickRandomSkin()
		{
			//Get all available skins
			int[]? skin_ids = await GetAvailableSkinsID();

			if (skin_ids == null) return;
			//If there is any skins available : select and change the next skin randomly
			if (skin_ids.Any())
			{
				int random_skin_index = random.Next(skin_ids.Length);
				await ClientRequest.ChangeSkinByID(skin_ids[random_skin_index]);
			}
		}

		#endregion Random Skin

		//Returns if it's the player turn to act
		//If true output the id of the action and the type (e.g : pick or ban)
		internal static bool IsCurrentPlayerTurn(IEnumerable<JObject> actions, int cellId, out int? actionId, out string? type)
		{
			//Get the player actions that are in progress
			var currentPlayerAction = actions.Where(action => action.Value<int>("actorCellId") == cellId && action.Value<bool>("isInProgress") == true)
				.Select(action => action).ToArray();

			bool isCurrentPlayerTurn = currentPlayerAction.Any();

			actionId = isCurrentPlayerTurn ? currentPlayerAction.First().Value<int>("id") : 0;
			type = isCurrentPlayerTurn ? currentPlayerAction.First()["type"]?.ToString() : string.Empty;

			if (type == null || actionId == null) return false;

			return isCurrentPlayerTurn;
		}

		internal static async Task<string?> GetChampionDefaultPosition(int championId)
		{

			return (await DataCache.GetChampionsRunesRecommendation())
																	.First(item => item.Value<int>("championId") == championId)
																	.Value<JArray>("runeRecommendations")?
																	.First(recommendation => recommendation.Value<string>("position") != "NONE" && recommendation.Value<bool>("isDefaultPosition"))
																	.Value<string>("position");
		}

		internal static async Task<int[]> GetAllChampionForPosition(string position)
		{
			return (await DataCache.GetChampionsRunesRecommendation())
							.Where(item =>
							{
								var runeRecommendations = item.Value<JArray>("runeRecommendations");
								return runeRecommendations != null &&
									runeRecommendations.Any(rune => rune.Value<string>("position") == position && rune.Value<bool>("isDefaultPosition"));
							})
							.Select(item => item.Value<int>("championId"))
							.ToArray();
		}

		#region Runes

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

			if (runesRecommendation == null) return null;

			var champRunesByPosition = runesRecommendation.Where(recommendation => recommendation["position"]?.ToString() == position.ToUpper()).Select(recommendation => recommendation).ToArray();
			if (!champRunesByPosition.Any())
			{
				champRunesByPosition = runesRecommendation.Where(recommendation => recommendation["position"]?.ToString() == "NONE").Select(recommendation => recommendation).ToArray();
			}
			if(recommendationNumber < champRunesByPosition.Length) return champRunesByPosition[recommendationNumber];
			return champRunesByPosition[0];
		}

		//Get the recommended champion spells for a champion depending on its position and the game mode
		internal static async Task<JToken?> GetSpellsRecommendationByPosition(int champId, string position)
		{
			var runesRecommendation = await GetRecommendedRunesById(champId);

			if (runesRecommendation == null) return null;

			IEnumerable<JToken>? champRunesByPosition = null;

			if (position != "") champRunesByPosition = runesRecommendation.Where(recommendation => recommendation["position"]?.ToString() == position);

			if (champRunesByPosition == null) return null;

			if (position == "" || !champRunesByPosition.Any()) champRunesByPosition = runesRecommendation.Where(recommendation => recommendation["position"]?.ToString() != "NONE");

			return champRunesByPosition.Select(recommendation => recommendation["summonerSpellIds"]).First(); ;
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

			if (!pages.Any()) return 0;
			//Get the page containing the name KALUS
			var kurwappRunes = pages.First(page => page.Value<bool>("current") == true);

			//Return the page id if there is a page else return 0
			return kurwappRunes.Any() ? int.Parse(kurwappRunes.ToString()) : 0;
		}

		//Check if we can create a new page
		internal static async Task<bool> CanCreateNewPage()
		{
			var inventory = await ClientRequest.GetRunesInventory();
			if (inventory == null) return false;
			return inventory.Value<bool>("canAddCustomPage");
		}

		//Set the recommended rune page
		internal static async Task SetRunesPage(int champId, string position = "NONE", int recommendationNumber = 0)
		{
			var appPageId = await DataCache.GetAppRunePageId();

			var champions = await DataCache.GetChampionsInformations();

			string? championName = champions.Where(champion => champion.Value<int>("id") == champId).Select(champion => champion["name"]?.ToString()).First();

			if (championName == null) return;

			//Get the recommended rune page
			var runesRecommendation = await GetChampRunesByPosition(champId, position, recommendationNumber);
			if (runesRecommendation == null) return;

			string recommendedRunes = FormatChampRunes(runesRecommendation, championName, position);

			if (appPageId != null)
			{
				await ClientRequest.EditRunePage(appPageId, recommendedRunes);
			}
			else if (await CanCreateNewPage())
			{
				await ClientRequest.CreateNewRunePage(recommendedRunes);
			}
			else if ((bool)Properties.Settings.Default["runesOverrideOldestPage"])
			{
				await EditOldestRunePage(recommendedRunes);
			}
		}

		private static async Task EditOldestRunePage(string newRunesPage)
		{
			var runesPages = await ClientRequest.GetRunePages();
			string? oldestPageId = runesPages.OrderBy(page => page["lastModified"]?.ToString()).First()["id"]?.ToString();

			if (oldestPageId == null) return;

			await ClientRequest.EditRunePage(oldestPageId, newRunesPage);
		}

		#endregion Runes

		internal static async void SetSummonerSpells(int[] recommendedSpells)
		{
			int flashPosition = (int)Properties.Settings.Default["flashPosition"];

			if (flashPosition != 2 && recommendedSpells.Contains(4))
			{
				if (Array.IndexOf(recommendedSpells, 4) != flashPosition)
				{
					(recommendedSpells[1], recommendedSpells[0]) = (recommendedSpells[0], recommendedSpells[1]);
				}
			}

			await ClientRequest.ChangeSummonerSpells(recommendedSpells);
		}

		internal static async Task<Tuple<byte[], byte[]>?> GetRunesIcons()
		{
			var runesStyles = await DataCache.GetRunesStyleInformation();

			if (runesStyles == null) return null;

			var currentRunes = await ClientRequest.GetActiveRunePage();

			if (currentRunes == null) return null;

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