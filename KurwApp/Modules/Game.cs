using League;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.AccessControl;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace KurwApp.Modules
{
    class Game
    {
        private string cellId;
        private string role;
        private string phase;
		private int champPick;
		private JToken[] availableChampions;

		private MainWindow mainWindow;

        public Game(MainWindow mainWindow) {
            this.mainWindow = mainWindow;
		}

        internal async void SetCurrentRole()
        {
			JObject sessionInfo = JObject.Parse(await Client_Request.GetSessionInfo());
            role = sessionInfo["myTeam"].Where(player => player["summonerId"].ToString() == Client_Control.summonerId)
                .Select(player => player["assignedPosition"].ToString()).First();
		}


		internal async void SetCellId()
		{
			JObject sessionInfo = JObject.Parse(await Client_Request.GetSessionInfo());
			cellId = sessionInfo["localPlayerCellId"].ToString();
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
			SetCurrentRole();

			do
			{
				switch (await GetChampSelectPhase())
				{
					default:
						mainWindow.ChangeCharacterIcon(reset: true);
						break;
					case "FINALIZATION":

						mainWindow.EnableRandomSkinButton(true);
						Client_Control.SetRunesPage(await Client_Request.GetCurrentChampionId(), mainWindow);

						//CHANGING THE ID


						break;
					//case "BAN_PICK":
					//	string sessionInfo = await Client_Request.GetSessionInfo();
					//	var actions = await GetSessionActions(sessionInfo);
					//	if (IsCurrentPlayerTurn(actions, out int actionId, out string type))
					//	{
					//		if (type == "ban" && (bool)mainWindow.autoBanSetting.IsChecked)
					//		{
					//			await Client_Request.SelectChampion(actionId, 140);
					//			await Client_Request.ConfirmAction(actionId);
								
					//		}

					//		if (type == "pick" && (bool)mainWindow.autoPickSetting.IsChecked)
					//		{
					//			champPick = 32;
					//			await Client_Request.SelectChampion(actionId, champPick);
					//			await Client_Request.ConfirmAction(actionId);

					//			var imageBytes = await Client_Request.GetChampionImageById(champPick);

					//			mainWindow.ChangeCharacterIcon(imageBytes);

					//		}
					//	}
					//	break;
					case "GAME_STARTING":
					case "":
						mainWindow.EnableRandomSkinButton(false);
						return;
					
				} 
				Thread.Sleep(3000);
			} while (true);
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
