﻿using Microsoft.Win32;
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

		public bool CanSave => SelectedPackage != null;

		public PackagesViewModel(IPackageDataProvider packageDataProvider)
		{
			_packageDataProvider = packageDataProvider;
			SaveCommand = new DelegateCommand(Save, () => CanSave);
		}

		public Package SelectedPackage
		{
			get => _selectedPackage!;
			set
			{
				_selectedPackage = value;
				RaisePropertyChanged();
				SaveCommand.RaiseCanExecuteChanged();
			}
		}

		protected virtual void RaisePropertyChanged([CallerMemberName] string? propertyName = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		public async Task LoadAsync()
		{
			if (Packages.Any())
				Packages.Clear();

			var packages = await _packageDataProvider.GetAllAsync();
			if (packages != null)
				packages.ToList().ForEach(p => Packages.Add(p));
		}

		public void Save()
		{
			if (SelectedPackage != null)
				_packageDataProvider.UpdateRecord(new List<Package>() { SelectedPackage });
		}

		public bool UpdateRecord(List<Package> packages)
		{
			if (SelectedPackage != null)
				return _packageDataProvider.UpdateRecord(packages);
			return false;
		}

		public async Task<bool> InsertAsync(Package package)
		{
			return await _packageDataProvider.InsertRecordAsync(package);
		}

		public bool Export(IEnumerable<Package> packages)
		{
			SaveFileDialog sfd = new SaveFileDialog();
			sfd.Filter = "Excel File|*.xlsx";
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
					ws.Cells[$"N2:N{ws.Dimension.End.Row + 1}"].Style.Numberformat.Format = "mm/dd/yyyy";
					ws.Cells[$"O2:O{ws.Dimension.End.Row + 1}"].Style.Numberformat.Format = "mm/dd/yyyy";
					ws.Cells[ws.Dimension.Address].AutoFitColumns();

					excelPack.Save();
				}

				libmiroppb.Log($"{JsonConvert.SerializeObject(packages)}");
			}
			MessageBox.Show($"Exported to: {sfd.FileName}");
			return true;
		}
	}
}
