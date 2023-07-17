using Kalus.Modules;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Resources;

namespace Kalus.UI.Controls.Tabs.Console
{
    public class LogData
    {
        public DateTime Timestamp { get; }
        public string Message { get; }
        public string Utility { get; }
        public LogLevel Level { get; }
        public string State { get; }

        public LogData(string message, Utility utility, LogLevel level)
        {

			ResourceManager resourceManager = new ResourceManager(typeof(Properties.Enums));
			//Transform the utility into its localized string
			string utilityLocalized = resourceManager.GetString(utility.ToString()) ?? utility.ToString();
            //Transform the state into its localized string
			string stateLocalized = resourceManager.GetString(ClientControl.state.ToString()) ?? ClientControl.state.ToString();


			Timestamp = DateTime.Now;
            Message = message;
			Utility = utilityLocalized;
            Level = level;
			State = stateLocalized;
        }
    }
}
