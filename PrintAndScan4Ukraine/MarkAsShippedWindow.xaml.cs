using CodingSeb.Localization;
using miroppb;
using Newtonsoft.Json;
using PrintAndScan4Ukraine.Model;
using PrintAndScan4Ukraine.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace PrintAndScan4Ukraine
{
	/// <summary>
	/// Interaction logic for ScannerWindow.xaml
	/// </summary>
	public partial class MarkAsShippedWindow : Window
	{
		private string barCode = string.Empty;
		private List<string> barCodes = new List<string>();
		public PackagesViewModel _viewModel;
		public MarkAsShippedWindow(PackagesViewModel viewModel)
		{
			InitializeComponent();
			_viewModel = viewModel;
		}

		private void Window_Loaded(object sender, RoutedEventArgs e)
		{
			PreviewKeyDown += labelBarCode_PreviewKeyDown;
		}

		private void labelBarCode_PreviewKeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Enter)
			{
				barCodes.Add(barCode.Replace("\0", ""));
				barCode = string.Empty;
				e.Handled = true;
				LblCodes.Text = $"{Loc.Tr("PAS4U.ScanShippedWindow.BarcodesScanned", "Barcodes Scanned")}: {barCodes.Count}";
			}
			barCode += ToChar(e.Key);
		}

		private void BtnDone_Click(object sender, RoutedEventArgs e)
		{
			LblCodes.Text = "Sending codes to database. Please wait...";
			libmiroppb.Log($"Scanned As Shipped: {JsonConvert.SerializeObject(barCodes)}");
			List<Package> packages = _viewModel.Packages.Where(x => barCodes.Contains(x.PackageId.ToString())).ToList();
			packages.ForEach(x => x.Date_Shipped = DateTime.Now);
			_viewModel.UpdateRecords(packages);
			if (barCodes.Count > 0)
			{
				_viewModel.Export(packages);
				if (MessageBox.Show("Should we remove these packages from the list?", "", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
				{
					packages.ForEach(x => x.Removed = true);
					_viewModel.UpdateRecords(packages);
				}
			}
			Close();
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
