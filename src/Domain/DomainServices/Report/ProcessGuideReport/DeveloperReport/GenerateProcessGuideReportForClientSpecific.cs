using Microsoft.Extensions.Logging;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.ExcelReports;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.Reports;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.Reports.ProcessGuideReport.DeveloperReport;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using static Selise.Ecap.SC.PraxisMonitor.Contracts.Constants.ReportConstants;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.Report.ProcessGuideReport.DeveloperReport
{
    public class GenerateProcessGuideReportForClientSpecific : IProcessGuideReportGenerate
    {
        private readonly ILogger<GenerateProcessGuideReportForClientSpecific> _logger;
        private readonly IPraxisReportService _praxisReportService;
        private readonly IPraxisFormService _praxisFormService;
        private readonly IUilmResourceKeyService _uilmResourceKeyService;
        private Dictionary<string, string> _translatedStringsAsDictionary = new Dictionary<string, string>();

        public GenerateProcessGuideReportForClientSpecific(
            ILogger<GenerateProcessGuideReportForClientSpecific> logger,
            IPraxisFormService praxisFormService,
            IPraxisReportService praxisReportService,
            IUilmResourceKeyService uilmResourceKeyService)
        {
            _logger = logger;
            _praxisFormService = praxisFormService;
            _praxisReportService = praxisReportService;
            _uilmResourceKeyService = uilmResourceKeyService;
        }

        public async Task<bool> GenerateReport(ExcelPackage excel, ExportProcessGuideReportForDeveloperCommand command)
        {
            string reportDateString = DateTime.Today.ToString("dd.MM.yyyy");
            _translatedStringsAsDictionary = _uilmResourceKeyService
                    .GetResourceValueByKeyName(ProcessGuideDeveloperReport.TranslationKeys, command.LanguageKey);
            var worksheet = excel.Workbook.Worksheets.Add(command.ReportHeader);

            WriteHeader(worksheet, command.ReportHeader, reportDateString, command.ClientName);

            var dataset = await _praxisFormService.GetDeveloperReportData(command.FilterString, "{CreateDate: -1}");

            int page = 0, pageSize = 100, totalPage = (int)Math.Ceiling((decimal)dataset.TotalRecordCount / pageSize);

            if (totalPage != 0)
            {
                for (int i = 0; i < totalPage; i++)
                {
                    var results = dataset.Results.Skip(page * pageSize).Take(pageSize).ToList();

                    var reportDataList = GetReportData(results, command.LanguageKey);
                    WriteReportData(worksheet, reportDataList);
                    SetColumnSpecificStyle(worksheet);

                    page++;
                }
            }
            else
            {
                var results = dataset.Results.Skip(page * pageSize).Take(pageSize).ToList();
                var reportDataList = GetReportData(results, command.LanguageKey);
                WriteReportData(worksheet, reportDataList);
                SetColumnSpecificStyle(worksheet);
            }
            return true;
        }

        private void SetColumnSpecificStyle(ExcelWorksheet worksheet)
        {
            const int tableColumns = ProcessGuideDeveloperReport.ColumnsForSpecificClientReport;
            for (var i = 1; i <= tableColumns; i++)
            {
                var column = worksheet.Column(i);
                var headerCell = worksheet.Cells[ProcessGuideDeveloperReport.HeaderRowIndexForSpecificClientReport, i];
                column.AutoFit();
                column.Style.WrapText = true;
                headerCell.Style.Fill.PatternType = ExcelFillStyle.Solid;
                headerCell.Style.Font.Bold = true;
                headerCell.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                headerCell.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                headerCell.Style.Border.Right.Style = ExcelBorderStyle.Thin;
                headerCell.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                headerCell.Style.Fill.BackgroundColor.SetColor(ProcessGuideOverviewReport.HeaderBackground);
            }
            worksheet.Column(1).Width = 25;
            worksheet.Column(2).Width = 15;
            worksheet.Column(3).Width = 15;
            worksheet.Column(4).Width = 25;
            worksheet.Column(5).Width = 25;
            worksheet.Column(6).Width = 15;
            worksheet.Column(7).Width = 15;
        }

        private void WriteReportData(ExcelWorksheet workSheetTable, List<DeveloperForProcessGuideReport> reportDataList)
        {
            try
            {
                var rowIndex = ProcessGuideDeveloperReport.HeaderRowIndexForSpecificClientReport + 1;
                var clientIndex = 0;
                var taskDetailIndex = 0;
                foreach (var report in reportDataList)
                {
                    var totalRow = 1;
                    var taskDetails = report.AssignOrganizations.Select(a => a.TaskDescriptions);
                    foreach (var task in taskDetails)
                    {
                        totalRow += task.Count;
                    }

                    workSheetTable.Cells[rowIndex, 7].Style.Border.Right.Style = ExcelBorderStyle.Thin;

                    workSheetTable.Cells[rowIndex, 1].Value = report.Title;
                    workSheetTable.Cells[rowIndex, 1].Style.WrapText = true;
                    workSheetTable.Cells[rowIndex, 1].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    workSheetTable.SelectedRange[rowIndex, 1, rowIndex + totalRow - 1, 1].Merge = true;
                    workSheetTable.Cells[rowIndex, 1, rowIndex + totalRow - 1, 1].Style.Border.BorderAround(ExcelBorderStyle.Thin);

                    workSheetTable.Cells[rowIndex, 2].Value = report.CreatedOn;
                    workSheetTable.Cells[rowIndex, 2].Style.WrapText = true;
                    workSheetTable.Cells[rowIndex, 2].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    workSheetTable.SelectedRange[rowIndex, 2, rowIndex + totalRow - 1, 2].Merge = true;
                    workSheetTable.Cells[rowIndex, 2, rowIndex + totalRow - 1, 2].Style.Border.BorderAround(ExcelBorderStyle.Thin);

                    workSheetTable.Cells[rowIndex, 3].Value = report.Topic;
                    workSheetTable.Cells[rowIndex, 3].Style.WrapText = true;
                    workSheetTable.Cells[rowIndex, 3].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    workSheetTable.SelectedRange[rowIndex, 3, rowIndex + totalRow - 1, 3].Merge = true;
                    workSheetTable.Cells[rowIndex, 3, rowIndex + totalRow - 1, 3].Style.Border.BorderAround(ExcelBorderStyle.Thin);

                    clientIndex = rowIndex;
                    taskDetailIndex = rowIndex;
                    var totalTask = 0;
                    double totalBudget = 0;
                    foreach (var assignOrganization in report.AssignOrganizations)
                    {
                        totalTask = assignOrganization.TaskDescriptions.Count == 1 ? 1 : assignOrganization.TaskDescriptions.Count;

                        workSheetTable.Cells[clientIndex, 4].Value = assignOrganization.ClientName;
                        workSheetTable.Cells[clientIndex, 4].Style.WrapText = true;
                        workSheetTable.Cells[clientIndex, 4].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        if (totalTask == 1)
                        {
                            workSheetTable.Cells[clientIndex, 4].Style.Border.BorderAround(ExcelBorderStyle.Thin);
                        }
                        else
                        {
                            workSheetTable.SelectedRange[clientIndex, 4, clientIndex + totalTask -1, 4].Merge = true;
                            workSheetTable.Cells[clientIndex, 4, clientIndex + totalTask -1, 4].Style.Border.BorderAround(ExcelBorderStyle.Thin);
                        }

                        foreach (var taskDetail in assignOrganization.TaskDescriptions)
                        {
                            workSheetTable.Cells[taskDetailIndex, 5].Value = taskDetail.TaskTitle;
                            workSheetTable.Cells[taskDetailIndex, 5].Style.WrapText = true;
                            workSheetTable.Cells[taskDetailIndex, 5].Style.Border.BorderAround(ExcelBorderStyle.Thin);
                            workSheetTable.Cells[taskDetailIndex, 5].Style.VerticalAlignment = ExcelVerticalAlignment.Center;

                            workSheetTable.Cells[taskDetailIndex, 6].Value = taskDetail.Attachment;
                            workSheetTable.Cells[taskDetailIndex, 6].Style.WrapText = true;
                            workSheetTable.Cells[taskDetailIndex, 6].Style.Border.BorderAround(ExcelBorderStyle.Thin);
                            workSheetTable.Cells[taskDetailIndex, 6].Style.VerticalAlignment = ExcelVerticalAlignment.Center;

                            workSheetTable.Cells[taskDetailIndex, 7].Value = taskDetail.Budget;
                            workSheetTable.Cells[taskDetailIndex, 7].Style.WrapText = true;
                            workSheetTable.Cells[taskDetailIndex, 7].Style.Border.BorderAround(ExcelBorderStyle.Thin);
                            workSheetTable.Cells[taskDetailIndex, 7].Style.VerticalAlignment = ExcelVerticalAlignment.Center;

                            taskDetailIndex++;
                        }

                        totalBudget += assignOrganization.TaskDescriptions.Select(b => b.Budget).Sum();

                        workSheetTable.Cells[rowIndex + totalRow - 1, 4].Style.Border.BorderAround(ExcelBorderStyle.Thin);
                        workSheetTable.Cells[rowIndex + totalRow - 1, 5].Style.Border.BorderAround(ExcelBorderStyle.Thin);
                        workSheetTable.Cells[rowIndex + totalRow - 1, 6].Style.Border.BorderAround(ExcelBorderStyle.Thin);

                        workSheetTable.Cells[rowIndex + totalRow - 1, 7].Value = totalBudget;
                        workSheetTable.Cells[rowIndex + totalRow - 1, 7].Style.WrapText = true;
                        workSheetTable.Cells[rowIndex + totalRow - 1, 7].Style.Font.Bold = true;
                        workSheetTable.Cells[rowIndex + totalRow - 1, 7].Style.Border.BorderAround(ExcelBorderStyle.Thin);
                        workSheetTable.Cells[rowIndex + totalRow - 1, 7].Style.VerticalAlignment = ExcelVerticalAlignment.Center;

                        clientIndex += totalTask;
                    }
                    rowIndex += totalRow;
                    rowIndex++;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Exception occurred during writing report data to generate Excel report. Exception message: {Message}. Exception Details: {StackTrace}", ex.Message, ex.StackTrace);
            }
        }
        private List<DeveloperForProcessGuideReport> GetReportData(List<PraxisForm> praxisForms, string language)
        {
            var developerForProcessGuideReportList = new List<DeveloperForProcessGuideReport>();
            foreach (var form in praxisForms)
            {
                var developerForProcessGuideReport = new DeveloperForProcessGuideReport
                {
                    Title = form.Title,
                    CreatedOn = form.CreateDate.ToString("dd.MM.yyyy"),
                    Topic = _uilmResourceKeyService.GetResourceValueByKeyName(form.TopicValue, language),
                };

                var assignOrganizationList = new List<AssignOrganization>();
                foreach (var task in form.ProcessGuideCheckList)
                {
                    var taskDescriptionList = new List<TaskDescriptionDto>();
                    var assignOrganization = new AssignOrganization()
                    {
                        ClientName = task.ClientName
                    };

                    foreach (var processGuideTask in task.ProcessGuideTask)
                    {
                        var taskDescription = new TaskDescriptionDto()
                        {
                            TaskTitle = processGuideTask.ProcessGuideTaskTitle,
                            Budget = (double)processGuideTask.Budget
                    };
                        taskDescriptionList.Add(taskDescription);

                        var documentNames = processGuideTask.Files.Select(t => t.DocumentName).ToList();
                        if(documentNames.Any())
                        {
                            taskDescription.Attachment = string.Join("/n", documentNames);
                        }
                    }
                    assignOrganization.TaskDescriptions = taskDescriptionList;
                    assignOrganizationList.Add(assignOrganization);
                }
                developerForProcessGuideReport.AssignOrganizations = assignOrganizationList;

                developerForProcessGuideReportList.Add(developerForProcessGuideReport);
            }

            return developerForProcessGuideReportList;
        }
        private void WriteHeader(ExcelWorksheet worksheet, string reportName, string dateString, string clientName)
        {
            try
            {
                worksheet.Cells[1, 1].Value = GetTranslation("CLIENT_NAME");
                worksheet.Cells[1, 1].Style.Font.Bold = true;
                worksheet.Cells[1, 2].Value = clientName;
                worksheet.Cells["B1:C1"].Merge = true;
                worksheet.Cells["B1:C1"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                worksheet.Cells[2, 1].Value = GetTranslation("REPORT_NAME");
                worksheet.Cells[2, 1].Style.Font.Bold = true;
                worksheet.Cells[2, 2].Value = reportName;
                worksheet.Cells["B2:C2"].Merge = true;
                worksheet.Cells["B2:C2"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                worksheet.Cells[3, 1].Value = GetTranslation("DATE_FILTER");
                worksheet.Cells[3, 1].Style.Font.Bold = true;
                worksheet.Cells[3, 2].Value = dateString;
                worksheet.Cells["B3:C3"].Merge = true;
                worksheet.Cells["B3:C3"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                worksheet.Cells["A1:A3"].Style.Font.Bold = true;
                // [firstRow, firstColumn, lastRow, LastColumn]
                worksheet.Cells[1, 1, 3, 3].Style.Border.Top.Style = ExcelBorderStyle.Thin;
                worksheet.Cells[1, 1, 3, 3].Style.Border.Left.Style = ExcelBorderStyle.Thin;
                worksheet.Cells[1, 1, 3, 3].Style.Border.Right.Style = ExcelBorderStyle.Thin;
                worksheet.Cells[1, 1, 3, 3].Style.Border.Bottom.Style = ExcelBorderStyle.Thin;

                var columnIndex = 1;
                const int headerRowIndex = ProcessGuideDeveloperReport.HeaderRowIndexForSpecificClientReport;
                worksheet.Row(headerRowIndex).Style.Font.Bold = true;
                worksheet.Row(headerRowIndex).Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                worksheet.Row(headerRowIndex).Style.VerticalAlignment = ExcelVerticalAlignment.Center;

                worksheet.Cells[headerRowIndex, columnIndex++].Value = GetTranslation("TITLE");
                worksheet.Cells[headerRowIndex, columnIndex++].Value = GetTranslation("CREATED_ON");
                worksheet.Cells[headerRowIndex, columnIndex++].Value = GetTranslation("TOPIC");
                worksheet.Cells[headerRowIndex, columnIndex++].Value = GetTranslation("ASSIGNED_ORGANIZATION");
                worksheet.Cells[headerRowIndex, columnIndex++].Value = GetTranslation("TASK_DESCRIPTION");
                worksheet.Cells[headerRowIndex, columnIndex++].Value = GetTranslation("ATTACHMENTS_INSTRUCTIONS");
                worksheet.Cells[headerRowIndex, columnIndex].Value = GetTranslation("BUDGET");

                //AddHeaderLogo(worksheet);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Exception in WriteHeader. Exception message: {Message}. Exception Details: {StackTrace}", e.Message, e.StackTrace);
            }
        }

        private string GetTranslation(string key)
        {
            return _translatedStringsAsDictionary[$"APP_FORM.{key}"];
        }

        private void AddHeaderLogo(ExcelWorksheet worksheet)
        {
            const int logoStartColumn = ProcessGuideDeveloperReport.ColumnsForSpecificClientReport;
            // [firstRow, firstColumn, lastRow, LastColumn]
            worksheet.Cells[1, logoStartColumn, 2, logoStartColumn].Merge = true;
            _praxisReportService.AddLogoInExcelReport(worksheet, ProcessGuideDeveloperReport.LogoSize, logoStartColumn, rqLatestLogo);
        }
    }
}
