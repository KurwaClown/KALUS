using Kalus.UI.Controls.Tabs.Settings;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace Kalus.UI.Controls.Tabs
{
    /// <summary>
    /// Interaction logic for Settings.xaml
    /// </summary>
    public partial class SettingsView : UserControl
	{
		private Thickness? margin = null;

		public SettingsView()
		{
			InitializeComponent();

			SettingsViewModel settingsViewModel = new();

			DataContext = settingsViewModel;
 		}

		private void SwitchButtonAnimation(bool toggleOn, ToggleButton switchButton, double animationDuration = 0.2)
		{

			Ellipse bubble = (Ellipse)switchButton.Template.FindName("bubble", switchButton);
			if (margin != null) margin = bubble.Margin;

			Thickness[] thicknesses = { margin ?? bubble.Margin, new Thickness(switchButton.Width - bubble.Width - bubble.Margin.Left, 0, 0, 0) };
			Debug.WriteLine(toggleOn);
			Debug.WriteLine(thicknesses[0]);
			ThicknessAnimation animation = new ThicknessAnimation
			{
				From = thicknesses[toggleOn ? 0 : 1],
				To = thicknesses[toggleOn ? 1 : 0],
				Duration = TimeSpan.FromSeconds(animationDuration)
			};
			bubble.BeginAnimation(MarginProperty, animation);
		}

		private void OnSwitchOn(object sender, System.Windows.RoutedEventArgs e)
		{
			SwitchButtonAnimation(true, (ToggleButton)sender);
		}

		private void OnSwitchOff(object sender, RoutedEventArgs e)
		{
			SwitchButtonAnimation(false, (ToggleButton)sender);
		}
	}
}
