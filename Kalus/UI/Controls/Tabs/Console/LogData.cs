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
        public LogLevel Level { get; }
        public ClientState State { get; }

        public LogData(string message, LogLevel level, ClientState state)
        {

            Timestamp = DateTime.Now;
            Message = message;
            Level = level;
            State = state;
        }
    }
}
