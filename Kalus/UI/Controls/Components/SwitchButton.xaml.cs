﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Kalus.UI.Controls.Components
{
	/// <summary>
	/// Interaction logic for SwitchButton.xaml
	/// </summary>
	public partial class SwitchButton : UserControl
	{

		private BubblePosition? _bubblePosition;
		internal class BubblePosition
		{
			internal Thickness Left;
			internal Thickness Right;

			internal BubblePosition(Thickness defaultMargin, Thickness rightMargin)
			{
				Left = defaultMargin;
				Right = rightMargin;
			}
		}

		public SwitchButton()
		{
			InitializeComponent();
		}

		private void ToggleAnimation(bool toggleOn, ToggleButton switchButton, double animationDuration = 0.2)
		{
			if (switchButton.Template.FindName("bubble", switchButton) is not Ellipse bubble || switchButton.Template.FindName("background", switchButton) is not Rectangle background) { return; }
			_bubblePosition ??= new BubblePosition(bubble.Margin, new Thickness(switchButton.ActualWidth - bubble.ActualWidth - bubble.Margin.Left, 0, 0, 0));

			ThicknessAnimation bubbleMarginAnimation = new()
			{
				To = toggleOn ? _bubblePosition.Right : _bubblePosition.Left,
				Duration = TimeSpan.FromSeconds(animationDuration)
			};

			ColorAnimation backgroundColorAnimation = new()
			{
				To = toggleOn ? Color.FromRgb(0, 120, 215) : Colors.Gray,
				Duration = TimeSpan.FromSeconds(animationDuration)
			};

			ColorAnimation bubbleColorAnimation = new()
			{
				To = toggleOn ? Colors.Black : Colors.White,
				Duration = TimeSpan.FromSeconds(animationDuration)
			};

			// Create the Storyboard and add the animation to it
			Storyboard storyboard = new();
			storyboard.Children.Add(backgroundColorAnimation);
			storyboard.Children.Add(bubbleMarginAnimation);
			storyboard.Children.Add(bubbleColorAnimation);

			// Set the target and property of the animation
			Storyboard.SetTarget(backgroundColorAnimation, background);
			Storyboard.SetTargetProperty(backgroundColorAnimation, new PropertyPath("(Rectangle.Fill).(SolidColorBrush.Color)"));

			Storyboard.SetTarget(bubbleMarginAnimation, bubble);
			Storyboard.SetTargetProperty(bubbleMarginAnimation, new PropertyPath("Margin"));

			Storyboard.SetTarget(bubbleColorAnimation, bubble);
			Storyboard.SetTargetProperty(bubbleColorAnimation, new PropertyPath("(Ellipse.Fill).(SolidColorBrush.Color)"));
			// Start the animation
			storyboard.Begin();
		}

		private void HoverAnimation(bool isHover, ToggleButton switchButton)
		{
			if (switchButton.Template.FindName("bubble", switchButton) is not Ellipse bubble) return;

			DoubleAnimation bubbleSizeAnimation = new()
			{
				To = isHover ? bubble.Width * 1.2 : bubble.Width / 1.2,
				Duration = TimeSpan.FromMilliseconds(100)
			};

			bubble.BeginAnimation(WidthProperty, bubbleSizeAnimation);
		}

		private void OnSwitchOn(object sender, System.Windows.RoutedEventArgs e)
		{
			ToggleAnimation(true, (ToggleButton)sender);
		}

		private void OnSwitchOff(object sender, RoutedEventArgs e)
		{
			ToggleAnimation(false, (ToggleButton)sender);
		}

		private void OnMouseEnter(object sender, MouseEventArgs e)
		{
			HoverAnimation(true, (ToggleButton)sender);
		}

		private void OnMouseLeave(object sender, MouseEventArgs e)
		{
			HoverAnimation(false, (ToggleButton)sender);
		}
	}
}
