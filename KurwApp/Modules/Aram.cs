using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace KurwApp.Modules
{
	internal class Aram : Game
	{
		internal Aram(MainWindow mainWindow)
		{
			this.mainWindow = mainWindow;
		}

		//Handler of the champion selections
		protected internal override async Task ChampSelectControl()
		{
			sessionInfo = await Client_Request.GetSessionInfo();
			if (sessionInfo is null) return;

			mainWindow.SetGamemodeName("ARAM");
			mainWindow.SetGameModeIcon("ARAM");

			while (Auth.IsAuthSet())
			{
				sessionInfo = await Client_Request.GetSessionInfo();

				if (sessionInfo is null) return;
				switch (sessionInfo.Value<string>("timer.phase"))
				{
					default:
						break;

					case "FINALIZATION":
						await Finalization();
						break;

					case "GAME_STARTING":
					case "":
						mainWindow.EnableRandomSkinButton(false);
						return;
				}
				Thread.Sleep(1000);
			}
		}

		protected override async Task Finalization()
		{
			if (Client_Control.GetSettingState("aramChampionSwap"))
			{
				int aramPick = GetBenchChampionPick();

				if (aramPick != 0)
				{
					await Client_Request.AramBenchSwap(aramPick);
					isRunePageChanged = false;
					await PostPickAction();
					championId = await Client_Request.GetCurrentChampionId();
				}
			}
			var currentChampionId = await Client_Request.GetCurrentChampionId();

			if (currentChampionId != championId)
			{
				championId = currentChampionId;
				isRunePageChanged = false;
				await PostPickAction();
			}
		}

		//Get the pick the aram champion to pick if any
		protected int GetBenchChampionPick()
		{
			var aramPicks = JArray.Parse(File.ReadAllText($"Picks/ARAM.json"));

			if (!aramPicks.Any()) return 0;

			List<int> aramBenchIds = GetAramBenchIds();
			if (!aramBenchIds.Any()) return 0;

			foreach (var pick in aramPicks)
			{
				if (aramBenchIds.Contains(pick.Value<int>())) return pick.Value<int>();
			}
			return 0;
		}

		//Get the champion benched (their id) in aram
		private List<int> GetAramBenchIds()
		{
			return sessionInfo["benchChampions"].Select(x => int.Parse(x["championId"].ToString())).ToList();
		}

		protected override async Task ChangeSpells()
		{
			var runesRecommendation = await Client_Control.GetSpellsRecommendationByPosition(championId, "NONE");

			var spellsId = runesRecommendation.ToObject<int[]>();

			if (!spellsId.Contains(32)) spellsId[1] = 32;

			Client_Control.SetSummonerSpells(spellsId);
		}

		protected override async Task ChangeRunes()
		{
			bool isSetActive = (bool)Client_Control.GetPreference("runes.notSetActive");

			string activeRunesPage = isSetActive ? (await Client_Request.GetActiveRunePage())["id"].ToString() : "0";

			await Client_Control.SetRunesPage(championId, "NONE");

			isRunePageChanged = true;

			if (isSetActive) await Client_Request.SetActiveRunePage(activeRunesPage);
		}
	}
}