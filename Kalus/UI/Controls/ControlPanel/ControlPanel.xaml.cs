using Kalus.Modules;
using Kalus.Modules.Networking;
using Kalus.UI.Windows;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using static Kalus.UI.Windows.MainWindow;

namespace Kalus.UI.Controls
{
	/// <summary>
	/// Interaction logic for ControlPanel.xaml
	/// </summary>
	public partial class ControlPanel : UserControl
	{
		internal delegate Task InventoryChange(int recommendationNumber = -1);

		internal InventoryChange? inventoryChange;

		public ControlPanel()
		{
			InitializeComponent();
		}


		private void ChangeRunes(object sender, SelectionChangedEventArgs e)
		{
			if(sender is ComboBox runesCombobox)
			{
				int selectedItem = runesCombobox.SelectedIndex;

				inventoryChange?.Invoke(selectedItem);

			}
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

		internal void EnableChangeRuneCombobox(bool isEnabled)
		{
			Dispatcher.Invoke(() =>
			{
				runesSelection.Items.Clear();
				runesSelection.IsEnabled = isEnabled;
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

		private void ToggleLiteMode(object sender, RoutedEventArgs e)
		{
			DependencyObject? parent = VisualTreeHelper.GetParent(this);
			while (parent != null && parent is not MainWindow)
			{
				parent = VisualTreeHelper.GetParent(parent);
			}

			if (parent != null && parent is MainWindow mainWindow) {
				if (mainWindow.mainTabControl.Visibility == Visibility.Visible) mainWindow.mainTabControl.Visibility = Visibility.Collapsed;
				else mainWindow.mainTabControl.Visibility = Visibility.Visible;

			}
        }


	}
}