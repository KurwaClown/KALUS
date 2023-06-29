using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using Kalus.UI.Controls.Tabs.Console;

namespace Kalus.UI.Converters
{
    class LogLevelToColor : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool isBackground = false;
            if (parameter is string strParameter)
            {
                if (strParameter == "FONT") return Application.Current.Resources["LOG_FONT"];
                else if (strParameter == "BACKGROUND") isBackground = true;
            }

            string colorResourceKey = value switch
            {
                LogLevel.Info => "LOG_INFO",
                LogLevel.Warn => "LOG_WARN",
                LogLevel.Error => "LOG_ERROR",
                _ => "LOG_DEFAULT"
            };

            if (isBackground) colorResourceKey += "_BACKGROUND";

            return Application.Current.Resources[colorResourceKey];
        }


        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) { throw new NotImplementedException(); }
    }
}
