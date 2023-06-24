using Kalus.Modules;
using Kalus.Modules.Networking;
using Kalus.Views.ErrorWindow;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace Kalus
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		private ObservableCollection<ListBoxItem> ChampListCollection { get; set; } = new ObservableCollection<ListBoxItem>();
		private ObservableCollection<ListBoxItem> UpdatedListCollection { get; set; } = new ObservableCollection<ListBoxItem>();
		private ObservableCollection<ListBoxItem> SelectedListCollection { get; set; } = new ObservableCollection<ListBoxItem>();

		internal static bool isStatusBoxDefault = false;

		internal delegate Task RuneChange(int recommendationNumber = 0);

		internal RuneChange? runeChange;

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

			string newReport =$"## Report\r" +
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

				// Re-enable the MainWindow after the error window is closed
				this.IsEnabled = true;

				this.Close();

			});
		}

		internal void EnableRandomSkinButton(bool isEnabled)
		{
			Dispatcher.Invoke(() => controlPanel.random_btn.IsEnabled = isEnabled);
		}

		internal void EnableChangeRuneButtons(bool isEnabled)
		{
			Dispatcher.Invoke(() => {
				controlPanel.runes_btn_1.IsEnabled = isEnabled;
				controlPanel.runes_btn_2.IsEnabled = isEnabled;
				controlPanel.runes_btn_3.IsEnabled = isEnabled;
				});
		}

		internal void ShowLolState(bool isEnabled)
		{
			Color borderColor = isEnabled ? Colors.Green : Colors.Red;

			Dispatcher.Invoke(() => controlPanel.statusBorder.BorderBrush = new SolidColorBrush(borderColor));
		}

		internal void SetChampionIcon(byte[] image)
		{
			SetImageBrush(controlPanel.characterIcon, image);

			Dispatcher.Invoke(() => isStatusBoxDefault = false);
		}

		internal void SetImageBrush(ImageBrush imageBrush, byte[] image)
		{
			using MemoryStream stream = new(image);
			Dispatcher.Invoke(() =>
			{
				BitmapImage bitmapImage = new();
				bitmapImage.BeginInit();
				bitmapImage.StreamSource = stream;
				bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
				bitmapImage.EndInit();
				imageBrush.ImageSource = bitmapImage;
			});
		}

		internal void SetImageSource(Image image, byte[] imageStream)
		{
			using MemoryStream stream = new(imageStream);
			Dispatcher.Invoke(() =>
			{
				BitmapImage bitmapImage = new();
				bitmapImage.BeginInit();
				bitmapImage.StreamSource = stream;
				bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
				bitmapImage.EndInit();
				image.Source = bitmapImage;
			});
		}

		internal void SetRunesIcons(byte[] primaryRune, byte[] subRune)
		{
			SetImageBrush(controlPanel.mainStyleIcon, primaryRune);
			SetImageBrush(controlPanel.subStyleIcon, subRune);

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

			SetImageSource(controlPanel.gameModeIcon, icon);

			Dispatcher.Invoke(() => isStatusBoxDefault = false);
		}

		internal async void SetDefaultIcons()
		{
			byte[] defaultRunesIcon = await DataCache.GetDefaultRuneIcon();
			SetImageBrush(controlPanel.mainStyleIcon, defaultRunesIcon);
			SetImageBrush(controlPanel.subStyleIcon, defaultRunesIcon);

			SetChampionIcon(await DataCache.GetDefaultChampionIcon());

			SetImageSource(controlPanel.gameModeIcon, await DataCache.GetDefaultMapIcon());
		}

		internal async void LoadAndSetCharacterList()
		{
			var champions = await DataCache.GetChampionsInformations();
			Dictionary<int, string?> championNames = champions.Where(champion => champion.Value<int>("id") != -1)
															.ToDictionary(champion => champion.Value<int>("id"), champion => champion["name"]?.ToString());
			championNames = championNames.OrderBy(champion => champion.Value).ToDictionary(champion => champion.Key, champion => champion.Value);

			Dispatcher.Invoke(() =>
			{
				foreach (KeyValuePair<int, string?> champName in championNames)
				{
					ListBoxItem championItem = new()
					{
						Tag = champName.Key,
						Content = champName.Value,
						VerticalContentAlignment = VerticalAlignment.Center,
						HorizontalContentAlignment = HorizontalAlignment.Left
					};
					ChampListCollection.Add(championItem);
				}
			});

			int[]? blindPicks = DataCache.GetBlindPick();
			blindPicks ??= Array.Empty<int>();

			var champListBoxItems = ChampListCollection.Where(champion =>
			{
				object tag = champion.Tag;
				if (tag != null)
				{
					if (int.TryParse(tag.ToString(), out int championId))
					{
						return blindPicks.Select(token => token).ToArray().Contains(championId);
					}
				}
				return false;
			});

			foreach (var champ in champListBoxItems)
			{
				SelectedListCollection.Add(champ);
			}

			UpdatedListCollection = new ObservableCollection<ListBoxItem>(ChampListCollection.Except(SelectedListCollection));

			champList.ItemsSource = UpdatedListCollection;
			selectionList.ItemsSource = SelectedListCollection;
		}

		private void RandomSkinClick(object sender, RoutedEventArgs e)
		{
			ClientControl.PickRandomSkin();
		}

		private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			Application.Current.Shutdown();
			Environment.Exit(0);
		}

		private async void ClientRestart(object sender, RoutedEventArgs e)
		{
			await ClientRequest.RestartLCU();
		}

		private void AddSelectionFromButton(object sender, RoutedEventArgs e)
		{
			AddSelection();
		}

		private void AddSelectionFromDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
		{
			AddSelection();
		}

		private void AddSelection()
		{
			if (champList.SelectedItem == null) return;

			if (champList.SelectedItem is not ListBoxItem selection) return;

			SelectedListCollection.Add(selection);
			UpdatedListCollection.Remove(selection);

			SavePicksModification();
		}

		private void RemoveSelectionFromButton(object sender, RoutedEventArgs e)
		{
			RemoveSelection();
		}

		private void RemoveSelectionFromDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
		{
			RemoveSelection();
		}

		private void RemoveSelection()
		{
			if (selectionList.SelectedItem == null) return;

			if (selectionList.SelectedItem is not ListBoxItem selection) return;
			//Add the removed item to the champion list and then re-order the list
			UpdatedListCollection.Add(selection);

			UpdatedListCollection = new ObservableCollection<ListBoxItem>(UpdatedListCollection.OrderBy(i => i.Content));

			champList.ItemsSource = UpdatedListCollection;

			_ = SelectedListCollection.Remove(selection);

			SavePicksModification();
		}

		private void SavePicksModification()
		{
			var gameType = ((ComboBoxItem)selectionListGameType.SelectedItem).Content.ToString();
			var newList = new JArray(SelectedListCollection
				.Where(i => i.Tag != null)
				.Select(i => new JValue(int.Parse(i.Tag.ToString()!))));
			switch (gameType)
			{
				default:
					break;

				case "Draft":
					var pickType = ((ComboBoxItem)selectionListType.SelectedItem).Content.ToString();

					var position = ((ComboBoxItem)selectionListPosition.SelectedItem).Content.ToString();

					if (position == null) return;

					var fileRole = position == "Support" ? "UTILITY" : position.ToUpper();

					var positionPicks = new JArray(SelectedListCollection
										.Where(i => i.Tag != null)
										.Select(i => new JValue(int.Parse(i.Tag.ToString()!))));

					if (pickType == "Pick") DataCache.SetDraftPick(fileRole, positionPicks);
					else DataCache.SetDraftBan(fileRole, positionPicks);
					break;

				case "Blind":
					DataCache.SetBlindPick(newList);
					break;

				case "ARAM":
					DataCache.SetAramPick(newList);
					break;
			}
		}

		private void OnControlInteraction(object sender, RoutedEventArgs e)
		{
			if (sender is CheckBox checkBox)
			{
				bool isChecked = checkBox.IsChecked ?? false;
				string? checkboxTag = checkBox.Tag.ToString();

				if (checkboxTag == null) return;
				DataCache.SetPreference(checkboxTag, isChecked);
			}
			else if (sender is RadioButton radioButton)
			{
				if (radioButton.Tag != null && int.TryParse(radioButton.Tag.ToString(), out int parsedValue))
				{
					DataCache.SetPreference(radioButton.GroupName, parsedValue);
				}
			}
		}

		private void OnSettingsControlInteraction(object sender, RoutedEventArgs e)
		{
			if (sender is not CheckBox checkBox) return;
			bool isChecked = checkBox.IsChecked ?? false;
			string? checkboxTag = checkBox.Tag.ToString();
			if (checkboxTag == null) return;

			DataCache.SetSetting(checkboxTag, isChecked);
		}

		private void IsEnabledModified(object sender, DependencyPropertyChangedEventArgs e)
		{
			if (sender is ComboBox comboBox)
			{
				if (comboBox.IsEnabled)
				{
					string? tag = comboBox.Tag.ToString();
					if (tag == null) return;
					if (DataCache.GetPreference(tag, out string? preference))
					{
						comboBox.SelectedIndex = int.Parse(preference!);
					}
				}
				else
				{
					comboBox.SelectedIndex = -1;
				}
			}
			else if (sender is CheckBox checkBox)
			{
				if (checkBox.IsEnabled)
				{
					string? tag = checkBox.Tag.ToString();
					if (tag == null) return;
					if (DataCache.GetPreference(tag, out string? preference)) checkBox.IsChecked = bool.Parse(preference!);
				}
			}
		}

		//Set the preferences saved in the preferences.json to the ui
		internal void SetPreferences()
		{
			var preferences = DataCache.GetPreferences();
			void setRadioByPreference(StackPanel stack, string token)
			{
				var preferences = DataCache.GetPreferences();
				var radioButtons = stack.Children.OfType<RadioButton>();
				var radioButtonToCheck = radioButtons.FirstOrDefault(rb => preferences.SelectToken(token)?.ToString() == rb.Tag.ToString());
				if (radioButtonToCheck != null)
				{
					radioButtonToCheck.IsChecked = true;
					return;
				}

				// Traverse the visual tree to check nested StackPanels
				foreach (var child in stack.Children)
				{
					if (child is StackPanel nestedPanel)
					{
						setRadioByPreference(nestedPanel, token);
					}
				}
			}

			Dispatcher.Invoke(() =>
			{
				setRadioByPreference(picksPreferences, "picks.userPreference");
				setRadioByPreference(bansPreferences, "bans.userPreference");
				setRadioByPreference(noAvailablePreferences, "noPicks.userPreference");
				setRadioByPreference(onSelectionPreferences, "selections.userPreference");
				setRadioByPreference(flashPosition, "summoners.flashPosition");


				notSetPageAsActive.IsChecked = (bool?)preferences.SelectToken("runes.notSetActive");
				overridePage.IsChecked = (bool?)preferences.SelectToken("runes.overridePage");

				addChromas.IsChecked = (bool?)preferences.SelectToken("randomSkin.addChromas");
				randomOnPick.IsChecked = (bool?)preferences.SelectToken("randomSkin.randomOnPick");
				alwaysSnowball.IsChecked = (bool?)preferences.SelectToken("summoners.alwaysSnowball");

				rerollForChampion.IsChecked = (bool?)preferences.SelectToken("aram.rerollForChampion");
				tradeForChampion.IsChecked = (bool?)preferences.SelectToken("aram.tradeForChampion");
				repickChampion.IsChecked = (bool?)preferences.SelectToken("aram.repickChampion");
			}
			);
		}

		internal void SetSettings()
		{
			var settings = DataCache.GetSettings();

			JToken checkIntervalIndex = settings.SelectToken("options.checkIntervalIndex") ?? new JValue(2);
			if (checkIntervalIndex.Value<int>()  > checkInterval.Items.Count) {
				checkIntervalIndex = new JValue(2);
				DataCache.SetSetting("options.checkIntervalIndex", 2);
			}
			ClientControl.checkInterval = int.Parse(((MenuItem)checkInterval.Items[checkIntervalIndex.Value<int>()]).Header.ToString()!);

			Dispatcher.Invoke(() =>
			{
				((MenuItem)checkInterval.Items[checkIntervalIndex.Value<int>()]).IsChecked = true;

				controlPanel.autoPickSetting.IsChecked = settings.Value<bool>("championPick");
				controlPanel.autoBanSetting.IsChecked = settings.Value<bool>("banPick");
				controlPanel.autoReadySetting.IsChecked = settings.Value<bool>("aramChampionSwap");
				controlPanel.autoRunesSetting.IsChecked = settings.Value<bool>("runesSwap");
				controlPanel.autoSpellSetting.IsChecked = settings.Value<bool>("autoSummoner");
				controlPanel.autoSwapSetting.IsChecked = settings.Value<bool>("autoReady");
			}
			);
		}

		private void Window_Loaded(object sender, RoutedEventArgs e)
		{
			LoadAndSetCharacterList();

			static void populateComboBox(int maxTime, ComboBox comboBox)
			{
				for (int i = 1; i <= maxTime / 5; i++)
				{
					comboBox.Items.Add(i * 5);
				}
			}
			populateComboBox(30, picksTimeLeft);
			populateComboBox(30, bansTimeLeft);

			SetSettings();
			SetPreferences();
			SetDefaultIcons();
			SetDefaultLabels();
		}

		internal void SetDefaultLabels()
		{
			Dispatcher.Invoke(() =>
			{
				controlPanel.gameModeLbl.Content = "Lobby";
				controlPanel.runesLbl.Content = "Runes";
				controlPanel.championLbl.Content = "Champion";
			});
		}

		private void OTLChange(object sender, SelectionChangedEventArgs e)
		{
			if (sender is ComboBox comboBox)
			{
				if (comboBox.SelectedIndex == -1) return;
				string? tag = comboBox.Tag.ToString();
				if (tag == null) return;
				DataCache.SetPreference(tag, comboBox.SelectedIndex);
			}
		}

		private void SelectionListChange(object sender, SelectionChangedEventArgs e)
		{
			var gameType = ((ComboBoxItem)selectionListGameType.SelectedItem).Content.ToString();

			IEnumerable<ListBoxItem> champListBoxItems;

			if (gameType == "Draft")
			{
				var pickType = ((ComboBoxItem)selectionListType.SelectedItem).Content.ToString();

				var position = ((ComboBoxItem)selectionListPosition.SelectedItem).Content.ToString();
				if (position == null) return;
				position = position == "Support" ? "UTILITY" : position.ToUpper();

				var champsId = pickType == "Pick" ? DataCache.GetDraftPick(position) : DataCache.GetDraftBan(position);
				if (champsId == null) return;
				champListBoxItems = champsId.Join(ChampListCollection,
													id => id,
													champion => int.Parse(champion.Tag.ToString()!),
													(id, champion) => champion);
			}
			else
			{
				var champsId = gameType == "Blind" ? DataCache.GetBlindPick() : DataCache.GetAramPick();
				if (champsId == null) return;
				champListBoxItems = champsId.Join(ChampListCollection,
													id => id,
													champion => int.Parse(champion.Tag.ToString()!),
													(id, champion) => champion);
			}

			SelectedListCollection.Clear();
			foreach (var champ in champListBoxItems)
			{
				SelectedListCollection.Add(champ);
			}

			UpdatedListCollection = new ObservableCollection<ListBoxItem>(ChampListCollection.Except(SelectedListCollection));
			champList.ItemsSource = UpdatedListCollection;
		}

		private void EmptySelectionList(object sender, RoutedEventArgs e)
		{
			SelectedListCollection.Clear();
			SavePicksModification();
		}

		private void ReorderSelectionFromButton(object sender, RoutedEventArgs e)
		{
			bool isPrevious = ((Button)sender).Name == "selectionOrderUp";
			ReorderSelection(isPrevious);
		}

		private void ReorderSelection(bool isPrevious)
		{
			if (selectionList.SelectedItem == null) return;
			if (selectionList.SelectedItem is not ListBoxItem selection) return;

			if (selectionList.ItemsSource is not ObservableCollection<ListBoxItem> observableCollection) return;

			int oldIndex = observableCollection.IndexOf(selection);

			if (isPrevious && oldIndex - 1 >= 0)
			{
				observableCollection.Move(oldIndex, oldIndex - 1);
			}
			else if (!isPrevious && oldIndex + 1 < observableCollection.Count)
			{
				observableCollection.Move(oldIndex, oldIndex + 1);
			}
			SavePicksModification();
		}


		internal void SetGamemodeName(string gamemodeName)
		{
			Dispatcher.Invoke(() =>
			{
				controlPanel.gameModeLbl.Content = gamemodeName;
				isStatusBoxDefault = false;
			});
		}


		internal string GetGamemodeName()
		{
			string? gameMode = "";
			Dispatcher.Invoke(() => gameMode = controlPanel.gameModeLbl.Content.ToString());
			if (gameMode == null) return "GameMode";
			return gameMode;
		}

		internal void SetChampionName(string championName)
		{
			Dispatcher.Invoke(() =>
			{
				controlPanel.championLbl.Content = championName;
				isStatusBoxDefault = false;
			});
		}

		private void ChangeRunes(object sender, RoutedEventArgs e)
		{
			int recommendationNumber = int.Parse((sender as Button)!.Content.ToString() ?? "1") - 1;
			runeChange?.Invoke(recommendationNumber);
		}

		private void OnSelectionKeyDown(object sender, KeyEventArgs e)
		{
			if(e.Key == Key.Enter || e.Key == Key.Delete)
			{
				RemoveSelection();
			}
			if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.Up)
			{
				e.Handled = true;
				ReorderSelection(true);
			}
			else if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.Down)
			{
				e.Handled = true;
				ReorderSelection(false);
			}
		}

		private void OnChampionListKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
		{
			if (e.Key == System.Windows.Input.Key.Enter)
			{
				AddSelection();
			}
		}

		private void SetCheckInterval(object sender, RoutedEventArgs e)
		{
			MenuItem interval = (MenuItem)sender;
			int newIntervalIndex = 0;
			foreach (var siblingInterval in ((MenuItem)interval.Parent).Items.OfType<MenuItem>())
			{
				siblingInterval.IsChecked = false;

				if(siblingInterval == interval) DataCache.SetSetting("options.checkIntervalIndex", newIntervalIndex);
				else newIntervalIndex++;
			}

			interval.IsChecked = true;

			int newIntervalValue = int.Parse(interval.Header.ToString()!);

			ClientControl.checkInterval = newIntervalValue;
		}
    }
}