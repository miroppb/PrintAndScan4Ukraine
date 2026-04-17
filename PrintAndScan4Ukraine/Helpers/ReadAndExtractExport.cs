using OfficeOpenXml;
using PrintAndScan4Ukraine.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace PrintAndScan4Ukraine.Helpers
{
    public class ReadAndExtractExport
    {
        public static async Task<List<string>> ExtractPackageIdsFromExportAsync(Export export)
        {
            // Step 2: Convert base64 to bytes
            byte[] fileBytes = Convert.FromBase64String(export.Content);

            ExcelPackage.License.SetNonCommercialOrganization("PrintAndScan4Ukraine");

            using var ms = new MemoryStream(fileBytes);
            using var package = new ExcelPackage(ms);

            var ws = package.Workbook.Worksheets[0]; // first worksheet

            var ids = new List<string>();
            int row = 7; // skip 5 rows + header row

            while (true)
            {
                var cellValue = ws.Cells[row, 1].Text?.Trim();

                if (string.IsNullOrWhiteSpace(cellValue))
                    break; // stop when we hit an empty row

                ids.Add(cellValue);
                row++;
            }

            return ids;
        }
    }
}
