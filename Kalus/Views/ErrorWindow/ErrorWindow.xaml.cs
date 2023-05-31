using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Kalus.Views.ErrorWindow
{
    /// <summary>
    /// Interaction logic for ErrorWindow.xaml
    /// </summary>
    public partial class ErrorWindow : Window
    {
		public string ErrorMessage { get; set; } = "";
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
			string baseUrl = "https://github.com/KurwaClown/KALUS/issues/new";
			string issueTitle = "KALUS report";
			string issueBody = Report;

			UriBuilder uriBuilder = new(baseUrl)
			{
				Query = $"title={Uri.EscapeDataString(issueTitle)}&body={Uri.EscapeDataString(issueBody)}"
			};

			string url = uriBuilder.ToString();

			Clipboard.SetText(url);
		}

	}
}
