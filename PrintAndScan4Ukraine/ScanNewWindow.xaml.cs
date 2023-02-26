using Newtonsoft.Json;
using PrintAndScan4Ukraine.Data;
using PrintAndScan4Ukraine.Model;
using PrintAndScan4Ukraine.ViewModel;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;

namespace PrintAndScan4Ukraine
{
	/// <summary>
	/// Interaction logic for ScanNewWindow.xaml
	/// </summary>
	public partial class ScanNewWindow : Window
	{
		private string barCode = string.Empty;
		private PackagesViewModel _viewModel;

		public ScanNewWindow()
		{
			InitializeComponent();
			_viewModel = new PackagesViewModel(new PackageDataProvider());
		}

		private void Window_Loaded(object sender, RoutedEventArgs e)
		{
			PreviewKeyDown += labelBarCode_PreviewKeyDown;
		}

		private void labelBarCode_PreviewKeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Enter)
			{
				if (barCode.Replace("\0", "") != string.Empty) //make sure that the barcode is an actual alphanumeric string
					_viewModel.Insert(new Package() { PackageId = barCode.Replace("\0", ""), Date_Added = DateTime.Now, Contents = JsonConvert.SerializeObject(new List<Contents>() { }) });
				
				barCode = string.Empty;
				e.Handled = true;
				Close();
			}
			barCode += ToChar(e.Key);
		}

		public static char ToChar(Key key)
		{
			char c = '\0';
			if ((key >= Key.A) && (key <= Key.Z))
			{
				c = (char)((int)'a' + (int)(key - Key.A)); //Ignore letters
			}

			else if ((key >= Key.D0) && (key <= Key.D9))
			{
				c = (char)((int)'0' + (int)(key - Key.D0));
			}

			return c;
		}
	}
}
