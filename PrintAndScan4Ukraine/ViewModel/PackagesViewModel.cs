using Microsoft.Win32;
using miroppb;
using Newtonsoft.Json;
using OfficeOpenXml;
using PrintAndScan4Ukraine.Command;
using PrintAndScan4Ukraine.Data;
using PrintAndScan4Ukraine.Model;
using ServiceStack;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;

namespace PrintAndScan4Ukraine.ViewModel
{
	public class PackagesViewModel : INotifyPropertyChanged
	{
		private readonly IPackageDataProvider _packageDataProvider;

		public event PropertyChangedEventHandler? PropertyChanged;
		protected virtual void RaisePropertyChanged([CallerMemberName] string? propertyName = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		public ObservableCollection<Package> Packages { get; } = new();
		public DelegateCommand SaveCommand { get; }
		public DelegateCommand ShowHistoryCommand { get; }
		public DelegateCommand SaveAllCommand { get; }
		public DelegateCommand ShipCommand { get; }
		public DelegateCommand AddNewCommand { get; }
		public DelegateCommand ArriveCommand { get; }
		public DelegateCommand DeliverCommand { get; }
		public DelegateCommand ExportCommand { get; }
		public DelegateCommand ExportShippedNotArrivedCommand { get; }

		public bool CanSave => SelectedPackage != null && IsOnline;
		public bool CanShowHistory => SelectedPackage != null && IsOnline;
		public bool CanShip => CurrentUserAccess.HasFlag(Access.Ship) && IsOnline;
		public bool CanAddNew => CurrentUserAccess.HasFlag(Access.AddNew) && IsOnline;
		public bool CanArrive => CurrentUserAccess.HasFlag(Access.Arrive) && IsOnline;
		public bool CanDeliver => CurrentUserAccess.HasFlag(Access.Deliver) && IsOnline;

		private Visibility _IsSelectedPackageShowing = Visibility.Hidden;

		public Visibility IsSelectedPackageShowing
		{
			get => _IsSelectedPackageShowing;
			set
			{
				_IsSelectedPackageShowing = value;
				RaisePropertyChanged();
			}
		}

		public PackagesViewModel(IPackageDataProvider packageDataProvider, Access UserAccess)
		{
			_packageDataProvider = packageDataProvider;
			CurrentUserAccess = UserAccess;
			SaveCommand = new DelegateCommand(Save, () => CanSave);
			ShowHistoryCommand = new DelegateCommand(ShowHistory, () => CanShowHistory);
			SaveAllCommand = new DelegateCommand(SaveAll);
			ShipCommand = new DelegateCommand(ShowShipWindow, () => CanShip);
			AddNewCommand = new DelegateCommand(ShowAddNewWindow, () => CanAddNew);
			ArriveCommand = new DelegateCommand(ShowArriveWindow, () => CanArrive);
			DeliverCommand = new DelegateCommand(ShowDeliverWindow, () => CanDeliver);
			ExportCommand = new DelegateCommand(ExecuteExport);
			ExportShippedNotArrivedCommand = new DelegateCommand(ExecuteExportShippedNotArrived);
		}

		private Package? _selectedPackage;
		public Package SelectedPackage
		{
			get => _selectedPackage!;
			set
			{
				if (_selectedPackage != null && _selectedPackage.Modified) //if package was modified
					Save(_selectedPackage, -1); //save previous package before changing to new package

				_selectedPackage = value;
				RaisePropertyChanged();
				if (_selectedPackage != null)
				{
					IsSelectedPackageShowing = Visibility.Visible;
					SelectedPackageLastStatus = _packageDataProvider.GetStatusByPackage(_selectedPackage.PackageId)!.LastOrDefault()!;
				}
				else
					IsSelectedPackageShowing = Visibility.Hidden;
				SaveCommand.RaiseCanExecuteChanged();
				ShowHistoryCommand.RaiseCanExecuteChanged();
			}
		}

		private Package_Status _SelectedPackageStatus = new Package_Status();

		public Package_Status SelectedPackageLastStatus
		{
			get => _SelectedPackageStatus;
			set
			{
				_SelectedPackageStatus = value;
				RaisePropertyChanged();
			}
		}

		private Access _CurrentUserAccess = Access.None;

		public Access CurrentUserAccess
		{
			get => _CurrentUserAccess;
			set
			{
				_CurrentUserAccess = value;
				RaisePropertyChanged();
				RaisePropertyChanged("AccessToSender");
				RaisePropertyChanged("AccessToEditSender");
				RaisePropertyChanged("AccessToEditReceipient");
			}
		}

		private bool _isOnline = true;

		public bool IsOnline
		{
			get => _isOnline;
			set
			{
				_isOnline = value;
				RaisePropertyChanged();
			}
		}

		private string _lastSaved = string.Empty;

		public string LastSaved
		{
			get => _lastSaved;
			set
			{
				_lastSaved = value;
				RaisePropertyChanged();
			}
		}

#if DEBUG
		public string Header => "Scan Packages v. " + Assembly.GetExecutingAssembly().GetName().Version!.ToString() + " -Debug";
#else
		public string Header => "Scan Packages v. " + Assembly.GetExecutingAssembly().GetName().Version!.ToString();
#endif

		public bool AccessToSeePackages => CurrentUserAccess.HasFlag(Access.SeePackages);
		public bool AccessToSeeSender => CurrentUserAccess.HasFlag(Access.SeeSender);
		public bool AccessToEditSender => !CurrentUserAccess.HasFlag(Access.EditSender);
		public bool AccessToEditReceipient => !CurrentUserAccess.HasFlag(Access.EditReceipient);
		public bool AccessToAddNew => CurrentUserAccess.HasFlag(Access.AddNew);
		public bool AccessToShip => CurrentUserAccess.HasFlag(Access.Ship);
		public bool AccessToArrive => CurrentUserAccess.HasFlag(Access.Arrive);
		public bool AccessToDeliver => CurrentUserAccess.HasFlag(Access.Deliver);
		public bool AccessToExport => CurrentUserAccess.HasFlag(Access.Export);

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

		public void SaveAll()
		{
			if (IsOnline && Packages != null)
				if (_packageDataProvider.UpdateRecords(Packages.ToList()))
				{
					LastSaved = $"Last Saved: {DateTime.Now.ToShortDateString()} {DateTime.Now.ToShortTimeString()}";
					foreach (var package in Packages)
						package.Modified = false; //everything was saved
				}

		}

		public bool UpdateRecords(List<Package> packages)
		{
			if (IsOnline)
				return _packageDataProvider.UpdateRecords(packages);
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
			if ((bool)sfd.ShowDialog()!)
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
						ws.Cells.LoadFromCollection(list, true, OfficeOpenXml.Table.TableStyles.Light8);
					}
					catch
					{
						ws = excelPack.Workbook.Worksheets[DateTime.Now.ToShortDateString()];
						int row = ws.Dimension.End.Row;
						ws.Cells[row + 1, 1].LoadFromCollection(list, false, OfficeOpenXml.Table.TableStyles.Light8);
					}
					ws.Cells.Style.VerticalAlignment = OfficeOpenXml.Style.ExcelVerticalAlignment.Top;
					ws.Cells[$"H2:C{ws.Dimension.End.Row + 1}"].Style.WrapText = true; //Sender Address
					ws.Cells[$"H2:F{ws.Dimension.End.Row + 1}"].Style.WrapText = true; //Recipient Address
					ws.Cells[$"H2:H{ws.Dimension.End.Row + 1}"].Style.WrapText = true; //Contents
					ws.Cells[$"H2:K{ws.Dimension.End.Row + 1}"].Style.WrapText = true; //Statuses
					//ws.Cells[$"K2:N{ws.Dimension.End.Row + 1}"].Style.Numberformat.Format = "mm/dd/yyyy"; //we're not doing dates separately
					//ws.Cells[$"L2:O{ws.Dimension.End.Row + 1}"].Style.Numberformat.Format = "mm/dd/yyyy";
					ws.Cells[ws.Dimension.Address].AutoFitColumns();

					excelPack.Save();
				}

				libmiroppb.Log($"{JsonConvert.SerializeObject(packages)}");
			}
			MessageBox.Show($"Exported to: {sfd.FileName}");
			return true;
		}

		private async void ShowHistory()
		{
			List<Package>? PreviousPackages = await LoadByNameAsync(SelectedPackage.Sender_Name!);
			HistoryWindow historyWindow = new HistoryWindow(SelectedPackage.Sender_Name!, PreviousPackages);
			libmiroppb.Log($"Showing History for {SelectedPackage.Sender_Name!}: {JsonConvert.SerializeObject(PreviousPackages)}");
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

		private async void ShowShipWindow()
		{
			int current = (SelectedPackage != null ? (int)SelectedPackage.Id! : 0);
			MarkAsShippedWindow shippedWindow = new MarkAsShippedWindow(this);
			shippedWindow.ShowDialog();
			await LoadAsync();
			SelectedPackage = Packages.FirstOrDefault(x => x.Id == current)!;
		}

		private async void ShowAddNewWindow()
		{
			ScanNewWindow scanNewWindow = new ScanNewWindow(Packages.Select(x => x.PackageId).ToList());
			scanNewWindow.ShowDialog();
			if (scanNewWindow.WasSomethingSet)
			{
				Save();
				await LoadAsync();
			}
			if (scanNewWindow.BarCodeThatWasSet != string.Empty)
			{
				try
				{
					SelectedPackage = Packages.FirstOrDefault(x => x.PackageId == scanNewWindow.BarCodeThatWasSet)!;
				}
				catch { }
			}
		}

		private async void ShowArriveWindow()
		{
			int current = (SelectedPackage != null ? (int)SelectedPackage.Id! : 0);
			MarkAsArrivedWindow shippedWindow = new MarkAsArrivedWindow(this);
			shippedWindow.ShowDialog();
			await LoadAsync();
			SelectedPackage = Packages.FirstOrDefault(x => x.Id == current)!;
		}

		private async void ShowDeliverWindow()
		{
			int current = (SelectedPackage != null ? (int)SelectedPackage.Id! : 0);
			MarkAsDeliveredWindow shippedWindow = new MarkAsDeliveredWindow(this);
			shippedWindow.ShowDialog();
			await LoadAsync();
			SelectedPackage = Packages.FirstOrDefault(x => x.Id == current)!;
		}

		private void ExecuteExport()
		{
			Export(Packages);
		}

		private void ExecuteExportShippedNotArrived()
		{
			Export(Packages, true);
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
	}
}
