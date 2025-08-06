using Microsoft.Extensions.Logging;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.Reports;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor;
using OfficeOpenXml;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using OfficeOpenXml.Style;
using System.Drawing;
using static Selise.Ecap.SC.PraxisMonitor.Contracts.Constants.ReportConstants;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Queries;
using ZXing;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.Client;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.Report
{
    public class GenerateSuppliersReport : IGenerateSuppliersReport
    {
        private readonly IRepository _repository;
        private readonly ILogger<GenerateCategoryReportService> _logger;
        private readonly IPraxisReportService _praxisReportService;
        private const string ReportListSheetName = "Reporting List";
        public GenerateSuppliersReport(
         IRepository repository,
         ILogger<GenerateCategoryReportService> logger,
         IPraxisReportService praxisReportService
     )
        {
            _repository = repository;
            _logger = logger;
            _praxisReportService = praxisReportService;
        }

        private List<ClientAdditionalInfo> GetDepartmentWiseSuppliers(PraxisClient client,
            Dictionary<string, string> supplierKeyNameTranslation)
        {
            try
            {
                var deptSuppliers = new List<ClientAdditionalInfo>();
                if (client != null && client.AdditionalInfos != null && client.AdditionalInfos.Any())
                {
                    foreach (var item in client.AdditionalInfos)
                    {
                        if (!string.IsNullOrEmpty(item.CategoryName) && supplierKeyNameTranslation.TryGetValue(item.CategoryName, out var translatedName))
                        {
                            item.CategoryName = translatedName;
                        }
                        deptSuppliers.Add(item);
                    }
                    return deptSuppliers;
                }

            }
            catch (Exception ex)
            {
                _logger.LogError($"Exception in GetDepartmentWiseSuppliers. Error -> {ex.Message}. Excetion Details: {ex.StackTrace}.");
            }

            return null;
        }

        private void WriteHeaderForSuppliersReport(
            PraxisClient client,
            ExcelPackage excel,
            string reportName,
            string reportSheetName,
            string reportDateString,
            int logoPosition,
            int logoSize,
            string Report_NameHeader,
            string DateHeader,
            string clinetHeader)
        {
            try
            {
                excel.Workbook.Worksheets.Add(reportSheetName);

                // Target a worksheet
                var workSheetTable = excel.Workbook.Worksheets[reportSheetName];

                workSheetTable.Cells[1, 1].Value = Report_NameHeader;
                workSheetTable.Cells[2, 1].Value = DateHeader;
                workSheetTable.Cells[3, 1].Value = clinetHeader;

                workSheetTable.Cells["A1:A3"].Style.Font.Bold = true;
                // [firstRow, firstColumn, lastRow, LastColumn]
                workSheetTable.Cells[1, 1, 3, 2].Style.Border.Top.Style = ExcelBorderStyle.Thin;
                workSheetTable.Cells[1, 1, 3, 2].Style.Border.Left.Style = ExcelBorderStyle.Thin;
                workSheetTable.Cells[1, 1, 3, 2].Style.Border.Right.Style = ExcelBorderStyle.Thin;
                workSheetTable.Cells[1, 1, 3, 2].Style.Border.Bottom.Style = ExcelBorderStyle.Thin;

                workSheetTable.Cells["B1"].Value = reportName;
                workSheetTable.Cells["B1"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                workSheetTable.Cells["B2"].Value = reportDateString;
                workSheetTable.Cells["B2"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                workSheetTable.Cells["B3"].Value = client.ClientName.ToString();
                workSheetTable.Cells["B3"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                workSheetTable.Cells["B4:C4"].Value = "";
                workSheetTable.Cells["B4:C4"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                workSheetTable.Cells[1, logoPosition, 2, 17].Merge = true;

                _praxisReportService.AddLogoInExcelReport(workSheetTable, logoSize, logoPosition, rqLatestLogo);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    $"WriteHearder Got Error: {ex.Message}"
                );
            }
        }

        private void WriteReportingSupplierExcelReport(List<ClientAdditionalInfo> dataset, ExcelPackage excel,
          string reportSheetName, ref int rowIndex, SuppliersReportTranslation suppliersReportTranslation)
        {
            try
            {
                var workSheetTable = excel.Workbook.Worksheets[reportSheetName];
                var headerRowIndex = 4;
                var headerColumnIndex = 1;
                List<string> headerColumns = new List<string>
                {
                    
                    suppliersReportTranslation.NAME,
                    suppliersReportTranslation.CATEGORY_NAME,
                    suppliersReportTranslation.ADDRESS,
                    suppliersReportTranslation.CUSTOMERNUMBER,
                    suppliersReportTranslation.VALUEADDEDTAXNUMBER,
                    suppliersReportTranslation.BILLINGADDRESS,
                    suppliersReportTranslation.CONTACTPERSON + " 1" + Environment.NewLine + suppliersReportTranslation.NAME,
                    suppliersReportTranslation.CONTACTPERSON + " 1" + Environment.NewLine + suppliersReportTranslation.EMAIL,
                    suppliersReportTranslation.CONTACTPERSON + " 1" + Environment.NewLine + suppliersReportTranslation.PHONENUMBER,
                    suppliersReportTranslation.CONTACTPERSON + " 1" + Environment.NewLine + suppliersReportTranslation.POSITION,

                    suppliersReportTranslation.CONTACTPERSON + " 2" + Environment.NewLine + suppliersReportTranslation.NAME,
                    suppliersReportTranslation.CONTACTPERSON + " 2" + Environment.NewLine + suppliersReportTranslation.EMAIL,
                    suppliersReportTranslation.CONTACTPERSON + " 2" + Environment.NewLine + suppliersReportTranslation.PHONENUMBER,
                    suppliersReportTranslation.CONTACTPERSON + " 2" + Environment.NewLine + suppliersReportTranslation.POSITION,

                    suppliersReportTranslation.CONTACTPERSON + " 3" + Environment.NewLine + suppliersReportTranslation.NAME,
                    suppliersReportTranslation.CONTACTPERSON + " 3" + Environment.NewLine + suppliersReportTranslation.EMAIL,
                    suppliersReportTranslation.CONTACTPERSON + " 3" + Environment.NewLine + suppliersReportTranslation.PHONENUMBER,
                    suppliersReportTranslation.CONTACTPERSON + " 3" + Environment.NewLine + suppliersReportTranslation.POSITION,
                };

                foreach (var column in headerColumns)
                {
                    var cell = workSheetTable.Cells[headerRowIndex, headerColumnIndex];

                    cell.Value = column;
                    cell.Style.WrapText = true;

                    workSheetTable.Column(headerColumnIndex).Width = 45;

                    headerColumnIndex++;

                }

                // workSheetTable.Column(1).Width = 45;
                //workSheetTable.Column(2).Width = 45;

                var headerRow = workSheetTable.Cells["A4:R4"];
                headerRow.Style.Font.Bold = true;
                headerRow.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                headerRow.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                headerRow.Style.Border.Right.Style = ExcelBorderStyle.Thin;
                headerRow.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                headerRow.Style.Fill.PatternType = ExcelFillStyle.Solid;
                headerRow.Style.Fill.BackgroundColor.SetColor(Color.LightGray);
                headerRow.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                headerRow.Style.VerticalAlignment = ExcelVerticalAlignment.Center;

                foreach (ClientAdditionalInfo data in dataset)
                {
                    workSheetTable.Cells[rowIndex, 2].Style.Border.Right.Style = ExcelBorderStyle.Thin;
                    try
                    {
                        workSheetTable.Cells[rowIndex, 1].Value = data.Name;
                        workSheetTable.Cells[rowIndex, 1].Style.WrapText = true;
                        workSheetTable.Cells[rowIndex, 1].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        workSheetTable.Cells[rowIndex, 1].Style.Border.BorderAround(ExcelBorderStyle.Thin);

                        workSheetTable.Cells[rowIndex, 2].Value = data.CategoryName;
                        workSheetTable.Cells[rowIndex, 2].Style.WrapText = true;
                        workSheetTable.Cells[rowIndex, 2].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        workSheetTable.Cells[rowIndex, 2].Style.Border.BorderAround(ExcelBorderStyle.Thin);

                        workSheetTable.Cells[rowIndex, 3].Value = data.Address?.AddressLine1 ?? string.Empty;
                        workSheetTable.Cells[rowIndex, 3].Style.WrapText = true;
                        workSheetTable.Cells[rowIndex, 3].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        workSheetTable.Cells[rowIndex, 3].Style.Border.BorderAround(ExcelBorderStyle.Thin);

                        workSheetTable.Cells[rowIndex, 4].Value = data?.CustomerNumber ?? string.Empty;
                        workSheetTable.Cells[rowIndex, 4].Style.WrapText = true;
                        workSheetTable.Cells[rowIndex, 4].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        workSheetTable.Cells[rowIndex, 4].Style.Border.BorderAround(ExcelBorderStyle.Thin);

                        workSheetTable.Cells[rowIndex, 5].Value = data?.VatNumber ?? string.Empty;
                        workSheetTable.Cells[rowIndex, 5].Style.WrapText = true;
                        workSheetTable.Cells[rowIndex, 5].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        workSheetTable.Cells[rowIndex, 5].Style.Border.BorderAround(ExcelBorderStyle.Thin);

                        workSheetTable.Cells[rowIndex, 6].Value = data.BillingAddress?.AddressLine1;
                        workSheetTable.Cells[rowIndex, 6].Style.WrapText = true;
                        workSheetTable.Cells[rowIndex, 6].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        workSheetTable.Cells[rowIndex, 6].Style.Border.BorderAround(ExcelBorderStyle.Thin);

                        workSheetTable.Cells[rowIndex, 7].Value = data?.SupplierContactPersons?.ElementAtOrDefault(0)?.Name ?? string.Empty;
                        workSheetTable.Cells[rowIndex, 7].Style.WrapText = true;
                        workSheetTable.Cells[rowIndex, 7].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        workSheetTable.Cells[rowIndex, 7].Style.Border.BorderAround(ExcelBorderStyle.Thin);

                        workSheetTable.Cells[rowIndex, 8].Value = data?.SupplierContactPersons?.ElementAtOrDefault(0)?.Email ?? string.Empty;
                        workSheetTable.Cells[rowIndex, 8].Style.WrapText = true;
                        workSheetTable.Cells[rowIndex, 8].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        workSheetTable.Cells[rowIndex, 8].Style.Border.BorderAround(ExcelBorderStyle.Thin);

                        workSheetTable.Cells[rowIndex, 9].Value = data?.SupplierContactPersons?.ElementAtOrDefault(0)?.PhoneNumber ?? string.Empty;
                        workSheetTable.Cells[rowIndex, 9].Style.WrapText = true;
                        workSheetTable.Cells[rowIndex, 9].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        workSheetTable.Cells[rowIndex, 9].Style.Border.BorderAround(ExcelBorderStyle.Thin);

                        workSheetTable.Cells[rowIndex, 10].Value = data?.SupplierContactPersons?.ElementAtOrDefault(0)?.Position ?? string.Empty;
                        workSheetTable.Cells[rowIndex, 10].Style.WrapText = true;
                        workSheetTable.Cells[rowIndex, 10].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        workSheetTable.Cells[rowIndex, 10].Style.Border.BorderAround(ExcelBorderStyle.Thin);

                        workSheetTable.Cells[rowIndex, 11].Value = data?.SupplierContactPersons?.ElementAtOrDefault(1)?.Name ?? string.Empty;
                        workSheetTable.Cells[rowIndex, 11].Style.WrapText = true;
                        workSheetTable.Cells[rowIndex, 11].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        workSheetTable.Cells[rowIndex, 11].Style.Border.BorderAround(ExcelBorderStyle.Thin);

                        workSheetTable.Cells[rowIndex, 12].Value = data?.SupplierContactPersons?.ElementAtOrDefault(1)?.Email ?? string.Empty;
                        workSheetTable.Cells[rowIndex, 12].Style.WrapText = true;
                        workSheetTable.Cells[rowIndex, 12].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        workSheetTable.Cells[rowIndex, 12].Style.Border.BorderAround(ExcelBorderStyle.Thin);

                        workSheetTable.Cells[rowIndex, 13].Value = data?.SupplierContactPersons?.ElementAtOrDefault(1)?.PhoneNumber ?? string.Empty;
                        workSheetTable.Cells[rowIndex, 13].Style.WrapText = true;
                        workSheetTable.Cells[rowIndex, 13].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        workSheetTable.Cells[rowIndex, 13].Style.Border.BorderAround(ExcelBorderStyle.Thin);

                        workSheetTable.Cells[rowIndex, 14].Value = data?.SupplierContactPersons?.ElementAtOrDefault(1)?.Position ?? string.Empty;
                        workSheetTable.Cells[rowIndex, 14].Style.WrapText = true;
                        workSheetTable.Cells[rowIndex, 14].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        workSheetTable.Cells[rowIndex, 14].Style.Border.BorderAround(ExcelBorderStyle.Thin);

                        workSheetTable.Cells[rowIndex, 15].Value = data?.SupplierContactPersons?.ElementAtOrDefault(2)?.Name ?? string.Empty;
                        workSheetTable.Cells[rowIndex, 15].Style.WrapText = true;
                        workSheetTable.Cells[rowIndex, 15].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        workSheetTable.Cells[rowIndex, 15].Style.Border.BorderAround(ExcelBorderStyle.Thin);

                        workSheetTable.Cells[rowIndex, 16].Value = data?.SupplierContactPersons?.ElementAtOrDefault(2)?.Email ?? string.Empty;
                        workSheetTable.Cells[rowIndex, 16].Style.WrapText = true;
                        workSheetTable.Cells[rowIndex, 16].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        workSheetTable.Cells[rowIndex, 16].Style.Border.BorderAround(ExcelBorderStyle.Thin);

                        workSheetTable.Cells[rowIndex, 17].Value = data?.SupplierContactPersons?.ElementAtOrDefault(2)?.PhoneNumber ?? string.Empty;
                        workSheetTable.Cells[rowIndex, 17].Style.WrapText = true;
                        workSheetTable.Cells[rowIndex, 17].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        workSheetTable.Cells[rowIndex, 17].Style.Border.BorderAround(ExcelBorderStyle.Thin);

                        workSheetTable.Cells[rowIndex, 18].Value = data?.SupplierContactPersons?.ElementAtOrDefault(2)?.Position ?? string.Empty;
                        workSheetTable.Cells[rowIndex, 18].Style.WrapText = true;
                        workSheetTable.Cells[rowIndex, 18].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        workSheetTable.Cells[rowIndex, 18].Style.Border.BorderAround(ExcelBorderStyle.Thin);

                        for (int i = 1; i <= 18; i++)
                        {
                            workSheetTable.Cells[rowIndex, i].Style.VerticalAlignment = ExcelVerticalAlignment.Top;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError("Write Got Error with row number: " + rowIndex.ToString(), ex);
                    }

                    rowIndex++;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    $"Write Got Error with row number: {ex.Message}"
                );
            }
        }

        private static void AddReportBorderLine(ExcelPackage excel, int rowIndex, string reportSheetName,
           int totalColumn)
        {
            var workSheetTable = excel.Workbook.Worksheets[reportSheetName];

            for (int i = 1; i <= totalColumn; i++)
            {
                workSheetTable.Cells[rowIndex - 1, i].Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
            }
        }


        public async Task<bool> PrepareSuppliersReport(string filter,
            PraxisClient client, ExcelPackage excel,
            SuppliersReportTranslation suppliersReportTranslation,
            Dictionary<string, string> supplierKeyNameTranslation)
        {
            try
            {
                int monitoringListRowIndex = 5;
                int LogoColumnPosition = 16;
                string reportDateString = DateTime.Today.ToString("dd.MM.yyyy");

                WriteHeaderForSuppliersReport(
                    client,
                    excel,
                    suppliersReportTranslation.SUPPLIER_REPORT,
                    ReportListSheetName,
                    reportDateString,
                    LogoColumnPosition,
                    2,
                    suppliersReportTranslation.REPORT_NAME,
                    suppliersReportTranslation.DATE,
                    suppliersReportTranslation.CLIENT);

                var response = GetDepartmentWiseSuppliers(client, supplierKeyNameTranslation);

                if (response == null) return await Task.FromResult(false);

                var dataset = new
                {
                    TotalRecordCount = response.Count,
                    Results = response
                };

                int page = 0, pageSize = 100, totalPage = (int)Math.Ceiling((decimal)dataset.TotalRecordCount / pageSize);
                if (totalPage != 0)
                {
                    for (int i = 0; i < totalPage; i++)
                    {
                        var results = dataset.Results.Skip(page * pageSize).Take(pageSize).ToList();
                        WriteReportingSupplierExcelReport(results, excel, ReportListSheetName, ref monitoringListRowIndex,
                            suppliersReportTranslation);
                        page++;
                    }
                }
                else
                {
                    var results = dataset.Results.Skip(page * pageSize).Take(pageSize).ToList();
                    WriteReportingSupplierExcelReport(results, excel, ReportListSheetName, ref monitoringListRowIndex,
                        suppliersReportTranslation);
                }

                AddReportBorderLine(excel, monitoringListRowIndex, ReportListSheetName, 10);
                return await Task.FromResult(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    "Write Got Error Generating PrepareSuppliersReport: {Message}", ex.Message
                );
            }
            return await Task.FromResult(false);
        }

        private string GetContactPersons(List<ContactPerson> contactPersons)
        {
            if (contactPersons == null || contactPersons.Count == 0)
            {
                return string.Empty;
            }
            var names = contactPersons.Select(p => p.Name).ToList();
            return string.Join(", ", names);
        }
    }
}
