using Microsoft.Extensions.Logging;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.Reports;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using static Selise.Ecap.SC.PraxisMonitor.Contracts.Constants.ReportConstants;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.Report
{
    public class GenerateCategoryReportService : IGenerateCategoryReport
    {
        private readonly IPraxisClientCategoryService _praxisClientCategoryService;
        private readonly ILogger<GenerateCategoryReportService> _logger;
        private readonly IPraxisReportService _praxisReportService;
        private const string ReportListSheetName = "Reporting List";

        public GenerateCategoryReportService(
            IPraxisClientCategoryService praxisClientCategoryService,
            ILogger<GenerateCategoryReportService> logger,
            IPraxisReportService praxisReportService
        )
        {
            _praxisClientCategoryService = praxisClientCategoryService;
            _logger = logger;
            _praxisReportService = praxisReportService;
        }

        public async Task<bool> PrepareCategoryReport(string filter, PraxisClient client, ExcelPackage excel,
            CategoryReportTranslation categoryReportTranslation)
        {
            int monitoringListRowIndex = 5;
            int LogoColumnPosition = 4;
            string reportDateString = DateTime.Today.ToString("dd.MM.yyyy");

            WriteHeaderForCategoryReport(client, excel, categoryReportTranslation.CATEGORY_REPORT, ReportListSheetName,
                reportDateString, LogoColumnPosition, 2, categoryReportTranslation.REPORT_NAME,
                categoryReportTranslation.DATE, categoryReportTranslation.ORGANIZATION);

            var dataset = await _praxisClientCategoryService.GetCategoryReportData(filter, "{CreateDate: -1}");

            int page = 0, pageSize = 100, totalPage = (int)Math.Ceiling((decimal)dataset.TotalRecordCount / pageSize);
            if (totalPage != 0)
            {
                for (int i = 0; i < totalPage; i++)
                {
                    var results = dataset.Results.Skip(page * pageSize).Take(pageSize).ToList();
                    WriteReportingCategoryExcelReport(results, excel, ReportListSheetName, ref monitoringListRowIndex,
                        categoryReportTranslation);
                    page++;
                }
            }
            else
            {
                var results = dataset.Results.Skip(page * pageSize).Take(pageSize).ToList();
                WriteReportingCategoryExcelReport(results, excel, ReportListSheetName, ref monitoringListRowIndex,
                    categoryReportTranslation);
            }

            AddReportBorderLine(excel, monitoringListRowIndex, ReportListSheetName, 2);
            return true;
        }

        private void WriteHeaderForCategoryReport(PraxisClient client, ExcelPackage excel, string reportName,
            string reportSheetName, string reportDateString, int logoPosition, int logoSize, string Report_NameHeader,
            string DateHeader, string OrganizationHeader)
        {
            try
            {
                excel.Workbook.Worksheets.Add(reportSheetName);

                // Target a worksheet
                var workSheetTable = excel.Workbook.Worksheets[reportSheetName];

                workSheetTable.Cells[1, 1].Value = Report_NameHeader;
                workSheetTable.Cells[2, 1].Value = DateHeader;
                workSheetTable.Cells[3, 1].Value = OrganizationHeader;

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

                workSheetTable.Cells[1, logoPosition, 2, 5].Merge = true;

                _praxisReportService.AddLogoInExcelReport(workSheetTable, logoSize, logoPosition, rqLatestLogo);
            }
            catch (Exception ex)
            {
                _logger.LogError("WriteHeader got error: {Message}", ex.Message);
            }
        }

        private void WriteReportingCategoryExcelReport(List<PraxisClientCategory> dataset, ExcelPackage excel,
            string reportSheetName, ref int rowIndex, CategoryReportTranslation categoryReportTranslation)
        {
            try
            {
                var workSheetTable = excel.Workbook.Worksheets[reportSheetName];
                var headerRowIndex = 4;
                var headerColumnIndex = 1;
                List<string> headerColumns = new List<string>
                {
                    categoryReportTranslation.NAME_OF_CATEGORIES,
                    categoryReportTranslation.SUB_CATEGORY
                };

                foreach (var column in headerColumns)
                {
                    workSheetTable.Cells[headerRowIndex, headerColumnIndex].Value = column;
                    headerColumnIndex++;
                }

                workSheetTable.Column(1).Width = 45;
                workSheetTable.Column(2).Width = 45;

                var headerRow = workSheetTable.Cells[$"A{headerRowIndex}:B{headerRowIndex}"];
                headerRow.Style.Font.Bold = true;
                headerRow.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                headerRow.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                headerRow.Style.Border.Right.Style = ExcelBorderStyle.Thin;
                headerRow.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                headerRow.Style.Fill.PatternType = ExcelFillStyle.Solid;
                headerRow.Style.Fill.BackgroundColor.SetColor(Color.LightGray);
                headerRow.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                headerRow.Style.VerticalAlignment = ExcelVerticalAlignment.Center;

                foreach (PraxisClientCategory data in dataset)
                {
                    workSheetTable.Cells[rowIndex, 2].Style.Border.Right.Style = ExcelBorderStyle.Thin;
                    try
                    {
                        workSheetTable.Cells[rowIndex, 1].Value = data.Name;
                        workSheetTable.Cells[rowIndex, 1].Style.WrapText = true;
                        workSheetTable.Cells[rowIndex, 1].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        workSheetTable.Cells[rowIndex, 1].Style.Border.BorderAround(ExcelBorderStyle.Thin);

                        var subCategories = string.Join('\n', data.SubCategories.Select(s => s.Name));

                        workSheetTable.Cells[rowIndex, 2].Value = subCategories; // GetMemberNames(data.ControlledMembers.ToList());title + "\n"
                        workSheetTable.Cells[rowIndex, 2].Style.WrapText = true;
                        workSheetTable.Cells[rowIndex, 2].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        workSheetTable.Cells[rowIndex, 2].Style.Border.BorderAround(ExcelBorderStyle.Thin);

                        for (int i = 1; i <= 2; i++)
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
                _logger.LogError("Write got error with row number: {RowNumber}. Error message: {Message}", rowIndex, ex.Message);
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
    }
}