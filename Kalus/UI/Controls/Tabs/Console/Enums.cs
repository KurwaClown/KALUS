using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kalus.UI.Controls.Tabs.Console
{

		public enum LogLevel
		{
			INFO,
			WARN,
			ERROR
		}

		public enum Utility
		{
			KALUS,
			CLIENT,
			POSTPICK,
			SWAPPING,
			READY,
			SKIN,
			GAME,
			REROLL,
			TRADE
		}

		public enum ClientState
		{
			NOCLIENT,
			NONE,
			LOBBY,
			MATCHMAKING,
			READYCHECK,
			CHAMPSELECT,
			GAMESTART
		}
}
