using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Kalus.UI.Controls
{
	/// <summary>
	/// Interaction logic for UtilityBanner.xaml
	/// </summary>
	public partial class UtilityBanner : UserControl
	{
		public static readonly DependencyProperty IsCheckedProperty = DependencyProperty.Register("IsChecked", typeof(bool), typeof(UtilityBanner), new PropertyMetadata(false, OnIsCheckedPropertyChanged));
		public static readonly DependencyProperty LabelTextProperty = DependencyProperty.Register("LabelText", typeof(string), typeof(UtilityBanner));

		public bool IsChecked
		{
			get { return (bool)GetValue(IsCheckedProperty); }
			set { SetValue(IsCheckedProperty, value); }
		}

		public string LabelText
		{
			get => (string)GetValue(LabelTextProperty);
			set => SetValue(LabelTextProperty, value);
		}

		public UtilityBanner()
		{
			InitializeComponent();
		}

		private static void OnIsCheckedPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			Properties.Settings.Default.Save();
		}

		private void MouseEnterBackground(object sender, System.Windows.Input.MouseEventArgs e)
		{

				background.Background = new SolidColorBrush(Color.FromRgb(245,245,245));

		}

		private void MouseLeaveBackground(object sender, System.Windows.Input.MouseEventArgs e)
		{

				background.Background = new SolidColorBrush(Colors.White);

		}
	}
}