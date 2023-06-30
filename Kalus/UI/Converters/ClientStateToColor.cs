using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using Kalus.UI.Controls.Tabs.Console;

namespace Kalus.UI.Converters
{
    class ClientStateToColor : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (parameter is string strParameter)
            {
                if (strParameter == "FONT") return Application.Current.Resources["STATE_FONT"];
            }

            string colorResourceKey = value switch
            {
                ClientState.NOCLIENT => "STATE_NOCLIENT",
                ClientState.NONE => "STATE_NONE",
                ClientState.LOBBY => "STATE_LOBBY",
                ClientState.MATCHMAKING => "STATE_MATCHMAKING",
                ClientState.READYCHECK => "STATE_READYCHECK",
                ClientState.CHAMPSELECT => "STATE_CHAMPSELECT",
                ClientState.GAMESTART => "STATE_GAMESTART",
                _ => "STATE_DEFAULT"
            };

            return Application.Current.Resources[colorResourceKey];
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
