using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace Kalus.UI.Converters
{
    class HeightToDefaultMargin : IValueConverter
    {
        public object Convert(object value, Type targetType,  object parameter, System.Globalization.CultureInfo culture) {
            if(value is double backgroundHeight)
            {
                return new Thickness(backgroundHeight * 0.2, 0, 0, 0);
            }
            return new Thickness(5, 0, 0, 0);
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) => throw new NotImplementedException();
    }
}
