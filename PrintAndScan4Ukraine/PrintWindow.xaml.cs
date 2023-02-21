using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Printing;
using System.Text;
using System;
using static System.Runtime.InteropServices.JavaScript.JSType;
using System.Windows.Controls;
using System.Windows.Documents;

namespace PrintAndScan4Ukraine
{
	/// <summary>
	/// Interaction logic for PrintWindow.xaml
	/// </summary>
	public partial class PrintWindow : Window
	{
		public PrintWindow()
		{
			InitializeComponent();
		}

		private void Window_Loaded(object sender, RoutedEventArgs e)
		{
			List<string> printers = new  LocalPrintServer().GetPrintQueues().Select(v => v.Name).ToList();
			foreach (string s in printers)
				CmbListOfPrinters.Items.Add(s);
		}

		private void BtnPrint_Click(object sender, RoutedEventArgs e)
		{
			int from = int.Parse(TxtFrom.Text);
			int to = int.Parse(TxtTo.Text);
			int copies = int.Parse(TxtCopies.Text);

			for (int a = from; a <= to; a++)
			{
				for (int b = 0; b < copies; b++)
				{
					StringBuilder sb = new StringBuilder("^XA");
					sb.AppendLine();
					sb.AppendLine("^BY5,2,270");
					sb.AppendLine($"^FO100,50^BC^FD{a.ToString("00000000")}^FS");
					sb.AppendLine();
					sb.AppendLine("^XZ");

					PrintDialog prtDlg = new PrintDialog();
					FlowDocument doc = new FlowDocument(new Paragraph(new Run(sb.ToString())));
					prtDlg.PrintQueue = new PrintQueue(new PrintServer(), CmbListOfPrinters.SelectedValue.ToString());
					IDocumentPaginatorSource idpSource = doc;
					prtDlg.PrintDocument(idpSource.DocumentPaginator, "Hello");
				}
			}
			
		}
	}
}
