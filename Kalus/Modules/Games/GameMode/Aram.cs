using Kalus.Modules.Networking;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Kalus.UI.Views;


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

			this.mainWindow.controlPanel.runeChange = this.ChangeRunes;
		}

		//Handler of the champion selections
		protected internal override async Task ChampSelectControl()
		{
			sessionInfo = await ClientRequest.GetSessionInfo();
			if (sessionInfo is null) return;
			rerollsRemaining = sessionInfo["rerollsRemaining"]!.Value<int>();

			if (mainWindow == null) return;
			mainWindow.controlPanel.SetGamemodeName("ARAM");
			mainWindow.controlPanel.SetGameModeIcon("ARAM");

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
						mainWindow.controlPanel.EnableRandomSkinButton(false);
						mainWindow.controlPanel.EnableChangeRuneButtons(false);
						return;
				}
				Thread.Sleep(ClientControl.checkInterval);
			}
		}

		protected override async Task Finalization()
		{

			int currentRerollsRemaining = sessionInfo!["rerollsRemaining"]!.Value<int>();
			if (currentRerollsRemaining < rerollsRemaining)
			{
				rerollsRemaining = currentRerollsRemaining;
				OnReroll.Invoke();
			}

			var currentChampionId = await ClientRequest.GetCurrentChampionId();
			if (currentChampionId != championId)
			{
				championId = currentChampionId;
				isRunePageChanged = false;
				await PostPickAction();
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


			bool tradeSent = sessionInfo?["trades"]?.Where(trade => trade?["state"]?.ToString() == "SENT").Any() ?? false;

			if (tradeSent) return;

			if (ClientControl.GetPreference<bool>("aram.tradeForChampion"))
			{
				var aramPicks = DataCache.GetAramPick();

				var availableTrades = sessionInfo?["trades"]?
											.Where(trade => trade?["state"]?.ToString() == "AVAILABLE")
											.ToList();

				if (availableTrades != null && availableTrades.Any())
				{
					var wantedTrades = sessionInfo!["myTeam"]!.Where(teammate => availableTrades.Any(trade => trade["cellId"]?.Value<int>() == teammate.Value<int>("cellId"))
																	&& aramPicks.Contains(teammate.Value<int>("championId")));

                    if (Array.IndexOf(aramPicks, championId) != -1) wantedTrades = wantedTrades.Where(teammate => Array.IndexOf(aramPicks, teammate.Value<int>("championId")) < Array.IndexOf(aramPicks, championId));

                    int? tradeId = wantedTrades.OrderBy(teammate => Array.IndexOf(aramPicks, teammate.Value<int>("championId")))
												.Select(teammate => availableTrades.FirstOrDefault(trade => trade["cellId"]?.Value<int>() == teammate.Value<int>("cellId"))?.Value<int>("id"))
												.FirstOrDefault();
					if (tradeId.HasValue && tradeId != 0)
					{
						await ClientRequest.AramTradeRequest(tradeId.Value);
						return;
					}
				}
			}


			bool needToReroll = !await IsCurrentChampionInSelection();
			if (needToReroll && ClientControl.GetPreference<bool>("aram.rerollForChampion") && rerollsRemaining != 0)
			{
				await ClientRequest.AramReroll();
				OnReroll.Invoke();
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

		//Get the pick the aram champion to pick if any
		protected int GetBenchChampionPick()
		{
			var aramPicks = DataCache.GetAramPick();

			List<int>? aramBenchIds = GetAramBenchIds();

			if (aramBenchIds == null) return 0;
			if (!aramBenchIds.Any()) return 0;

			foreach (var pick in aramPicks)
			{
				if (pick == championId) return 0;
				if (aramBenchIds.Contains(pick)) return pick;
			}
			return 0;
		}

		private async Task<bool> IsCurrentChampionInSelection()
		{
			int championId = await ClientRequest.GetCurrentChampionId();

			if (DataCache.GetAramPick().Contains(championId))
			{
				return true;
			}

			return false;
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

		protected override async Task ChangeRunes(int recommendationNumber = 0)
		{
			bool isSetActive = ClientControl.GetPreference<bool>("runes.notSetActive");

			string? activeRunesPage = isSetActive ? (await ClientRequest.GetActiveRunePage())?["id"]?.ToString() : "0";

			if (activeRunesPage == null) return;

			await ClientControl.SetRunesPage(championId, "NONE", recommendationNumber);

			isRunePageChanged = true;

			if (isSetActive) await ClientRequest.SetActiveRunePage(activeRunesPage);
		}
	}
}