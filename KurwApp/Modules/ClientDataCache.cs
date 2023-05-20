using Newtonsoft.Json.Linq;
using System.Threading.Tasks;

namespace KurwApp.Modules
{
	internal static class ClientDataCache
	{
		private static JArray? championsInformation;
		private static JArray? championsRunesRecommendation;

		private static string? summonerId;

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

		internal static void ResetCachedData()
		{
			championsInformation = null;
			championsRunesRecommendation = null;

			summonerId = null;
		}
	}
}