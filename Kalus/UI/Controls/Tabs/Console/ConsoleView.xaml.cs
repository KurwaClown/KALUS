﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using Forms = System.Windows.Forms;
using System.Windows.Input;
using Kalus.Modules;
using Kalus.UI.Controls.Tabs.Console;
using System.Resources;

namespace Kalus.UI.Controls.Tabs.Console
{
    /// <summary>
    /// Interaction logic for ConsoleView.xaml
    /// </summary>
    public partial class ConsoleView : UserControl
    {
        public ObservableCollection<LogData> Logs { get; } = new ObservableCollection<LogData>();
        public ConsoleView()
        {
            InitializeComponent();

            scrollViewer.PreviewMouseWheel += ScrollViewer_PreviewMouseWheel;

            DataContext = this;
        }

        private void ScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            // Determine the direction of the scroll
            int delta = e.Delta;
            ScrollDirection direction = delta > 0 ? ScrollDirection.Up : ScrollDirection.Down;

            // If the user scrolls up or down manually, prevent automatic scrolling
            if (direction == ScrollDirection.Up && scrollViewer.VerticalOffset == 0)
            {
                e.Handled = false;
                return;
            }
            else if (direction == ScrollDirection.Down && scrollViewer.VerticalOffset == scrollViewer.ScrollableHeight)
            {
                e.Handled = false;
                return;
            }

            // Allow manual scrolling using the scroll wheel
            if (e.Delta > 0)
            {
                scrollViewer.LineUp();
            }
            else
            {
                scrollViewer.LineDown();
            }

            // Prevent the event from bubbling up and triggering automatic scrolling
            e.Handled = true;
        }

        private enum ScrollDirection
        {
            Up,
            Down
        }

        internal void AddLog(string message, Utility utility, LogLevel level)
        {
			LogData newLog = new(message, utility, level);
            Application.Current.Dispatcher.Invoke(() =>
            {
                Logs.Add(newLog);
                // Scroll to the bottom of the ScrollViewer
                scrollViewer.ScrollToEnd();
            });
        }
    }
}
