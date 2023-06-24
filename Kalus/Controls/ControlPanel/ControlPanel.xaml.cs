using Kalus.Modules;
using Kalus.Modules.Networking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using static Kalus.MainWindow;

namespace Kalus.Controls.ControlPanel
{
	/// <summary>
	/// Interaction logic for ControlPanel.xaml
	/// </summary>
	public partial class ControlPanel : UserControl
	{
		internal delegate Task RuneChange(int recommendationNumber = 0);

		internal RuneChange? runeChange;

		public ControlPanel()
		{
			InitializeComponent();
		}

		private void ChangeRunes(object sender, RoutedEventArgs e)
		{
			int recommendationNumber = int.Parse((sender as Button)!.Content.ToString() ?? "1") - 1;
			runeChange?.Invoke(recommendationNumber);
		}

		private void OnSettingsControlInteraction(object sender, RoutedEventArgs e)
		{
			if (sender is not CheckBox checkBox) return;
			bool isChecked = checkBox.IsChecked ?? false;
			string? checkboxTag = checkBox.Tag.ToString();
			if (checkboxTag == null) return;

			DataCache.SetSetting(checkboxTag, isChecked);
		}

		private void RandomSkinClick(object sender, RoutedEventArgs e)
		{
			ClientControl.PickRandomSkin();
		}

		private async void ClientRestart(object sender, RoutedEventArgs e)
		{
			await ClientRequest.RestartLCU();
		}
	}
}
