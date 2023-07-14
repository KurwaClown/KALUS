using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace Kalus.UI.Controls.Components
{
    class SaveableSlider : Slider
    {
        public SaveableSlider() {
			this.ValueChanged += SaveableSlider_ValueChanged;
        }

		private void SaveableSlider_ValueChanged(object sender, System.Windows.RoutedPropertyChangedEventArgs<double> e)
		{
			Properties.Settings.Default.Save();
		}
	}
}
