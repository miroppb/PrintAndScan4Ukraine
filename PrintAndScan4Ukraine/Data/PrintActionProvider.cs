using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Printing;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Documents;

namespace PrintAndScan4Ukraine.Data
{
	public interface IPrintDataProvider
	{
		ObservableCollection<string> LoadPrinters();
		bool PrintBarcodes(int starting, int ending, int copies, string printer);
	}

	public class PrintDataProvider : IPrintDataProvider
	{
		public ObservableCollection<string> LoadPrinters()
		{
			List<string> temp = new LocalPrintServer().GetPrintQueues().Select(v => v.Name).ToList();
			ObservableCollection<string> list = new();
			temp.ForEach(x => list.Add(x));
			return list;
		}

		public bool PrintBarcodes(int starting, int ending, int copies, string printer)
		{

			for (int a = starting; a <= ending; a++)
			{
				for (int b = 0; b < copies; b++)
				{
					StringBuilder sb = new StringBuilder("^XA");
					sb.AppendLine();
					sb.AppendLine("^BY4,2,270");
					sb.AppendLine($"^FO100,50^BC^FDCV{a.ToString("0000000")}US^FS");
					sb.AppendLine();
					sb.AppendLine("^XZ");

					PrintDialog prtDlg = new PrintDialog();
					FlowDocument doc = new FlowDocument(new Paragraph(new Run(sb.ToString())));
					prtDlg.PrintQueue = new PrintQueue(new PrintServer(), printer);
					IDocumentPaginatorSource idpSource = doc;
					prtDlg.PrintDocument(idpSource.DocumentPaginator, "Hello");
				}
			}
			return true;
		}
	}
}
