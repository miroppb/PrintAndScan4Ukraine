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
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;

namespace PrintAndScan4Ukraine.ViewModel
{
	public class PackagesViewModel : INotifyPropertyChanged
	{
		private readonly IPackageDataProvider _packageDataProvider;
		private Package? _selectedPackage;

		public event PropertyChangedEventHandler? PropertyChanged;

		public ObservableCollection<Package> Packages { get; } = new();
		public DelegateCommand SaveCommand { get; }
		public DelegateCommand ShowHistoryCommand { get; }
		public DelegateCommand SaveAllCommand { get; }

		public bool CanSave => (SelectedPackage != null && IsOnline);
		public bool CanShowHistory => (SelectedPackage != null && IsOnline);

		public PackagesViewModel(IPackageDataProvider packageDataProvider)
		{
			_packageDataProvider = packageDataProvider;
			SaveCommand = new DelegateCommand(Save, () => CanSave);
			ShowHistoryCommand = new DelegateCommand(ShowHistory, () => CanShowHistory);
			SaveAllCommand = new DelegateCommand(SaveAll);
		}

		public Package SelectedPackage
		{
			get => _selectedPackage!;
			set
			{
				if (_selectedPackage != null)
					Save(_selectedPackage); //save previous package before changing to new package
				_selectedPackage = value;
				RaisePropertyChanged();
				SaveCommand.RaiseCanExecuteChanged();
				ShowHistoryCommand.RaiseCanExecuteChanged();
			}
		}

		protected virtual void RaisePropertyChanged([CallerMemberName] string? propertyName = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		public async Task LoadAsync()
		{
			if (IsOnline)
			{
				if (Packages.Any())
					Packages.Clear();

				var packages = await _packageDataProvider.GetAllAsync();
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
			if (SelectedPackage != null && IsOnline)
			{
				if (_packageDataProvider.UpdateRecords(new List<Package>() { SelectedPackage }))
					LastSaved = $"Last Saved: {DateTime.Now.ToShortDateString()} {DateTime.Now.ToShortTimeString()}";
			}
		}

		public void Save(Package package)
		{
			if (IsOnline)
			{
				if (_packageDataProvider.UpdateRecords(new List<Package>() { package }))
					LastSaved = $"Last Saved: {DateTime.Now.ToShortDateString()} {DateTime.Now.ToShortTimeString()}";
			}
		}

		public void SaveAll()
		{
			if (IsOnline && Packages != null)
				if (_packageDataProvider.UpdateRecords(Packages.ToList()))
					LastSaved = $"Last Saved: {DateTime.Now.ToShortDateString()} {DateTime.Now.ToShortTimeString()}";

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

		public bool Export(IEnumerable<Package> packages)
		{
			SaveFileDialog sfd = new SaveFileDialog
			{
				Filter = "Excel File|*.xlsx"
			};
			if ((bool)sfd.ShowDialog()!)
			{
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
					ws.Cells[$"H2:H{ws.Dimension.End.Row + 1}"].Style.WrapText = true;
					ws.Cells[$"K2:N{ws.Dimension.End.Row + 1}"].Style.Numberformat.Format = "mm/dd/yyyy";
					ws.Cells[$"L2:O{ws.Dimension.End.Row + 1}"].Style.Numberformat.Format = "mm/dd/yyyy";
					ws.Cells[ws.Dimension.Address].AutoFitColumns();

					excelPack.Save();
				}

				libmiroppb.Log($"{JsonConvert.SerializeObject(packages)}");
			}
			MessageBox.Show($"Exported to: {sfd.FileName}");
			return true;
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

		public void ReloadPackagesAndUpdateIfChanged()
		{
			if (IsOnline)
				_packageDataProvider.ReloadPackagesAndUpdateIfChanged(Packages, SelectedPackage);
		}
	}
}
