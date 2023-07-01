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

		internal Aram(MainWindow mainWindow) : base(mainWindow)
		{
			mainWindow.consoleTab.AddLog("Joined ARAM Game", UI.Controls.Tabs.Console.Utility.GAME, UI.Controls.Tabs.Console.LogLevel.INFO);

			OnReroll += LogReroll;
			OnReroll += ExecutePreferencesOnReroll;

			this.mainWindow.controlPanel.runeChange = this.ChangeRunes;
		}

		//Handler of the champion selections
		protected internal override async Task ChampSelectControl()
		{
			sessionInfo = await ClientRequest.GetSessionInfo();
			if (sessionInfo == null) return;
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
				mainWindow.consoleTab.AddLog("User reroll detected", UI.Controls.Tabs.Console.Utility.REROLL, UI.Controls.Tabs.Console.LogLevel.INFO);
				rerollsRemaining = currentRerollsRemaining;
				OnReroll.Invoke();
			}

			var currentChampionId = await ClientRequest.GetCurrentChampionId();
			if (currentChampionId != championId)
			{
				championId = currentChampionId;
				mainWindow.consoleTab.AddLog($"Setting runes for {await ChampionIdtoName(championId)}", UI.Controls.Tabs.Console.Utility.RUNES, UI.Controls.Tabs.Console.LogLevel.INFO);

				isRunePageChanged = false;
				await PostPickAction();
			}

			if (ClientControl.GetSettingState("aramChampionSwap"))
			{
				int aramPick = GetBenchChampionPick();

				if (aramPick != 0)
				{
					await ClientRequest.AramBenchSwap(aramPick);
					mainWindow.consoleTab.AddLog($"Swapped with {await ChampionIdtoName(aramPick)}", UI.Controls.Tabs.Console.Utility.SWAPPING, UI.Controls.Tabs.Console.LogLevel.INFO);
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
						mainWindow.consoleTab.AddLog("Sending trade request", UI.Controls.Tabs.Console.Utility.TRADE, UI.Controls.Tabs.Console.LogLevel.INFO);
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

		private async void LogReroll()
		{
			int currentChampionId = await ClientRequest.GetCurrentChampionId();
			string championName = await ChampionIdtoName(currentChampionId);

			mainWindow.consoleTab.AddLog($"Rerolled to {championName}", UI.Controls.Tabs.Console.Utility.REROLL, UI.Controls.Tabs.Console.LogLevel.INFO);
		}
		private async void ExecutePreferencesOnReroll()
		{

			int currentChampionId = await ClientRequest.GetCurrentChampionId();
			if (ClientControl.GetPreference<bool>("aram.repickChampion"))
			{
				var aramPicks = DataCache.GetAramPick();

				if (!aramPicks.Contains(currentChampionId))
				{
					await ClientRequest.AramBenchSwap(championId);
					mainWindow.consoleTab.AddLog($"Reroll was not better than current : picking back", UI.Controls.Tabs.Console.Utility.REROLL, UI.Controls.Tabs.Console.LogLevel.INFO);
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
				if (pick == championId) return 0;
				if (aramBenchIds.Contains(pick)) return pick;
			}
			return 0;
		}

		private async Task<bool> IsCurrentChampionInSelection()
		{
			int championId = await ClientRequest.GetCurrentChampionId();

			return DataCache.GetAramPick().Contains(championId);
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