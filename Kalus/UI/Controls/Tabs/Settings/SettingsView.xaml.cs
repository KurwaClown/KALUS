using Kalus.UI.Controls.Tabs.Settings;
using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace Kalus.UI.Controls.Tabs
{
    /// <summary>
    /// Interaction logic for Settings.xaml
    /// </summary>
    public partial class SettingsView : UserControl
	{


		public SettingsView()
		{
			InitializeComponent();

			SettingsViewModel settingsViewModel = new();

			DataContext = settingsViewModel;
 		}

		private void AddRunOnStartup(object sender, RoutedEventArgs e)
		{
			RegistryKey? rk = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
			rk?.SetValue("KALUS", Process.GetCurrentProcess().MainModule?.FileName ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Kalus.exe"));
		}

		private void RemoveRunOnStartup(object sender, RoutedEventArgs e)
		{
			RegistryKey? rk = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
			rk?.DeleteValue("KALUS");

		}
	}
}
