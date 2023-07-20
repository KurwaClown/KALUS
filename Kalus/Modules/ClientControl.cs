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
					mainWindow.consoleTab.AddLog(Properties.Logs.ClientClosed, Utility.CLIENT, LogLevel.WARN);

					if (Properties.Settings.Default.closeWithClient)
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
						mainWindow.consoleTab.AddLog(Properties.Logs.ClientFound, Utility.CLIENT, LogLevel.INFO);

						if (Properties.Settings.Default.openWithClient)
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

						mainWindow.consoleTab.AddLog(Properties.Logs.KALUSReady, Utility.KALUS, LogLevel.INFO);
					}
				}
				Thread.Sleep(Properties.Settings.Default.checkInterval);
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
							if (Properties.Settings.Default.utilityReadyCheck)
							{
								await ClientRequest.Accept();
								mainWindow.consoleTab.AddLog(Properties.Logs.ReadyCheck, Utility.READY, LogLevel.INFO);
							}
							//Prevent being auto-ready multiple times
							while (await ClientRequest.GetClientPhase() == "ReadyCheck")
							{
								Thread.Sleep(Properties.Settings.Default.checkInterval);
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
				Thread.Sleep(Properties.Settings.Default.checkInterval);
			}
		}


		#region Random Skin

		//Get the id of all available skins
		internal static async Task<int[]?> GetAvailableSkinsID()
		{
			JArray currentChampionSkins = await ClientRequest.GetCurrentChampionSkins();

			var availableSkinsId = currentChampionSkins.Where(skin => skin.Value<bool>("unlocked"))
														.Select(skin => skin.Value<int>("id"));


			if (Properties.Settings.Default.randomSkinAddChromas)
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

		//Get the recommended champion spells for a champion depending on its position and the game mode
		internal static async Task<JToken?> GetSpellsRecommendationByPosition(int champId, string position)
		{
			var runesRecommendation = await Runes.GetRecommendedRunesById(champId);

			if (runesRecommendation == null)
				return null;

			IEnumerable<JToken>? champRunesByPosition = null;

			if (position != "")
				champRunesByPosition = runesRecommendation.Where(recommendation => recommendation["position"]?.ToString() == position);

			if (champRunesByPosition == null)
				return null;

			if (position == "" || !champRunesByPosition.Any())
				champRunesByPosition = runesRecommendation.Where(recommendation => recommendation["position"]?.ToString() != "NONE");

			return champRunesByPosition.Select(recommendation => recommendation["summonerSpellIds"]).First();
			;
		}


		internal static async void SetSummonerSpells(int[] recommendedSpells)
		{
			int flashPosition = Properties.Settings.Default.flashPosition;

			if (flashPosition != 2 && recommendedSpells.Contains(4))
			{
				if (Array.IndexOf(recommendedSpells, 4) != flashPosition)
				{
					(recommendedSpells[1], recommendedSpells[0]) = (recommendedSpells[0], recommendedSpells[1]);
				}
			}

			await ClientRequest.ChangeSummonerSpells(recommendedSpells);
		}


	}
}