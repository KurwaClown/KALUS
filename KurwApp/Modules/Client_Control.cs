using System.Threading;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Serialization;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Http;
using System.Net;
using System.IO;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using KurwApp.Modules;
using System.Windows.Controls;
using KurwApp;
using System.Windows.Media;

namespace League
{
	internal static class Client_Control
	{
		internal static string summonerId = string.Empty;

		internal static Random random = new();

		private static bool isClientOpen = false;

		internal static async void EnsureAuthentication(KurwApp.MainWindow mainWindow)
		{
			do
			{
				
				bool authenticated = Auth.IsAuthSet();
				isClientOpen = IsClientOpen();

				if (!isClientOpen)
				{
					mainWindow.ShowLolState(false);
					SetSummonerId(isReset: true);
					if(authenticated) Auth.ResetAuth();
					continue;
				}

				if (!authenticated)
				{
					Auth.SetBasicAuth(Process.GetProcessesByName("LeagueClientUx").First().MainModule.FileName);
					SetSummonerId();
					mainWindow.ShowLolState(true);
					//mainWindow.LoadAndSetCharacterList();
				}
				Thread.Sleep(5000);
			} while (true);
		}

		internal static bool IsClientOpen()
		{
			return Process.GetProcessesByName("LeagueClientUx").Length > 0;
		}

		internal static async void ClientPhase(KurwApp.MainWindow mainWindow)
		{
			
			while (true)
			{
				if (Auth.IsAuthSet())
				{
					string client_phase = await Client_Request.GetClientPhase();
					switch (client_phase)
					{
						default:
							mainWindow.ChangeCharacterIcon(reset: true);
							break;
						case "ReadyCheck":
							if (mainWindow.IsCheckboxChecked(mainWindow.autoReadySetting)) await Client_Request.Accept();
							break;
						case "ChampSelect":
							var game = new Game(mainWindow);
							Task gameTask = Task.Run(() => game.ChampSelectControl());
							await gameTask;
							break;
					}
				}
				Thread.Sleep(5000);
			}
		}

		internal static void SetPreferences(MainWindow mainWindow)
		{
			var preferences = JObject.Parse(File.ReadAllText("Configurations/preferences.json"));

			Action<StackPanel, string> setRadioByPreference = (stack, token) =>
			{
				stack.Children.OfType<RadioButton>()
					.Where(child => child.Tag.ToString() == preferences[token]["userPreference"].ToString()).First().IsChecked = true;
				var comboboxes = stack.Children.OfType<ComboBox>();
				if (comboboxes.Any())
				{
					comboboxes.First().SelectedIndex = comboboxes.First().IsEnabled ? (int)preferences[token]["OTLTimeIndex"] : -1;
				}
			};


			mainWindow.Dispatcher.Invoke(() => {

				setRadioByPreference(mainWindow.picksPreferences, "picks");
				setRadioByPreference(mainWindow.bansPreferences, "bans");
				setRadioByPreference(mainWindow.noAvailablePreferences, "noPicks");
				setRadioByPreference(mainWindow.onSelectionPreferences, "selections");

				if (mainWindow.stillAutoPickOTL.IsEnabled) { mainWindow.stillAutoPickOTL.IsChecked = (bool)preferences["selections"]["OTL"]; }

				mainWindow.setPageAsActive.IsChecked = (bool)preferences["runes"]["setActive"];
				mainWindow.overridePage.IsChecked = (bool)preferences["runes"]["overridePage"];

				mainWindow.addChromas.IsChecked = (bool)preferences["randomSkin"]["addChromas"];
				mainWindow.randomOnPick.IsChecked = (bool)preferences["randomSkin"]["randomOnPick"];

				mainWindow.rightSideFlash.IsChecked = (bool)preferences["summoners"]["rightSideFlash"];
				mainWindow.alwaysSnowball.IsChecked = (bool)preferences["summoners"]["alwaysSnowball"];

			}
			);
		}

		internal static void SetSettings(MainWindow mainWindow)
		{
			var settings = JObject.Parse(File.ReadAllText("Configurations/settings.json"));

			mainWindow.Dispatcher.Invoke(() => {
				mainWindow.autoPickSetting.IsChecked = (bool)settings["championPick"];
				mainWindow.autoBanSetting.IsChecked = (bool)settings["banPick"];
				mainWindow.autoReadySetting.IsChecked = (bool)settings["aramChampionSwap"];
				mainWindow.autoRunesSetting.IsChecked = (bool)settings["runesSwap"];
				mainWindow.autoSwapSetting.IsChecked = (bool)settings["autoReady"];
				

			}
			);
		}


		internal static async void SetSummonerId(bool isReset = false)
		{
			if (isReset)
			{
				summonerId = string.Empty;
				return;
			}
			var summonerInfo = JObject.Parse(await Client_Request.GetCurrentSummonerInfo());
			summonerId = summonerInfo["summonerId"].ToString();
		}


		internal static async void PickRandomSkin()
		{
			int[] skin_ids = await GetAvailableSkinsID();
			if (skin_ids.Any())
			{
				int random_skin_index = random.Next(skin_ids.Length);
				await Client_Request.ChangeSkinByID(skin_ids[random_skin_index]);
			}
		}

		internal static async Task<string> GetChampSelectPhase()
		{
			JObject champ_select_timer = JObject.Parse(await Client_Request.GetSessionTimer());
			return champ_select_timer["phase"].ToString();
		}

		internal static async Task<int> GetCellId()
		{
			JObject sessionInfo = JObject.Parse(await Client_Request.GetSessionInfo());
			JArray myTeam = sessionInfo["myTeam"] as JArray;
			//Return the first cellId (player position in the session) that match our summonerId
			return (int)sessionInfo["localPlayerCellId"];
		}

		internal static async Task<int[]> GetAvailableSkinsID()
		{
			JArray current_champion_skins = JArray.Parse(await Client_Request.GetCurrentChampionSkins());
			return current_champion_skins.Where(j => (bool)j["unlocked"]).Select(j => (int)j["id"]).ToArray();
		}

		internal static bool IsCurrentPlayerTurn(IEnumerable<JObject> actions, int cellId, out int actionId, out string type)
		{

			
			var currentPlayerAction = actions.Where(action => (int)action["actorCellId"] == cellId && (bool)action["isInProgress"] == true)
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

		internal static async Task<IEnumerable<JObject>> GetSessionActions(string sessionInfo)
		{
			return JObject.Parse(sessionInfo)["actions"].SelectMany(innerArray => innerArray).OfType<JObject>();
		}

		internal static async Task<JArray> GetRecommendedRunesById(int champId)
		{
			var runesRecommendation = JArray.Parse(await Client_Request.GetRecommendedRunes());
			
			JArray champRunes = runesRecommendation
				.Where(obj => (int)obj["championId"] == champId)
				.Select(obj => (JArray)obj["runeRecommendations"]).First();
			return new JArray(champRunes);
		}
		
		internal static async Task<JToken> GetChampRunesByPosition(int champId, string position = "NONE")
		{
			var runesRecommendation = await GetRecommendedRunesById(champId);

			var champRunesByPosition = runesRecommendation.Where(recommendation => (string)recommendation["position"] == position.ToUpper()).Select(recommendation => recommendation);
			if (!champRunesByPosition.Any())
			{
				champRunesByPosition = runesRecommendation.Where(recommendation => recommendation["position"].ToString() == "NONE").Select(recommendation => recommendation);
			}
				return champRunesByPosition.FirstOrDefault();
		}

		internal static async Task<string> FormatChampRunes(JToken runes)
		{
			string runesTemplate = "{\"current\": true,\"name\": \"Kurwapp\",\"primaryStyleId\": 0,\"subStyleId\": 0, \"selectedPerkIds\": []}";
			JObject runesObject = JObject.Parse(runesTemplate);
			runesObject["primaryStyleId"] = runes["primaryPerkStyleId"];
			runesObject["subStyleId"] = runes["secondaryPerkStyleId"];
			runesObject["selectedPerkIds"] = runes["perkIds"];
			return runesObject.ToString();
		}

		internal static async Task<int> GetAppRunePageId()
		{
			var pages = await Client_Request.GetRunePages();
			var kurwappRunes = JArray.Parse(pages).Where(page => page["name"].ToString() == "Kurwapp");
			return kurwappRunes.Any() ? kurwappRunes.Select(page => (int)page["id"]).First() : 0;
		}

		internal static async Task<bool> CanCreateNewPage()
		{
			var inventory = JObject.Parse(await Client_Request.GetRunesInventory());
			return (bool)inventory["canAddCustomPage"];
		}

		internal static async Task<JToken[]> GetAvailablePicks()
		{

			var actions = await GetSessionActions(await Client_Request.GetSessionInfo());
			var availableChampion = JArray.Parse(await Client_Request.GetAvailableChampions());
			var nonAvailableChampion = (JArray)actions.Where(action => (bool)action["completed"] == true && (string)action["type"] != "ten_bans_reveal").Select(action => action["championId"]);
			return availableChampion.Where(championId => nonAvailableChampion.Contains(championId)).ToArray();
		}

		internal static async void SetRunesPage(int champId, MainWindow mainWindow,string position = "NONE")
		{
			var appPageId = await GetAppRunePageId();
			mainWindow.ChangeTest(appPageId.ToString());
			string newRunesPage = await FormatChampRunes(await GetChampRunesByPosition(champId));

			if(appPageId == 0 && await CanCreateNewPage())
			{
				await Client_Request.CreateNewRunePage(newRunesPage);
			}
			else
			{
				await Client_Request.EditRunePage(appPageId, newRunesPage);
			}
		}
	}
}
