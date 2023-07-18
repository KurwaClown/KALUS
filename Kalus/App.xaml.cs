using Kalus.UI.Windows;
using System;
using System.Globalization;
using System.Windows;
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
			CultureInfo.CurrentUICulture = new CultureInfo((string)Kalus.Properties.Settings.Default["localization"]);
			CultureInfo.CurrentCulture = new CultureInfo((string)Kalus.Properties.Settings.Default["localization"]);

			mainWindow = new MainWindow();

			if (!(bool)Kalus.Properties.Settings.Default["runInBackground"])
				mainWindow.Show();
			try
			{
				SetNotifyIcon();
			}
			catch
			{
				mainWindow.Show();
				MessageBox.Show("There was an issue setting KALUS system tray icon");
			}
			base.OnStartup(e);
		}

		private void SetNotifyIcon()
		{
			Forms.ContextMenuStrip notifyContextMenu = new();

			notifyContextMenu.Items.Add(Kalus.Properties.UIStrings.NotifyIconOption1, null, NotifyIconShowWindow);
			notifyContextMenu.Items.Add(Kalus.Properties.UIStrings.NotifyIconOption2, null, NotifyIconMinimizeWindow);
			notifyContextMenu.Items.Add(Kalus.Properties.UIStrings.NotifyIconOption3, null, NotifyIconCloseKalus);

			notifyIcon = new Forms.NotifyIcon
			{
				Icon = Kalus.Properties.Resources.KALUS_Icon,
				Text = "KALUS",
				Visible = true,
				ContextMenuStrip = notifyContextMenu
			};

			notifyIcon.Click += NotifyIcon_Click;
		}

		private void NotifyIcon_Click(object? sender, EventArgs e)
		{
			if (mainWindow != null && !mainWindow.IsVisible && ((Forms.MouseEventArgs)e).Button == Forms.MouseButtons.Left)
			{
				mainWindow.Show();
				mainWindow.WindowState = WindowState.Normal;
				mainWindow.Activate();
			}
		}

		private void NotifyIconShowWindow(object? sender, System.EventArgs e)
		{
			if (mainWindow != null && !mainWindow.IsVisible)
			{
				mainWindow.Show();
				mainWindow.WindowState = WindowState.Normal;
				mainWindow.Activate();
			}
		}

		private void NotifyIconMinimizeWindow(object? sender, System.EventArgs e)
		{
			if (mainWindow != null && mainWindow.IsVisible)
			{
				mainWindow.Hide();
			}
		}

		private void NotifyIconCloseKalus(object? sender, System.EventArgs e)
		{
			this.Shutdown();
			Environment.Exit(0);
		}
	}
}