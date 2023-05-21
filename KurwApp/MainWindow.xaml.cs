﻿using KurwApp.Modules;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace KurwApp
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

		public MainWindow()
		{
			InitializeComponent();

			Thread authentication = new(() => Client_Control.EnsureAuthentication(this));
			Thread clientPhase = new(() => Client_Control.ClientPhase(this));

			authentication.Start();
			clientPhase.Start();
		}

		internal void EnableRandomSkinButton(bool isEnabled)
		{
			Dispatcher.Invoke(() => random_btn.IsEnabled = isEnabled);
		}

		internal void ShowLolState(bool isEnabled)
		{
			Color borderColor = isEnabled ? Colors.Green : Colors.Red;

			Dispatcher.Invoke(() => statusBorder.BorderBrush = new SolidColorBrush(borderColor));
		}

		internal void SetChampionIcon(byte[] image)
		{
			SetImageBrush(characterIcon, image);

			Dispatcher.Invoke(() =>isStatusBoxDefault = false);

		}

		internal void SetImageBrush(ImageBrush imageBrush, byte[] image)
		{
			using (MemoryStream stream = new(image))
			{
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
		}
		internal void SetImageSource(Image image, byte[] imageStream)
		{
			using (MemoryStream stream = new(imageStream))
			{
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
		}

		internal void SetRunesIcons(byte[] primaryRune, byte[] subRune)
		{
			SetImageBrush(mainStyleIcon, primaryRune);
			SetImageBrush(subStyleIcon, subRune);

			Dispatcher.Invoke(() => isStatusBoxDefault = false);
		}

		internal async void SetGameModeIcon(string gameMode, bool inGame = false)
		{

			byte[] icon = gameMode switch
			{
				"Draft" or "Blind" => await ClientDataCache.GetClassicMapIcon(inGame),
				"ARAM" => await ClientDataCache.GetAramMapIcon(inGame),
				_ => await ClientDataCache.GetDefaultMapIcon(),
			};

			SetImageSource(gameModeIcon, icon);

			Dispatcher.Invoke(() => isStatusBoxDefault = false);
		}

		internal async void SetDefaultIcons()
		{
			byte[] defaultRunesIcon = await ClientDataCache.GetDefaultRuneIcon();
			SetImageBrush(mainStyleIcon, defaultRunesIcon);
			SetImageBrush(subStyleIcon, defaultRunesIcon);

			SetChampionIcon(await ClientDataCache.GetDefaultChampionIcon());

			SetImageSource(gameModeIcon, await ClientDataCache.GetDefaultMapIcon());
		}



		internal async void LoadAndSetCharacterList()
		{
			var champions = await ClientDataCache.GetChampionsInformations();
			Dictionary<int, string> championNames = champions.Where(champion => (int)champion["id"] != -1)
															.ToDictionary(champion => (int)champion["id"], champion => (string)champion["name"]);
			championNames = championNames.OrderBy(champion => champion.Value).ToDictionary(champion => champion.Key, champion => champion.Value);

			Dispatcher.Invoke(() =>
			{
				foreach (KeyValuePair<int, string> champName in championNames)
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

			JArray blindPicks = JArray.Parse(File.ReadAllText("Picks/Blind.json"));
			var champListBoxItems = ChampListCollection.Where(champion => blindPicks
														.Select(token => (int)token).ToArray()
														.Contains(int.Parse(champion.Tag.ToString())));

			foreach (var champ in champListBoxItems)
			{
				SelectedListCollection.Add(champ);
			}

			UpdatedListCollection = new ObservableCollection<ListBoxItem>(ChampListCollection.Except(SelectedListCollection));

			champList.ItemsSource = UpdatedListCollection;
			selectionList.ItemsSource = SelectedListCollection;

		}

		internal static void SaveConfiguration(string token, dynamic value, string file = "preferences.json")
		{
			JObject preferences = JObject.Parse(File.ReadAllText($"Configurations/{file}"));
			preferences.SelectToken(token).Replace(value);
			File.WriteAllText($"Configurations/{file}", preferences.ToString());
		}

		private void RandomSkinClick(object sender, RoutedEventArgs e)
		{
			Client_Control.PickRandomSkin();
		}

		private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			Application.Current.Shutdown();
			Environment.Exit(0);
		}

		private async void ClientRestart(object sender, RoutedEventArgs e)
		{
			await Client_Request.RestartLCU();
		}

		private void AddSelection(object sender, RoutedEventArgs e)
		{
			if (champList.SelectedItem == null) return;
			var selection = champList.SelectedItem as ListBoxItem;

			SelectedListCollection.Add(selection);
			UpdatedListCollection.Remove(selection);

			SavePicksModification();
		}

		private void RemoveSelection(object sender, RoutedEventArgs e)
		{
			if (selectionList.SelectedItem == null) return;
			var selection = selectionList.SelectedItem as ListBoxItem;

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
			switch (gameType)
			{
				default:
					break;

				case "Draft":
					var pickType = ((ComboBoxItem)selectionListType.SelectedItem).Content.ToString();

					var draftFile = JObject.Parse(File.ReadAllText($"Picks/{pickType}.json"));

					var position = ((ComboBoxItem)selectionListPosition.SelectedItem).Content.ToString();

					var fileRole = position == "Support" ? "UTILITY" : position.ToUpper();

					draftFile[fileRole] = new JArray(SelectedListCollection.Select(i => new JValue(int.Parse(i.Tag.ToString()))));

					File.WriteAllText($"Picks/{pickType}.json", draftFile.ToString());
					break;

				case "Blind":
				case "ARAM":
					var file = JArray.Parse(File.ReadAllText($"Picks/{gameType}.json"));
					file = new JArray(SelectedListCollection.Select(i => new JValue(int.Parse(i.Tag.ToString()))));
					File.WriteAllText($"Picks/{gameType}.json", file.ToString());
					break;
			}
		}

		private void OnControlInteraction(object sender, RoutedEventArgs e)
		{
			if (sender is CheckBox checkBox)
			{
				SaveConfiguration(checkBox.Tag.ToString(), (bool)checkBox.IsChecked);
			}
			else if (sender is RadioButton radioButton)
			{
				if ((bool)!radioButton.IsChecked) return;
				int radioPreference = int.Parse(radioButton.Tag.ToString());
				SaveConfiguration(radioButton.GroupName, radioPreference);
			}
		}

		private void OnSettingsControlInteraction(object sender, RoutedEventArgs e)
		{
			SaveConfiguration((sender as CheckBox).Tag.ToString(), (bool)(sender as CheckBox).IsChecked, file: "settings.json");
		}

		private void IsEnabledModified(object sender, DependencyPropertyChangedEventArgs e)
		{
			if (sender is ComboBox comboBox)
			{
				if (comboBox.IsEnabled)
				{
					JObject preference = JObject.Parse(File.ReadAllText("Configurations/preferences.json"));

					comboBox.SelectedIndex = Int32.Parse(preference.SelectToken(comboBox.Tag.ToString()).ToString());
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
					JObject preference = JObject.Parse(File.ReadAllText("Configurations/preferences.json"));

					checkBox.IsChecked = (bool)preference.SelectToken(checkBox.Tag.ToString());
				}
			}
		}

		//Set the preferences saved in the preferences.json to the ui
		internal void SetPreferences()
		{
			var preferences = JObject.Parse(File.ReadAllText("Configurations/preferences.json"));

			void setRadioByPreference(StackPanel stack, string token)
			{
				stack.Children.OfType<RadioButton>()
					.Where(child => child.Tag.ToString() == preferences[token]["userPreference"].ToString()).First().IsChecked = true;
				var comboboxes = stack.Children.OfType<ComboBox>();
				if (comboboxes.Any())
				{
					comboboxes.First().SelectedIndex = comboboxes.First().IsEnabled ? (int)preferences[token]["OTLTimeIndex"] : -1;
				}
			}

			Dispatcher.Invoke(() =>
			{
				setRadioByPreference(picksPreferences, "picks");
				setRadioByPreference(bansPreferences, "bans");
				setRadioByPreference(noAvailablePreferences, "noPicks");
				setRadioByPreference(onSelectionPreferences, "selections");

				if (stillAutoPickOTL.IsEnabled) { stillAutoPickOTL.IsChecked = (bool)preferences["selections"]["OTL"]; }

				notSetPageAsActive.IsChecked = (bool)preferences["runes"]["notSetActive"];
				overridePage.IsChecked = (bool)preferences["runes"]["overridePage"];

				addChromas.IsChecked = (bool)preferences["randomSkin"]["addChromas"];
				randomOnPick.IsChecked = (bool)preferences["randomSkin"]["randomOnPick"];

				alwaysSnowball.IsChecked = (bool)preferences["summoners"]["alwaysSnowball"];
			}
			);
		}

		internal void SetSettings()
		{
			var settings = JObject.Parse(File.ReadAllText("Configurations/settings.json"));

			Dispatcher.Invoke(() =>
			{
				autoPickSetting.IsChecked = (bool)settings["championPick"];
				autoBanSetting.IsChecked = (bool)settings["banPick"];
				autoReadySetting.IsChecked = (bool)settings["aramChampionSwap"];
				autoRunesSetting.IsChecked = (bool)settings["runesSwap"];
				autoSwapSetting.IsChecked = (bool)settings["autoReady"];
			}
			);
		}

		private void Window_Loaded(object sender, RoutedEventArgs e)
		{
			LoadAndSetCharacterList();

			Action<int, ComboBox> populateComboBox = (maxTime, comboBox) =>
			{
				for (int i = 1; i <= maxTime / 5; i++)
				{
					comboBox.Items.Add(i * 5);
				}
			};
			populateComboBox(30, picksTimeLeft);
			populateComboBox(30, bansTimeLeft);
			populateComboBox(30, stillPickTimeLeft);

			SetSettings();
			SetPreferences();
			SetDefaultIcons();
			SetDefaultLabels();
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

		private void OTLChange(object sender, SelectionChangedEventArgs e)
		{
			if (sender is ComboBox comboBox)
			{
				if (comboBox.SelectedIndex == -1) return;

				SaveConfiguration(comboBox.Tag.ToString(), comboBox.SelectedIndex);
			}
		}

		private void SelectionListChange(object sender, SelectionChangedEventArgs e)
		{
			var gameType = ((ComboBoxItem)selectionListGameType.SelectedItem).Content.ToString();

			IEnumerable<ListBoxItem> champListBoxItems;

			if (gameType == "Draft")
			{
				var pickType = ((ComboBoxItem)selectionListType.SelectedItem).Content.ToString();
				var draftFile = JObject.Parse(File.ReadAllText($"Picks/{pickType}.json"));

				var position = ((ComboBoxItem)selectionListPosition.SelectedItem).Content.ToString();
				var fileRole = position == "Support" ? "UTILITY" : position.ToUpper();

				var champsId = (JArray)draftFile[fileRole];

				champListBoxItems = ChampListCollection.Where(champion => champsId.Select(token => (int)token).ToArray().Contains(int.Parse(champion.Tag.ToString())));
			}
			else
			{
				var champsId = JArray.Parse(File.ReadAllText($"Picks/{gameType}.json"));
				champListBoxItems = ChampListCollection.Where(champion => champsId.Select(token => (int)token).ToArray().Contains(int.Parse(champion.Tag.ToString())));
			}

			SelectedListCollection.Clear();
			foreach (var champ in champListBoxItems)
			{
				SelectedListCollection.Add(champ);
			}

			UpdatedListCollection = new ObservableCollection<ListBoxItem>(ChampListCollection.Except(SelectedListCollection));
			champList.ItemsSource = UpdatedListCollection;
		}

		private void ReorderSelection(object sender, RoutedEventArgs e)
		{
			if (selectionList.SelectedItem == null) return;
			var selection = selectionList.SelectedItem as ListBoxItem;

			ObservableCollection<ListBoxItem>? observableCollection = selectionList.ItemsSource as ObservableCollection<ListBoxItem>;
			if (observableCollection == null) return;

			int oldIndex = observableCollection.IndexOf(selection);
			bool isPrevious = ((Button)sender).Name == "selectionOrderUp";
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
			Dispatcher.Invoke(() => {
				gameModeLbl.Content = gamemodeName;
				isStatusBoxDefault = false;
			});
		}
		internal string GetGamemodeName()
		{
			string gameMode = "";
			Dispatcher.Invoke(() => gameMode = gameModeLbl.Content.ToString());
			return gameMode;
		}

		internal void SetChampionName(string championName)
		{
			Dispatcher.Invoke (() => {
				championLbl.Content = championName;
				isStatusBoxDefault = false;
			});
		}
	}
}