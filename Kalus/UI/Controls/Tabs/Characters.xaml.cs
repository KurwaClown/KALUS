using Kalus.Modules;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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

namespace Kalus.UI.Controls.Tabs
{
	/// <summary>
	/// Interaction logic for Characters.xaml
	/// </summary>
	public partial class Characters : UserControl
	{
		private ObservableCollection<ListBoxItem> ChampListCollection { get; set; } = new ObservableCollection<ListBoxItem>();
		private ObservableCollection<ListBoxItem> UpdatedListCollection { get; set; } = new ObservableCollection<ListBoxItem>();
		private ObservableCollection<ListBoxItem> SelectedListCollection { get; set; } = new ObservableCollection<ListBoxItem>();

		public Characters()
		{
			InitializeComponent();
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

		private void OnSelectionKeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Enter || e.Key == Key.Delete)
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
	}
}
