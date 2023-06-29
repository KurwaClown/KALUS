using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using Kalus.UI.Controls.Tabs.Console;

namespace Kalus.UI.Controls.Tabs.Console
{
    /// <summary>
    /// Interaction logic for Log.xaml
    /// </summary>
    public partial class Log : UserControl
    {
        public static readonly DependencyProperty TimestampProperty =
        DependencyProperty.Register("Timestamp", typeof(DateTime), typeof(Log), new PropertyMetadata(DateTime.Now));

        public static readonly DependencyProperty MessageProperty =
            DependencyProperty.Register("Message", typeof(string), typeof(Log), new PropertyMetadata(string.Empty));

        public static readonly DependencyProperty LevelProperty =
            DependencyProperty.Register("Level", typeof(LogLevel), typeof(Log), new PropertyMetadata(LogLevel.Error));


        public static readonly DependencyProperty StateProperty =
            DependencyProperty.Register("State", typeof(ClientState), typeof(Log), new PropertyMetadata(ClientState.NoClient));

        public DateTime Timestamp
        {
            get { return (DateTime)GetValue(TimestampProperty); }
            set
            {
                SetValue(TimestampProperty, value);
            }
        }

        public string Message
        {
            get { return (string)GetValue(MessageProperty); }
            set
            {
                SetValue(MessageProperty, value);
            }
        }

        public LogLevel Level
        {
            get { return (LogLevel)GetValue(LevelProperty); }
            set
            {
                SetValue(LevelProperty, value);
            }
        }

        public ClientState State
        {
            get { return (ClientState)GetValue(StateProperty); }
            set
            {
                SetValue(StateProperty, value);
            }
        }
        public Log()
        {
            InitializeComponent();
        }
    }
}
