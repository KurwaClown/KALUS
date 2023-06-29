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
                ClientState.BLIND => "STATE_BLIND",
                ClientState.DRAFT => "STATE_DRAFT",
                ClientState.ARAM => "STATE_ARAM",
                ClientState.CHAMPSELECT => "STATE_CHAMPSELECT",
                ClientState.READYCHECK => "STATE_READYCHECK",
                ClientState.LOBBY => "STATE_LOBBY",
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
