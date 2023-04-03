﻿using CodingSeb.Localization;
using miroppb;
using Newtonsoft.Json;
using PrintAndScan4Ukraine.Model;
using PrintAndScan4Ukraine.ViewModel;
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
using System.Windows.Shapes;

namespace PrintAndScan4Ukraine
{
	/// <summary>
	/// Interaction logic for MarkAsDeliveredWindow.xaml
	/// </summary>
	public partial class MarkAsDeliveredWindow : Window
	{
		private string barCode = string.Empty;
		private List<string> barCodes = new List<string>();
		public PackagesViewModel _viewModel;
		public MarkAsDeliveredWindow(PackagesViewModel viewModel)
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
			List<Package_Status> statuses = new List<Package_Status>();
			barCodes.ForEach(x => statuses.Add(new() { PackageId = x, CreatedDate = DateTime.Now, Status = 4 }));

			_viewModel.InsertRecordStatus(statuses);

			Close();
		}

		public static char ToChar(Key key)
		{
			char c = '\0';
			if ((key >= Key.A) && (key <= Key.Z))
			{
				c = (char)('a' + (key - Key.A)); //Ignore letters
			}

			else if ((key >= Key.D0) && (key <= Key.D9))
			{
				c = (char)('0' + (key - Key.D0));
			}

			return c;
		}
	}
}
