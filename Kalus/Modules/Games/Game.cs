using Kalus.Modules.Networking;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Kalus.Modules.Games
{
	public abstract class Game
	{
		protected int championId = 0;

		protected JObject? sessionInfo;

		protected bool isRunePageChanged = false;

		protected MainWindow? mainWindow;

		internal static async Task<Game?> CreateGame(MainWindow mainWindow)
		{
			JObject? lobbyInfo = await ClientRequest.GetLobbyInfo();
			if (lobbyInfo is null) return null;
			string? gameMode = lobbyInfo.SelectToken("gameConfig.gameMode")?.ToString();
			if (gameMode == null) return null;
			bool hasPositions = lobbyInfo.Value<bool>("gameConfig.showPositionSelector");

			//if the gamemode is aram set to ARAM
			if (gameMode == "ARAM") return new GameMode.Aram(mainWindow);

			//if it's not aram and has positions set to Draft
			if (hasPositions) return new GameMode.Classic(mainWindow, "Draft");

			return new GameMode.Classic(mainWindow, "Blind");
		}

		//Handler of the champion selections
		protected internal abstract Task ChampSelectControl();

		//Act on finalization
		protected abstract Task Finalization();

		protected abstract Task ChangeSpells();

		protected abstract Task ChangeRunes();

		protected async Task PostPickAction()
		{
			var imageBytes = await ClientRequest.GetChampionImageById(championId);

			var champions = await DataCache.GetChampionsInformations();

			string? championName = champions.Where(champion => champion.Value<int>("id") == championId).Select(champion => champion["name"]?.ToString()).FirstOrDefault();

			if (championName == null) return;

			if (mainWindow == null) return;

			//Set the current champion image and name on the UI
			mainWindow.SetChampionIcon(imageBytes);
			mainWindow.SetChampionName(championName);
			//Toggle the random skin button on
			mainWindow.EnableRandomSkinButton(true);

			//Set runes if the the auto rune is toggled
			if (ClientControl.GetSettingState("runesSwap") && !isRunePageChanged)
			{
				await ChangeRunes();
			}

			//Random skin on pick
			bool randomSkinOnPick = ClientControl.GetPreference<bool>("randomSkin.randomOnPick");
			if (randomSkinOnPick)
			{
				ClientControl.PickRandomSkin();
			}

			if (ClientControl.GetSettingState("autoSummoner")) await ChangeSpells();
		}

		//Get sessions actions
		internal IEnumerable<JObject>? GetSessionActions()
		{
			return sessionInfo?["actions"]?.SelectMany(innerArray => innerArray).OfType<JObject>();
		}
	}
}