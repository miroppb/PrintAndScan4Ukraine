using miroppb;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http;
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
        bool CancelPrinting(string selectedPrinter);
        Task<bool> FindPackagesBetweenRange(int starting, int ending);
    }

    public class PrintDataProvider(IApiService apiService) : IPrintDataProvider
    {
        public ObservableCollection<string> LoadPrinters()
        {
            List<string> temp = new LocalPrintServer().GetPrintQueues().Select(v => v.Name).ToList();
            ObservableCollection<string> list = new();
            temp.ForEach(list.Add);
            return list;
        }

        public bool PrintBarcodes(int starting, int ending, int copies, string printer)
        {
            try
            {
                for (int a = starting; a <= ending; a++)
                {
                    for (int b = 0; b < copies; b++)
                    {
                        StringBuilder sb = new("^XA");
                        sb.AppendLine();
                        sb.AppendLine("^BY4,2,270");
                        sb.AppendLine($"^FO55,55^BC^FDCV{a.ToString("000000000")}US^FS");
                        sb.AppendLine();
                        sb.AppendLine("^XZ");

                        PrintDialog prtDlg = new();
                        FlowDocument doc = new(new Paragraph(new Run(sb.ToString())));
                        prtDlg.PrintQueue = new PrintQueue(new PrintServer(), printer);
                        IDocumentPaginatorSource idpSource = doc;
                        prtDlg.PrintDocument(idpSource.DocumentPaginator, "Hello");
                    }
                }
            }
            catch (Exception ex) { Libmiroppb.Log($"Exception: {ex.Message}"); }

            return true;
        }

        public bool CancelPrinting(string selectedPrinter)
        {
            if (selectedPrinter != null)
            {
                StringBuilder sb = new("~JA");
                PrintDialog prtDlg = new();
                FlowDocument doc = new(new Paragraph(new Run(sb.ToString())));
                prtDlg.PrintQueue = new PrintQueue(new PrintServer(), selectedPrinter);
                IDocumentPaginatorSource idpSource = doc;
                prtDlg.PrintDocument(idpSource.DocumentPaginator, "Hello");
                return true;
                //try
                //{
                //	PrintQueue printQueue = new PrintServer().GetPrintQueue(selectedPrinter);
                //	var jobs = printQueue.GetPrintJobInfoCollection();

                //	foreach (var jobInfo in jobs)
                //	{
                //		jobInfo.Cancel();
                //	}
                //	return true;
                //}
                //catch { return false; }
            }
            return false;
        }

        public async Task<bool> FindPackagesBetweenRange(int starting, int ending)
        {
            var client = apiService.Client;
            string url = $"https://pas4u.miroppb.com/package-ids-exists?starting={starting}&ending={ending}";

            var response = await client.GetAsync(url);
            if (!response.IsSuccessStatusCode)
                return false;

            var json = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<Dictionary<string, bool>>(json);
            return result != null ? result.TryGetValue("exists", out var exists) && exists : false;
        }

        //public bool FindPackagesBetweenRange(int starting, int ending)
        //{
        //	using MySqlConnection db = Secrets.GetConnectionString();
        //	return db.ExecuteScalar<int>("SELECT COUNT(id) FROM packages WHERE SUBSTRING(packageid, 3, 9) BETWEEN @starting AND @ending AND packageid LIKE 'cv%us' LIMIT 1", new { starting, ending }) > 0;
        //}
    }
}
