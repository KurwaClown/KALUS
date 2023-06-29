using System;
using System.Windows;

namespace Kalus.UI.Views
{
    /// <summary>
    /// Interaction logic for ErrorWindow.xaml
    /// </summary>
    public partial class ErrorWindow : Window
    {
		public string ErrorMessage { get; set; } = "An error was encountered";
		public string HelpMeImprove { get; set; } = "Help me improve KALUS!\r\r"
													+ "Add any message you want below then paste the link into your browser and click 'Submit new issue'.\r"
													+ "Any contributions are most welcomed!";
		public string Report { get; set; } = "";

		public ErrorWindow()
        {
            InitializeComponent();

            DataContext = this;
        }

		private void CloseApp(object sender, RoutedEventArgs e)
		{
			Close();
		}

		private void SendReport(object sender, RoutedEventArgs e)
		{
			string userMessage = reportMessage.Text == "" ? "No message" : reportMessage.Text;
			string baseUrl = "https://github.com/KurwaClown/KALUS/issues/new";
			string issueTitle = "KALUS auto report";
			string issueBody = $"## User Message\r{userMessage}\r"+ Report;

			UriBuilder uriBuilder = new(baseUrl)
			{
				Query = $"title={Uri.EscapeDataString(issueTitle)}&body={Uri.EscapeDataString(issueBody)}"
			};

			string url = uriBuilder.ToString();

			Clipboard.SetText(url);
		}

	}
}
