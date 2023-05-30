using Kalus.Modules.Networking;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Kalus.Modules.Games.GameMode
{
	internal class Aram : Game
	{
		private int rerollsRemaining = 0;
		private delegate void OnRerollHandler();
		private event OnRerollHandler OnReroll;

		internal Aram(MainWindow mainWindow)
		{
			OnReroll += ExecutePreferencesOnReroll;
			this.mainWindow = mainWindow;

			this.mainWindow.runeChange = this.ChangeRunes;
		}

		//Handler of the champion selections
		protected internal override async Task ChampSelectControl()
		{
			sessionInfo = await ClientRequest.GetSessionInfo();
			if (sessionInfo is null) return;
			rerollsRemaining = sessionInfo["rerollsRemaining"]!.Value<int>();

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
						mainWindow.EnableChangeRuneButton(false);
						return;
				}
				Thread.Sleep(1000);
			}
		}

		protected override async Task Finalization()
		{
			int currentRerollsRemaining = sessionInfo!["rerollsRemaining"]!.Value<int>();
			Debug.WriteLine(currentRerollsRemaining < rerollsRemaining);
			if (currentRerollsRemaining < rerollsRemaining)
			{
				rerollsRemaining = currentRerollsRemaining;
				OnReroll.Invoke();
			}

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

			await ExecuteAramPreference();

			var currentChampionId = await ClientRequest.GetCurrentChampionId();
			if (currentChampionId != championId)
			{
				championId = currentChampionId;
				isRunePageChanged = false;
				await PostPickAction();
			}
		}

		private async void ExecutePreferencesOnReroll()
		{

			int currentChampionId = await ClientRequest.GetCurrentChampionId();
			if (ClientControl.GetPreference<bool>("aram.repickChampion"))
			{
				var aramPicks = DataCache.GetAramPick();


				if (!aramPicks.Contains(currentChampionId)) await ClientRequest.AramBenchSwap(championId);
			}
		}

		private async Task ExecuteAramPreference()
		{

			if (ClientControl.GetPreference<bool>("aram.rerollForChampion") && rerollsRemaining != 0)
			{
				await ClientRequest.AramReroll();
				rerollsRemaining--;
				OnReroll.Invoke();
			}


			if (ClientControl.GetPreference<bool>("aram.tradeForChampion"))
			{
				var aramPicks = DataCache.GetAramPick();

				var availableLikedChampion = sessionInfo!["myTeam"]!.Where(teammate => aramPicks.Contains(teammate.Value<int>("championId")))
																	.Select(teammate => teammate.Value<int>("cellId"))
																	.ToArray();
				var availableTrades = sessionInfo?["trades"]?.Where(trade => availableLikedChampion.Contains(trade["cellId"]?.Value<int>() ?? 0) && trade?["state"]?.ToString() == "AVAILABLE");

				if (availableTrades != null && availableTrades.Any())
				{
					await ClientRequest.AramTradeRequest(availableTrades.First().Value<int>("id"));
				}
			}

		}

		//Get the pick the aram champion to pick if any
		protected int GetBenchChampionPick()
		{
			var aramPicks = DataCache.GetAramPick();

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
			bool isSetActive = ClientControl.GetPreference<bool>("runes.notSetActive");

			string? activeRunesPage = isSetActive ? (await ClientRequest.GetActiveRunePage())?["id"]?.ToString() : "0";

			if (activeRunesPage == null) return;

			await ClientControl.SetRunesPage(championId, "NONE");

			isRunePageChanged = true;

			if (isSetActive) await ClientRequest.SetActiveRunePage(activeRunesPage);
		}
	}
}