using Kalus.Modules;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace Kalus.Controls.Tabs
{
    /// <summary>
    /// Interaction logic for Preferences.xaml
    /// </summary>
    public partial class Preferences : UserControl
    {
        public Preferences()
        {
            InitializeComponent();
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
	}
}
