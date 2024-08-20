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

		public enum Summonners
		{
			CLEANSE = 1,
			EXHAUST = 3,
			FLASH = 4,
			GHOST = 6,
			HEAL = 7,
			SMITE = 11,
			TELEPORT = 12,
			CLARITY = 13,
			IGNITE = 14,
			BARRIER = 21,
			SNOWBALL = 32,
			SNOWBALL_URF = 39
		}

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

			//If the spells already contains a flash
			if (recommendedSpells.Contains((int)Summonners.FLASH))
			{
				// If the flash has a given position and is not in the right position we swap position
				if (flashPosition != 2 && Array.IndexOf(recommendedSpells, (int)Summonners.FLASH) != flashPosition)
				{
						(recommendedSpells[1], recommendedSpells[0]) = (recommendedSpells[0], recommendedSpells[1]);
				}
			}
			// If the spells doesn't contain a flash and the user wants to always have flash
			else if (Properties.Settings.Default.summonersAlwaysFlash)
			{
				recommendedSpells[flashPosition != 2 ? flashPosition : 0] = (int)Summonners.FLASH;
			}

			await ClientRequest.ChangeSummonerSpells(recommendedSpells);
		}
	}
}
