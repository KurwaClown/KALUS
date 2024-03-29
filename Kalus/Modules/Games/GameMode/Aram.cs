﻿using Kalus.Modules.Networking;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Kalus.UI.Windows;


namespace Kalus.Modules.Games.GameMode
{
	internal class Aram : Game
	{
		private int rerollsRemaining = 0;
		private delegate void OnRerollHandler();
		private event OnRerollHandler OnReroll;

		internal Aram(MainWindow mainWindow) : base(mainWindow)
		{
			mainWindow.consoleTab.AddLog(Properties.Logs.JoinedAram, UI.Controls.Tabs.Console.Utility.GAME, UI.Controls.Tabs.Console.LogLevel.INFO);

			OnReroll += LogReroll;
			OnReroll += ExecutePreferencesOnReroll;

			this.mainWindow.controlPanel.inventoryChange = this.ChangeInventoryBySelection;
		}

		//Handler of the champion selections
		protected internal override async Task ChampSelectControl()
		{
			sessionInfo = await ClientRequest.GetSessionInfo();
			if (sessionInfo == null) return;
			rerollsRemaining = (sessionInfo["rerollsRemaining"] ?? new JValue("0")).Value<int>();

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
						mainWindow.controlPanel.EnableChangeRuneCombobox(false);
						return;
				}
				Thread.Sleep(Properties.Settings.Default.checkInterval);
			}
		}

		protected override async Task Finalization()
		{

			int currentRerollsRemaining = sessionInfo!["rerollsRemaining"]!.Value<int>();
			if (currentRerollsRemaining < rerollsRemaining)
			{
				mainWindow.consoleTab.AddLog(Properties.Logs.RerollDetected, UI.Controls.Tabs.Console.Utility.REROLL, UI.Controls.Tabs.Console.LogLevel.INFO);
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

			if (Properties.Settings.Default.utilityAram)
			{
				int aramPick = GetBenchChampionPick();

				if (aramPick != 0)
				{
					await ClientRequest.AramBenchSwap(aramPick);
					mainWindow.consoleTab.AddLog($"{Properties.Logs.AramSwap} {await ChampionIdtoName(aramPick)}", UI.Controls.Tabs.Console.Utility.SWAPPING, UI.Controls.Tabs.Console.LogLevel.INFO);
					championId = aramPick;
					isRunePageChanged = false;
					await PostPickAction();
				}
			}

			bool tradeSent = sessionInfo?["trades"]?.Where(trade => trade?["state"]?.ToString() == "SENT").Any() ?? false;

			if (tradeSent) return;

			if (Properties.Settings.Default.aramTradeForChampion)
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
						mainWindow.consoleTab.AddLog(Properties.Logs.AramTradeRequest, UI.Controls.Tabs.Console.Utility.TRADE, UI.Controls.Tabs.Console.LogLevel.INFO);
						await ClientRequest.AramTradeRequest(tradeId.Value);
						return;
					}
				}
			}


			bool needToReroll = !await IsCurrentChampionInSelection();
			if (Properties.Settings.Default.aramRerollForChampion && rerollsRemaining != 0)
			{
				await ClientRequest.AramReroll();
				OnReroll.Invoke();
			}
		}

		private async void LogReroll()
		{
			int currentChampionId = await ClientRequest.GetCurrentChampionId();
			string championName = await ChampionIdtoName(currentChampionId);

			mainWindow.consoleTab.AddLog($"{Properties.Logs.AramReroll}{championName}", UI.Controls.Tabs.Console.Utility.REROLL, UI.Controls.Tabs.Console.LogLevel.INFO);
		}
		private async void ExecutePreferencesOnReroll()
		{

			int currentChampionId = await ClientRequest.GetCurrentChampionId();
			if (Properties.Settings.Default.aramRepickChampion)
			{
				var aramPicks = DataCache.GetAramPick();

				if (!aramPicks.Contains(currentChampionId))
				{
					await ClientRequest.AramBenchSwap(championId);
					mainWindow.consoleTab.AddLog(Properties.Logs.AramRetrieveChampion, UI.Controls.Tabs.Console.Utility.REROLL, UI.Controls.Tabs.Console.LogLevel.INFO);
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

		private static async Task<bool> IsCurrentChampionInSelection()
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

		protected override async Task ChangeSpells(int recommendationNumber = -1)
		{
			var runesRecommendation = recommendationNumber == -1 ? await SummonerSpells.GetSpellsRecommendationByPosition(championId, "NONE") : await SummonerSpells.GetSpellsRecommendationBySelectionIndex(championId, recommendationNumber);
			if (runesRecommendation == null) return;
			int[]? spellsId = runesRecommendation.ToObject<int[]>();

			if (spellsId == null) return;

			if (!spellsId.Contains(32) && Properties.Settings.Default.summonersAlwaysSnowball) spellsId[1] = 32;

			SummonerSpells.SetSummonerSpells(spellsId);
		}

		protected override async Task ChangeRunes(int recommendationNumber = -1)
		{
			if (recommendationNumber == -1)
				Runes.SetControlPanelRunesSelection(championId, mainWindow.controlPanel);

			bool isSetActive = Properties.Settings.Default.runesPageNotAsActive;

			string? activeRunesPage = isSetActive ? (await ClientRequest.GetActiveRunePage())?["id"]?.ToString() : "0";

			if (activeRunesPage == null) return;

			await Runes.SetRunesPage(championId, "NONE", recommendationNumber);

			isRunePageChanged = true;

			if (isSetActive) await ClientRequest.SetActiveRunePage(activeRunesPage);
		}
	}
}