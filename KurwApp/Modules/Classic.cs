using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace KurwApp.Modules
{
	internal class Classic : Game
	{
		private readonly string gameType;
		private readonly bool isDraft;

		private int cellId;
		private string position = "NONE";
		private bool isChampionRandom = false;
		private bool champSelectFinalized = false;
		private bool hasPicked = false;
		private Timer? delayedPick;
		private string delayedPickType = string.Empty;

		internal Classic(MainWindow mainWindow, string gameType)
		{
			this.mainWindow = mainWindow;
			this.gameType = gameType;
			this.isDraft = gameType == "Draft";
		}

		//Setting the player position and cell position id
		internal void SetPositionAndCellId()
		{
			cellId = sessionInfo.Value<int>("localPlayerCellId");
			if (isDraft) position = sessionInfo["myTeam"].Where(player => player.Value<int>("cellId") == cellId).Select(player => player["assignedPosition"].ToString()).First().ToUpper();
		}



		//Handler of the champion selections
		internal override async Task ChampSelectControl()
		{
			mainWindow.SetGamemodeName(gameType);
			mainWindow.SetGameModeIcon(gameType);

			while (Auth.IsAuthSet())
			{
				sessionInfo = await Client_Request.GetSessionInfo();
				if (sessionInfo is null) return;
				//Set the position and cell id after every new session check : in case of cell change
				SetPositionAndCellId();
				switch (sessionInfo.SelectToken("timer.phase").ToString())
				{
					default:
						break;

					case "FINALIZATION":
						await Finalization();
						break;

					case "BAN_PICK":
						if (hasPicked) goto case "FINALIZATION";
						await PickPhase();
						break;

					case "GAME_STARTING":
					case "":
						mainWindow.EnableRandomSkinButton(false);
						return;
				}
				Thread.Sleep(1000);
			}
		}

		//Act on finalization
		protected override async Task Finalization()
		{
			var currentRuneIcons = await Client_Control.GetRunesIcons();

			if (currentRuneIcons is not null) mainWindow.SetRunesIcons(currentRuneIcons.Item1, currentRuneIcons.Item2); ;

			if (championId == 0) championId = await Client_Request.GetCurrentChampionId();

			if (!isDraft) position = await Client_Control.GetChampionDefaultPosition(championId);

			if (!champSelectFinalized)
			{
				await PostPickAction();
				champSelectFinalized = true;
			}
		}

		protected override async Task ChangeSpells()
		{
			string positionForSpells = position;
			var runesRecommendation = await Client_Control.GetSpellsRecommendationByPosition(championId, positionForSpells);
			var spellsId = runesRecommendation.ToObject<int[]>();

			Client_Control.SetSummonerSpells(spellsId);
		}

		//Act on pick phase
		private async Task PickPhase()
		{
			if (champSelectFinalized) return;
			var actions = GetSessionActions();

			int currentChampionId = await Client_Request.GetCurrentChampionId();
			if (currentChampionId != 0)
			{
				championId = currentChampionId;
				hasPicked = true;
				return;
			}

			if (IsCurrentPlayerTurn(actions, out int actionId, out string type))
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

				if (type == "ban" && Client_Control.GetSettingState("banPick"))
				{
					int banPick = await GetChampionBan();
					if (banPick == 0) return;
					await SelectionAction(actionId, banPick, type);
				}

				if (type == "pick" && Client_Control.GetSettingState("championPick"))
				{
					championId = await GetChampionPick();
					int championSelectionId = actions.First(action => action.Value<bool>("isInProgress") && action.Value<int>("actorCellId") == cellId)
														.Value<int>("championId");
					if (championSelectionId != championId && championSelectionId != 0)
					{
						switch (Client_Control.GetPreference("selections.userPreference").Value<int>())
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
						switch (Client_Control.GetPreference("noPicks.userPreference").Value<int>())
						{
							default:
								championId = await GetRandomChampionPick();
								isChampionRandom = true;
								break;

							case 2:
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

			await Client_Request.SelectChampion(actionId, championId);
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
			int preference = Client_Control.GetPreference($"{preferenceToken}.userPreference").Value<int>();
			if (preference == 1) return;
			if (preference == 2)
			{
				//Return if the delayed pick has already been set to the current action type
				if (delayedPick != null && delayedPickType == pickType) return;

				//+1 to prevent index 0 (5 seconds) to be 0 seconds
				int otlTimeIndex = Client_Control.GetPreference($"{preferenceToken}.OTLTimeIndex").Value<int>() + 1;
				int sessionTimer = sessionInfo.SelectToken("timer.adjustedTimeLeftInPhase").Value<int>();

				delayedPick = new Timer(ConfirmActionDelayed, actionId, TimeSpan.FromSeconds(sessionTimer / 1000 - (otlTimeIndex * 5)), TimeSpan.Zero);
				delayedPickType = pickType;
				return;
			}
			await Client_Request.ConfirmAction(actionId);
			hasPicked = true;
		}

		private async void ConfirmActionDelayed(object sender)
		{
			await Client_Request.ConfirmAction((int)sender);
			hasPicked = true;
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
			var filename = isDraft ? "Pick.json" : $"{gameType}.json";
			var pickFile = File.ReadAllText($"Picks/{filename}");
			var picks = isDraft ? JObject.Parse(pickFile)[position] as JArray : JArray.Parse(pickFile);

			if (!picks.Any()) return 0;

			var availablePicks = picks.Select(x => int.Parse(x.ToString()))
												.ToArray()
												.Except(GetNonAvailableChampions())
												.Intersect(await Client_Request.GetAvailableChampionsPick());

			if (!availablePicks.Any()) return 0;
			return availablePicks.First();
		}

		//Get the champion ban for draft game, if any
		private async Task<int> GetChampionBan()
		{
			var banFile = File.ReadAllText($"Picks/Ban.json");
			var bans = JObject.Parse(banFile)[position];

			if (!bans.Any()) return 0;

			var availableBans = bans.Select(x => int.Parse(x.ToString()))
												.ToArray()
												.Except(GetNonAvailableChampions());

			if (!availableBans.Any()) return 0;

			return availableBans.First();
		}

		private async Task<int> GetRandomChampionPick()
		{
			int noPicksPreferences = Client_Control.GetPreference("noPicks.userPreference").Value<int>();

			var availableChampions = await Client_Request.GetAvailableChampionsPick();

			//Random pick by position
			if (noPicksPreferences == 0)
			{
				var allChampionsByPosition = await Client_Control.GetAllChampionForPosition(position);

				var availablesChampionsForPosition = allChampionsByPosition.Intersect(availableChampions).ToArray();

				return availablesChampionsForPosition[new Random().Next(maxValue: availablesChampionsForPosition.Length)];
			}

			//Random pick (no position)
			return availableChampions[new Random().Next(maxValue: availableChampions.Length)];
		}

		//Get if it's the current player turn and output the action id and type of action
		internal bool IsCurrentPlayerTurn(IEnumerable<JObject> actions, out int actionId, out string type)
		{
			var currentPlayerAction = actions.Where(action => action.Value<int>("actorCellId") == cellId && (bool)action["isInProgress"] == true)
				.Select(action => action).ToArray();

			bool isCurrentPlayerTurn = currentPlayerAction.Any();

			actionId = isCurrentPlayerTurn ? (int)currentPlayerAction.First()["id"] : 0;
			type = isCurrentPlayerTurn ? currentPlayerAction.First()["type"].ToString() : string.Empty;
			return isCurrentPlayerTurn;
		}

		//Get a list of all champions (their ids) that got banned or picked
		internal int[] GetNonAvailableChampions()
		{
			return GetSessionActions().Where(action => (bool)action["completed"] && (string)action["type"] != "ten_bans_reveal")
										.Select(action => (int)action["championId"]).ToArray();
		}

		protected override async Task ChangeRunes()
		{
			bool isSetActive = (bool)Client_Control.GetPreference("runes.notSetActive");
			string activeRunesPage = isSetActive ? (await Client_Request.GetActiveRunePage())["id"].ToString() : "0";
			await Client_Control.SetRunesPage(championId, position);
			isRunePageChanged = true;

			if (isSetActive) await Client_Request.SetActiveRunePage(activeRunesPage);
		}
	}
}