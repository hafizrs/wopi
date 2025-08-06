using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using Selise.Ecap.Entities.PrimaryEntities.PlatformDataService;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.ProcessGuide;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.ExcelReports;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.Reports;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.Reports.ProcessGuideReport;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using static Selise.Ecap.SC.PraxisMonitor.Contracts.Constants.ReportConstants;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants;
using Aspose.Pdf;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Queries;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.Report.ProcessGuideReport
{
    public class ProcessGuideCaseOverviewReport : IProcessGuideCaseOverviewReport
    {
        private readonly IBlocksMongoDbDataContextProvider _ecapMongoDbDataContextProvider;
        private readonly IUilmResourceKeyService _uilmResourceKeyService;
        private readonly IRepository _repository;
        private readonly ILogger<ProcessGuideCaseOverviewReport> _logger;
        private readonly IPraxisReportService _praxisReportService;
        private readonly IStorageDataService _storageDataService;
        private readonly ICommonUtilService _commonUtilService;
        private ExcelWorksheet _currentWorksheet;

        private Dictionary<string, string> _translatedStringsAsDictionary = new Dictionary<string, string>();

        public ProcessGuideCaseOverviewReport(
            IBlocksMongoDbDataContextProvider ecapMongoDbDataContextProvider,
            IUilmResourceKeyService uilmResourceKeyService,
            IRepository repository,
            ILogger<ProcessGuideCaseOverviewReport> logger,
            IPraxisReportService praxisReportService,
            IStorageDataService storageDataService,
            ICommonUtilService commonUtilService)
        {
            _ecapMongoDbDataContextProvider = ecapMongoDbDataContextProvider;
            _uilmResourceKeyService = uilmResourceKeyService;
            _repository = repository;
            _logger = logger;
            _praxisReportService = praxisReportService;
            _storageDataService = storageDataService;
            _commonUtilService = commonUtilService;
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        }

        public async Task<bool> ExportReport(ExportProcessGuideCaseOverviewReportCommand command)
        {
            try
            {
                using var excel = new ExcelPackage();
                var reportDateString = DateTime.Today.ToString("dd.MM.yyyy");
                _currentWorksheet = excel.Workbook.Worksheets.Add(command.ReportHeader);

                SetTranslation(command.LanguageKey);

                if (command.IsShiftPlan)
                {
                    WriteHeaderForShiftPlan(command.ReportHeader, reportDateString);
                }
                else
                {
                    WriteHeader(command.ReportHeader, reportDateString);
                }

                var isReportPrepared = await GenerateReport(command);
                if (isReportPrepared)
                {
                    var isSuccess = await _storageDataService.UploadFileAsync(command.ReportFileId, command.FileNameWithExtension,
                        excel.GetAsByteArray());

                    _logger.LogInformation("Process guide developer report uploaded to storage success -> {IsSuccess}", isSuccess);

                    return isSuccess;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Exception occured during exporting Process Guide overview report");
                _logger.LogError($"Exception message: {ex.Message}. Exception Details: {ex.StackTrace}.");
                return false;
            }

            _currentWorksheet = null;
            return true;
        }


        private async Task<bool> GenerateReport(ExportProcessGuideCaseOverviewReportCommand command)
        {
            try
            {
                var dataset = await _commonUtilService.GetEntityQueryResponse<PraxisProcessGuide>(command.FilterString, "{CreateDate: -1}");

                var clientIds = dataset.Results.SelectMany(pg => pg.Clients.Select(cl => cl.ClientId)).Distinct();
                await _praxisReportService.AddClientIdsToPraxisReport(command.ReportFileId, clientIds);
                
                int page = 0, pageSize = 100, totalPage = (int)Math.Ceiling((decimal)dataset.TotalRecordCount / pageSize);
                if (totalPage != 0)
                {
                    for (int i = 0; i < totalPage; i++)
                    {
                        var results = dataset.Results.Skip(page * pageSize).Take(pageSize).ToList();
                        var reportData = PrepareReportData(results, command.LanguageKey, command.TimezoneOffsetInMinutes ?? 0);
                        if (command.IsShiftPlan)
                        {
                            WriteProcessGuideOverviewShiftPlan(reportData);
                        }
                        else
                        {
                            WriteProcessGuideOverview(reportData);
                        }
                    }
                }
                else
                {
                    var results = dataset.Results.Skip(page * pageSize).Take(pageSize).ToList();
                    var reportData = PrepareReportData(results, command.LanguageKey, command.TimezoneOffsetInMinutes ?? 0);
                    if (command.IsShiftPlan)
                    {
                        WriteProcessGuideOverviewShiftPlan(reportData);
                    }
                    else
                    {
                        WriteProcessGuideOverview(reportData);
                    }
                }
                SetColumnSpecificStyle(command.IsShiftPlan);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Exception occured during generate report for process guide overview. Exception message: {ex.Message}. Exception Details: {ex.StackTrace}.");
                return false;
            }
            return true;
        }

        private void WriteHeader(string reportName, string dateString)
        {
            var worksheet = _currentWorksheet;
            try
            {
                worksheet.Cells[1, 1].Value = GetTranslation("REPORT_NAME");
                worksheet.Cells[1, 1].Style.Font.Bold = true;
                worksheet.Cells[1, 2].Value = reportName;
                worksheet.Cells["B1:C1"].Merge = true;
                worksheet.Cells["B1:C1"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                worksheet.Cells["B1:C1"].Style.VerticalAlignment = ExcelVerticalAlignment.Center;

                worksheet.Cells[2, 1].Value = GetTranslation("DATE_FILTER");
                worksheet.Cells[2, 1].Style.Font.Bold = true;
                worksheet.Cells[2, 2].Value = dateString;
                worksheet.Cells["B2:C2"].Merge = true;
                worksheet.Cells["B2:C2"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                worksheet.Cells["B2:C2"].Style.VerticalAlignment = ExcelVerticalAlignment.Center;

                worksheet.Cells["A1:A3"].Style.Font.Bold = true;
                // [firstRow, firstColumn, lastRow, LastColumn]
                worksheet.Cells[1, 1, 3, 3].Style.Border.Top.Style = ExcelBorderStyle.Thin;
                worksheet.Cells[1, 1, 3, 3].Style.Border.Left.Style = ExcelBorderStyle.Thin;
                worksheet.Cells[1, 1, 3, 3].Style.Border.Right.Style = ExcelBorderStyle.Thin;
                worksheet.Cells[1, 1, 3, 3].Style.Border.Bottom.Style = ExcelBorderStyle.Thin;

                var columnIndex = 1;
                const int headerRowIndex = ProcessGuideOverviewReport.HeaderRowIndexForAllDataReport;
                worksheet.Row(headerRowIndex).Style.Font.Bold = true;
                worksheet.Row(headerRowIndex).Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                worksheet.Row(headerRowIndex).Style.VerticalAlignment = ExcelVerticalAlignment.Center;

                worksheet.Cells[headerRowIndex, columnIndex++].Value = GetTranslation("FORM_TITLE");
                worksheet.Cells[headerRowIndex, columnIndex++].Value = GetTranslation("TITLE"); 
                worksheet.Cells[headerRowIndex, columnIndex++].Value = GetTranslation("TOPIC");
                worksheet.Cells[headerRowIndex, columnIndex++].Value = GetTranslation("BIRTHDAY");
                worksheet.Cells[headerRowIndex, columnIndex++].Value = GetTranslation("PATIENT_NAME");
                worksheet.Cells[headerRowIndex, columnIndex++].Value = GetTranslation("CASE_ID");
                worksheet.Cells[headerRowIndex, columnIndex++].Value = GetTranslation("ASSIGNED_ON");
                worksheet.Cells[headerRowIndex, columnIndex++].Value = GetTranslation("ASSIGNED_ORGANIZATION");
                worksheet.Cells[headerRowIndex, columnIndex++].Value = GetTranslation("ASSIGNED_USERS");
                worksheet.Cells[headerRowIndex, columnIndex++].Value = GetTranslation("COMPLETED_BY_USER");
                worksheet.Cells[headerRowIndex, columnIndex++].Value = GetTranslation("DUE_DATE");
                worksheet.Cells[headerRowIndex, columnIndex++].Value = GetTranslation("COMPLETION_STATUS");
                worksheet.Cells[headerRowIndex, columnIndex++].Value = GetTranslation("OVERALL_COMPLETION");
                worksheet.Cells[headerRowIndex, columnIndex++].Value = GetTranslation("DATE_OF_COMPLETION");
                worksheet.Cells[headerRowIndex, columnIndex++].Value = GetTranslation("BUDGET");
                worksheet.Cells[headerRowIndex, columnIndex++].Value = GetTranslation("EFFECTIVE_COST");
                worksheet.Cells[headerRowIndex, columnIndex].Value = GetTranslation("STATUS");

                //AddHeaderLogo(worksheet);
            }
            catch (Exception e)
            {
                 _logger.LogError(e,"Exception in WriteHeader Error message: {Message}. Full stacktrace: {StackTrace}", e.Message, e.StackTrace);
               
            }
        }


        private void WriteHeaderForShiftPlan(string reportName, string dateString)
        {
            var worksheet = _currentWorksheet;
            try
            {
                worksheet.Cells[1, 1].Value = GetTranslation("REPORT_NAME");
                worksheet.Cells[1, 1].Style.Font.Bold = true;
                worksheet.Cells[1, 2].Value = reportName;
                worksheet.Cells["B1:C1"].Merge = true;
                worksheet.Cells["B1:C1"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                worksheet.Cells["B1:C1"].Style.VerticalAlignment = ExcelVerticalAlignment.Center;

                worksheet.Cells[2, 1].Value = GetTranslation("DATE_FILTER");
                worksheet.Cells[2, 1].Style.Font.Bold = true;
                worksheet.Cells[2, 2].Value = dateString;
                worksheet.Cells["B2:C2"].Merge = true;
                worksheet.Cells["B2:C2"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                worksheet.Cells["B2:C2"].Style.VerticalAlignment = ExcelVerticalAlignment.Center;

                worksheet.Cells["A1:A3"].Style.Font.Bold = true;
                // [firstRow, firstColumn, lastRow, LastColumn]
                worksheet.Cells[1, 1, 3, 3].Style.Border.Top.Style = ExcelBorderStyle.Thin;
                worksheet.Cells[1, 1, 3, 3].Style.Border.Left.Style = ExcelBorderStyle.Thin;
                worksheet.Cells[1, 1, 3, 3].Style.Border.Right.Style = ExcelBorderStyle.Thin;
                worksheet.Cells[1, 1, 3, 3].Style.Border.Bottom.Style = ExcelBorderStyle.Thin;

                var columnIndex = 1;
                const int headerRowIndex = ProcessGuideOverviewReport.HeaderRowIndexForAllDataReport;
                worksheet.Row(headerRowIndex).Style.Font.Bold = true;
                worksheet.Row(headerRowIndex).Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                worksheet.Row(headerRowIndex).Style.VerticalAlignment = ExcelVerticalAlignment.Center;

                worksheet.Cells[headerRowIndex, columnIndex++].Value = GetTranslation("TITLE");
                worksheet.Cells[headerRowIndex, columnIndex++].Value = GetTranslation("SHIFT_NAME_TITLE");
                worksheet.Cells[headerRowIndex, columnIndex++].Value = GetTranslation("ASSIGNED_ORGANIZATION");
                worksheet.Cells[headerRowIndex, columnIndex++].Value = GetTranslation("COMPLETED_BY_USER");
                worksheet.Cells[headerRowIndex, columnIndex++].Value = GetTranslation("COMPLETION_STATUS");
                worksheet.Cells[headerRowIndex, columnIndex].Value = GetTranslation("STATUS");

                //AddHeaderLogo(worksheet);
            }
            catch (Exception e)
            {
                _logger.LogInformation("Exception in WriteHeader, {Exception}", e);
            }
        }

        private void SetBorderAndSetValue(
            object value,
            int startRow,
            int startColumn,
            int endRow = 1,
            int endColumn = 1
        )
        {
            var worksheet = _currentWorksheet;
            try
            {
                endRow = Math.Max(startRow, endRow);
                endColumn = Math.Max(startColumn, endColumn);
                worksheet.Cells[startRow, startColumn].Value = value;
                worksheet.Cells[startRow, startColumn].Style.WrapText = true;
                worksheet.Cells[startRow, startColumn].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                worksheet.Cells[startRow, startColumn, endRow, endColumn].Merge = true;
            }
            catch (Exception)
            {
                _logger.LogError(
                    "Error occured while setting value: `{0}` to Cell with coordinate [{1}, {2}, {3}, {4}]",
                    value,
                    startRow,
                    startColumn,
                    endRow,
                    endColumn
                );
            }
        }

        private void WriteProcessGuideOverview(
            List<ProcessGuideCaseOverviewReportModel> processGuideDataList
        )
        {
            ExcelWorksheet workSheetTable = _currentWorksheet;
            var rowIndex = ProcessGuideOverviewReport.HeaderRowIndexForAllDataReport + 1;
            var clock = new Stopwatch();
            clock.Start();
            const int completionColumnIndex = 13; //13
            const int completionDateColumnIndex = 14; // 14
            const int statusColumnIndex = 17; // 17
            foreach (var processGuide in processGuideDataList)
            {
                var startRow = rowIndex;
                _logger.LogInformation($"Writing details for {processGuide.Title}");
                try
                {
                    processGuide.TaskDetails ??= new List<TaskAssignDetails>();

                    var totalRow = processGuide.TaskDetails.Count + 1;
                    var currentColumn = 1;
                    SetBorderAndSetValue(processGuide.FormTitle, rowIndex, currentColumn++, rowIndex + totalRow - 1);
                    SetBorderAndSetValue(processGuide.FormDescription, rowIndex, currentColumn++, rowIndex + totalRow - 1);
                    SetBorderAndSetValue(processGuide.Topic, rowIndex, currentColumn++, rowIndex + totalRow - 1);
                    SetBorderAndSetValue(processGuide.BirthDay, rowIndex, currentColumn++, rowIndex + totalRow - 1);
                    SetBorderAndSetValue(processGuide.Name, rowIndex, currentColumn++, rowIndex + totalRow - 1); // case id 1
                    SetBorderAndSetValue(processGuide.CaseNo, rowIndex, currentColumn++, rowIndex + totalRow - 1); // case id 2
                    SetBorderAndSetValue(processGuide.AssignedOn, rowIndex, currentColumn++, rowIndex + totalRow - 1);

                    var index = rowIndex;
                    foreach (var taskAssign in processGuide.TaskDetails)
                    {
                        var taskDetailsStartColumn = currentColumn;
                        var columnIndex = taskDetailsStartColumn;
                        workSheetTable.Cells[index, columnIndex++].Value = taskAssign.ClientName;
                        //workSheetTable.Cells[index, columnIndex++].Value = taskAssign.CategoryName;
                        //workSheetTable.Cells[index, columnIndex++].Value = taskAssign.SubCategoryName;
                        workSheetTable.Cells[index, columnIndex++].Value = taskAssign.AssignedUsers;
                        workSheetTable.Cells[index, columnIndex++].Value = taskAssign.CompletedUsers;
                        workSheetTable.Cells[index, columnIndex++].Value = taskAssign.DueDate;
                        workSheetTable.Cells[index, columnIndex++].Value = taskAssign.CompletionStatus;
                        columnIndex += 2;
                        workSheetTable.Cells[index, columnIndex++].Value = taskAssign.Budget;
                        workSheetTable.Cells[index, columnIndex].Value = taskAssign.EffectiveCost;

                        for (var i = taskDetailsStartColumn; i <= columnIndex; i++)
                        {
                            if (i == completionColumnIndex || i == completionDateColumnIndex) continue;
                            workSheetTable.Cells[index, i].Style.WrapText = true;
                            workSheetTable.Cells[index, i].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        }

                        index++;
                    }

                    SetBorderAndSetValue((int)processGuide.TaskDetails.Select(t => t.CompletionStatus).Average(), rowIndex, completionColumnIndex, rowIndex + processGuide.TaskDetails.Count - 1, completionColumnIndex);

                    var dateOfCompletion = processGuide.TaskDetails.Select(t => t.DateOfCompletion).FirstOrDefault();
                    SetBorderAndSetValue(dateOfCompletion, rowIndex, completionDateColumnIndex, rowIndex + processGuide.TaskDetails.Count - 1, completionDateColumnIndex);

                    workSheetTable.Cells[rowIndex, statusColumnIndex].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    SetBorderAndSetValue(processGuide.Status, rowIndex, statusColumnIndex, rowIndex + totalRow - 1, statusColumnIndex);

                    rowIndex += totalRow;

                    var totalBudget = processGuide.TaskDetails.Select(t => t.Budget).Sum();
                    var totalEffectiveCost = processGuide.TaskDetails.Select(t => t.EffectiveCost).Sum();

                    var totalBudgetCell = workSheetTable.Cells[rowIndex - 1, 15];
                    totalBudgetCell.Value = totalBudget;
                    totalBudgetCell.Style.WrapText = true;
                    totalBudgetCell.Style.Font.Bold = true;
                    totalBudgetCell.Style.VerticalAlignment = ExcelVerticalAlignment.Center;

                    var totalEffectiveCostCell = workSheetTable.Cells[rowIndex - 1, 16];
                    totalEffectiveCostCell.Value = totalEffectiveCost;
                    totalEffectiveCostCell.Style.WrapText = true;
                    totalEffectiveCostCell.Style.Font.Bold = true;
                    totalEffectiveCostCell.Style.VerticalAlignment = ExcelVerticalAlignment.Center;

                    var pgCellRangeBorder = workSheetTable.Cells[
                            startRow,
                            1,
                            rowIndex - 1,
                            ProcessGuideOverviewReport.ColumnsForOverviewReport
                        ]
                        .Style.Border;
                    pgCellRangeBorder.Top.Style = ExcelBorderStyle.Thin;
                    pgCellRangeBorder.Right.Style = ExcelBorderStyle.Thin;
                    pgCellRangeBorder.Bottom.Style = ExcelBorderStyle.Thin;
                    pgCellRangeBorder.Left.Style = ExcelBorderStyle.Thin;
                    rowIndex++;
                }
                catch (Exception ex)
                {
                    _logger.LogError("Exception occured during writing report data to generate excel report. ");
                    _logger.LogError($"Exception message: {ex.Message}. Exception Details: {ex.StackTrace}.");
                }
            }
            clock.Stop();
            _logger.LogInformation($"Time Elapsed: {clock.Elapsed.TotalMilliseconds}ms");
        }

        private void WriteProcessGuideOverviewShiftPlan(
            List<ProcessGuideCaseOverviewReportModel> processGuideDataList
        )
        {
            ExcelWorksheet workSheetTable = _currentWorksheet;
            var rowIndex = ProcessGuideOverviewReport.HeaderRowIndexForAllDataReport + 1;
            var clock = new Stopwatch();
            clock.Start();
            const int statusColumnIndex = 6;
            foreach (var processGuide in processGuideDataList)
            {
                var startRow = rowIndex;
                _logger.LogInformation("Writing details for {ProcessGuideTitle}", processGuide.Title);
                try
                {
                    processGuide.TaskDetails ??= new List<TaskAssignDetails>();
                    
                    var totalRow = processGuide.TaskDetails.Count + 1;
                    var currentColumn = 1;
                    SetBorderAndSetValue(processGuide.Title, rowIndex, currentColumn++, rowIndex + totalRow - 1);
                    SetBorderAndSetValue(processGuide.Shifts, rowIndex, currentColumn++, rowIndex + totalRow - 1);
                    
                    var index = rowIndex;
                    foreach (var taskAssign in processGuide.TaskDetails)
                    {
                        var taskDetailsStartColumn = currentColumn;
                        var columnIndex = taskDetailsStartColumn;
                        workSheetTable.Cells[index, columnIndex++].Value = taskAssign.ClientName;
                        workSheetTable.Cells[index, columnIndex++].Value = taskAssign.CompletedUsers;
                        workSheetTable.Cells[index, columnIndex++].Value = taskAssign.CompletionStatus;
                        index++;
                    }

                   
                    workSheetTable.Cells[rowIndex, statusColumnIndex].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    SetBorderAndSetValue(processGuide.Status,rowIndex, statusColumnIndex, rowIndex + totalRow - 1, statusColumnIndex);

                    rowIndex += totalRow;

                    var pgCellRangeBorder = workSheetTable.Cells[
                            startRow,
                            1,
                            rowIndex - 1,
                            ProcessGuideOverviewReport.ColumnsForOverviewShiftPlanReport
                        ]
                        .Style.Border;
                    pgCellRangeBorder.Top.Style = ExcelBorderStyle.Thin;
                    pgCellRangeBorder.Right.Style = ExcelBorderStyle.Thin;
                    pgCellRangeBorder.Bottom.Style = ExcelBorderStyle.Thin;
                    pgCellRangeBorder.Left.Style = ExcelBorderStyle.Thin;
                    rowIndex++;
                }
                catch (Exception ex)
                {
                    _logger.LogError("Exception occured during writing report data to generate excel report. ");
                    _logger.LogError($"Exception message: {ex.Message}. Exception Details: {ex.StackTrace}.");
                }
            }
            clock.Stop();
            _logger.LogInformation("Time Elapsed: {ElapsedMilliseconds}ms", clock.Elapsed.TotalMilliseconds);
        }

        private void SetColumnSpecificStyle(bool isShiftPlan)
        {
            ExcelWorksheet worksheet = _currentWorksheet;
            int tableColumns = isShiftPlan ? ProcessGuideOverviewReport.ColumnsForOverviewShiftPlanReport : ProcessGuideOverviewReport.ColumnsForOverviewReport;
            for (var i = 1; i <= tableColumns; i++)
            {
                var column = worksheet.Column(i);
                var headerCell = worksheet.Cells[ProcessGuideOverviewReport.HeaderRowIndexForAllDataReport, i];
                column.AutoFit();
                column.Style.WrapText = true;
                headerCell.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                headerCell.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                headerCell.Style.Border.Right.Style = ExcelBorderStyle.Thin;
                headerCell.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                headerCell.Style.Fill.PatternType = ExcelFillStyle.Solid;
                headerCell.Style.Font.Bold = true;
                headerCell.Style.Fill.BackgroundColor.SetColor(ProcessGuideOverviewReport.HeaderBackground);
            }

            var columIndex = 1;
            worksheet.Column(columIndex++).Width = 25;
            worksheet.Column(columIndex++).Width = 15;
            worksheet.Column(columIndex++).Width = 15;
            worksheet.Column(columIndex++).Width = 15;
            worksheet.Column(columIndex++).Width = 15;
            worksheet.Column(columIndex++).Width = 15;
            worksheet.Column(columIndex++).Width = 15;
            worksheet.Column(columIndex++).Width = 25;
            worksheet.Column(columIndex++).Width = 15;
            worksheet.Column(columIndex++).Width = 15;
            worksheet.Column(columIndex++).Width = 30;
            worksheet.Column(columIndex++).Width = 30;
            worksheet.Column(columIndex++).Width = 15;
            worksheet.Column(columIndex++).Width = 15;
            worksheet.Column(columIndex++).Width = 15;
            worksheet.Column(columIndex++).Width = 20;
            worksheet.Column(columIndex++).Width = 15;
            worksheet.Column(columIndex++).Width = 15;
            worksheet.Column(columIndex).Width = 20;
            
            worksheet.Column(14).Style.Numberformat.Format = "0\\%";
            worksheet.Column(15).Style.Numberformat.Format = "0\\%";
        }


        private string GetShiftName(PraxisProcessGuide processGuide)
        {
            var shiftNames = processGuide?.Shifts?.Select(x => x.Name) ?? new List<string>();
            return shiftNames.Any() ?  string.Join(", ", shiftNames) : string.Empty;

        }
        private List<ProcessGuideCaseOverviewReportModel> PrepareReportData(
            List<PraxisProcessGuide> praxisProcessGuideList,
            string language,
            int timezoneOffsetInMinutes
        )
        {
            var reportDataList = new List<ProcessGuideCaseOverviewReportModel>();

            var processGuideAnswerRepo = _ecapMongoDbDataContextProvider.GetTenantDataContext()
                .GetCollection<PraxisProcessGuideAnswer>("PraxisProcessGuideAnswers");
            foreach (var processGuide in praxisProcessGuideList)
            {
                try
                {

                    var reportData = new ProcessGuideCaseOverviewReportModel();
                    var taskDetailList = new List<TaskAssignDetails>();

                    reportData.Title = processGuide.Title;
                    reportData.CaseNo = processGuide.PatientId;
                    reportData.BirthDay = processGuide.PatientDateOfBirth.ToString("dd.MMM.yyyy");
                    reportData.Name = processGuide.PatientName;
                    reportData.AssignedOn = processGuide.CreateDate.ToString("dd.MMM.yyyy");
                    reportData.Status = _uilmResourceKeyService.GetResourceValueByKeyName(
                        processGuide.IsActive ? "ACTIVE" : "INACTIVE",
                        language
                    );
                    reportData.Topic = GetTopicTranslation(processGuide.TopicValue);

                    reportData.Shifts = GetShiftName(processGuide);
                    // var praxisForm = _repository.GetItem<PraxisForm>(f => f.ItemId == processGuide.FormId);
                    var praxisForm = _repository.GetItem<AssignedTaskForm>
                                        (p => p.AssignedEntityId == processGuide.ItemId
                                        && p.ClonedFormId == processGuide.FormId
                                        && p.AssignedEntityName == nameof(PraxisProcessGuide));
                    if (praxisForm != null)
                    {
                        reportData.FormTitle = praxisForm.Title;
                        reportData.FormDescription = praxisForm.Description;
                        foreach (var client in processGuide.Clients)
                        {
                            var taskAssignDetails = new TaskAssignDetails
                            {
                                ClientName = client.ClientName,
                                CategoryName = client.CategoryName,
                                SubCategoryName = client.SubCategoryName
                            };

                            var assignMembers = GetAssignUserName(client.ControlledMembers.ToList(), client.ClientId);
                            if (assignMembers.Any())
                            {
                                taskAssignDetails.AssignedUsers = string.Join("\n", assignMembers);
                            }

                            var completedUserList = GetCompletedUserName(
                                processGuide.ItemId,
                                client.ControlledMembers.ToList(),
                                client.ClientId
                            );
                            if (completedUserList.Any())
                            {
                                taskAssignDetails.CompletedUsers = string.Join("\n", completedUserList);
                            }

                            taskAssignDetails.DueDate = processGuide.DueDate != null
                                ? processGuide.DueDate.Value.ToString("dd.MMM.yyyy")
                                : processGuide.PatientDateOfBirth.ToString("dd.MMM.yyyy");

                            taskAssignDetails.CompletionStatus = processGuide.ClientCompletionInfo?
                                .Where(c => c.ClientId == client.ClientId)?
                                .Select(c => c.CompletionPercentage)?
                                .FirstOrDefault() ?? 0;

                            var answerFilter = Builders<PraxisProcessGuideAnswer>.Filter.In("SubmittedBy",client.ControlledMembers.ToArray()) &
                                               Builders<PraxisProcessGuideAnswer>.Filter.Eq("ProcessGuideId",processGuide.ItemId) &
                                               Builders<PraxisProcessGuideAnswer>.Filter.Eq("ClientId", client.ClientId) &
                                               Builders<PraxisProcessGuideAnswer>.Filter.Eq("IsMarkedToDelete", false);
                            var answerList = processGuideAnswerRepo.Find(answerFilter)
                                .Sort(BsonDocument.Parse("{CreateDate: -1}"))
                                .ToList();
                            if (answerList.Any())
                            {
                                taskAssignDetails.EffectiveCost = answerList.Sum(
                                    answer => answer.Answers.Select(a => a.ActualBudget ?? 0).Sum()
                                );
                            }

                            taskAssignDetails.DateOfCompletion = processGuide.CompletionDate.HasValue
                                ? processGuide.CompletionDate.Value.AddMinutes(timezoneOffsetInMinutes)
                                    .ToString("dd.MMM.yyyy, h:mm tt")
                                : "n/a";

                            var processGuideTaskList = praxisForm?.ProcessGuideCheckList?
                                .First(p => p.ClientId == client.ClientId || (p.ClientInfos != null && p.ClientInfos.Any(c => c.ClientId == client.ClientId)))
                                ?.ProcessGuideTask ?? new List<ProcessGuideTask>();
                            taskAssignDetails.Budget = processGuideTaskList.Select(s => s.Budget ?? 0).Sum();
                            taskDetailList.Add(taskAssignDetails);
                        }
                    }
                    if (taskDetailList.Any())
                    {
                        reportData.TaskDetails = taskDetailList;
                    }

                    reportDataList.Add(reportData);
                }
                catch (Exception e)
                {
                    _logger.LogError(
                        "Error occured while trying to generate report data for process guide " +
                        $"with ItemId: {processGuide.ItemId}({processGuide.Title})"
                    );
                    _logger.LogError($"Error message {e.Message}\nFull StackTrace: {e.StackTrace}");
                }
            }
            return reportDataList;
        }

        private void AddHeaderLogo(ExcelWorksheet worksheet)
        {
            const int logoStartColumn = ProcessGuideOverviewReport.ColumnsForOverviewReport;
            // [firstRow, firstColumn, lastRow, LastColumn]
            worksheet.Cells[1, logoStartColumn, 2, logoStartColumn].Merge = true;
            _praxisReportService.AddLogoInExcelReport(worksheet, PraxisUserListReport.LogoSize, logoStartColumn, rqLatestLogo);
        }

        private List<string> GetCompletedUserName(string processGuideId, List<string> submittedBy, string clientId)
        {
            var completedUserList = new List<string>();
            var processGuideAnswerRepo = _ecapMongoDbDataContextProvider.GetTenantDataContext().GetCollection<PraxisProcessGuideAnswer>("PraxisProcessGuideAnswers");
            var personRepo = _ecapMongoDbDataContextProvider.GetTenantDataContext().GetCollection<Person>("Persons");

            var ansList = new List<PraxisProcessGuideAnswer>();
            if (submittedBy.Any())
            {
                var filter = Builders<PraxisProcessGuideAnswer>.Filter.In("SubmittedBy", submittedBy.ToArray()) &
                             Builders<PraxisProcessGuideAnswer>.Filter.Eq("ProcessGuideId", processGuideId) &
                             Builders<PraxisProcessGuideAnswer>.Filter.Eq("IsMarkedToDelete", false);
                ansList = processGuideAnswerRepo.Find(filter).ToList();
            }
            if (ansList.Any())
            {
                var answerIds = ansList.Select(a => a.ItemId).ToList();
                var ansFilter = Builders<PraxisProcessGuideAnswer>.Filter.Eq("ProcessGuideId", processGuideId) &
                    Builders<PraxisProcessGuideAnswer>.Filter.Eq("ClientId", clientId) &
                    Builders<PraxisProcessGuideAnswer>.Filter.Nin("_id", answerIds.ToArray()) &
                    Builders<PraxisProcessGuideAnswer>.Filter.Eq("IsMarkedToDelete", false);
                var deletedAnswerList = processGuideAnswerRepo.Find(ansFilter).ToList();
                if (deletedAnswerList.Any())
                {
                    ansList.AddRange(deletedAnswerList);
                }
            }
            else
            {
                var ansFilter = Builders<PraxisProcessGuideAnswer>.Filter.Eq("ProcessGuideId", processGuideId) &
                    Builders<PraxisProcessGuideAnswer>.Filter.Eq("ClientId", clientId) &
                    Builders<PraxisProcessGuideAnswer>.Filter.Eq("IsMarkedToDelete", false);
                var deletedAnswerList = processGuideAnswerRepo.Find(ansFilter).ToList();
                if (deletedAnswerList.Any())
                {
                    ansList.AddRange(deletedAnswerList);
                }
            }
            if (ansList.Any())
            {
                var completedPersonIds = ansList.Select(a => a.SubmittedBy).ToList();
                var personFilter = Builders<Person>.Filter.In("_id", completedPersonIds.ToArray()) & Builders<Person>.Filter.Eq("IsMarkedToDelete", false);
                var assignMembers = personRepo.Find(personFilter).ToList();
                var assignmemberNames = assignMembers.Select(a => a.DisplayName).ToList();
                completedUserList.AddRange(assignmemberNames);
            }
            return completedUserList;
        }
        private List<string> GetAssignUserName(List<string> assignUsers, string clientId)
        {
            var praxisUserRepo = _ecapMongoDbDataContextProvider.GetTenantDataContext().GetCollection<PraxisUser>("PraxisUsers");
            List<string> assignUserList;
            if (assignUsers.Any())
            {
                var filter = Builders<PraxisUser>.Filter.In("_id", assignUsers.ToArray()) & Builders<PraxisUser>.Filter.Eq("IsMarkedToDelete", false) & Builders<PraxisUser>.Filter.Eq("Active", true);
                var assignMembers = praxisUserRepo.Find(filter).ToList();
                assignUserList = assignMembers.Select(a => a.DisplayName).ToList();
            }
            else
            {
                var filter = Builders<PraxisUser>.Filter.Eq("ClientList.ClientId", clientId) & 
                            Builders<PraxisUser>.Filter.Eq("IsMarkedToDelete", false) &
                            Builders<PraxisUser>.Filter.Not(Builders<PraxisUser>.Filter.AnyEq("Roles", RoleNames.GroupAdmin)) &
                            Builders<PraxisUser>.Filter.Eq("Active", true);
                var assignMembers = praxisUserRepo.Find(filter).ToList();
                assignUserList = assignMembers.Where(a=> !a.IsMarkedToDelete).Select(a => a.DisplayName).ToList();
            }
            return assignUserList;
        }
        private string GetTranslation(string key)
        {
            return _translatedStringsAsDictionary[$"APP_PROCESS_GUIDE.{key}"];
        }
        


        private void SetTranslation(string languageKey)
        {
            _translatedStringsAsDictionary = _uilmResourceKeyService.GetResourceValueByKeyName(
                ProcessGuideOverviewReport.TranslationKeys,
                languageKey
            );
            
            _translatedStringsAsDictionary["APP_PROCESS_GUIDE.FORM_TITLE"] =
                _uilmResourceKeyService.GetResourceValueByKeyName("APP_FORM.TITLE", languageKey);
            
            _translatedStringsAsDictionary["APP_PROCESS_GUIDE.FORM_DESCRIPTION"] =
                _uilmResourceKeyService.GetResourceValueByKeyName("APP_FORM.FORM_DESCRIPTION", languageKey);

            _translatedStringsAsDictionary["APP_PROCESS_GUIDE.SHIFT_NAME_TITLE"] =
               _uilmResourceKeyService.GetResourceValueByKeyName("APP_SHIFT_PLANNER.SHIFT_NAME", languageKey);

            foreach (var key in TopicTranslationKeys)
            {
                _translatedStringsAsDictionary[key] = _uilmResourceKeyService.GetResourceValueByKeyName(key, languageKey);
            }
        }

        private string GetTopicTranslation(string key)
        {
            return _translatedStringsAsDictionary.ContainsKey(key) ? _translatedStringsAsDictionary[key] : key;
        }
    }
}
