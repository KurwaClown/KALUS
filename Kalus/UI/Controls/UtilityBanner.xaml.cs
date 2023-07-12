using System.Windows;
using System.Windows.Controls;

namespace Kalus.UI.Controls
{
	/// <summary>
	/// Interaction logic for UtilityBanner.xaml
	/// </summary>
	public partial class UtilityBanner : UserControl
	{
		public static readonly DependencyProperty IsCheckedProperty = DependencyProperty.Register("IsChecked", typeof(bool), typeof(UtilityBanner));

		public bool IsChecked
		{
			get { return (bool)GetValue(IsCheckedProperty); }
			set { SetValue(IsCheckedProperty, value); }
		}

		public UtilityBanner()
		{
			InitializeComponent();
		}
	}
}