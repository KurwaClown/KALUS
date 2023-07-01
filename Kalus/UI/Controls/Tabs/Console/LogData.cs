using Kalus.Modules;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kalus.UI.Controls.Tabs.Console
{
    public class LogData
    {
        public DateTime Timestamp { get; }
        public string Message { get; }
        public Utility Utility { get; }
        public LogLevel Level { get; }
        public ClientState State { get; }

        public LogData(string message, Utility utility, LogLevel level)
        {

            Timestamp = DateTime.Now;
            Message = message;
            Utility = utility;
            Level = level;
            State = ClientControl.state;
        }
    }
}
