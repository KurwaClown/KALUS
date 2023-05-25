using Kalus.Modules.Networking;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Kalus.Modules.Games.GameMode
{
    internal class Aram : Game
    {
        internal Aram(MainWindow mainWindow)
        {
            this.mainWindow = mainWindow;
        }

        //Handler of the champion selections
        protected internal override async Task ChampSelectControl()
        {
            sessionInfo = await ClientRequest.GetSessionInfo();
            if (sessionInfo is null) return;

            mainWindow.SetGamemodeName("ARAM");
            mainWindow.SetGameModeIcon("ARAM");

            while (Auth.IsAuthSet())
            {
                sessionInfo = await ClientRequest.GetSessionInfo();
                if (sessionInfo is null) return;
                switch (sessionInfo.SelectToken("timer.phase").ToString())
                {
                    default:
                        break;

                    case "FINALIZATION":
                        await Finalization();
                        break;

                    case "GAME_STARTING":
                    case "":
                        mainWindow.EnableRandomSkinButton(false);
                        return;
                }
                Thread.Sleep(1000);
            }
        }

        protected override async Task Finalization()
        {
            if (ClientControl.GetSettingState("aramChampionSwap"))
            {
                int aramPick = GetBenchChampionPick();

                if (aramPick != 0)
                {
                    await ClientRequest.AramBenchSwap(aramPick);
                    isRunePageChanged = false;
                    await PostPickAction();
                    championId = await ClientRequest.GetCurrentChampionId();
                }
            }
            var currentChampionId = await ClientRequest.GetCurrentChampionId();
            Debug.WriteLine($"Current champion : {currentChampionId}, stored champion : {championId}");
            if (currentChampionId != championId)
            {
                championId = currentChampionId;
                isRunePageChanged = false;
                await PostPickAction();
            }
        }

        //Get the pick the aram champion to pick if any
        protected int GetBenchChampionPick()
        {
            var aramPicks = DataCache.GetAramPick();

            if (!aramPicks.Any()) return 0;

            List<int> aramBenchIds = GetAramBenchIds();

            if (!aramBenchIds.Any()) return 0;

            foreach (var pick in aramPicks)
            {
                if (aramBenchIds.Contains(pick)) return pick;
            }
            return 0;
        }

        //Get the champion benched (their id) in aram
        private List<int> GetAramBenchIds()
        {
            return sessionInfo["benchChampions"].Select(x => int.Parse(x["championId"].ToString())).ToList();
        }

        protected override async Task ChangeSpells()
        {
            var runesRecommendation = await ClientControl.GetSpellsRecommendationByPosition(championId, "NONE");

            var spellsId = runesRecommendation.ToObject<int[]>();

            if (!spellsId.Contains(32)) spellsId[1] = 32;

            ClientControl.SetSummonerSpells(spellsId);
        }

        protected override async Task ChangeRunes()
        {
            bool isSetActive = (bool)ClientControl.GetPreference("runes.notSetActive");

            string activeRunesPage = isSetActive ? (await ClientRequest.GetActiveRunePage())["id"].ToString() : "0";

            await ClientControl.SetRunesPage(championId, "NONE");

            isRunePageChanged = true;

            if (isSetActive) await ClientRequest.SetActiveRunePage(activeRunesPage);
        }
    }
}