using System;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Media.Animation;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Input;
using System.Windows.Data;
using System.Globalization;
using Kalus.UI.Converters;
using System.Windows.Controls;

namespace Kalus.UI.Controls.Components
{
	public class ToggleSwitch : ToggleButton
	{
		private double _transformToState;


		static ToggleSwitch()
		{
			DefaultStyleKeyProperty.OverrideMetadata(typeof(ToggleSwitch), new FrameworkPropertyMetadata(typeof(ToggleSwitch)));
		}

		public ToggleSwitch()
		{
			Checked += OnSwitchOn;
			Unchecked += OnSwitchOff;
			MouseEnter += OnMouseEnter;
			MouseLeave += OnMouseLeave;
			Loaded += SwitchButton_Loaded;


			ResourceDictionary resourceDictionary = new()
			{
				Source = new Uri("/Kalus;component/UI/Resources/Template/ToggleSwitch.xaml", UriKind.Relative)
			};
			Style = (Style)resourceDictionary["ToggleSwitchStyle"];



		}

		protected override Size ArrangeOverride(Size arrangeBounds)
		{
			if (ActualHeight > 0)
			{
				ToggleSwitchHeightToWidth converter = new(); // Create an instance of the converter
				double width = (double)converter.Convert(ActualHeight, typeof(double), -1, CultureInfo.CurrentCulture);
				if (!double.IsNaN(width))
				{
					Width = width;
				}
			}

			return base.ArrangeOverride(arrangeBounds);
		}

		private void SwitchButton_Loaded(object sender, RoutedEventArgs e)
		{
			Binding binding = new("ActualHeight")
			{
				Source = this,
				Converter = new ToggleSwitchHeightToWidth()
			};
			this.SetBinding(WidthProperty, binding);

			_transformToState = ActualHeight / 2;
			if(Template.FindName("bubble", this) is Ellipse bubble) bubble.RenderTransform = new TranslateTransform(-_transformToState, 0);
		}

		private void OnSwitchOn(object sender, System.Windows.RoutedEventArgs e)
		{
			ToggleAnimation(true);
		}

		private void OnSwitchOff(object sender, RoutedEventArgs e)
		{
			ToggleAnimation(false);
		}

		private void OnMouseEnter(object sender, MouseEventArgs e)
		{
			HoverAnimation(true);
		}

		private void OnMouseLeave(object sender, MouseEventArgs e)
		{
			HoverAnimation(false);
		}

		private void ToggleAnimation(bool toggleOn, double animationDuration = 0.2)
		{
			if (this.Template.FindName("bubble", this) is not Ellipse bubble || this.Template.FindName("background", this) is not Rectangle background) { return; }
			//_bubblePosition ??= new BubblePosition(bubble.Margin, new Thickness(this.ActualWidth - bubble.ActualWidth - bubble.Margin.Left, 0, 0, 0));

			DoubleAnimation bubbleTranslateAnimation = new()
			{
				To = toggleOn ? _transformToState : -_transformToState,
				Duration = TimeSpan.FromSeconds(animationDuration)
			};


			ColorAnimation backgroundColorAnimation = new()
			{
				To = toggleOn ? Color.FromRgb(0, 120, 215) : Colors.Transparent,
				Duration = TimeSpan.FromSeconds(animationDuration)
			};
			ColorAnimation backgroundStrokeAnimation = new()
			{
				To = toggleOn ? Colors.Transparent : Color.FromRgb(33, 33, 33),
				Duration = TimeSpan.FromSeconds(animationDuration)
			};

			ColorAnimation bubbleColorAnimation = new()
			{
				To = toggleOn ? Colors.White : Color.FromRgb(33, 33, 33),
				Duration = TimeSpan.FromSeconds(animationDuration)
			};

			// Create the Storyboard and add the animation to it
			Storyboard storyboard = new();
			storyboard.Children.Add(backgroundColorAnimation);
			storyboard.Children.Add(backgroundStrokeAnimation);
			storyboard.Children.Add(bubbleTranslateAnimation);
			storyboard.Children.Add(bubbleColorAnimation);

			// Set the target and property of the animation
			Storyboard.SetTarget(backgroundColorAnimation, background);
			Storyboard.SetTargetProperty(backgroundColorAnimation, new PropertyPath("(Rectangle.Fill).(SolidColorBrush.Color)"));

			Storyboard.SetTarget(backgroundStrokeAnimation, background);
			Storyboard.SetTargetProperty(backgroundStrokeAnimation, new PropertyPath("(Rectangle.Stroke).(SolidColorBrush.Color)"));

			Storyboard.SetTarget(bubbleTranslateAnimation, bubble);
			Storyboard.SetTargetProperty(bubbleTranslateAnimation, new PropertyPath("(Ellipse.RenderTransform).(TranslateTransform.X)"));

			Storyboard.SetTarget(bubbleColorAnimation, bubble);
			Storyboard.SetTargetProperty(bubbleColorAnimation, new PropertyPath("(Ellipse.Fill).(SolidColorBrush.Color)"));
			// Start the animation
			storyboard.Begin();
		}

		private void HoverAnimation(bool isHover)
		{
			if (this.Template.FindName("bubble", this) is not Ellipse bubble) return;

			DoubleAnimation bubbleSizeAnimation = new()
			{
				To = isHover ? bubble.Width * 1.1 : bubble.Width / 1.1,
				Duration = TimeSpan.FromMilliseconds(200)
			};

			bubble.BeginAnimation(WidthProperty, bubbleSizeAnimation);
		}
	}
}
