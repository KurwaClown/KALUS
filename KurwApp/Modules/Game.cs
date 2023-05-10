using League;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;
using System.Security.AccessControl;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace KurwApp.Modules
{
    class Game
    {
        private string cellId;
        private string position;
        private string phase;
		private string gameType;

		private MainWindow mainWindow;

        public Game(MainWindow mainWindow) {
            this.mainWindow = mainWindow;
		}



		internal async void SetRoleAndCellId()
		{
			JObject sessionInfo = JObject.Parse(await Client_Request.GetSessionInfo());
			cellId = sessionInfo["localPlayerCellId"].ToString();
			position = sessionInfo["myTeam"].Where(player => player["cellId"].ToString() == cellId).Select(player => player["assignedPosition"].ToString()).First();
		}

		internal async void SetGameType()
		{
			JObject lobbyInfo = JObject.Parse(await Client_Request.GetLobbyInfo());
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

		internal async Task<int[]> GetNonAvailableChampions()
		{
			var sessionAction = await GetSessionActions(await Client_Request.GetSessionInfo());
			return sessionAction.Where(action => (bool)action["completed"] && (string)action["type"] != "ten_bans_reveal").Select(action => (int)action["championId"]).ToArray();

		}

		internal async Task<string> GetChampSelectPhase()
		{
			var session_timer = await Client_Request.GetSessionTimer();
			if (session_timer == "")
			{
				return "";
			}
			JObject champ_select_timer = JObject.Parse(await Client_Request.GetSessionTimer());
			
			return champ_select_timer["phase"].ToString().ToUpper();
		}



		internal async void ChampSelectControl()
        {
			SetRoleAndCellId();
			SetGameType();
			int i = 0;
			do
			{
				switch (await GetChampSelectPhase())
				{
					default:
					case "FINALIZATION":

						if (gameType == "ARAM" && Client_Control.GetSettingState("aramChampionSwap"))
						{
							int aramPick = await GetAramPick();

							if (aramPick != 0)await Client_Request.AramBenchSwap(aramPick);
						}


						var championId = await Client_Request.GetCurrentChampionId();
						mainWindow.ChangeCharacterIcon(await Client_Request.GetChampionImageById(championId));
						mainWindow.EnableRandomSkinButton(true);
						if(Client_Control.GetSettingState("runesSwap")) Client_Control.SetRunesPage(await Client_Request.GetCurrentChampionId(), position == "" ? "NONE" : position.ToUpper() );
						break;
					case "BAN_PICK":
						string sessionInfo = await Client_Request.GetSessionInfo();
						var actions = await GetSessionActions(sessionInfo);
						
						if (IsCurrentPlayerTurn(actions, out int actionId, out string type))
						{
							if (type == "ban" && Client_Control.GetSettingState("banPick"))
							{
								int banPick = await GetChampionBan();
								if (banPick == 0) continue;

								await Client_Request.SelectChampion(actionId, banPick);
								await Client_Request.ConfirmAction(actionId);

							}

							if (type == "pick" && Client_Control.GetSettingState("championPick"))
							{
								int champPick = await GetChampionPick();
								if(champPick == 0) continue;

								await Client_Request.SelectChampion(actionId, champPick);
								await Client_Request.ConfirmAction(actionId);

								var imageBytes = await Client_Request.GetChampionImageById(champPick);

								mainWindow.ChangeCharacterIcon(imageBytes);
							}
						}
						break;
					case "GAME_STARTING":
					case "":
						mainWindow.EnableRandomSkinButton(false);
						return;
					
				}
				mainWindow.ChangeTest(i++.ToString());
				Thread.Sleep(3000);
			} while (true);
		}

		private async Task<int> GetAramPick()
		{
			var filename = "ARAM.json";
			var aramPicks = JArray.Parse($"Picks/{filename}");

			if (!aramPicks.Any()) return 0;

			int[] aramBenchIds = await GetAramBenchIds();
			return aramPicks.Select(x => int.Parse(x.ToString()))
												.ToArray()
												.Where(x => aramBenchIds.Contains(x)).First();
		}

		private async Task<int[]> GetAramBenchIds()
		{

			string sessionInfo = await Client_Request.GetSessionInfo();
			var benchChampions = JObject.Parse(sessionInfo)["benchChampionIds"].Select(x => int.Parse(x.ToString())).ToArray();

			return benchChampions;
		}

		private async Task<int> GetChampionPick()
		{
			var filename = gameType == "Draft" ? "Pick.json" : $"{gameType}.json";
			var pickFile = File.ReadAllText($"Picks/{filename}");
			var picks = gameType == "Draft" ? JObject.Parse(pickFile)[position] : JArray.Parse(pickFile);

			if (!picks.Any()) return 0;

			return picks.Select(x => int.Parse(x.ToString()))
												.ToArray()
												.Except(await GetNonAvailableChampions()).First();
		}

		private async Task<int> GetChampionBan()
		{
			var banFile = File.ReadAllText($"Picks/Ban.json");
			var bans = JArray.Parse(banFile);

			if (!bans.Any()) return 0;

			return bans.Select(x => int.Parse(x.ToString()))
												.ToArray()
												.Except(await GetNonAvailableChampions()).First();
		}

		internal bool IsCurrentPlayerTurn(IEnumerable<JObject> actions, out int actionId, out string type)
		{


			var currentPlayerAction = actions.Where(action => action["actorCellId"].ToString() == cellId && (bool)action["isInProgress"] == true)
				.Select(action => action).ToArray();

			if (currentPlayerAction.Any())
			{
				actionId = (int)currentPlayerAction.First()["id"];
				type = currentPlayerAction.First()["type"].ToString();
				return true;
			}
			actionId = 0;
			type = string.Empty;
			return false;

		}

		internal async Task<IEnumerable<JObject>> GetSessionActions(string sessionInfo)
		{
			return JObject.Parse(sessionInfo)["actions"].SelectMany(innerArray => innerArray).OfType<JObject>();
		}
	}
}
