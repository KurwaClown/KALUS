using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace KurwApp.Modules
{
    class Game
    {
        private string cellId;
        private string position;
		private string? gameType = null;
		JObject? sessionInfo;
		bool isRunePageChanged = false;

		private MainWindow mainWindow;

        public Game(MainWindow mainWindow) {
            this.mainWindow = mainWindow;
		}

		//Setting the player position and cell position id
		internal void SetRoleAndCellId()
		{
			cellId = sessionInfo["localPlayerCellId"].ToString();
			position = sessionInfo["myTeam"].Where(player => player["cellId"].ToString() == cellId).Select(player => player["assignedPosition"].ToString()).First();
		}

		//Set the game type (draft, blind or aram)
		internal async Task SetGameType()
		{
			JObject? lobbyInfo = await Client_Request.GetLobbyInfo();
			if (lobbyInfo is null) return;
			string gameMode = lobbyInfo["gameConfig"]["gameMode"].ToString();
			bool hasPositions = (bool)lobbyInfo["gameConfig"]["showPositionSelector"];

			//if the gamemode is aram set to ARAM
			if (gameMode == "ARAM") {
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
			SetRoleAndCellId();
			await SetGameType();
			if (gameType is null) return;

			do
			{
				switch (sessionInfo.SelectToken("timer.phase").ToString())
				{
					default:
						break;
					case "FINALIZATION":
						await Finalization();
						break;
					case "BAN_PICK":
						await PickPhase();
						break;
					case "GAME_STARTING":
					case "":
						mainWindow.EnableRandomSkinButton(false);
						return;
				}
				Thread.Sleep(1000);

				sessionInfo = await Client_Request.GetSessionInfo();

				if (sessionInfo is null) return;

			} while (true);
		}

		//Act on finalization
		private async Task Finalization()
		{
			if (gameType == "ARAM" && Client_Control.GetSettingState("aramChampionSwap"))
			{
				int aramPick = GetAramPick();

				if (aramPick != 0) await Client_Request.AramBenchSwap(aramPick);
			}

			//Set the current champion image on the UI
			var championId = await Client_Request.GetCurrentChampionId();
			if (championId == 0) return;
			mainWindow.ChangeCharacterIcon(await Client_Request.GetChampionImageById(championId));

			//Toggle the random skin button on
			mainWindow.EnableRandomSkinButton(true);

			//Set runes if the the auto rune is toggled
			if (Client_Control.GetSettingState("runesSwap") && !isRunePageChanged)
			{
				Client_Control.SetRunesPage(championId, position == "" ? "NONE" : position.ToUpper());
				isRunePageChanged = true;
			}
		}

		//Act on pick phase
		private async Task PickPhase()
		{
			var actions = GetSessionActions();

			if (IsCurrentPlayerTurn(actions, out int actionId, out string type))
			{
				if (type == "ban" && Client_Control.GetSettingState("banPick"))
				{
					int banPick = await GetChampionBan();

					await SelectionAction(actionId, banPick);

				}

				if (type == "pick" && Client_Control.GetSettingState("championPick"))
				{
					int champPick = await GetChampionPick();

					await SelectionAction(actionId, champPick);

					var imageBytes = await Client_Request.GetChampionImageById(champPick);

					mainWindow.ChangeCharacterIcon(imageBytes);

					//Set runes if the the auto rune is toggled
					if (Client_Control.GetSettingState("runesSwap")) {
						Client_Control.SetRunesPage(champPick, position == "" ? "NONE" : position.ToUpper());
						isRunePageChanged = true;
					}

					//Random skin on pick
					if((bool)Client_Control.GetPreference("randomSkin.randomOnPick")) Client_Control.PickRandomSkin();
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

			int[] aramBenchIds = GetAramBenchIds();
			if (!aramBenchIds.Any()) return 0;
			return aramPicks.Select(x => int.Parse(x.ToString()))
												.ToArray()
												.Where(x => aramBenchIds.Contains(x)).First();
		}

		//Get the champion benched (their id) in aram
		private int[] GetAramBenchIds()
		{
			return sessionInfo["benchChampions"].Select(x => int.Parse(x["championId"].ToString())).ToArray();
		}

		//Get the champion pick for blind or draft game, if any
		private async Task<int> GetChampionPick()
		{
			var filename = gameType == "Draft" ? "Pick.json" : $"{gameType}.json";
			var pickFile = File.ReadAllText($"Picks/{filename}");
			var picks = gameType == "Draft" ? JObject.Parse(pickFile)[position] : JArray.Parse(pickFile);

			if (!picks.Any()) return 0;

			return picks.Select(x => int.Parse(x.ToString()))
												.ToArray()
												.Except(GetNonAvailableChampions()).First();
		}

		//Get the champion ban for draft game, if any
		private async Task<int> GetChampionBan()
		{
			var banFile = File.ReadAllText($"Picks/Ban.json");
			var bans = JObject.Parse(banFile)[position];

			if (!bans.Any()) return 0;

			return bans.Select(x => int.Parse(x.ToString()))
												.ToArray()
												.Except(GetNonAvailableChampions()).First();
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
