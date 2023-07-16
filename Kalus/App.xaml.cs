using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Kalus.UI.Windows;
using Forms = System.Windows.Forms;

namespace Kalus
{
	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	public partial class App : Application
	{
		private Forms.NotifyIcon? notifyIcon;
		private MainWindow? mainWindow;

		protected override void OnStartup(StartupEventArgs e)
		{

			mainWindow = new MainWindow();

			if (!(bool)Kalus.Properties.Settings.Default["runInBackground"]) mainWindow.Show();
			try
			{
				notifyIcon = new Forms.NotifyIcon
					{
						Icon = Kalus.Properties.Resources.KALUS_Icon,
						Text = "KALUS",
						Visible = true
					};

			notifyIcon.Click += NotifyIcon_Click;
			}
			catch
			{
				mainWindow.Show();
				MessageBox.Show("There was an issue setting KALUS system tray icon");
			}
			base.OnStartup(e);
		}



		private void NotifyIcon_Click(object? sender, System.EventArgs e)
		{
			if(mainWindow != null && !mainWindow.IsVisible)
			{
				mainWindow!.Show();
				mainWindow.WindowState = WindowState.Normal;
				mainWindow.Activate();
			}

		}

	}
}