using CodingSeb.Localization;
using miroppb;
using Newtonsoft.Json;
using OfficeOpenXml;
using PrintAndScan4Ukraine.Model;
using ServiceStack;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using Xceed.Document.NET;
using Xceed.Words.NET;

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
				packages?.ToList().ForEach(Packages.Add);
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
			if (Save())
				System.Windows.MessageBox.Show($"{Loc.Tr("PAS4U.MainWindow.PackageSaved", "Package has been saved manually")}", "");
		}

		public bool Save() => Save(SelectedPackage);

		public bool Save(Package package, int type = 0)
		{
			if (IsOnline && package != null)
			{
				if (_packageDataProvider.UpdateRecords(new List<Package>() { package }, type))
				{
					LastSaved = $"Last Saved: {DateTime.Now.ToShortDateString()} {DateTime.Now.ToShortTimeString()}";
					package.Modified = false; //setting back as it was saved
					package.PackageIDModified = false;
					return true;
				}
			}
			return false;
		}

		public void SaveAll(object a)
		{
			if (IsOnline && Packages != null)
				if (_packageDataProvider.UpdateRecords(Packages.ToList()))
				{
					LastSaved = $"Last Saved: {DateTime.Now.ToShortDateString()} {DateTime.Now.ToShortTimeString()}";
					foreach (var package in Packages)
					{
						package.Modified = false; //everything was saved
						package.PackageIDModified = false;
					}
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

		public bool Export(IEnumerable<Package> packages, bool useArchive = false)
		{
			Libmiroppb.Log($"Starting Export");
			SaveFileDialog sfd = new()
			{
				Filter = "Excel File|*.xlsx"
			};
			if (sfd.ShowDialog() == DialogResult.OK)
			{
				//Check if file selected already exists
				bool FileExists = File.Exists(sfd.FileName);
				if (FileExists)
				{
					if (System.Windows.Forms.MessageBox.Show($"{Loc.Tr("PAS4U.ScanShippedWindow.ExportingFileAlreadyExists", "Selected file already exists. Do you want to add selected packages to it?")}", "",
					MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.No)
					{
						Libmiroppb.Log($"Selected file {sfd.FileName} exists. Not adding packages to file");
						return false;
					}
					else
						Libmiroppb.Log($"Selected file {sfd.FileName} exists. Adding packages to file");
				}
				else
					Libmiroppb.Log($"Selected file {sfd.FileName} is a new file");


				//lets get all statuses
				List<Package_Status>? statuses = _packageDataProvider.GetAllStatuses(packages.Select(x => x.PackageId).ToList(), useArchive)!.ToList();

				Libmiroppb.Log($"Exporting to XLSX. Filename: {sfd.FileName}");
				ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.NonCommercial;

				using (var excelPack = new ExcelPackage(new FileInfo(sfd.FileName)))
				{
					List<Package_less> list = _packageDataProvider.MapPackagesAndStatusesToLess(packages, statuses);

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
					ws.Cells[$"D7:D{ws.Dimension.End.Row}"].Style.WrapText = true; //Sender Address
					ws.Cells[$"G7:G{ws.Dimension.End.Row + 1}"].Style.WrapText = true; //Recipient Address
					ws.Cells[$"I7:I{ws.Dimension.End.Row + 1}"].Style.WrapText = true; //Contents
					ws.Cells[$"L7:L{ws.Dimension.End.Row + 1}"].Style.WrapText = true; //Statuses
																					   //ws.Cells[$"N2:N{ws.Dimension.End.Row + 1}"].Style.Numberformat.Format = "mm/dd/yyyy"; //we're not doing dates separately
																					   //ws.Cells[$"L2:O{ws.Dimension.End.Row + 1}"].Style.Numberformat.Format = "mm/dd/yyyy";

					ws.Cells[$"B7:B{ws.Dimension.End.Row}"].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
					ws.Cells[ws.Dimension.Address].AutoFitColumns();
					ws.Cells[$"L7:L{ws.Dimension.End.Row}"].AutoFitColumns(35);

					excelPack.Save();
				}

				_packageDataProvider.UploadExportedFile(sfd.FileName);

				Libmiroppb.Log($"Packages exported: {JsonConvert.SerializeObject(packages.Select(x => x.Id).ToList())}");
				System.Windows.MessageBox.Show($"Exported to: {sfd.FileName}");
			}
			return true;
		}

		private async void ShowHistory(object a)
		{
			List<Package>? PreviousPackages = await LoadByNameAsync(SelectedPackage.Sender_Name!);
			HistoryWindow historyWindow = new(SelectedPackage.Sender_Name!, PreviousPackages);
			Libmiroppb.Log($"Showing History for {SelectedPackage.Sender_Name!}: {JsonConvert.SerializeObject(PreviousPackages!.Select(x => x.PackageId).ToList())}");
			historyWindow.ShowDialog();

			if (historyWindow.DoubleClicked && historyWindow.SelectedPackageToUse != null)
			{
				Libmiroppb.Log($"Replacing current Package {SelectedPackage.Id} with {JsonConvert.SerializeObject(historyWindow.SelectedPackageToUse)}");
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
			int current = SelectedPackage != null ? SelectedPackage.Id! : 0;
			MarkAsShippedWindow shippedWindow = new(this);
			Libmiroppb.Log("Opening Scan as Shipped Window");
			shippedWindow.ShowDialog();
			await LoadAsync();
			SelectedPackage = Packages.FirstOrDefault(x => x.Id == current)!;
		}

		private async void ShowAddNewWindow(object a)
		{
			if (IsEditingPackageID)
			{
				AddMultipleText = Loc.Tr("PAS4U.ScanNewWindow.TopTextFind", "Scan New Barcode to Find");
				AddMultipleVisible = Visibility.Hidden;
			}
			else
			{
				AddMultipleText = Loc.Tr("PAS4U.ScanNewWindow.TopText", "Scan New Barcode to Add");
				AddMultipleVisible = Visibility.Visible;
			}
			ScanNewWindow scanNewWindow = new(this);
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
			BarCode = string.Empty;
			barCodes.Clear();
			BarCodeThatWasSet = string.Empty;
			WasSomethingSet = false;
			AddMultipleVisible = Visibility.Visible;
			if (AddMultipleNew)
				ExecuteAddMultiple(new object()); //flip it back
		}

		private async void ShowArriveWindow(object a)
		{
			int current = SelectedPackage != null ? SelectedPackage.Id! : 0;
			MarkAsArrivedWindow arrivedWindow = new(CurrentUser);
			Libmiroppb.Log("Opening Scan as Arrived Window");
			arrivedWindow.ShowDialog();
			await LoadAsync();
			SelectedPackage = Packages.FirstOrDefault(x => x.Id == current)!;
		}

		private async void ShowDeliverWindow(object a)
		{
			int current = SelectedPackage != null ? SelectedPackage.Id! : 0;
			MarkAsDeliveredWindow deliveredWindow = new(CurrentUser);
			Libmiroppb.Log("Opening Scan as Delivered Window");
			deliveredWindow.ShowDialog();
			await LoadAsync();
			SelectedPackage = Packages.FirstOrDefault(x => x.Id == current)!;
		}

		private void ExecuteExport(object a)
		{
			ExportButtonEnabled = true;
			ReportSelection win = new(this);
			win.ShowDialog();
		}

		private async void ExecuteDoneCommand(object FromWhere)
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

			DoneButtonEnabled = false;

			CodesScanned = "Sending codes to database. Please wait...";
			Libmiroppb.Log($"Scanned As {FromWhere}: {JsonConvert.SerializeObject(barCodes)}");
			List<Package_Status> statuses = new();
			barCodes.ForEach(x => statuses.Add(new() { PackageId = x, Createdbyuser = CurrentUser.Id, CreatedDate = DateTime.Now, Status = status }));
			statuses = statuses.GroupBy(x => x.PackageId).Select(x => x.First()).ToList(); //remove duplicates

			Libmiroppb.Log("Updating statuses");
			InsertRecordStatus(statuses);
			Libmiroppb.Log("Done");

			if (status == 2)
			{
				List<Package> temp = new();
				statuses.ForEach(x => temp.Add(new Package() { PackageId = x.PackageId }));

				List<Package> packages = Packages.Where(x => barCodes.Contains(x.PackageId.ToString())).ToList(); //this will not select any packages that aren't on list

				List<string> BarcodesNotInPackages = temp.Where(t => !packages.Where(x => x.PackageId == t.PackageId).Any()).Select(package => package.PackageId.ToString()).ToList(); //Find packages that were scanned but aren't on list

				if (BarcodesNotInPackages.Count > 0)
				{
					System.Windows.MessageBox.Show(string.Format($"{Loc.Tr("PAS4U.ScanShippedWindow.ScannedButNotOnListText", "Following barcodes were scanned but not on list of packages:{0}{0}{1}{0}{0}The new statuses will be added to the database. " +
						"List will be saved on the desktop")}", Environment.NewLine, string.Join(", ", BarcodesNotInPackages)));
					StreamWriter w = new(Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + $"\\Shipped_{DateTime.Now:MM-dd-yyyy}.txt");
					w.WriteLine($"Barcodes scanned as shipped that weren't on the list on {DateTime.Now:MM/dd/yyyy}:");
					BarcodesNotInPackages.ForEach(w.WriteLine);
					w.Close();

					if (System.Windows.Forms.MessageBox.Show("Do you want a report of the missing packages?", "", MessageBoxButtons.YesNo) == DialogResult.Yes)
					{
						var lst = _packageDataProvider.FindMissingPackages(BarcodesNotInPackages);
						var CodesWithoutPackage = lst.Where(x => !x.InPackages).ToList();
						var CodesWithPackages = lst.Where(x => x.InPackages).ToList();
						var users = _packageDataProvider.GetUserIDsAndNames();

						using (DocX document = DocX.Create(Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + $"\\MissingPackages_{DateTime.Now:MM-dd-yyyy}.docx"))
						{
							// Add a new Paragraph to the document.
							Paragraph p = document.InsertParagraph();

							// Append some text.
							p.AppendLine($"These packages don't have any record in the database: {string.Join(", ", CodesWithoutPackage.Select(x => x.Packageid))}").Font("Calibri");
							p.AppendLine();

							foreach (var item in CodesWithPackages)
							{
								//lets find the last time the package was scanned in
								List<string> Changes = new();
								if (item.Statuses != null)
								{
									item.Statuses.Reverse();
									foreach (var st in item.Statuses)
									{
										string user = st.Createdbyuser != null ? users.First(x => x.Id == st.Createdbyuser).Comment : st.Status == 1 ? "Vika" : "Vitaliy"; //Because they were the initial users. Remove after some time...
										if (st.Status == 1)
										{
											Changes.Add($"Last Scanned in by {user} on {st.CreatedDate.ToShortDateString()}");
											break; //only latest scanned in, in case there's duplicates
										}
										else if (st.Status == 2)
											Changes.Add($"Shipped by {user} on {st.CreatedDate.ToShortDateString()}");
										else if (st.Status == 3)
											Changes.Add($"Arrived by {user} on {st.CreatedDate.ToShortDateString()}");
										else
											Changes.Add($"Delivered by {user} on {st.CreatedDate.ToShortDateString()}");
									}
								}
								else
									Changes.Add("There have been no status changes, somehow"); //shouldn't happen
								Changes.Reverse();
								p.AppendLine($"{item.Packageid.ToUpper()} - {string.Join(", ", Changes)}");
								p.AppendLine();
							}
							document.Save();
						}
						System.Windows.Forms.MessageBox.Show($"File saved to Desktop: MissingPackages_{DateTime.Now:MM-dd-yyyy}.docx");
					}
				}

				if (packages.Any())
				{
					Libmiroppb.Log($"Starting Export as {FromWhere}");
					Export(packages);
					Libmiroppb.Log("Done");
					if (System.Windows.MessageBox.Show($"{Loc.Tr("PAS4U.ScanShippedWindow.RemoveFromListText", "Should we remove these packages from the list?")}", "", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
					{
						packages.ForEach(x => x.Removed = true);
						UpdateRecords(packages, -2);
						Libmiroppb.Log("Done");
					}
				}
			}
			else if (status == 3)
			{
				//get packages from db for all scanned barcodes
				IEnumerable<Package> packages = await _packageDataProvider.GetPackagesAsync(barCodes, true);
				List<Package> packages1 = packages.ToList();
				
				//find packages that don't have records
				List<string> BarcodesNotInPackages = barCodes.Where(s => !packages.Any(p => p.PackageId == s)).ToList();

				//prompt to save the list to desktop
				if (BarcodesNotInPackages.Count > 0)
				{
					System.Windows.MessageBox.Show(string.Format($"{Loc.Tr("PAS4U.ScanShippedWindow.ScannedButNotOnListText", "Following barcodes were scanned but not on list of packages:{0}{0}{1}{0}{0}The new statuses will be added to the database. " +
						"List will be saved on the desktop")}", Environment.NewLine, string.Join(", ", BarcodesNotInPackages)));
					StreamWriter w = new(Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + $"\\Arrived_{DateTime.Now:MM-dd-yyyy}.txt");
					w.WriteLine($"Barcodes scanned as arrived that weren't on the list on {DateTime.Now:MM/dd/yyyy}:");
					BarcodesNotInPackages.ForEach(w.WriteLine);
					w.Close();
				}
				//if barcodes > 0, export Excel
				if (packages.Any())
				{
					Libmiroppb.Log($"Starting Export as {FromWhere}");
					Export(packages);
					Libmiroppb.Log("Done");
				}
			}
			DoneButtonEnabled = true;
			barCodes.Clear();
			OnClosingRequest();
		}

		public async void ExecuteGenerateReport(object a)
		{
			SpinnerVisible = Visibility.Visible;
			ExportButtonEnabled = false;

			if (ReportAll)
			{
				ExportStartDate = DateTime.MinValue;
				ExportEndDate = DateTime.MaxValue;
			}

			Stopwatch stopwatch = new();
			stopwatch.Start();

			IEnumerable<Package>? PackagesFromDates = await _packageDataProvider.GetPackagesByDateAndLastStatusAsync(ExportStartDate, ExportEndDate, ReportLastStatus);

			stopwatch.Stop();
			Libmiroppb.Log($"Generating report for: {ExportStartDate}, {ExportEndDate}, {ReportLastStatus}. Ran for: {stopwatch.Elapsed}");

			OnClosingRequest();

			if (PackagesFromDates != null)
				Export(PackagesFromDates);
		}

		public void ExecuteRadioDateChecked(object AllOrDates)
		{
			ReportAll = AllOrDates.ToString() == "All";
		}

		public void ExecuteRadioStatusChecked(object LastStatus)
		{
			if (LastSaved.ToString() == "Scanned")
				ReportLastStatus = 0;
			else if (LastSaved.ToString() == "Shipped")
				ReportLastStatus = 1;
			else if (LastSaved.ToString() == "Arrived")
				ReportLastStatus = 2;
			else
				ReportLastStatus = 3;
		}

		private void ExecuteAddMultiple(object obj)
		{
			AddMultipleNew = !AddMultipleNew;
			if (!AddMultipleNew)
			{
				AddMultipleButton = $"{Loc.Tr("PAS4U.ScanNewWindow.Multiple", "Add Multiple")}";
				AddMultipleText = $"{Loc.Tr("PAS4U.ScanNewWindow.TopText", "Scan Barcode to Add...")}";
			}
			else
			{
				AddMultipleButton = $"{Loc.Tr("PAS4U.ScanNewWindow.Single", "Add One")}";
				AddMultipleText = $"{Loc.Tr("PAS4U.ScanNewWindow.TopTextMultiple", "Scan Multiple Barcodes to Add")}: 0";
			}
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
				barCodes.Add(BarCode.Replace("\0", ""));
				BarCode = string.Empty;
				e.Handled = true;
				CodesScanned = $"{Loc.Tr("PAS4U.ScanShippedWindow.BarcodesScanned", "Barcodes Scanned")}: {barCodes.Count}";
			}
			BarCode += ToChar(e.Key).ToString().Replace("\0", "");
		}

		public void NewPreviewKeyDownEvent(object sender, System.Windows.Input.KeyEventArgs e)
		{
			if (AddMultipleNew && IsOnline && e.Key == Key.Enter)
			{
				BarCode += ",";
				string latestBarcode = BarCode.Split(',').ToList().SkipLast(1).Last();
				if (latestBarcode != string.Empty)
				{
					ValidateAndInsertBarcode(latestBarcode);
					if (!string.IsNullOrEmpty(BarCodeThatWasSet))
						barCodes.Add(latestBarcode);
					AddMultipleText = $"{Loc.Tr("PAS4U.ScanNewWindow.TopTextMultiple", "Scan Multiple Barcodes to Add")}: {barCodes.Count}";
				}
				if (BarCode.Length > 1 && BarCode[^2..] == ",,")
				{
					//we're done
					BarCode = string.Empty;
					e.Handled = true;
					ExecuteAddMultiple(new object()); //set it back
					OnClosingRequest();
				}
			}
			else if (e.Key == Key.Enter)
			{
				BarCode = BarCode.Replace("\0", "").Trim();
				if (BarCode != string.Empty) //make sure that the barcode is an actual alphanumeric string
				{
					ValidateAndInsertBarcode(BarCode);
				}

				BarCode = string.Empty;
				e.Handled = true;
				OnClosingRequest();
			}
			if (e.Key == Key.Back && BarCode.Length > 0 && BarCode.Substring(BarCode.Length - 1, 1) != ",") //if the last character isn't a comma (enter was pressed and barcode has been saved)
				BarCode = BarCode[..^1];
			BarCode += ToChar(e.Key).ToString().Replace("\0", "");
			AddMultipleVisible = Visibility.Collapsed;
		}

		private void ValidateAndInsertBarcode(string _barcode)
		{
#if DEBUG
			Regex regex = new("");
#else
			Regex regex = new Regex("^cv\\d{7,9}us$");
#endif
			Match match = regex.Match(_barcode);
			if (match.Success)
			{
				if (Packages.FirstOrDefault(x => x.PackageId == _barcode) == null)
				{
					bool? DoubleCheck = VerifyIfExists(_barcode);
					if (DoubleCheck == null) { System.Windows.MessageBox.Show(Loc.Tr("PAS4U.MainWindow.Offline", "You're Offline")); }
					else if (DoubleCheck.Value)
					{
						WasSomethingSet = false;
						Libmiroppb.Log("Package Already Exists: " + _barcode);
						if (!IsEditingPackageID)
							System.Windows.MessageBox.Show(Loc.Tr("PAS4U.ScanNewWindow.AlreadyExistsText", "Package already exists"));
						BarCodeThatWasSet = _barcode;
					}
					else
					{
						if (!IsEditingPackageID | (IsEditingPackageID && System.Windows.MessageBox.Show(Loc.Tr("PAS4U.ScanNewWindow.NewWhileEditing", "You're in the process of editing barcodes, and are about to add a new barcode"),
							"Are you sure?", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes))
						{
							Insert(new()
							{
								PackageId = _barcode,
								Contents = JsonConvert.SerializeObject(new List<Contents>() { })
							});
							InsertRecordStatus(new()
							{
								new() {
									PackageId = _barcode, Createdbyuser = CurrentUser.Id, CreatedDate = DateTime.Now, Status = 1
								}
							});
							WasSomethingSet = true;
							BarCodeThatWasSet = _barcode;
						}
					}
				}
				else
				{
					WasSomethingSet = false;
					Libmiroppb.Log("Package Already Exists: " + _barcode);
					if (!IsEditingPackageID)
						System.Windows.MessageBox.Show(Loc.Tr("PAS4U.ScanNewWindow.AlreadyExistsText", "Package already exists"));
					BarCodeThatWasSet = _barcode;
				}
			}
			else
			{
				WasSomethingSet = false;
				Libmiroppb.Log("Wrong Format: " + _barcode);
				string WrongText = string.Format(Loc.Tr("PAS4U.ScanNewWindow.WrongFormatText", "Package number not in correct format\n\nNumber: {0}"), _barcode);
				System.Windows.MessageBox.Show(WrongText);
				BarCodeThatWasSet = string.Empty;
			}
		}

		private void ExecuteEditPackageID(object obj)
		{
			IsEditingPackageID = !IsEditingPackageID;
		}

		private void ExecuteShowSearch(object obj)
		{
			SearchSelectionWindow searchSelection = new();
			searchSelection.ShowDialog();
			if (SearchSelectedPackage != string.Empty)
			{
				Libmiroppb.Log($"{SearchSelectedPackage} has been selected");
				var temp = Packages.FirstOrDefault(x => x.PackageId == SearchSelectedPackage);
				if (temp != null)
					SelectedPackage = temp;
				else
					System.Windows.MessageBox.Show(Loc.Tr($"PAS4U.SearchSelectionWindow.PackageNotOnList", "This package isn't on the list"), "Selected package isn't on the list, and can't be displayed");
			}
		}

		public static string SearchSelectedPackage = string.Empty;
	}
}
