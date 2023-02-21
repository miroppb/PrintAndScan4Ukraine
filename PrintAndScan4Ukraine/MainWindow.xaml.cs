using System;
using System.Collections.Generic;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace PrintAndScan4Ukraine
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		public MainWindow()
		{
			InitializeComponent();
		}

		private void BtnPrint_Click(object sender, RoutedEventArgs e)
		{
			this.Hide();
			PrintWindow pw = new PrintWindow();
			pw.ShowDialog();
			this.Show();
		}

		private void BtnScan_Click(object sender, RoutedEventArgs e)
		{
			this.Hide();
			ScanWindow sw = new ScanWindow();
			sw.ShowDialog();
			this.Show();
		}
	}
}
