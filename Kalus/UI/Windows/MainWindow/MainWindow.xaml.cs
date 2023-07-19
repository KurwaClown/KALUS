using Kalus.Modules;
using Kalus.Modules.Networking;
using Kalus.Properties;
using System;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
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

				this.consoleTab.AddLog($"{Properties.Logs.Error} {exception?.GetType()}", Controls.Tabs.Console.Utility.KALUS, Controls.Tabs.Console.LogLevel.ERROR);
				// Re-enable the MainWindow after the error window is closed
				this.IsEnabled = true;

				this.Close();
			});
		}

		private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			if (Settings.Default.minimizeOnClosing)
			{
				e.Cancel = true;
				this.Visibility = Visibility.Hidden;
			}
			else
			{
				Application.Current.Shutdown();
				Environment.Exit(0);
			}
		}

		private void Window_Loaded(object sender, RoutedEventArgs e)
		{
			UpdateVerifier.CheckForUpdate();

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

			controlPanel.SetDefaultIcons();
			controlPanel.SetDefaultLabels();

			this.consoleTab.AddLog(Properties.Logs.UIInitialized, Controls.Tabs.Console.Utility.KALUS, Controls.Tabs.Console.LogLevel.INFO);
		}


		internal string GetGamemodeName()
		{
			string? gameMode = "";
			Dispatcher.Invoke(() => gameMode = controlPanel.gameModeLbl.Content.ToString());
			if (gameMode == null)
				return "GameMode";
			return gameMode;
		}
	}
}