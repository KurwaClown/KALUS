using Kalus.UI.Controls.Tabs.Settings;
using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.Globalization;
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

		private void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if(sender is ComboBox localization)
			{
				//Prevents loop on data binding on start up
				if ((string)((ComboBoxItem)localization.SelectedItem).Content == CultureInfo.CurrentUICulture.Name)
					return;
				Properties.Settings.Default.Save();

				System.Threading.Thread.CurrentThread.CurrentUICulture = new CultureInfo((string)((ComboBoxItem)localization.SelectedItem).Content);
				System.Threading.Thread.CurrentThread.CurrentCulture = new CultureInfo((string)((ComboBoxItem)localization.SelectedItem).Content);

				Process.Start(Process.GetCurrentProcess().MainModule?.FileName!);
				App.Current.Shutdown();
			}
        }
    }
}
