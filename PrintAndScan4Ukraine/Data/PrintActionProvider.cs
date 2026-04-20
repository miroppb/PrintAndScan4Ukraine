using miroppb;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Printing;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Documents;

namespace PrintAndScan4Ukraine.Data
{
    public interface IPrintDataProvider
    {
        ObservableCollection<string> LoadPrinters();
        Task<bool> PrintBarcodes(int starting, int ending, int copies, string printer);
        bool CancelPrinting(string selectedPrinter);
        Task<bool> FindPackagesBetweenRange(int starting, int ending);
    }

    public class PrintDataProvider : IPrintDataProvider
    {
        private readonly IApiService apiService;
        private readonly System.Collections.Concurrent.ConcurrentDictionary<string, CancellationTokenSource> _cancellationSources = new();

        public PrintDataProvider(IApiService apiService)
        {
            this.apiService = apiService;
        }
        public ObservableCollection<string> LoadPrinters()
        {
            List<string> temp = new LocalPrintServer().GetPrintQueues().Select(v => v.Name).ToList();
            ObservableCollection<string> list = new();
            temp.ForEach(list.Add);
            return list;
        }

        public async Task<bool> PrintBarcodes(int starting, int ending, int copies, string printer)
        {
            if (string.IsNullOrEmpty(printer))
                return false;

            // Cancel any existing print operation for this printer and replace with a new token
            try
            {
                if (_cancellationSources.TryGetValue(printer, out var existing))
                {
                    try { existing.Cancel(); } catch { }
                    try { existing.Dispose(); } catch { }
                    _cancellationSources.TryRemove(printer, out _);
                }

                var cts = new CancellationTokenSource();
                _cancellationSources[printer] = cts;
                var token = cts.Token;

                for (int a = starting; a <= ending; a++)
                {
                    for (int b = 0; b < copies; b++)
                    {
                        token.ThrowIfCancellationRequested();

                        StringBuilder sb = new("^XA");
                        sb.AppendLine();
                        sb.AppendLine("^BY4,2,270");
                        sb.AppendLine($"^FO55,55^BC^FDCV{a.ToString("000000000")}US^FS");
                        sb.AppendLine();
                        sb.AppendLine("^XZ");

                        try
                        {
                            PrintDialog prtDlg = new();
                            FlowDocument doc = new(new Paragraph(new Run(sb.ToString())));
                            prtDlg.PrintQueue = new PrintQueue(new PrintServer(), printer);
                            IDocumentPaginatorSource idpSource = doc;
                            prtDlg.PrintDocument(idpSource.DocumentPaginator, "PrintBarcode");
                        }
                        catch (Exception ex)
                        {
                            Libmiroppb.Log($"Print exception: {ex.Message}");
                        }
                    }
                    // Wait between individual numbers so user can cancel
                    try
                    {
                        await Task.Delay(TimeSpan.FromMilliseconds(500), token);
                    }
                    catch (OperationCanceledException) { token.ThrowIfCancellationRequested(); }
                }

                return true;
            }
            catch (OperationCanceledException)
            {
                Libmiroppb.Log("Printing cancelled by user.");
                return false;
            }
            catch (Exception ex)
            {
                Libmiroppb.Log($"Exception: {ex.Message}");
                return false;
            }
            finally
            {
                if (!string.IsNullOrEmpty(printer))
                {
                    if (_cancellationSources.TryRemove(printer, out var finished))
                    {
                        try { finished.Dispose(); } catch { }
                    }
                }
            }
        }

        public bool CancelPrinting(string selectedPrinter)
        {
            if (string.IsNullOrEmpty(selectedPrinter))
                return false;

            try
            {
                // Signal cancellation to any running print loop for this printer
                if (_cancellationSources.TryGetValue(selectedPrinter, out var cts))
                {
                    try { cts.Cancel(); } catch { }
                }

                // Try to cancel jobs in the Windows print queue for this printer
                try
                {
                    var ps = new PrintServer();
                    var pq = ps.GetPrintQueue(selectedPrinter);
                    pq.Refresh();
                    var jobs = pq.GetPrintJobInfoCollection();
                    foreach (var jobInfo in jobs)
                    {
                        try { jobInfo.Cancel(); } catch { }
                    }
                }
                catch (Exception ex)
                {
                    Libmiroppb.Log($"Cancel print queue exception: {ex.Message}");
                }

                // Also send a ZPL abort sequence to the printer as a best-effort attempt
                try
                {
                    StringBuilder sb = new("~JA");
                    PrintDialog prtDlg = new();
                    FlowDocument doc = new(new Paragraph(new Run(sb.ToString())));
                    prtDlg.PrintQueue = new PrintQueue(new PrintServer(), selectedPrinter);
                    IDocumentPaginatorSource idpSource = doc;
                    prtDlg.PrintDocument(idpSource.DocumentPaginator, "Cancel");
                }
                catch (Exception ex)
                {
                    Libmiroppb.Log($"Best-effort ZPL cancel failed: {ex.Message}");
                }

                return true;
            }
            catch (Exception ex)
            {
                Libmiroppb.Log($"CancelPrinting exception: {ex.Message}");
                return false;
            }
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
