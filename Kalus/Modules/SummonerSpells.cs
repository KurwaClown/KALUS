using Kalus.Modules.Networking;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kalus.Modules
{
	internal static class SummonerSpells
	{

		//Get the recommended champion spells for a champion depending on its position and the game mode
		internal static async Task<JToken?> GetSpellsRecommendationByPosition(int championId, string position)
		{
			var runesRecommendation = await Runes.GetChampRunesByPosition(championId, position);

			if (runesRecommendation == null)
				return null;

			return runesRecommendation["summonerSpellIds"];
		}

		internal static async Task<JToken?> GetSpellsRecommendationBySelectionIndex(int championId, int selectionIndex)
		{
			JToken? runesRecommendation = await Runes.GetAllRecommendedRunesForChampion(championId);

			if (runesRecommendation == null) return null;

			return runesRecommendation[selectionIndex]?.SelectToken("summonerSpellIds");
		}

		internal static async void SetSummonerSpells(int[] recommendedSpells)
		{
			int flashPosition = Properties.Settings.Default.flashPosition;

			if (flashPosition != 2 && recommendedSpells.Contains(4))
			{
				if (Array.IndexOf(recommendedSpells, 4) != flashPosition)
				{
					(recommendedSpells[1], recommendedSpells[0]) = (recommendedSpells[0], recommendedSpells[1]);
				}
			}

			await ClientRequest.ChangeSummonerSpells(recommendedSpells);
		}
	}
}
