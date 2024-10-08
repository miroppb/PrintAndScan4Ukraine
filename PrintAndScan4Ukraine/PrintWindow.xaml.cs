﻿using miroppb;
using Newtonsoft.Json;
using PrintAndScan4Ukraine.Data;
using PrintAndScan4Ukraine.ViewModel;
using System;
using System.Linq;
using System.Reflection;
using System.Windows;

namespace PrintAndScan4Ukraine
{
	/// <summary>
	/// Interaction logic for PrintWindow.xaml
	/// </summary>
	public partial class PrintWindow : Window
	{
		private readonly PrintViewModel _viewmodel;

		public PrintWindow()
		{
			InitializeComponent();
			Libmiroppb.Log("Welcome to (Print) And Scan 4 Ukraine. v" + Assembly.GetEntryAssembly()!.GetName().Version);
			_viewmodel = new PrintViewModel(new PrintDataProvider());
			DataContext = _viewmodel;
			Closing += PrintWindow_Closing;
		}

		private void PrintWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
		{
			UploadLogs(true);
		}

		private void Window_Loaded(object sender, RoutedEventArgs e)
		{
			_viewmodel.Printers.Clear();
			_viewmodel.LoadPrinters();
			Libmiroppb.Log($"Getting List of Printers: {JsonConvert.SerializeObject(_viewmodel.Printers)}");
			try
			{
				if (_viewmodel.Printers.Count > 0)
					_viewmodel.SelectedPrinter = _viewmodel.Printers.Where(x => x.Contains("ZPL")).FirstOrDefault()!;
			}
			catch (Exception ex) { Libmiroppb.Log($"Exception: {ex.Message}"); }
			
		}

		private async void UploadLogs(bool deleteAfter) => await Libmiroppb.UploadLogAsync(Secrets.GetConnectionString().ConnectionString, deleteAfter);
	}
}
