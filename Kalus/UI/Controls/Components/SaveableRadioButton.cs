using Kalus.UI.Converters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace Kalus.UI.Controls.Components
{
	internal class SaveableRadioButton : RadioButton
	{
		public SaveableRadioButton()
		{
			this.Checked += SaveOnCheckedStateChange;
			this.Loaded += SaveableRadioButton_Loaded;
		}

		private void SaveableRadioButton_Loaded(object sender, RoutedEventArgs e)
		{
			var binding = new Binding(GroupName)
			{

				Converter = new GroupNameAndTagToCheckedState(),
				ConverterParameter = Tag,
				Mode = BindingMode.TwoWay,
				Source = Properties.Settings.Default,
			};

			this.SetBinding(IsCheckedProperty, binding);
		}

		private void SaveOnCheckedStateChange(object sender, System.Windows.RoutedEventArgs e)
		{
			Properties.Settings.Default.Save();
		}
	}
}
