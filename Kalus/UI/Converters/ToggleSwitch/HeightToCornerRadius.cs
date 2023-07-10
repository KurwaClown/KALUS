using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Markup;

namespace Kalus.UI.Converters
{
    class HeightToCornerRadius : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if(value is double backgroundHeight)
            {
                return backgroundHeight / 2;
            }
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter , System.Globalization.CultureInfo culture) => throw new NotImplementedException();
    }
}
