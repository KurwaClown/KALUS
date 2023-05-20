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

		internal static void ResetCachedData()
		{
			championsInformation = null;
			championsRunesRecommendation = null;

			summonerId = null;
		}
	}
}