using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace Kalus.UI.Controls.Components
{
	internal class SaveableCombobox : ComboBox
	{
		public SaveableCombobox()
		{
			this.SelectionChanged += SaveableCombobox_SelectionChanged;
		}

		private void SaveableCombobox_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			Properties.Settings.Default.Save();
		}
	}
}
