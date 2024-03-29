﻿using Kalus.Modules.Networking;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Kalus.UI.Windows;

namespace Kalus.Modules.Games
{
	public abstract class Game
	{
		protected int championId = 0;

		protected JObject? sessionInfo;

		protected bool isRunePageChanged = false;

		protected MainWindow mainWindow;


		internal Game(MainWindow mainWindow) {
			this.mainWindow = mainWindow;
		}

		internal static async Task<Game?> CreateGame(MainWindow mainWindow)
		{
			JObject? session = await ClientRequest.GetClientSession();
			if (session is null) return null;

			string? gameMode = session.SelectToken("map.gameMode")?.ToString();

			if (gameMode == null) return DetermineGameMode(session, mainWindow);

			//if the gamemode is aram set to ARAM
			if (gameMode == "ARAM") return new GameMode.Aram(mainWindow);

			//if it's classic, we are determine if it's a blind or a draft gametype
			if (gameMode == "CLASSIC")
			{
				string? gameType = session.Value<string>("gameData.queue.gameTypeConfig.name");
				if (gameType == null) return DetermineGameMode(session, mainWindow);

				if (gameType == "GAME_CFG_TEAM_BUILDER_DRAFT") return new GameMode.Classic(mainWindow, "Draft");
				else if (gameType == "GAME_CFG_TEAM_BUILDER_BLIND") return new GameMode.Classic(mainWindow, "Blind");
			}

			if (gameMode == "TFT") return null;
			return DetermineGameMode(session, mainWindow);
		}

		// Used if the game mode cannot be created by solely checking the gamemode and gametype
		internal static Game? DetermineGameMode(JObject session, MainWindow mainWindow)
		{
			return session.Value<int>("gameData.queue.id") switch
			{
				//Refers to draft, ranked solo/due, ranked flex ids
				400 or 420 or 440 => new GameMode.Classic(mainWindow, "Draft"),
				//Refers to aram gamemode id
				450 => new GameMode.Aram(mainWindow),
				//Refers to tft gamemodes ids
				1090 or 1100 or 1130 or 1160 => null,
				_ => new GameMode.Classic(mainWindow, "Blind"),
			};
		}

		//Handler of the champion selections
		protected internal abstract Task ChampSelectControl();

		//Act on finalization
		protected abstract Task Finalization();

		protected async Task ChangeInventoryBySelection(int recommendationNumber)
		{
			mainWindow.consoleTab.AddLog(Properties.Logs.ChangingInventory, UI.Controls.Tabs.Console.Utility.POSTPICK, UI.Controls.Tabs.Console.LogLevel.INFO);


			await ChangeRunes(recommendationNumber);

			if (Properties.Settings.Default.setSummonerOnSelection)
			{
				await ChangeSpells(recommendationNumber);
			}

		}

		protected abstract Task ChangeSpells(int recommendationNumber = -1);

		protected abstract Task ChangeRunes(int recommendationNumber = -1);

		protected async Task PostPickAction()
		{
			List<string> logMessages = new List<string>();
			var imageBytes = await ClientRequest.GetChampionImageById(championId);

			var champions = await DataCache.GetChampionsInformations();

			string? championName = champions.Where(champion => champion.Value<int>("id") == championId).Select(champion => champion["name"]?.ToString()).FirstOrDefault();

			if (championName == null) return;

			if (mainWindow == null) return;

			//Set the current champion image and name on the UI
			mainWindow.controlPanel.SetChampionIcon(imageBytes);
			mainWindow.controlPanel.SetChampionName(championName);
			//Toggle the random skin button on
			mainWindow.controlPanel.EnableRandomSkinButton(true);
			mainWindow.controlPanel.EnableChangeRuneCombobox(true);

			//Random skin on pick
			if (Properties.Settings.Default.randomSkinOnPick)
			{
				ClientControl.PickRandomSkin();
				logMessages.Add(Properties.Logs.RandomSkin);
			}

			//Set runes if the the auto rune is toggled
			if (Properties.Settings.Default.utilityRunes && !isRunePageChanged)
			{
				await ChangeRunes();
				logMessages.Add(Properties.Logs.SettingRunes);
			}

			if (Properties.Settings.Default.utilitySummoners)
			{
				await ChangeSpells();
				logMessages.Add(Properties.Logs.SettingSummoners);
			}

			string concatLog = $"{string.Join(" | ", logMessages)} {Properties.Logs.PostPickEnd} {await ChampionIdtoName(championId)}";

			mainWindow.consoleTab.AddLog(concatLog, UI.Controls.Tabs.Console.Utility.POSTPICK , UI.Controls.Tabs.Console.LogLevel.INFO);

		}


		//Get sessions actions
		internal IEnumerable<JObject>? GetSessionActions()
		{
			return sessionInfo?["actions"]?.SelectMany(innerArray => innerArray).OfType<JObject>();
		}

		internal async Task<string> ChampionIdtoName(int championId)
		{
			var champions = await DataCache.GetChampionsInformations();
			return champions.Where(champion => champion.Value<int>("id") == championId)
											.Select(champion => champion["name"]?.ToString())
											.FirstOrDefault()
											?? "undefined";
		}
	}
}