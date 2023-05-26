using Kalus.Modules.Networking;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Kalus.Modules.Games.GameMode
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
			sessionInfo = await ClientRequest.GetSessionInfo();
			if (sessionInfo is null) return;

			if (mainWindow == null) return;

			mainWindow.SetGamemodeName("ARAM");
			mainWindow.SetGameModeIcon("ARAM");

			while (Auth.IsAuthSet())
			{
				sessionInfo = await ClientRequest.GetSessionInfo();
				if (sessionInfo == null) return;
				switch (sessionInfo.SelectToken("timer.phase")?.ToString())
				{
					default:
						break;

					case null:
						return;

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
			if (ClientControl.GetSettingState("aramChampionSwap"))
			{
				int aramPick = GetBenchChampionPick();

				if (aramPick != 0)
				{
					await ClientRequest.AramBenchSwap(aramPick);
					championId = aramPick;
					isRunePageChanged = false;
					await PostPickAction();
				}
			}
			var currentChampionId = await ClientRequest.GetCurrentChampionId();
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
			var aramPicks = DataCache.GetAramPick();

			if (aramPicks == null) return 0;
			if (!aramPicks.Any()) return 0;

			List<int>? aramBenchIds = GetAramBenchIds();

			if (aramBenchIds == null) return 0;
			if (!aramBenchIds.Any()) return 0;

			foreach (var pick in aramPicks)
			{
				if (aramBenchIds.Contains(pick)) return pick;
			}
			return 0;
		}

		//Get the champion benched (their id) in aram
		private List<int>? GetAramBenchIds()
		{
			return sessionInfo?["benchChampions"]?.Select(x => int.TryParse(x?["championId"]?.ToString(), out int championId) ? championId : (int?)null)
													.Where(championId => championId.HasValue)
													.Select(championId => championId!.Value)
													.ToList();
		}

		protected override async Task ChangeSpells()
		{
			var runesRecommendation = await ClientControl.GetSpellsRecommendationByPosition(championId, "NONE");
			if (runesRecommendation == null) return;
			int[]? spellsId = runesRecommendation.ToObject<int[]>();

			if (spellsId == null) return;

			if (!spellsId.Contains(32)) spellsId[1] = 32;

			ClientControl.SetSummonerSpells(spellsId);
		}

		protected override async Task ChangeRunes()
		{
			JToken? preference = ClientControl.GetPreference("runes.notSetActive");
			bool isSetActive = (preference != null) && (bool)preference;

			string? activeRunesPage = isSetActive ? (await ClientRequest.GetActiveRunePage())?["id"]?.ToString() : "0";

			if (activeRunesPage == null) return;

			await ClientControl.SetRunesPage(championId, "NONE");

			isRunePageChanged = true;

			if (isSetActive) await ClientRequest.SetActiveRunePage(activeRunesPage);
		}
	}
}