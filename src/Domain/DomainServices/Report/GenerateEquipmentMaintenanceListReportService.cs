using Microsoft.Extensions.Logging;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.Equipment;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.Reports;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.Report
{
    public class GenerateEquipmentMaintenanceListReportService : IGenerateEquipmentMaintenanceListReport
    {
        private const int logoPosition = 13;
        private readonly ILogger<GenerateEquipmentMaintenanceListReportService> _logger;
        private readonly IUilmResourceKeyService _uilmResourceKeyService;
        private readonly IPraxisReportService _praxisReportService;
        private const string ReportListSheetName = "Maintenance Report";
        private Dictionary<string, string> _maintenanceStatusTranslation = new Dictionary<string, string>();
        private readonly ICommonUtilService _commonUtilService;
        private readonly IRepository _repository;

        public GenerateEquipmentMaintenanceListReportService(
            ILogger<GenerateEquipmentMaintenanceListReportService> logger,
            IUilmResourceKeyService uilmResourceKeyService,
            IPraxisReportService praxisReportService,
            ICommonUtilService commonUtilService,
            IRepository repository
        )
        {
            _logger = logger;
            _uilmResourceKeyService = uilmResourceKeyService;
            _praxisReportService = praxisReportService;
            _commonUtilService = commonUtilService;
            _repository = repository;
        }

        public async Task<bool> PrepareEquipmentMaintenanceListReport(string filter, PraxisClient client,
            ExcelPackage excel, EquipmentMaintenanceListTranslation translationEquipmentList, bool isValidationReport)
        {
            _maintenanceStatusTranslation = _uilmResourceKeyService
                .GetResourceValueByKeyName(new List<string> { "DONE", "PENDING", "SUBMITTED", "IN_PROGRESS", "MAINTENANCE", "VALIDATION" });
            var monitoringListRowIndex = 5;
            var reportDateString = DateTime.Today.ToString("dd.MM.yyyy");
            var reportName = isValidationReport
                ? translationEquipmentList.EQUIPMENT_VALIDATION_REPORT
                : translationEquipmentList.EQUIPMENT_MAINTENANCE_REPORT;

            WriteHeaderForEquipmentMaintenanceReport(client, excel, reportName,
                ReportListSheetName, reportDateString, translationEquipmentList.REPORT_NAME,
                translationEquipmentList.DATE, translationEquipmentList.ORGANIZATION);

            var dataset = await _commonUtilService.GetEntityQueryResponse<PraxisEquipmentMaintenance>(filter, "{MaintenanceEndDate: 1}");

            var page = 0;
            const int pageSize = 100;
            var totalPage = (int)Math.Ceiling((decimal)dataset.TotalRecordCount / pageSize);

            if (totalPage != 0)
            {
                for (var i = 0; i < totalPage; i++)
                {
                    var results = dataset?
                                      .Results?
                                      .Skip(page * pageSize)
                                      .Take(pageSize)
                                      .ToList() ??
                                  new List<PraxisEquipmentMaintenance>();
                    WriteContentOfEquipmentMaintenanceReport(results, excel, ReportListSheetName,
                        ref monitoringListRowIndex, translationEquipmentList, isValidationReport);
                    ++page;
                }
            }
            else
            {
                var results = dataset
                    .Results
                    .ToList();
                WriteContentOfEquipmentMaintenanceReport(results, excel, ReportListSheetName,
                    ref monitoringListRowIndex, translationEquipmentList, isValidationReport);
            }
            AddReportBorderLine(excel, monitoringListRowIndex, ReportListSheetName, 13);
            return true;
        }

        private void WriteHeaderForEquipmentMaintenanceReport(PraxisClient client, ExcelPackage excel, string reportName,
            string reportSheetName, string reportDateString, string reportNameHeader, string dateHeader, string organizationHeader)
        {
            try
            {
                excel.Workbook.Worksheets.Add(reportSheetName);

                // Target a worksheet
                var workSheetTable = excel.Workbook.Worksheets[reportSheetName];

                workSheetTable.Cells[1, 1].Value = reportNameHeader;
                workSheetTable.Cells[2, 1].Value = dateHeader;
                workSheetTable.Cells[3, 1].Value = organizationHeader;

                workSheetTable.Cells["A1:A3"].Style.Font.Bold = true;

                // [firstRow, firstColumn, lastRow, LastColumn]
                workSheetTable.Cells[1, 1, 3, 3].Style.Border.Top.Style = ExcelBorderStyle.Thin;
                workSheetTable.Cells[1, 1, 3, 3].Style.Border.Left.Style = ExcelBorderStyle.Thin;
                workSheetTable.Cells[1, 1, 3, 3].Style.Border.Right.Style = ExcelBorderStyle.Thin;
                workSheetTable.Cells[1, 1, 3, 3].Style.Border.Bottom.Style = ExcelBorderStyle.Thin;

                workSheetTable.Cells["B1:C1"].Merge = true;
                workSheetTable.Cells["B1:C1"].Value = reportName;
                workSheetTable.Cells["B1:C1"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                workSheetTable.Cells["B2:C2"].Merge = true;
                workSheetTable.Cells["B2:C2"].Value = reportDateString;
                workSheetTable.Cells["B2:C2"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                workSheetTable.Cells["B3:C3"].Merge = true;
                workSheetTable.Cells["B3:C3"].Value = client.ClientName;
                workSheetTable.Cells["B3:C3"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                workSheetTable.Cells[1, logoPosition, 2, logoPosition].Merge = true;
                _praxisReportService.AddLogoInExcelReport(workSheetTable, 2, logoPosition, "");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Exception occurred during equipment report header generation process.");
                _logger.LogError("Exception Message: {Message}. Exception Details: {StackTrace}.", ex.Message, ex.StackTrace);
            }
        }

        private void WriteContentOfEquipmentMaintenanceReport(List<PraxisEquipmentMaintenance> dataset,
            ExcelPackage excel, string reportSheetName, ref int rowIndex,
            EquipmentMaintenanceListTranslation translationList, bool isValidationReport)
        {
            try
            {
                var workSheetTable = excel.Workbook.Worksheets[reportSheetName];

                SetContentHeader(workSheetTable, translationList, isValidationReport);

                foreach (PraxisEquipmentMaintenance data in dataset)
                {
                    workSheetTable.Cells[rowIndex, 13].Style.Border.Right.Style = ExcelBorderStyle.Thin;

                    // Maintenance/Validation
                    workSheetTable.Cells[$"A{rowIndex}"].Value = GetTranslatedValueOrDefault(data?.ScheduleType?.ToUpper(), data?.ScheduleType);
                    workSheetTable.Cells[$"A{rowIndex}"].Style.WrapText = true;
                    workSheetTable.Cells[$"A{rowIndex}"].Style.VerticalAlignment = ExcelVerticalAlignment.Top;
                    workSheetTable.Cells[$"A{rowIndex}"].Style.Border.BorderAround(ExcelBorderStyle.Thin);

                    // Equipment Name
                    workSheetTable.Cells[$"B{rowIndex}"].Value = data?.Title;
                    workSheetTable.Cells[$"B{rowIndex}"].Style.WrapText = true;
                    workSheetTable.Cells[$"B{rowIndex}"].Style.VerticalAlignment = ExcelVerticalAlignment.Top;
                    workSheetTable.Cells[$"B{rowIndex}"].Style.Border.BorderAround(ExcelBorderStyle.Thin);

                    // Remarks
                    workSheetTable.Cells[$"C{rowIndex}"].Value =
                        HtmlToTextAgilityPackage.ExtractStyledText(data?.Remarks);
                    workSheetTable.Cells[$"C{rowIndex}"].Style.WrapText = true;
                    workSheetTable.Cells[$"C{rowIndex}"].Style.VerticalAlignment = ExcelVerticalAlignment.Top;
                    workSheetTable.Cells[$"C{rowIndex}"].Style.Border.BorderAround(ExcelBorderStyle.Thin);

                    // Maintenance Start Date
                    workSheetTable.Cells[$"D{rowIndex}"].Value = data?.MaintenanceDate != null ? $"{data.MaintenanceDate:dd.MM.yyyy} (UTC)" : "";
                    workSheetTable.Cells[$"D{rowIndex}"].Style.WrapText = true;
                    workSheetTable.Cells[$"D{rowIndex}"].Style.VerticalAlignment = ExcelVerticalAlignment.Top;
                    workSheetTable.Cells[$"D{rowIndex}"].Style.Border.BorderAround(ExcelBorderStyle.Thin);

                    // Maintenance End Date
                    workSheetTable.Cells[$"E{rowIndex}"].Value = data?.MaintenanceEndDate != null ? $"{data.MaintenanceEndDate:dd.MM.yyyy} (UTC)" : "";
                    workSheetTable.Cells[$"E{rowIndex}"].Style.WrapText = true;
                    workSheetTable.Cells[$"E{rowIndex}"].Style.VerticalAlignment = ExcelVerticalAlignment.Top;
                    workSheetTable.Cells[$"E{rowIndex}"].Style.Border.BorderAround(ExcelBorderStyle.Thin);

                    // Status
                    workSheetTable.Cells[$"F{rowIndex}"].Value = GetTranslatedValueOrDefault(data?.CompletionStatus?.Value);
                    workSheetTable.Cells[$"F{rowIndex}"].Style.WrapText = true;
                    workSheetTable.Cells[$"F{rowIndex}"].Style.VerticalAlignment = ExcelVerticalAlignment.Top;
                    workSheetTable.Cells[$"F{rowIndex}"].Style.Border.BorderAround(ExcelBorderStyle.Thin);

                    // Executing group
                    workSheetTable.Cells[$"G{rowIndex}"].Value = (data?.ExecutivePersonIds?.Any() == true)
                        ? GetPraxisUsersByIds(data.ExecutivePersonIds.ToList())
                        : "";
                    workSheetTable.Cells[$"G{rowIndex}"].Style.WrapText = true;
                    workSheetTable.Cells[$"G{rowIndex}"].Style.VerticalAlignment = ExcelVerticalAlignment.Top;
                    workSheetTable.Cells[$"G{rowIndex}"].Style.Border.BorderAround(ExcelBorderStyle.Thin);

                    // Approver
                    workSheetTable.Cells[$"H{rowIndex}"].Value = (data?.ApprovedPersonIds?.Any() == true)
                        ? GetPraxisUsersByIds(data.ApprovedPersonIds.ToList())
                        : "";
                    workSheetTable.Cells[$"H{rowIndex}"].Style.WrapText = true;
                    workSheetTable.Cells[$"H{rowIndex}"].Style.VerticalAlignment = ExcelVerticalAlignment.Top;
                    workSheetTable.Cells[$"H{rowIndex}"].Style.Border.BorderAround(ExcelBorderStyle.Thin);

                    // Supplier
                    workSheetTable.Cells[$"I{rowIndex}"].Value = (data?.ExternalUserInfos?.Any() == true)
                        ? GetSupplierNames(data.ExternalUserInfos.ToList())
                        : "";
                    workSheetTable.Cells[$"I{rowIndex}"].Style.WrapText = true;
                    workSheetTable.Cells[$"I{rowIndex}"].Style.VerticalAlignment = ExcelVerticalAlignment.Top;
                    workSheetTable.Cells[$"I{rowIndex}"].Style.Border.BorderAround(ExcelBorderStyle.Thin);

                    // Process guide name
                    workSheetTable.Cells[$"J{rowIndex}"].Value = data?.PraxisFormInfo != null
                        ? data.PraxisFormInfo.FormName
                        : "";
                    workSheetTable.Cells[$"J{rowIndex}"].Style.WrapText = true;
                    workSheetTable.Cells[$"J{rowIndex}"].Style.VerticalAlignment = ExcelVerticalAlignment.Top;
                    workSheetTable.Cells[$"J{rowIndex}"].Style.Border.BorderAround(ExcelBorderStyle.Thin);

                    // Library form name
                    workSheetTable.Cells[$"K{rowIndex}"].Value = (data?.LibraryForms?.Any() == true)
                        ? string.Join(", ", data.LibraryForms.Select(form => form.LibraryFormName).ToList())
                        : "";
                    workSheetTable.Cells[$"K{rowIndex}"].Style.WrapText = true;
                    workSheetTable.Cells[$"K{rowIndex}"].Style.VerticalAlignment = ExcelVerticalAlignment.Top;
                    workSheetTable.Cells[$"K{rowIndex}"].Style.Border.BorderAround(ExcelBorderStyle.Thin);

                    // Completed by
                    workSheetTable.Cells[$"L{rowIndex}"].Value = (data?.Answers?.Any() == true)
                        ? GetPraxisUsersByIds(data.Answers.Where(answer => answer.ReportedBy != null).Select(answer => answer.ReportedBy).ToList())
                        : "";
                    workSheetTable.Cells[$"L{rowIndex}"].Style.WrapText = true;
                    workSheetTable.Cells[$"L{rowIndex}"].Style.VerticalAlignment = ExcelVerticalAlignment.Top;
                    workSheetTable.Cells[$"L{rowIndex}"].Style.Border.BorderAround(ExcelBorderStyle.Thin);

                    // Approved by
                    workSheetTable.Cells[$"M{rowIndex}"].Value = (data?.Answers?.Any() == true)
                        ? GetPraxisUsersByIds(data.Answers
                            .Where(answer => answer?.ApprovalResponse != null && answer?.ApprovalResponse.ReportedBy != null)
                            .Select(answer => answer.ApprovalResponse.ReportedBy).ToList())
                        : "";
                    workSheetTable.Cells[$"M{rowIndex}"].Style.WrapText = true;
                    workSheetTable.Cells[$"M{rowIndex}"].Style.VerticalAlignment = ExcelVerticalAlignment.Top;
                    workSheetTable.Cells[$"M{rowIndex}"].Style.Border.BorderAround(ExcelBorderStyle.Thin);

                    rowIndex++;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Exception occurred during equipment report data write process.");
                _logger.LogError("Exception Message: {Message}. Exception Details: {StackTrace}.", ex.Message, ex.StackTrace);
            }
        }

        private void SetContentHeader(ExcelWorksheet workSheetTable, EquipmentMaintenanceListTranslation translationList, bool isValidationReport)
        {
            var contentHeaderRowIndex = 4;
            var contentHeaderColumnIndex = 1;
            List<string> contentHeaderValues = new List<string>()
            {
                translationList.MAINTENANCE_OR_VALIDATION, //0
                translationList.EQUIPMENT_NAME, // 1
                translationList.REMARKS, // 2
                translationList.MAINTENANCE_START_DATE, //3
                translationList.MAINTENANCE_END_DATE, // 4
                translationList.MAINTENANCE_STATUS, // 5
                translationList.EXECUTING_GROUP, // 6
                translationList.APPROVER, // 7
                translationList.SUPPLIER, // 8
                translationList.PROCESS_GUIDE_NAME, // 9
                translationList.LIBRARY_FORM_NAME, // 10
                translationList.COMPLETED_BY, // 11
                translationList.APPROVED_BY // 12
            };
            if (isValidationReport)
            {
                contentHeaderValues[3] = translationList.VALIDATION_START_DATE;
                contentHeaderValues[4] = translationList.VALIDATION_END_DATE;
                contentHeaderValues[5] = translationList.VALIDATION_STATUS;
            }
            foreach (var column in contentHeaderValues)
            {
                workSheetTable.Cells[contentHeaderRowIndex, contentHeaderColumnIndex].Value = column;
                ++contentHeaderColumnIndex;
            }

            for (int i = 1; i <= contentHeaderValues.Count; i++)
            {
                workSheetTable.Row(i).CustomHeight = true;
            }
            workSheetTable.Column(1).Width = 25; // Maintenance/Validation
            workSheetTable.Column(2).Width = 20; // Equipment Name
            CustomBestFitColumn(workSheetTable.Column(3), workSheetTable.Row(3), 30, 35); // Remarks
            workSheetTable.Column(4).Width = 22; // Maintenance Start Date
            workSheetTable.Column(5).Width = 22; // Maintenance End Date
            workSheetTable.Column(6).Width = 12; // Status
            workSheetTable.Column(7).Width = 20; // Executing Group
            workSheetTable.Column(8).Width = 20; // Approver
            workSheetTable.Column(9).Width = 20; // Supplier
            workSheetTable.Column(10).Width = 20; // Process guide name
            workSheetTable.Column(11).Width = 20; // Library form name
            workSheetTable.Column(12).Width = 20; // Completed by
            workSheetTable.Column(13).Width = 20; // Approved by

            var headerRow = workSheetTable.Cells[$"A{contentHeaderRowIndex}:M{contentHeaderRowIndex}"];
            headerRow.Style.Font.Bold = true;
            headerRow.Style.Border.Top.Style = ExcelBorderStyle.Thin;
            headerRow.Style.Border.Left.Style = ExcelBorderStyle.Thin;
            headerRow.Style.Border.Right.Style = ExcelBorderStyle.Thin;
            headerRow.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
            headerRow.Style.Fill.PatternType = ExcelFillStyle.Solid;
            headerRow.Style.Fill.BackgroundColor.SetColor(Color.LightGray);
            headerRow.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            headerRow.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
        }
        private void CustomBestFitColumn(ExcelColumn column, ExcelRow row, int minWidth, int maxWidth)
        {
            row.CustomHeight = false;
            column.AutoFit();
            column.Width = Math.Min(column.Width, maxWidth);
            column.Width = Math.Max(column.Width, minWidth);
        }
        private static void AddReportBorderLine(ExcelPackage excel, int rowIndex, string reportSheetName, int totalColumn)
        {
            var workSheetTable = excel.Workbook.Worksheets[reportSheetName];

            for (int i = 1; i <= totalColumn; i++)
            {
                workSheetTable.Cells[rowIndex - 1, i].Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
            }
        }

        private string GetPraxisUsersByIds(List<string> ids)
        {
            var praxisUsers = _repository
                                  .GetItems<PraxisUser>(item => ids.Contains(item.ItemId))?
                                  .Select(user => user.DisplayName)?
                                  .ToList()
                              ?? new List<string>();
            return string.Join(", ", praxisUsers);
        }

        private string GetSupplierNames(List<PraxisEquipmentMaintenanceByExternalUser> externalUserInfos)
        {
            return string.Join(", ",
                externalUserInfos?
                    .Select(supplier => supplier
                        .SupplierInfo
                        .SupplierName
                    )?
                    .ToList()
                ?? new List<string>()
            );
        }

        private string GetTranslatedValueOrDefault(string key, string value = null)
        {
            return _maintenanceStatusTranslation.GetValueOrDefault(key, value ?? key);
        }
    }
}
