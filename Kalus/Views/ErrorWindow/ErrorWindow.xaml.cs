using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

		private void Button_Click(object sender, RoutedEventArgs e)
		{
            this.Close();
		}

		private void CopyReport(object sender, RoutedEventArgs e)
		{
			Clipboard.SetText(this.Report);
		}

	}
}
