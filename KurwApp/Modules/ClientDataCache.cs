using Newtonsoft.Json.Linq;
using System.Threading.Tasks;

namespace KurwApp.Modules
{
	internal static class ClientDataCache
	{
		private static JArray? championsInformation;
		private static JArray? championsRunesRecommendation;

		private static string? summonerId;

		private static byte[]? defaultChampionIcon;
		private static byte[]? defaultRuneIcon;
		private static byte[]? defaultMapIcon;

		private static byte[]? ingameClassicIcon;
		private static byte[]? champSelectClassicIcon;

		private static byte[]? ingameAramIcon;
		private static byte[]? champSelectAramIcon;

		internal static async Task<JArray> GetChampionsInformations()
		{
			championsInformation ??= await Client_Request.GetChampionsInfo();

			return championsInformation;
		}

		internal static async Task<JArray> GetChampionsRunesRecommendation()
		{
			championsRunesRecommendation ??= await Client_Request.GetRecommendedRunes();

			return championsRunesRecommendation;
		}

		internal static async Task<string> GetSummonerId()
		{
			if (summonerId == null)
			{
				JObject summonerAndAccountId = await Client_Request.GetSummonerAndAccountId();
				summonerId = summonerAndAccountId["summonerId"].ToString();
			}

			return summonerId;
		}

		internal static async Task<byte[]> GetDefaultChampionIcon()
		{
			defaultChampionIcon ??= await Client_Request.GetChampionImageById(-1);

			return defaultChampionIcon;
		}

		internal static async Task<byte[]> GetDefaultRuneIcon()
		{
			defaultRuneIcon ??= await Client_Request.GetDefaultRuneImage();

			return defaultRuneIcon;
		}

		internal static async Task<byte[]> GetDefaultMapIcon()
		{
			defaultMapIcon ??= await Client_Request.GetDefaultMapImage();

			return defaultMapIcon;
		}

		internal static async Task<byte[]> GetAramMapIcon(bool isInGame)
		{
			if (isInGame)
			{
				ingameAramIcon ??= await Client_Request.GetAramMapImage(inGame: true);
				return ingameAramIcon;
			}

			champSelectAramIcon ??= await Client_Request.GetAramMapImage(inGame: false);
			return champSelectAramIcon;
		}

		internal static async Task<byte[]> GetClassicMapIcon(bool isInGame)
		{
			if (isInGame)
			{
				ingameClassicIcon ??= await Client_Request.GetClassicMapImage(inGame: true);
				return ingameClassicIcon;
			}

			champSelectClassicIcon ??= await Client_Request.GetClassicMapImage(inGame: false);
			return champSelectClassicIcon;
		}

		//Reset every cached variable
		internal static void ResetCachedData()
		{
			championsInformation = null;
			championsRunesRecommendation = null;

			summonerId = null;

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