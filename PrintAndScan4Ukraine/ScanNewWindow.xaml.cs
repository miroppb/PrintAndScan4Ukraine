using CodingSeb.Localization;
using Newtonsoft.Json;
using PrintAndScan4Ukraine.Data;
using PrintAndScan4Ukraine.Model;
using PrintAndScan4Ukraine.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
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
				barCode = barCode.Replace("\0", "").Trim();
				if (barCode != string.Empty) //make sure that the barcode is an actual alphanumeric string
				{
					Regex regex = new Regex("cv\\d\\d\\d\\d\\d\\d\\dus");
					Match match = regex.Match(barCode);
					if (match.Success)
					{
						if (Packages.FirstOrDefault(x => x == barCode) == null)
						{
							bool? DoubleCheck = _viewModel.VerifyIfExists(barCode);
							if (DoubleCheck == null) { MessageBox.Show(Loc.Tr("PAS4U.MainWindow.Offline", "You're Offline")); }
							else if (DoubleCheck.Value)
							{
								WasSomethingSet = false;
								MessageBox.Show(Loc.Tr("PAS4U.ScanNewWindow.AlreadyExistsText", "Package already exists"));
								BarCodeThatWasSet = barCode;
							}
							else
							{
								_viewModel.Insert(new()
								{
									PackageId = barCode,
									Contents = JsonConvert.SerializeObject(new List<Contents>() { })
								});
								_viewModel.InsertRecordStatus(new()
						{
							new() {
								PackageId = barCode, CreatedDate = DateTime.Now, Status = 1
							}
						});
								WasSomethingSet = true;
								BarCodeThatWasSet = barCode;
							}

						}
						else
						{
							WasSomethingSet = false;
							MessageBox.Show(Loc.Tr("PAS4U.ScanNewWindow.AlreadyExistsText", "Package already exists"));
							BarCodeThatWasSet = barCode;
						}
					}
					else
					{
						WasSomethingSet = false;
						MessageBox.Show(Loc.Tr("PAS4U.ScanNewWindow.WrongFormatText", "Package number not in correct format"));
						BarCodeThatWasSet = string.Empty;
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
