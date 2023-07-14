using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace Kalus.UI.Controls.Components
{
	internal class SaveableCheckbox : CheckBox
	{
		public SaveableCheckbox() {
			this.Checked += SaveOnCheckedStateChange;
			this.Unchecked += SaveOnCheckedStateChange;
		}

		private void SaveOnCheckedStateChange(object sender, System.Windows.RoutedEventArgs e)
		{
			Properties.Settings.Default.Save();
		}
	}
}
