using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KurwApp.Modules
{
	public abstract class Game
	{
		protected int championId = 0;

		protected JObject? sessionInfo;

		protected bool isRunePageChanged = false;

		protected MainWindow mainWindow;

		internal static async Task<Game?> CreateGame(MainWindow mainWindow)
		{
			JObject? lobbyInfo = await Client_Request.GetLobbyInfo();
			if (lobbyInfo is null) return null;
			string gameMode = lobbyInfo["gameConfig"]["gameMode"].ToString();
			bool hasPositions = (bool)lobbyInfo["gameConfig"]["showPositionSelector"];

			//if the gamemode is aram set to ARAM
			if (gameMode == "ARAM") return new Aram(mainWindow);

			//if it's not aram and has positions set to Draft
			if (hasPositions) return new Classic(mainWindow, "Draft");

			return new Classic(mainWindow, "Blind");
		}

		//Handler of the champion selections
		protected internal abstract Task ChampSelectControl();

		//Act on finalization
		protected abstract Task Finalization();

		protected abstract Task ChangeSpells();

		protected abstract Task ChangeRunes();

		protected async Task PostPickAction()
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
				await ChangeRunes();
			}

			//Random skin on pick
			if ((bool)Client_Control.GetPreference("randomSkin.randomOnPick")) Client_Control.PickRandomSkin();

			if (Client_Control.GetSettingState("autoSummoner")) await ChangeSpells();
		}

		//Get sessions actions
		internal IEnumerable<JObject> GetSessionActions()
		{
			return sessionInfo["actions"].SelectMany(innerArray => innerArray).OfType<JObject>();
		}
	}
}