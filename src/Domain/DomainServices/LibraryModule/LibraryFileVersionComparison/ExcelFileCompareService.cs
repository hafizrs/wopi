using Microsoft.Extensions.Logging;
using OfficeOpenXml.Style;
using OfficeOpenXml;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices;
using Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.LibraryModule;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.LibraryFileVersionComparison;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.LibraryFileVersionComparison
{
    public class ExcelFileCompareService : ICompareFileVersionFromStreamService
    {
        private readonly ILogger<ExcelFileCompareService> _logger;
       

        public ExcelFileCompareService(ILogger<ExcelFileCompareService> logger)
        {
            _logger = logger;
            
        }

        public Task<byte[]> CompareDeleteFileVersionFromStream(Stream latestVersionFileStream, Stream oldVersionFileStream)
        {
            throw new NotImplementedException();
        }

        public async Task<byte[]> CompareFileVersionFromStream(Stream latestVersionFileStream, Stream oldVersionFileStream)
        {
            
            var comparisonResult = await GetLibraryVersionComparisonExcel(latestVersionFileStream, oldVersionFileStream);
            return comparisonResult;
        }

        public Task<byte[]> CompareUpdateFileVersionFromStream(Stream latestVersionFileStream, Stream oldVersionFileStream)
        {
            throw new NotImplementedException();
        }

        private async Task<byte[]> GetLibraryVersionComparisonExcel(Stream latestVersionFileStream, Stream oldVersionFileStream)
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            using (var outputStream = new MemoryStream())
            {
                using (var package1 = new ExcelPackage(latestVersionFileStream))
                using (var package2 = new ExcelPackage(oldVersionFileStream))
                using (var outputPackage = new ExcelPackage())
                {
                    var workbook1 = package1.Workbook;
                    var workbook2 = package2.Workbook;
                    var outputWorkbook = outputPackage.Workbook;

                    foreach (var worksheet1 in workbook1.Worksheets)
                    {
                        var worksheet2 = workbook2.Worksheets[worksheet1.Name];
                        var outputWorksheet = outputWorkbook.Worksheets.Add(worksheet1.Name);

                        int maxRows = Math.Max(worksheet1.Dimension?.Rows ?? 0, worksheet2?.Dimension?.Rows ?? 0);
                        int maxCols = Math.Max(worksheet1.Dimension?.Columns ?? 0, worksheet2?.Dimension?.Columns ?? 0);

                        for (int row = 1; row <= maxRows; row++)
                        {
                            for (int col = 1; col <= maxCols; col++)
                            {
                                var cell1 = worksheet1.Cells[row, col];
                                var cell2 = worksheet2?.Cells[row, col];

                                var value1 = cell1?.Value?.ToString();
                                var value2 = cell2?.Value?.ToString();

                                if (value1 != null && value2 != null)
                                {
                                    outputWorksheet.Cells[row, col].Value = value1;

                                    if (value1 != value2)
                                    {
                                        outputWorksheet.Cells[row, col].Style.Fill.PatternType = ExcelFillStyle.Solid;
                                        outputWorksheet.Cells[row, col].Style.Fill.BackgroundColor.SetColor(Color.Yellow);
                                    }
                                }
                                else if (value1 != null && value2 == null)
                                {
                                    outputWorksheet.Cells[row, col].Value = value1;
                                    outputWorksheet.Cells[row, col].Style.Fill.PatternType = ExcelFillStyle.Solid;
                                    outputWorksheet.Cells[row, col].Style.Fill.BackgroundColor.SetColor(Color.Red);
                                    outputWorksheet.Cells[row, col].Style.Font.Strike = true;
                                }
                                else if (value1 == null && value2 != null)
                                {

                                    outputWorksheet.Cells[row, col].Value = value2;
                                    outputWorksheet.Cells[row, col].Style.Fill.PatternType = ExcelFillStyle.Solid;
                                    outputWorksheet.Cells[row, col].Style.Fill.BackgroundColor.SetColor(Color.Green);
                                }
                            }
                        }
                    }

                    await outputPackage.SaveAsAsync(outputStream);
                    return outputStream.ToArray();
                }
            }


        }


    }


}
