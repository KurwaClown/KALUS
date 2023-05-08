using League;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace KurwApp
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		public ObservableCollection<string> champListCollection { get; set; } = new ObservableCollection<string>();
		public ObservableCollection<string> selectedListCollection { get; set; } = new ObservableCollection<string>();

		internal bool IsAutoReadyOn;
		internal int summonerId = 0;

		public MainWindow()
		{
			InitializeComponent();
			DataContext = this;
			LoadAndSetCharacterList();

			Thread auth_thread = new Thread(() => Client_Control.EnsureAuthentication(this));
			Thread clientphase_thread = new Thread(() => Client_Control.ClientPhase(this));
			clientphase_thread.Start();
			auth_thread.Start();
		}



		internal void EnableRandomSkinButton(bool isEnabled)
		{
			Dispatcher.Invoke(() => random_btn.IsEnabled = isEnabled);
		}

		internal void ShowLolState(bool isEnabled)
		{
			Color borderColor = isEnabled ? Colors.Green : Colors.Red;

			Dispatcher.Invoke(()=> statusBorder.BorderBrush = new SolidColorBrush(borderColor));
		}

		internal void ChangeCharacterIcon(byte[] image)
		{
			BitmapImage bitmapImage = new BitmapImage();

			using (MemoryStream stream = new MemoryStream(image))
			{
				Dispatcher.Invoke(() =>
				{
					BitmapImage bitmapImage = new BitmapImage();
					bitmapImage.BeginInit();
					bitmapImage.StreamSource = stream;
					bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
					bitmapImage.EndInit();
					characterIcon.ImageSource = bitmapImage;
				});
			}

		}
		
		internal void ChangeCharacterIcon(bool reset)
		{
			BitmapImage bitmapImage = new BitmapImage();


				Dispatcher.Invoke(() =>
				{
					BitmapImage bitmapImage = new BitmapImage(new Uri("4285.jpg", UriKind.RelativeOrAbsolute));
					characterIcon.ImageSource = bitmapImage;
				});
		}

		internal async void LoadAndSetCharacterList()
		{
			var response = await Client_Request.GetChampionsInfo();
			var champions = JArray.Parse(response);
			var championNames = champions.Where(champion => (int)champion["id"]!=-1).Select(champion => champion["name"].ToString()).ToArray();
			Array.Sort(championNames);
			foreach (string champName in championNames)
			{
				Dispatcher.Invoke((Delegate)(()=> champListCollection.Add(champName)));
			}
		}

		internal void SavePreference(string token, bool value, string file = "preferences.json")
		{
			JObject preferences = JObject.Parse(File.ReadAllText($"Configurations/{file}"));
			preferences.SelectToken(token).Replace(value);
			File.WriteAllText($"Configurations/{file}", preferences.ToString());
		}

		internal void SavePreference(string token, int value, string file = "preferences.json")
		{
			JObject preferences = JObject.Parse(File.ReadAllText($"Configurations/{file}"));
			preferences.SelectToken(token).Replace(value);
			File.WriteAllText($"Configurations/{file}", preferences.ToString());
		}


		private void random_btn_Click(object sender, RoutedEventArgs e)
		{
			Client_Control.PickRandomSkin();
		}

		private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			
			Application.Current.Shutdown();
			Environment.Exit(0);
		}

		private async void Button_Click(object sender, RoutedEventArgs e) { 
			await Client_Request.RestartLCU();
		}

		private void CheckBox_Checked(object sender, RoutedEventArgs e)
		{
			
		}

		internal int GetRadioStackPreference(RadioButton radioButton)
		{
			var parentStack = radioButton.Parent as StackPanel;
			var preference = parentStack.Children.OfType<RadioButton>().FirstOrDefault(radio => radio.IsChecked == true);
			return Int32.Parse(preference.Tag.ToString());
		}

		private void selectionListAdd_Click(object sender, RoutedEventArgs e)
		{
			if (champList.SelectedItem == null) return;
			var selection = champList.SelectedValue.ToString();
			
			champListCollection.Remove(selection);
			selectedListCollection.Add(selection);
			
		}

		private void selectionListRemove_Click(object sender, RoutedEventArgs e)
		{
			if(selectionList.SelectedItem == null) return;
			var selection = selectionList.SelectedValue.ToString();
			
			champListCollection.Add(selection);
			champListCollection = new ObservableCollection<string>(champListCollection.OrderBy(i => i));	
			champList.ItemsSource = champListCollection;

			selectedListCollection.Remove(selection);
			
		}

		private void randomOnPick_Checked(object sender, RoutedEventArgs e)
		{
			SavePreference("randomSkin.randomOnPick", true);
		}

		private void OnControlInteraction(object sender, RoutedEventArgs e)
		{
			if(sender is CheckBox checkBox)
			{
				SavePreference(checkBox.Tag.ToString(), (bool)checkBox.IsChecked);
			}
			else if(sender is RadioButton radioButton)
			{
				int radioPreference = GetRadioStackPreference(radioButton);
				StackPanel parent = radioButton.Parent as StackPanel;
				SavePreference(parent.Tag.ToString() + ".userPreference", radioPreference);

			}
		}

		private void OnSettingsControlInteraction(object sender, RoutedEventArgs e)
		{
			
			SavePreference((sender as CheckBox).Tag.ToString(), (bool)(sender as CheckBox).IsChecked, file: "settings.json");
		}

		private void IsEnabledChanged(object sender, DependencyPropertyChangedEventArgs e)
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


			Thread preferences = new Thread(() => Client_Control.SetPreferences(this));
			preferences.Start();

			Thread settings = new Thread(()=> Client_Control.SetSettings(this));
			settings.Start();
		}

		private void ComboboxSelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (sender is ComboBox comboBox)
			{
				if (comboBox.SelectedIndex == -1)
				{
					return;
				}
				SavePreference(comboBox.Tag.ToString(), comboBox.SelectedIndex);
			}
		}

		internal bool IsCheckboxChecked(CheckBox checkBox)
		{
			return (bool)checkBox.IsChecked;
		}

		internal void ChangeTest(string debug)
		{
			Dispatcher.Invoke(() => testing.Text = debug);
		}
	}
}
