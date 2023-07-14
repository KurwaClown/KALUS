using Kalus.Modules;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace Kalus.UI.Windows
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		internal static bool isStatusBoxDefault = false;

		public MainWindow()
		{
			InitializeComponent();

			AppDomain.CurrentDomain.UnhandledException += UnhandledException;

			Thread authentication = new(() => ClientControl.EnsureAuthentication(this));
			Thread clientPhase = new(() => ClientControl.ClientPhase(this));

			authentication.Start();
			clientPhase.Start();
		}

		private void UnhandledException(object sender, UnhandledExceptionEventArgs e)
		{
			// Extract the exception from the event arguments
			Exception? exception = e.ExceptionObject as Exception;

			string currentGamePhase = ClientControl.gamePhase == "" ? "Client Closed" : ClientControl.gamePhase;

			string newReport = $"## Report\r" +
								$"### Client Status: \r`{currentGamePhase}`\r" +
								$"### Type: \r`{exception?.GetType()}`\r" +
								$"### Exception: \r`{exception?.Message}`\r" +
								$"### Stack Trace: \r```{exception?.StackTrace}";

			// Invoke the error window on the UI thread
			Dispatcher.Invoke(() =>
			{
				// Create a new instance of the ErrorWindow view
				ErrorWindow errorWindow = new()
				{
					// Set the error message
					ErrorMessage = $"An error was encountered",
					// Set the report string
					Report = newReport,
					// Set the owner of the error window as the MainWindow
					Owner = this
				};
				// Pop the error window on top
				this.Topmost = true;
				this.Topmost = false;

				// Disable the MainWindow while the error window is displayed
				this.IsEnabled = false;
				// Show the error window as a dialog
				errorWindow.ShowDialog();

				this.consoleTab.AddLog("An error occured : " + exception?.GetType(), Controls.Tabs.Console.Utility.KALUS, Controls.Tabs.Console.LogLevel.ERROR);
				// Re-enable the MainWindow after the error window is closed
				this.IsEnabled = true;

				this.Close();
			});
		}

		private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			Application.Current.Shutdown();
			Environment.Exit(0);
		}

		internal void SetSettings()
		{
			var settings = DataCache.GetSettings();

			JToken checkIntervalIndex = settings.SelectToken("options.checkIntervalIndex") ?? new JValue(2);
			if (checkIntervalIndex.Value<int>() > checkInterval.Items.Count)
			{
				checkIntervalIndex = new JValue(2);
				DataCache.SetSetting("options.checkIntervalIndex", 2);
			}
			ClientControl.checkInterval = int.Parse(((MenuItem)checkInterval.Items[checkIntervalIndex.Value<int>()]).Header.ToString()!);

			Dispatcher.Invoke(() =>
			{
				((MenuItem)checkInterval.Items[checkIntervalIndex.Value<int>()]).IsChecked = true;
			}
			);
		}

		private void Window_Loaded(object sender, RoutedEventArgs e)
		{
			charactersTab.LoadAndSetCharacterList();

			static void populateComboBox(int maxTime, ComboBox comboBox)
			{
				for (int i = 1; i <= maxTime / 5; i++)
				{
					comboBox.Items.Add(i * 5);
				}
			}
			populateComboBox(30, preferencesTab.picksTimeLeft);
			populateComboBox(30, preferencesTab.bansTimeLeft);

			SetSettings();
			controlPanel.SetDefaultIcons();
			controlPanel.SetDefaultLabels();

			this.consoleTab.AddLog("UI Initialized", Controls.Tabs.Console.Utility.KALUS, Controls.Tabs.Console.LogLevel.INFO);
		}

		internal string GetGamemodeName()
		{
			string? gameMode = "";
			Dispatcher.Invoke(() => gameMode = controlPanel.gameModeLbl.Content.ToString());
			if (gameMode == null) return "GameMode";
			return gameMode;
		}

		private void SetCheckInterval(object sender, RoutedEventArgs e)
		{
			MenuItem interval = (MenuItem)sender;
			int newIntervalIndex = 0;
			foreach (var siblingInterval in ((MenuItem)interval.Parent).Items.OfType<MenuItem>())
			{
				siblingInterval.IsChecked = false;

				if (siblingInterval == interval) DataCache.SetSetting("options.checkIntervalIndex", newIntervalIndex);
				else newIntervalIndex++;
			}

			interval.IsChecked = true;

			int newIntervalValue = int.Parse(interval.Header.ToString()!);

			ClientControl.checkInterval = newIntervalValue;

			this.consoleTab.AddLog("Changing checks interval", Controls.Tabs.Console.Utility.KALUS, Controls.Tabs.Console.LogLevel.INFO);
		}
	}
}