using CodingSeb.Localization;
using miroppb;
using Newtonsoft.Json;
using OfficeOpenXml;
using OfficeOpenXml.ConditionalFormatting;
using PrintAndScan4Ukraine.Model;
using ServiceStack;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;

namespace PrintAndScan4Ukraine.ViewModel
{
	public partial class PackagesViewModel : INotifyPropertyChanged
	{
		public async Task LoadAsync()
		{
			if (IsOnline)
			{
				if (Packages.Any())
					Packages.Clear();

				var packages = await _packageDataProvider.GetAllAsync(true);
				if (packages != null)
					packages.ToList().ForEach(Packages.Add);
			}
		}

		public async Task<List<Package>?> LoadByNameAsync(string SenderName)
		{
			if (IsOnline)
			{
				List<Package> ListOfPackages = new List<Package>();

				var packages = await _packageDataProvider.GetByNameAsync(SenderName);
				if (packages != null)
					packages.ToList().ForEach(ListOfPackages.Add);

				return ListOfPackages;
			}
			return null;
		}

		public void ExecuteSave(object a)
		{
			Save();
            System.Windows.MessageBox.Show($"{Loc.Tr("PAS4U.MainWindow.PackageSaved", "Package has been saved manually")}", "");
		}

		public void Save()
		{
			Save(SelectedPackage);
		}

		public void Save(Package package, int type = 0)
		{
			if (IsOnline && package != null)
			{
				if (_packageDataProvider.UpdateRecords(new List<Package>() { package }, type))
				{
					LastSaved = $"Last Saved: {DateTime.Now.ToShortDateString()} {DateTime.Now.ToShortTimeString()}";
					package.Modified = false; //setting back as it was saved
				}
			}
		}

		public void SaveAll(object a)
		{
			if (IsOnline && Packages != null)
				if (_packageDataProvider.UpdateRecords(Packages.ToList()))
				{
					LastSaved = $"Last Saved: {DateTime.Now.ToShortDateString()} {DateTime.Now.ToShortTimeString()}";
					foreach (var package in Packages)
						package.Modified = false; //everything was saved
				}

		}

		public bool UpdateRecords(List<Package> packages, int type = 0)
		{
			if (IsOnline)
				return _packageDataProvider.UpdateRecords(packages, type);
			return false;
		}

		public bool Insert(Package package)
		{
			if (IsOnline)
			{
				_packageDataProvider.InsertRecord(package);
				return true;
			}
			else
				return false;
		}

		public bool InsertRecordStatus(List<Package_Status> package_statuses)
		{
			if (IsOnline)
			{
				_packageDataProvider.InsertRecordStatus(package_statuses);
				return true;
			}
			else
				return false;
		}

		public bool Export(IEnumerable<Package> packages, bool shippedButNotArrived = false)
		{
			SaveFileDialog sfd = new SaveFileDialog
			{
				Filter = "Excel File|*.xlsx"
			};
			if (sfd.ShowDialog() == DialogResult.OK)
			{
				//lets get all statuses
				List<Package_Status>? statuses = _packageDataProvider.GetAllStatuses()!.ToList();

				libmiroppb.Log($"Exporting to XLSX. Filename: {sfd.FileName}");
				ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.NonCommercial;

				using (var excelPack = new ExcelPackage(new FileInfo(sfd.FileName)))
				{
					List<Package_less> list = new List<Package_less>();
					foreach (Package package in packages)
					{
						Package temp = package.CreateCopy();
						List<Contents> t = package.Recipient_Contents.CreateCopy();
						List<string> output = new List<string>();
						foreach (var item in t) { output.Add($"{item.Name}: {item.Amount}"); }

						var jsonParent = JsonConvert.SerializeObject(temp);
						Package_less c = JsonConvert.DeserializeObject<Package_less>(jsonParent)!;
						c.Contents = output.Join(Environment.NewLine);

						List<Package_Status> s = statuses.Where(s => s.PackageId == c.PackageId).ToList();
						List<string> so = new List<string>();
						s.ForEach(x => so.Add(x.ToString()));
						c.Statuses = so.Join(Environment.NewLine);

						if (shippedButNotArrived)
						{
							if (s.FirstOrDefault(x => x.Status == 2) != null && s.FirstOrDefault(x => x.Status == 3) == null) //if contains status 2 but not 3
								list.Add(c);
						}
						else
							list.Add(c);
					}

					ExcelWorksheet? ws;
					try
					{
						ws = excelPack.Workbook.Worksheets.Add(DateTime.Now.ToShortDateString());
						ws.Cells[6, 1].LoadFromCollection(list, true, OfficeOpenXml.Table.TableStyles.Light8);
					}
					catch
					{
						ws = excelPack.Workbook.Worksheets[DateTime.Now.ToShortDateString()];
						int row = ws.Dimension.End.Row;
						ws.Cells[row + 1, 1].LoadFromCollection(list, false, OfficeOpenXml.Table.TableStyles.Light8);
					}
					ws.Cells.Style.VerticalAlignment = OfficeOpenXml.Style.ExcelVerticalAlignment.Top;
					ws.Cells[$"C7:C{ws.Dimension.End.Row}"].Style.WrapText = true; //Sender Address
					ws.Cells[$"F7:F{ws.Dimension.End.Row + 1}"].Style.WrapText = true; //Recipient Address
					ws.Cells[$"H7:H{ws.Dimension.End.Row + 1}"].Style.WrapText = true; //Contents
					ws.Cells[$"K7:K{ws.Dimension.End.Row + 1}"].Style.WrapText = true; //Statuses
					//ws.Cells[$"N2:N{ws.Dimension.End.Row + 1}"].Style.Numberformat.Format = "mm/dd/yyyy"; //we're not doing dates separately
					//ws.Cells[$"L2:O{ws.Dimension.End.Row + 1}"].Style.Numberformat.Format = "mm/dd/yyyy";
					ws.Cells[ws.Dimension.Address].AutoFitColumns();

					excelPack.Save();
				}

				libmiroppb.Log($"Packages exported: {JsonConvert.SerializeObject(packages.Select(x => x.Id).ToList())}");
			}
            System.Windows.MessageBox.Show($"Exported to: {sfd.FileName}");
			return true;
		}

		private async void ShowHistory(object a)
		{
			List<Package>? PreviousPackages = await LoadByNameAsync(SelectedPackage.Sender_Name!);
			HistoryWindow historyWindow = new HistoryWindow(SelectedPackage.Sender_Name!, PreviousPackages);
			libmiroppb.Log($"Showing History for {SelectedPackage.Sender_Name!}: {JsonConvert.SerializeObject(PreviousPackages!.Select(x => x.PackageId).ToList())}");
			historyWindow.ShowDialog();

			if (historyWindow.SelectedPackageToUse != null)
			{
				libmiroppb.Log($"Replacing current Package {SelectedPackage.Id} with {JsonConvert.SerializeObject(historyWindow.SelectedPackageToUse)}");
				Package p = historyWindow.SelectedPackageToUse;
				SelectedPackage.Recipient_Name = p.Recipient_Name;
				SelectedPackage.Recipient_Address = p.Recipient_Address;
				SelectedPackage.Recipient_Phone = p.Recipient_Phone;
				SelectedPackage.Recipient_Contents = p.Recipient_Contents.Count > 0 ? p.Recipient_Contents : SelectedPackage.Recipient_Contents;
				SelectedPackage.Weight = p.Weight != null ? p.Weight : SelectedPackage.Weight;
				SelectedPackage.Value = p.Value != null ? p.Value : SelectedPackage.Value;
			}
		}

		private async void ShowShipWindow(object a)
		{
			int current = (SelectedPackage != null ? SelectedPackage.Id! : 0);
			MarkAsShippedWindow shippedWindow = new MarkAsShippedWindow(this);
			shippedWindow.ShowDialog();
			await LoadAsync();
			SelectedPackage = Packages.FirstOrDefault(x => x.Id == current)!;
		}

		private async void ShowAddNewWindow(object a)
		{
			ScanNewWindow scanNewWindow = new ScanNewWindow(this);
			scanNewWindow.ShowDialog();
			if (WasSomethingSet)
			{
				Save();
				await LoadAsync();
			}
			if (BarCodeThatWasSet != string.Empty)
			{
				try
				{
					SelectedPackage = Packages.FirstOrDefault(x => x.PackageId == BarCodeThatWasSet)!;
				}
				catch { }
			}
		}

		private async void ShowArriveWindow(object a)
		{
			int current = (SelectedPackage != null ? SelectedPackage.Id! : 0);
			MarkAsArrivedWindow shippedWindow = new MarkAsArrivedWindow(CurrentUser);
			shippedWindow.ShowDialog();
			await LoadAsync();
			SelectedPackage = Packages.FirstOrDefault(x => x.Id == current)!;
		}

		private async void ShowDeliverWindow(object a)
		{
			int current = (SelectedPackage != null ? SelectedPackage.Id! : 0);
			MarkAsDeliveredWindow shippedWindow = new MarkAsDeliveredWindow(CurrentUser);
			shippedWindow.ShowDialog();
			await LoadAsync();
			SelectedPackage = Packages.FirstOrDefault(x => x.Id == current)!;
		}

		private void ExecuteExport(object a)
		{
			Export(Packages);
		}

		private void ExecuteExportShippedNotArrived(object a)
		{
			Export(Packages, true);
		}

		private void ExecuteDoneCommand(object FromWhere)
		{
			int status = 1;
			switch (FromWhere.ToString())
			{
				case "Shipped":
					status = 2;
					break;
				case "Arrived":
					status = 3;
					break;
				case "Delivered":
					status = 4;
					break;
			}

			CodesScanned = "Sending codes to database. Please wait...";
			libmiroppb.Log($"Scanned As {FromWhere}: {JsonConvert.SerializeObject(barCodes)}");
			List<Package_Status> statuses = new List<Package_Status>();
			barCodes.ForEach(x => statuses.Add(new() { PackageId = x, Createdbyuser = CurrentUser.Id, CreatedDate = DateTime.Now, Status = status }));
			statuses = statuses.GroupBy(x => x.PackageId).Select(x => x.First()).ToList(); //remove duplicates

			InsertRecordStatus(statuses);

			if (status == 2)
			{
				List<Package> temp = new List<Package>();
				statuses.ForEach(x => temp.Add(new Package() { PackageId = x.PackageId }));

				List<Package> packages = Packages.Where(x => barCodes.Contains(x.PackageId.ToString())).ToList(); //this will not select any packages that aren't on list

				List<string> BarcodesNotInPackages = temp.Where(t => !packages.Where(x => x.PackageId == t.PackageId).Any()).Select(package => package.PackageId.ToString()).ToList(); //Find packages that were scanned but aren't on list

				if (BarcodesNotInPackages.Count > 0)
				{
                    System.Windows.MessageBox.Show(string.Format($"{Loc.Tr("PAS4U.ScanShippedWindow.ScannedButNotOnListText", "Following barcodes were scanned but not on list of packages:{0}{0}{1}{0}{0}The new statuses will be added to the database. " +
						"List will be saved on the desktop")}", Environment.NewLine, string.Join(", ", BarcodesNotInPackages)));
					StreamWriter w = new(Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + $"\\Shipped_{DateTime.Now:MM-dd-yyyy}.txt");
					w.WriteLine($"Barcodes scanned as shipped that weren't on the list on {DateTime.Now:MM/dd/yyyy}:");
					BarcodesNotInPackages.ForEach(x => w.WriteLine(x));
					w.Close();
				}

				if (barCodes.Count > 0)
				{
					Export(packages);
					if (System.Windows.MessageBox.Show($"{Loc.Tr("PAS4U.ScanShippedWindow.RemoveFromListText", "Should we remove these packages from the list?")}", "", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
					{
						packages.ForEach(x => x.Removed = true);
						UpdateRecords(packages, -2);
					}
				}
			}

			OnClosingRequest();
		}

		public void ReloadPackagesAndUpdateIfChanged()
		{
			if (IsOnline)
				_packageDataProvider.ReloadPackagesAndUpdateIfChanged(Packages, SelectedPackage);
		}

		internal bool? VerifyIfExists(string barCode)
		{
			if (IsOnline)
				return _packageDataProvider.VerifyIfExists(barCode);
			else
				return null;
		}

		public void PreviewKeyDownEvent(object sender, System.Windows.Input.KeyEventArgs e)
		{
			if (e.Key == Key.Enter)
			{
				barCodes.Add(barCode.Replace("\0", ""));
				barCode = string.Empty;
				e.Handled = true;
				CodesScanned = $"{Loc.Tr("PAS4U.ScanShippedWindow.BarcodesScanned", "Barcodes Scanned")}: {barCodes.Count}";
			}
			barCode += ToChar(e.Key);
		}

		public void NewPreviewKeyDownEvent(object sender, System.Windows.Input.KeyEventArgs e)
		{
			if (e.Key == Key.Enter)
			{
				barCode = barCode.Replace("\0", "").Trim();
				if (barCode != string.Empty) //make sure that the barcode is an actual alphanumeric string
				{
#if DEBUG
					Regex regex = new Regex("");
#else
					Regex regex = new Regex("^cv\\d\\d\\d\\d\\d\\d\\dus");
#endif
					Match match = regex.Match(barCode);
					if (match.Success)
					{
						if (Packages.FirstOrDefault(x => x.PackageId == barCode) == null)
						{
							bool? DoubleCheck = VerifyIfExists(barCode);
							if (DoubleCheck == null) { System.Windows.MessageBox.Show(Loc.Tr("PAS4U.MainWindow.Offline", "You're Offline")); }
							else if (DoubleCheck.Value)
							{
								WasSomethingSet = false;
                                System.Windows.MessageBox.Show(Loc.Tr("PAS4U.ScanNewWindow.AlreadyExistsText", "Package already exists"));
								BarCodeThatWasSet = barCode;
							}
							else
							{
								Insert(new()
								{
									PackageId = barCode,
									Contents = JsonConvert.SerializeObject(new List<Contents>() { })
								});
								InsertRecordStatus(new()
								{
									new() {
										PackageId = barCode, Createdbyuser = CurrentUser.Id, CreatedDate = DateTime.Now, Status = 1
									}
								});
								WasSomethingSet = true;
								BarCodeThatWasSet = barCode;
							}
						}
						else
						{
							WasSomethingSet = false;
                            System.Windows.MessageBox.Show(Loc.Tr("PAS4U.ScanNewWindow.AlreadyExistsText", "Package already exists"));
							BarCodeThatWasSet = barCode;
						}
					}
					else
					{
						WasSomethingSet = false;
                        System.Windows.MessageBox.Show(Loc.Tr("PAS4U.ScanNewWindow.WrongFormatText", "Package number not in correct format"));
						BarCodeThatWasSet = string.Empty;
					}
				}

				barCode = string.Empty;
				e.Handled = true;
				OnClosingRequest();
			}
			barCode += ToChar(e.Key);
		}
	}
}
