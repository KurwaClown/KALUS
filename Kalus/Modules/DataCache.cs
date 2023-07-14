﻿using Kalus.Modules.Networking;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Kalus.Modules
{
	internal static class DataCache
	{
		private static readonly string settingsPath = "Configurations/settings.json";
		private static readonly JObject settings = JObject.Parse(File.ReadAllText(settingsPath));

		private static readonly string pickBanPath = "Picks/PickBan.json";
		private static readonly JObject pickBan = InitializePickBan();

		private static JArray? championsInformation;
		private static JArray? championsRunesRecommendation;
		private static JArray? runesStyleInformation;

		private static string? appRunePageId;

		private static byte[]? defaultChampionIcon;
		private static byte[]? defaultRuneIcon;
		private static byte[]? defaultMapIcon;

		private static byte[]? ingameClassicIcon;
		private static byte[]? champSelectClassicIcon;

		private static byte[]? ingameAramIcon;
		private static byte[]? champSelectAramIcon;

		#region Files

		internal static JObject InitializePickBan()
		{
			try
			{
				return JObject.Parse(File.ReadAllText(pickBanPath));
			}
			catch (FileNotFoundException)
			{
				var pickBan = new JObject();

				pickBan["Draft"] ??= new JObject();  // Initialize "Draft" if it is null

				pickBan["Draft"]!["Pick"] ??= new JObject();  // Initialize "Ban" if it is null

				pickBan["Draft"]!["Ban"] ??= new JObject();  // Initialize "Pick" if it is null

				pickBan["Blind"] ??= new JArray();
				pickBan["Aram"] ??= new JArray();

				File.WriteAllText(pickBanPath, pickBan.ToString());
				return pickBan;
			}
		}

		#region Configuration
		internal static JObject GetSettings()
		{
			return settings;
		}


		internal static void SetSetting(string settingName, dynamic newValue)
		{
			if (settings.SelectToken(settingName) != null)
			{
				settings.SelectToken(settingName)?.Replace(newValue);
			}

			File.WriteAllText(settingsPath, settings.ToString());
		}

		#endregion Configuration


		#region PickBan
		internal static void SavePickBan()
		{
			File.WriteAllText(pickBanPath, pickBan.ToString());
		}

		internal static int[]? GetDraftPick(string position)
		{
			var picks = pickBan.SelectToken($"Draft.Pick.{position}")?.Values<int>().ToArray();

			return picks;
		}

		internal static void SetDraftPick(string position, JArray pick)
		{
			pickBan["Draft"]!["Pick"]![position] = pick;

			SavePickBan();
		}

		internal static int[]? GetDraftBan(string position)
		{
			int[]? bans = pickBan.SelectToken($"Draft.Ban.{position}")?.Values<int>().ToArray();

			return bans;
		}

		internal static void SetDraftBan(string position, JArray ban)
		{
			pickBan["Draft"]!["Ban"]![position] = ban;

			SavePickBan();
		}

		internal static int[]? GetBlindPick()
		{
			var picks = pickBan["Blind"]?.Select(pick => (int)pick).ToArray();

			return picks;
		}

		internal static void SetBlindPick(JArray pick)
		{
			pickBan["Blind"] = pick;

			SavePickBan();
		}

		internal static int[] GetAramPick()
		{
			var picks = pickBan["Aram"]?.Select(pick => (int)pick).ToArray();
			if (picks == null) return Array.Empty<int>();
			return picks;
		}

		internal static void SetAramPick(JArray pick)
		{
			pickBan["Aram"] = pick;

			SavePickBan();
		}
		#endregion PickBan

		#endregion Files

		internal static async Task<JArray> GetChampionsInformations()
		{
			championsInformation ??= await ClientRequest.GetChampionsInfo();

			return championsInformation;
		}

		internal static async Task<JArray> GetChampionsRunesRecommendation()
		{
			championsRunesRecommendation ??= await ClientRequest.GetRecommendedRunes();

			return championsRunesRecommendation;
		}

		internal static async Task<JArray?> GetRunesStyleInformation()
		{
			runesStyleInformation ??= await ClientRequest.GetRunesStyles();

			if (runesStyleInformation == null) return null;

			return runesStyleInformation;
		}

		internal static async Task<string?> GetAppRunePageId()
		{
			if (appRunePageId == null)
			{
				//Get all pages
				var pages = await ClientRequest.GetRunePages();

				//Get the page containing the name KALUS
				var kurwappRunes = pages.Where(page => page["name"] != null && page["name"]?.ToString().ToLower().Contains("kalus") == true);

				//Assign the page id if there is any
				if (kurwappRunes.Any()) appRunePageId = kurwappRunes.Select(page => page.Value<int>("id")).First().ToString();
			}

			return appRunePageId;
		}

		internal static async Task<byte[]> GetDefaultChampionIcon()
		{
			defaultChampionIcon ??= await ClientRequest.GetChampionImageById(-1);

			return defaultChampionIcon;
		}

		internal static async Task<byte[]> GetDefaultRuneIcon()
		{
			defaultRuneIcon ??= await ClientRequest.GetDefaultRuneImage();

			return defaultRuneIcon;
		}

		internal static async Task<byte[]> GetDefaultMapIcon()
		{
			defaultMapIcon ??= await ClientRequest.GetDefaultMapImage();

			return defaultMapIcon;
		}

		internal static async Task<byte[]> GetAramMapIcon(bool isInGame)
		{
			if (isInGame)
			{
				ingameAramIcon ??= await ClientRequest.GetAramMapImage(inGame: true);
				return ingameAramIcon;
			}

			champSelectAramIcon ??= await ClientRequest.GetAramMapImage(inGame: false);
			return champSelectAramIcon;
		}

		internal static async Task<byte[]> GetClassicMapIcon(bool isInGame)
		{
			if (isInGame)
			{
				ingameClassicIcon ??= await ClientRequest.GetClassicMapImage(inGame: true);
				return ingameClassicIcon;
			}

			champSelectClassicIcon ??= await ClientRequest.GetClassicMapImage(inGame: false);
			return champSelectClassicIcon;
		}

		//Reset every cached variable
		internal static void ResetCachedData()
		{
			championsInformation = null;
			championsRunesRecommendation = null;
			runesStyleInformation = null;

			appRunePageId = null;

			defaultChampionIcon = null;
			defaultRuneIcon = null;
			defaultMapIcon = null;

			ingameClassicIcon = null;
			champSelectClassicIcon = null;

			ingameAramIcon = null;
			champSelectAramIcon = null;
		}
	}
}