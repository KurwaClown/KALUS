using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace Kalus.UI.Converters
{
	internal class GroupNameAndTagToCheckedState : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo cultureInfo)
		{
			if(value is int setting && parameter is string tag)
			{
				return setting == int.Parse(tag);
			}

			return false;
		}
		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo cultureInfo)
		{
			if (value is bool isChecked && parameter is string tag && isChecked)
			{
				return int.Parse(tag);
			}
			return 0;
		}
	}

}

