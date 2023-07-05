using Kalus.Modules.Networking;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Kalus.UI.Windows;

namespace Kalus.Modules.Games.GameMode
{
	internal class Classic : Game
	{
		private readonly string gameType;
		private readonly bool isDraft;

		private int cellId;
		private string? position = "NONE";
		private bool isChampionRandom = false;
		private bool champSelectFinalized = false;
		private bool hasPicked = false;
		private Timer? delayedPick;
		private string delayedPickType = string.Empty;

		internal Classic(MainWindow mainWindow, string gameType) : base(mainWindow)
		{
			this.mainWindow = mainWindow;
			this.mainWindow.controlPanel.runeChange = this.ChangeRunes;

			this.gameType = gameType;
			isDraft = gameType == "Draft";
			mainWindow.consoleTab.AddLog($"Joined {gameType} Game", UI.Controls.Tabs.Console.Utility.GAME, UI.Controls.Tabs.Console.LogLevel.INFO);
		}

		//Setting the player position and cell position id
		internal void SetPositionAndCellId()
		{
			if (sessionInfo == null) return;
			cellId = sessionInfo.Value<int>("localPlayerCellId");
			if (isDraft) position = (sessionInfo["myTeam"]?.Where(player => player.Value<int>("cellId") == cellId).Select(player => player["assignedPosition"]?.ToString()).FirstOrDefault() ?? "NONE").ToUpper();
		}

		//Handler of the champion selections
		protected internal override async Task ChampSelectControl()
		{
			if (mainWindow == null) return;

			mainWindow.controlPanel.SetGamemodeName(gameType);
			mainWindow.controlPanel.SetGameModeIcon(gameType);

			while (Auth.IsAuthSet())
			{
				sessionInfo = await ClientRequest.GetSessionInfo();
				if (sessionInfo == null) return;
				//Set the position and cell id after every new session check : in case of cell change
				SetPositionAndCellId();
				switch (sessionInfo.SelectToken("timer.phase")?.ToString())
				{
					default:
						break;

					case null:
						return;

					case "FINALIZATION":
						await Finalization();
						break;

					case "BAN_PICK":
						if (hasPicked) goto case "FINALIZATION";
						await PickPhase();
						break;

					case "GAME_STARTING":
					case "":
						mainWindow.controlPanel.EnableRandomSkinButton(false);
						mainWindow.controlPanel.EnableChangeRuneButtons(false);
						return;
				}
				Thread.Sleep(ClientControl.checkInterval);
			}
		}

		//Act on finalization
		protected override async Task Finalization()
		{
			if (championId != await ClientRequest.GetCurrentChampionId()) champSelectFinalized = false;

			var currentRuneIcons = await ClientControl.GetRunesIcons();

			if (currentRuneIcons != null && mainWindow != null) mainWindow.controlPanel.SetRunesIcons(currentRuneIcons.Item1, currentRuneIcons.Item2); ;

			if (championId == 0) championId = await ClientRequest.GetCurrentChampionId();
			if (delayedPick != null)
			{
				championId = await ClientRequest.GetCurrentChampionId();
				CancelDelayedPick();
			}
			if (!isDraft)
			{
				position = await ClientControl.GetChampionDefaultPosition(championId);
			}
			if (!champSelectFinalized)
			{
				await PostPickAction();
				champSelectFinalized = true;
			}
		}

		protected override async Task ChangeSpells()
		{
			if (position == null) return;
			string positionForSpells = position;
			var runesRecommendation = await ClientControl.GetSpellsRecommendationByPosition(championId, positionForSpells);

			if (runesRecommendation == null) return;

			var spellsId = runesRecommendation.ToObject<int[]>();
			if (spellsId != null) ClientControl.SetSummonerSpells(spellsId);
		}

		//Act on pick phase
		private async Task PickPhase()
		{
			if (champSelectFinalized) return;
			var actions = GetSessionActions();
			if (actions == null) return;
			int currentChampionId = await ClientRequest.GetCurrentChampionId();
			if (currentChampionId != 0)
			{
				championId = currentChampionId;
				hasPicked = true;
				return;
			}

			if (IsCurrentPlayerTurn(actions, out int actionId, out string? type))
			{
				//Return if there is a delayed action of the current phase type awaiting
				//Cancel it if it's not of the current phase type
				if (delayedPick != null)
				{
					if (delayedPickType == type)
					{
						return;
					}
					else
					{
						CancelDelayedPick();
					}
				}

				if (type == "ban" && ClientControl.GetSettingState("banPick"))
				{
					int banPick = GetChampionBan();
					if (banPick == 0) return;
					await SelectionAction(actionId, banPick, type);
				}

				if (type == "pick" && ClientControl.GetSettingState("championPick"))
				{
					championId = await GetChampionPick();
					int championSelectionId = actions.First(action => action.Value<bool>("isInProgress") && action.Value<int>("actorCellId") == cellId)
														.Value<int>("championId");

					if (championSelectionId != championId && championSelectionId != 0)
					{
						switch (ClientControl.GetPreference<int>("selections.userPreference"))
						{
							default:
								break;

							case 0:
								return;

							case 1:
								break;

							case 2:
								championId = championSelectionId;
								break;
						}
					}

					if (championId == 0)
					{
						switch (ClientControl.GetPreference<int>("noPicks.userPreference"))
						{
							default:
								int randomChampionId = await GetRandomChampionPick();
								if (randomChampionId == 0) return;
								championId = randomChampionId;
								isChampionRandom = true;
								break;

							case 0:
								return;
						}
					}
					await SelectionAction(actionId, championId, type);
				}
			}
		}

		private async Task SelectionAction(int actionId, int championId, string type)
		{
			if (championId == 0 || delayedPickType == type) return;

			await ClientRequest.SelectChampion(actionId, championId);
			if (type == "pick")
			{
				if (isChampionRandom) return;
				await ExecutePickBanPreference("picks", actionId, type);
			}

			if (type == "ban")
			{
				await ExecutePickBanPreference("bans", actionId, type);
			}
		}

		private async Task ExecutePickBanPreference(string preferenceToken, int actionId, string pickType)
		{
			int? preference = ClientControl.GetPreference<int>($"{preferenceToken}.userPreference");
			if (preference == null) return;
			if (preference == 1) return;
			if (preference == 2)
			{
				//Return if the delayed pick has already been set to the current action type
				if (delayedPick != null && delayedPickType == pickType) return;

				//+1 to prevent index 0 (5 seconds) to be 0 seconds
				int? otlTimeIndex = ClientControl.GetPreference<int>($"{preferenceToken}.OTLTimeIndex") + 1;
				int? sessionTimer = sessionInfo?.SelectToken("timer.adjustedTimeLeftInPhase")?.Value<int>();

				if (sessionTimer == null || otlTimeIndex == null) return;

				delayedPick = new Timer(state =>
				{
					ConfirmActionDelayed(actionId, pickType); // Call the callback method with the passed argument
				}, actionId, TimeSpan.FromSeconds((double)(sessionTimer / 1000 - otlTimeIndex * 5)), TimeSpan.Zero);
				delayedPickType = pickType;
				return;
			}
			await ClientRequest.ConfirmAction(actionId);
			if (pickType == "pick") hasPicked = true;
		}

		private async void ConfirmActionDelayed(int actionId, string pickType)
		{
			await ClientRequest.ConfirmAction(actionId);
			if (pickType == "pick") hasPicked = true;
			CancelDelayedPick();
		}

		private void CancelDelayedPick()
		{
			if (delayedPick != null)
			{
				delayedPick.Dispose();
				delayedPick = null;
				delayedPickType = string.Empty;
			}
		}

		//Get the champion pick for blind or draft game, if any
		protected virtual async Task<int> GetChampionPick()
		{
			if (position == null) return 0;
			var picks = isDraft ? DataCache.GetDraftPick(position) : DataCache.GetBlindPick();

			if (picks == null) return 0;
			if (!picks.Any()) return 0;

			IEnumerable<int> availablePicks = picks.Select(x => int.Parse(x.ToString()))
												.ToArray()
												.Except(GetNonAvailableChampions() ?? Array.Empty<int>())
												.Intersect(await ClientRequest.GetAvailableChampionsPick() ?? Enumerable.Empty<int>());

			if (!availablePicks.Any()) return 0;
			return availablePicks.First();
		}

		//Get the champion ban for draft game, if any
		private int GetChampionBan()
		{
			if (position == null) return 0;
			var bans = DataCache.GetDraftBan(position);

			if (bans == null) return 0;
			if (!bans.Any()) return 0;

			var availableBans = bans.Select(x => int.Parse(x.ToString()))
												.ToArray()
												.Except(GetNonAvailableChampions() ?? Array.Empty<int>());

			if (!availableBans.Any()) return 0;

			return availableBans.First();
		}

		private async Task<int> GetRandomChampionPick()
		{
			int noPicksPreferences = ClientControl.GetPreference<int>("noPicks.userPreference");

			var availableChampions = await ClientRequest.GetAvailableChampionsPick();
			if (availableChampions == null) return 0;

			//Random pick by position
			if (noPicksPreferences == 2)
			{
				if (position == null) return 0;
				var allChampionsByPosition = await ClientControl.GetAllChampionForPosition(position);

				var availablesChampionsForPosition = allChampionsByPosition.Intersect(availableChampions).ToArray();

				return availablesChampionsForPosition[new Random().Next(maxValue: availablesChampionsForPosition.Length)];
			}

			//Random pick (no position)
			return availableChampions[new Random().Next(maxValue: availableChampions.Length)];
		}

		//Get if it's the current player turn and output the action id and type of action
		internal bool IsCurrentPlayerTurn(IEnumerable<JObject> actions, out int actionId, out string? type)
		{
			SetPositionAndCellId();
			var currentPlayerAction = actions.Where(action => action.Value<int>("actorCellId") == cellId && action.Value<bool>("isInProgress") == true)
				.Select(action => action).ToArray();

			bool isCurrentPlayerTurn = currentPlayerAction.Any();

			actionId = isCurrentPlayerTurn ? currentPlayerAction.First().Value<int>("id") : 0;
			type = isCurrentPlayerTurn ? currentPlayerAction.First()["type"]?.ToString() : string.Empty;

			if (actionId == 0 || type == null) return false;

			return isCurrentPlayerTurn;
		}

		//Get a list of all champions (their ids) that got banned or picked
		internal int[]? GetNonAvailableChampions()
		{
			return GetSessionActions()?.Where(action => action.Value<bool>("completed") && action["type"]?.ToString() != "ten_bans_reveal")
										.Select(action => action.Value<int>("championId")).ToArray();
		}

		protected override async Task ChangeRunes(int recommendationNumber = 0)
		{
			bool isSetActive = ClientControl.GetPreference<bool>("runes.notSetActive");

			string? activeRunesPage = isSetActive ? (await ClientRequest.GetActiveRunePage())?["id"]?.ToString() : "0";

			if (activeRunesPage == null) return;

			if (position == null) return;
			await ClientControl.SetRunesPage(championId, position, recommendationNumber);
			isRunePageChanged = true;

			if (isSetActive) await ClientRequest.SetActiveRunePage(activeRunesPage);
		}
	}
}