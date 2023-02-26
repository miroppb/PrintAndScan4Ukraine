using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Printing;
using System.Text;
using System;
using static System.Runtime.InteropServices.JavaScript.JSType;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Threading.Tasks;
using PrintAndScan4Ukraine.ViewModel;
using PrintAndScan4Ukraine.Data;

namespace PrintAndScan4Ukraine
{
	/// <summary>
	/// Interaction logic for PrintWindow.xaml
	/// </summary>
	public partial class PrintWindow : Window
	{
		private PrintViewModel _viewmodel;

		public PrintWindow()
		{
			InitializeComponent();
			_viewmodel = new PrintViewModel(new PrintDataProvider());
			DataContext = _viewmodel;
		}

		private void Window_Loaded(object sender, RoutedEventArgs e)
		{
			_viewmodel.Printers.Clear();
			_viewmodel.LoadPrinters();
			if (_viewmodel.Printers.Count > 0)
				_viewmodel.SelectedPrinter = _viewmodel.Printers.Where(x => x.Contains("ZPL")).FirstOrDefault()!;
		}

		private void BtnPrint_Click(object sender, RoutedEventArgs e)
		{
			int from = int.Parse(TxtFrom.Text);
			int to = int.Parse(TxtTo.Text);
			int copies = int.Parse(TxtCopies.Text);
		}

		
	}
}
