using CodingSeb.Localization;
using Newtonsoft.Json;
using PrintAndScan4Ukraine.Data;
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
	/// Interaction logic for ScanNewWindow.xaml
	/// </summary>
	public partial class ScanNewWindow : Window
	{
		private string barCode = string.Empty;
		private PackagesViewModel _viewModel;
		public bool WasSomethingSet = false;
		private List<string> Packages = new List<string>();
		public string BarCodeThatWasSet = string.Empty;

		public ScanNewWindow(List<string> packages)
		{
			InitializeComponent();
			_viewModel = new PackagesViewModel(new PackageDataProvider(), Access.None);
			Packages = packages;
		}

		private void Window_Loaded(object sender, RoutedEventArgs e)
		{
			PreviewKeyDown += labelBarCode_PreviewKeyDown;
		}

		private void labelBarCode_PreviewKeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Enter)
			{
				if (barCode.Replace("\0", "").Trim() != string.Empty) //make sure that the barcode is an actual alphanumeric string
				{
					if (Packages.FirstOrDefault(x => x == barCode.Replace("\0", "").Trim()) == null)
					{
						_viewModel.Insert(new()
						{
							PackageId = barCode.Replace("\0", "").Trim(),
							Contents = JsonConvert.SerializeObject(new List<Contents>() { })
						});
						_viewModel.InsertRecordStatus(new()
						{
							new() {
								PackageId = barCode.Replace("\0", "").Trim(), CreatedDate = DateTime.Now, Status = 1
							}
						});
						WasSomethingSet = true;
						BarCodeThatWasSet = barCode.Replace("\0", "").Trim();
					}
					else
					{
						WasSomethingSet = false;
						MessageBox.Show(Loc.Tr("PAS4U.ScanNewWindow.AlreadyExistsText", "Package already exists"));
						BarCodeThatWasSet = barCode.Replace("\0", "").Trim();
					}
				}

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
