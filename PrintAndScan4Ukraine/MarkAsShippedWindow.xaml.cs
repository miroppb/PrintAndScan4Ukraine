﻿using CodingSeb.Localization;
using miroppb;
using Newtonsoft.Json;
using PrintAndScan4Ukraine.Model;
using PrintAndScan4Ukraine.ViewModel;
using System;
using System.Collections.Generic;
using System.IO;
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
			List<Package_Status> statuses = new List<Package_Status>();
			barCodes.ForEach(x => statuses.Add(new() { PackageId = x, CreatedDate = DateTime.Now, Status = 2 }));
			statuses = statuses.GroupBy(x => x.PackageId).Select(x => x.First()).ToList(); //remove duplicates
			List<Package> temp = new List<Package>();
			statuses.ForEach(x => temp.Add(new Package() { PackageId = x.PackageId }));

			List<Package> packages = _viewModel.Packages.Where(x => barCodes.Contains(x.PackageId.ToString())).ToList(); //this will not select any packages that aren't on list

			List<string> BarcodesNotInPackages = temp.Where(t => !packages.Where(x => x.PackageId == t.PackageId).Any()).Select(package => package.PackageId.ToString()).ToList(); //Find packages that were scanned but aren't on list

			if (BarcodesNotInPackages.Count > 0)
			{
				MessageBox.Show(string.Format($"{Loc.Tr("PAS4U.ScanShippedWindow.ScannedButNotOnListText", "Following barcodes were scanned but not on list of packages:{0}{0}{1}{0}{0}The new statuses will be added to the database.")}",
					Environment.NewLine, string.Join(", ", BarcodesNotInPackages)));
				StreamWriter w = new(Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + $"\\Shipped_{DateTime.Now:MM/dd/yyyy}.txt");
				w.WriteLine($"Barcodes scanned as shipped that weren't on the list on {DateTime.Now:MM/dd/yyyy}:");
				BarcodesNotInPackages.ForEach(x => w.WriteLine(x));
				w.Close();
			}


			_viewModel.InsertRecordStatus(statuses);
			if (barCodes.Count > 0)
			{
				_viewModel.Export(packages);
				if (MessageBox.Show($"{Loc.Tr("PAS4U.ScanShippedWindow.RemoveFromListText", "Should we remove these packages from the list?")}", "", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
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
