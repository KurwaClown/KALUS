using Kalus.Modules;
using Kalus.Modules.Networking;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
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

		internal void EnableRandomSkinButton(bool isEnabled)
		{
			Dispatcher.Invoke(() => random_btn.IsEnabled = isEnabled);
		}

		internal void EnableChangeRuneButtons(bool isEnabled)
		{
			Dispatcher.Invoke(() =>
			{
				runes_btn_1.IsEnabled = isEnabled;
				runes_btn_2.IsEnabled = isEnabled;
				runes_btn_3.IsEnabled = isEnabled;
			});
		}

		internal void ShowLolState(bool isEnabled)
		{
			Color borderColor = isEnabled ? Colors.Green : Colors.Red;

			Dispatcher.Invoke(() => statusBorder.BorderBrush = new SolidColorBrush(borderColor));
		}

		internal void SetChampionIcon(byte[] image)
		{
			SetImageSource(characterIcon, image);

			Dispatcher.Invoke(() => isStatusBoxDefault = false);
		}

		internal void SetImageSource<T>(T imageSource, byte[] imageStream)
		{
			using MemoryStream stream = new(imageStream);
			Dispatcher.Invoke(() =>
			{
				BitmapImage bitmapImage = new();
				bitmapImage.BeginInit();
				bitmapImage.StreamSource = stream;
				bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
				bitmapImage.EndInit();
				switch (imageSource)
				{
					case ImageBrush imageBrush:
						imageBrush.ImageSource = bitmapImage;
						break;
					case Image image:
						image.Source = bitmapImage;
						break;
				}
			});
		}

		internal void SetRunesIcons(byte[] primaryRune, byte[] subRune)
		{
			SetImageSource(mainStyleIcon, primaryRune);
			SetImageSource(subStyleIcon, subRune);

			Dispatcher.Invoke(() => isStatusBoxDefault = false);
		}

		internal async void SetGameModeIcon(string gameMode, bool inGame = false)
		{
			byte[] icon = gameMode switch
			{
				"Draft" or "Blind" => await DataCache.GetClassicMapIcon(inGame),
				"ARAM" => await DataCache.GetAramMapIcon(inGame),
				_ => await DataCache.GetDefaultMapIcon(),
			};

			SetImageSource<Image>(gameModeIcon, icon);

			Dispatcher.Invoke(() => isStatusBoxDefault = false);
		}

		internal async void SetDefaultIcons()
		{
			byte[] defaultRunesIcon = await DataCache.GetDefaultRuneIcon();
			SetImageSource(mainStyleIcon, defaultRunesIcon);
			SetImageSource(subStyleIcon, defaultRunesIcon);

			SetChampionIcon(await DataCache.GetDefaultChampionIcon());

			SetImageSource(gameModeIcon, await DataCache.GetDefaultMapIcon());
		}

		internal void SetGamemodeName(string gamemodeName)
		{
			Dispatcher.Invoke(() =>
			{
				gameModeLbl.Content = gamemodeName;
				isStatusBoxDefault = false;
			});
		}

		internal void SetDefaultLabels()
		{
			Dispatcher.Invoke(() =>
			{
				gameModeLbl.Content = "Lobby";
				runesLbl.Content = "Runes";
				championLbl.Content = "Champion";
			});
		}

		internal void SetChampionName(string championName)
		{
			Dispatcher.Invoke(() =>
			{
				championLbl.Content = championName;
				isStatusBoxDefault = false;
			});
		}
	}
}