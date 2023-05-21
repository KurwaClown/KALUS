using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace KurwApp.Modules
{
	public class Game
	{
		private string cellId;
		private string position;
		private string? gameType = null;
		private int championId = 0;
		private JObject? sessionInfo;

		private bool isRunePageChanged = false;
		private bool hasPicked = false;
		private bool champSelectFinalized = false;

		private MainWindow mainWindow;

		public Game(MainWindow mainWindow)
		{
			this.mainWindow = mainWindow;
		}

		//Setting the player position and cell position id
		internal void SetRoleAndCellId()
		{
			cellId = sessionInfo["localPlayerCellId"].ToString();
			position = sessionInfo["myTeam"].Where(player => player["cellId"].ToString() == cellId).Select(player => player["assignedPosition"].ToString()).First().ToUpper();
		}

		//Set the game type (draft, blind or aram)
		internal async Task SetGameType()
		{
			JObject? lobbyInfo = await Client_Request.GetLobbyInfo();
			if (lobbyInfo is null) return;
			string gameMode = lobbyInfo["gameConfig"]["gameMode"].ToString();
			bool hasPositions = (bool)lobbyInfo["gameConfig"]["showPositionSelector"];

			//if the gamemode is aram set to ARAM
			if (gameMode == "ARAM")
			{
				gameType = "ARAM";
				return;
			}

			//if it's not aram and has positions set to Draft
			if (hasPositions)
			{
				gameType = "Draft";
				return;
			}

			//else set it to Blind
			gameType = "Blind";
		}

		//Get a list of all champions (their ids) that got banned or picked
		internal int[] GetNonAvailableChampions()
		{
			return GetSessionActions().Where(action => (bool)action["completed"] && (string)action["type"] != "ten_bans_reveal")
										.Select(action => (int)action["championId"]).ToArray();
		}

		//Handler of the champion selections
		internal async Task ChampSelectControl()
		{
			sessionInfo = await Client_Request.GetSessionInfo();
			if (sessionInfo is null) return;
			//Set the properties
			await SetGameType();
			SetRoleAndCellId();
			if (gameType is null) return;

			mainWindow.SetGamemodeName(gameType);
			mainWindow.SetGameModeIcon(gameType);

			while (Auth.IsAuthSet())
			{
				sessionInfo = await Client_Request.GetSessionInfo();

				if (sessionInfo is null) return;
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
		private async Task Finalization()
		{
			var currentRuneIcons = await Client_Control.GetRunesIcons();

			if (currentRuneIcons is not null) mainWindow.SetRunesIcons(currentRuneIcons.Item1, currentRuneIcons.Item2); ;

			if (gameType == "ARAM")
			{
				await AramFinalization();
				return;
			}

			if (championId == 0) championId = await Client_Request.GetCurrentChampionId();

			if (!champSelectFinalized)
			{
				await PostPickAction();
				champSelectFinalized = true;
			}

		}

		private async Task PostPickAction()
		{
			var imageBytes = await Client_Request.GetChampionImageById(championId);

			var champions = await ClientDataCache.GetChampionsInformations();

			string championName = champions.Where(champion => (int)champion["id"] == championId).Select(champion => champion["name"].ToString()).First();

			//Set the current champion image and name on the UI
			mainWindow.SetChampionIcon(imageBytes);
			mainWindow.SetChampionName(championName);
			//Toggle the random skin button on
			mainWindow.EnableRandomSkinButton(true);

			//Set runes if the the auto rune is toggled
			if (Client_Control.GetSettingState("runesSwap") && !isRunePageChanged)
			{
				await AutoRuneSwap();
			}

			//Random skin on pick
			if ((bool)Client_Control.GetPreference("randomSkin.randomOnPick")) Client_Control.PickRandomSkin();

			if (Client_Control.GetSettingState("autoSummoner")) await ChangeSpells(gameType == "ARAM");

		}

		private async Task ChangeSpells(bool isAram)
		{
			string positionForSpells = "";
			if (gameType == "Draft") positionForSpells = position;
			if (gameType == "ARAM") positionForSpells = "NONE";
			var runesRecommendation = await Client_Control.GetSpellsRecommendationByPosition(championId, positionForSpells);
			var spellsId = runesRecommendation.ToObject<int[]>();

			if (isAram && !spellsId.Contains(32)) spellsId[1] = 32;

			Client_Control.SetSummonerSpells(spellsId);
		}

		private async Task AramFinalization()
		{
			if (Client_Control.GetSettingState("aramChampionSwap"))
			{
				int aramPick = GetAramPick();

				if (aramPick != 0)
				{
					await Client_Request.AramBenchSwap(aramPick);
					await PostPickAction();
					championId = await Client_Request.GetCurrentChampionId();

					if (Client_Control.GetSettingState("runesSwap") && !isRunePageChanged)await AutoRuneSwap();

					if (Client_Control.GetSettingState("autoSummoner")) await ChangeSpells(true);
				}
			}
			var currentChampionId = await Client_Request.GetCurrentChampionId();

			if (currentChampionId != championId)
			{
				championId = currentChampionId;
				await PostPickAction();
				if (Client_Control.GetSettingState("runesSwap")) await AutoRuneSwap();

				if (Client_Control.GetSettingState("autoSummoner")) await ChangeSpells(true);
			}
		}

		private async Task AutoRuneSwap()
		{
			bool isSetActive = (bool)Client_Control.GetPreference("runes.notSetActive");
			string activeRunesPage = isSetActive ? (await Client_Request.GetActiveRunePage())["id"].ToString() : "0";
			await Client_Control.SetRunesPage(championId, position == "" ? "NONE" : position.ToUpper());
			isRunePageChanged = true;

			if (isSetActive) await Client_Request.SetActiveRunePage(activeRunesPage);
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
				if (type == "ban" && Client_Control.GetSettingState("banPick"))
				{
					int banPick = await GetChampionBan();
					if (banPick == 0) return;
					await SelectionAction(actionId, banPick);
				}

				if (type == "pick" && Client_Control.GetSettingState("championPick"))
				{
					championId = GetChampionPick();
					if (championId == 0) return;

					await SelectionAction(actionId, championId);
					hasPicked = true;
				}
			}
		}

		private static async Task SelectionAction(int actionId, int championId)
		{
			if (championId == 0) return;

			await Client_Request.SelectChampion(actionId, championId);
			await Client_Request.ConfirmAction(actionId);
		}

		//Get the pick the aram champion to pick if any
		private int GetAramPick()
		{
			var filename = "ARAM.json";
			var aramPicks = JArray.Parse(File.ReadAllText($"Picks/{filename}"));

			if (!aramPicks.Any()) return 0;

			List<int> aramBenchIds = GetAramBenchIds();
			if (!aramBenchIds.Any()) return 0;

			foreach (var pick in aramPicks)
			{
				if (aramBenchIds.Contains(pick.Value<int>()))
				{
					return pick.Value<int>();
				}
			}
			return 0;
		}

		//Get the champion benched (their id) in aram
		private List<int> GetAramBenchIds()
		{
			return sessionInfo["benchChampions"].Select(x => int.Parse(x["championId"].ToString())).ToList();
		}

		//Get the champion pick for blind or draft game, if any
		private int GetChampionPick()
		{
			var filename = gameType == "Draft" ? "Pick.json" : $"{gameType}.json";
			var pickFile = File.ReadAllText($"Picks/{filename}");
			var picks = gameType == "Draft" ? JObject.Parse(pickFile)[position] as JArray : JArray.Parse(pickFile);

			if (!picks.Any()) return 0;

			var availablePicks = picks.Select(x => int.Parse(x.ToString()))
												.ToArray()
												.Except(GetNonAvailableChampions());

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

		//Get if it's the current player turn and output the action id and type of action
		internal bool IsCurrentPlayerTurn(IEnumerable<JObject> actions, out int actionId, out string type)
		{
			var currentPlayerAction = actions.Where(action => action["actorCellId"].ToString() == cellId && (bool)action["isInProgress"] == true)
				.Select(action => action).ToArray();

			bool isCurrentPlayerTurn = currentPlayerAction.Any();

			actionId = isCurrentPlayerTurn ? (int)currentPlayerAction.First()["id"] : 0;
			type = isCurrentPlayerTurn ? currentPlayerAction.First()["type"].ToString() : string.Empty;
			return isCurrentPlayerTurn;
		}

		//Get sessions actions
		internal IEnumerable<JObject> GetSessionActions()
		{
			return sessionInfo["actions"].SelectMany(innerArray => innerArray).OfType<JObject>();
		}
	}
}