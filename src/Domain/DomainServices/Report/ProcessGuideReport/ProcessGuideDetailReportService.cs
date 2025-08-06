using Microsoft.Extensions.Logging;
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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.Shared;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Infrastructure;
using static Selise.Ecap.SC.PraxisMonitor.Contracts.Constants.ReportConstants;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.Report.ProcessGuideReport
{
    public class ProcessGuideDetailReportService : IProcessGuideDetailReport
    {
        private readonly ILogger<ProcessGuideDetailReportService> _logger;
        private readonly IUilmResourceKeyService _uilmResourceKeyService;
        private readonly IPraxisReportService _praxisReportService;
        private readonly IRepository _repository;
        private readonly IBlocksMongoDbDataContextProvider _ecapMongoDbDataContextProvider;
        private readonly IStorageDataService _storageDataService;
        private Dictionary<string, string> _translatedStringsAsDictionary = new Dictionary<string, string>();
        private ExcelWorksheet _currentWorksheet;
        private readonly ISecurityContextProvider _securityContextProvider;
        private readonly ISecurityHelperService _securityHelperService;
        public ProcessGuideDetailReportService(
            ILogger<ProcessGuideDetailReportService> logger,
            IUilmResourceKeyService uilmResourceKeyService,
            IPraxisReportService praxisReportService,
            IRepository repository,
            IBlocksMongoDbDataContextProvider ecapMongoDbDataContextProvider,
            IStorageDataService storageDataService,
            ISecurityContextProvider securityContextProvider,
            ISecurityHelperService securityHelperService)
        {
            _logger = logger;
            _uilmResourceKeyService = uilmResourceKeyService;
            _praxisReportService = praxisReportService;
            _repository = repository;
            _ecapMongoDbDataContextProvider = ecapMongoDbDataContextProvider;
            _storageDataService = storageDataService;
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            _securityContextProvider = securityContextProvider;
            _securityHelperService = securityHelperService;
        }

        public async Task<bool> ExportReport(ExportProcessGuideDetailReportCommand command)
        {
            try
            {
                using (ExcelPackage excel = new ExcelPackage())
                {
                    var isReportPrepared = await GenerateReport(excel, command);
                    if (isReportPrepared)
                    {
                        var isSuccess = await _storageDataService.UploadFileAsync(command.ReportFileId.ToString(), command.FileNameWithExtension,
                            excel.GetAsByteArray());

                        _logger.LogInformation("ExportTaskListReport upload to storage success -> {IsSuccess}", isSuccess);

                        return isSuccess;
                    }
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception occurred during export process guide details report. Exception Message: {Message}. Exception Details: {StackTrace}.", ex.Message, ex.StackTrace);
                return false;
            }
        }

        private async Task<bool> GenerateReport(ExcelPackage excel, ExportProcessGuideDetailReportCommand command)
        {
            try
            {
                string reportDateString = DateTime.Today.ToString("dd.MM.yyyy");
                _currentWorksheet = excel.Workbook.Worksheets.Add(command.ReportHeader);

                SetTranslation(command.LanguageKey);
                WriteHeader(command.ReportHeader, reportDateString, command.ClientName);

                var reportData = PrepareReportData(command.ProcessGuideId, command.LanguageKey, command.TimezoneOffsetInMinutes ?? 0);

                var clientIds = reportData.TaskDescriptions.Select(task => task.ClientId).Distinct();
                await _praxisReportService.AddClientIdsToPraxisReport(command.ReportFileId, clientIds);

                var endRow = WriteProcessGuideDetails(reportData);
                SetColumnSpecificStyle(endRow);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception occured during generate process guide details report. Exception Message: {Message}. Exception Details: {StackTrace}.", ex.Message, ex.StackTrace);
                return false;
            }

            return true;
        }

        private ProcessGuideDetailReport PrepareReportData(string processGuideId, string language, int timezoneOffsetInMinutes)
        {
            try
            {
                var reportBody = new ProcessGuideDetailReport();
                var taskDetails = new List<TaskDescription>();

                var processGuideAnswerRepo = _ecapMongoDbDataContextProvider.GetTenantDataContext()
                    .GetCollection<PraxisProcessGuideAnswer>("PraxisProcessGuideAnswers");
                var processGuide = _repository.GetItem<PraxisProcessGuide>(p => p.ItemId == processGuideId);
                if (processGuide == null) return reportBody;

                reportBody.Title = processGuide.Title;
                reportBody.CaseNo = processGuide.PatientId;
                reportBody.BirthDay = processGuide.PatientDateOfBirth.ToString("dd.MM.yyyy");
                reportBody.Name = processGuide.PatientName;
                reportBody.AssignedOn = processGuide.CreateDate.ToString("dd.MM.yyyy");
                reportBody.OverAllCompletion = processGuide.CompletionStatus;
                reportBody.Topic = _uilmResourceKeyService.GetResourceValueByKeyName(processGuide.TopicValue, language);
                reportBody.Status = _uilmResourceKeyService.GetResourceValueByKeyName(
                    processGuide.IsActive ? "ACTIVE" : "INACTIVE",
                    language
                );
                // var form = _repository.GetItem<PraxisForm>(f => f.ItemId == processGuide.FormId);
                var form = _repository.GetItem<AssignedTaskForm>
                                        (p => p.AssignedEntityId == processGuide.ItemId
                                        && p.ClonedFormId == processGuide.FormId
                                        && p.AssignedEntityName == nameof(PraxisProcessGuide));
                if (form != null)
                {
                    reportBody.FormTitle = form.Title;
                    reportBody.FormDescription = form.Description;
                    reportBody.AdditionalDescription = form.AdditionalDescription;
                    reportBody.Attachments = GetFormAttachments(form, processGuide);

                    foreach (var client in processGuide.Clients)
                    {
                        var taskDescription = new TaskDescription();
                        var taskCompilationList = new List<TaskCompletedInfo>();

                        taskDescription.ClientId = client.ClientId;
                        taskDescription.ClientName = client.ClientName;
                        taskDescription.CategoryName = client.CategoryName;
                        taskDescription.SubCategoryName = client.SubCategoryName;
                        taskDescription.CompletionPercentage = processGuide.ClientCompletionInfo
                            .Where(c => c.ClientId == client.ClientId)
                            .Select(c => c.CompletionPercentage)
                            .FirstOrDefault();

                        var taskList = form.ProcessGuideCheckList?
                            .Where(p => 
                                p.ClientId == client.ClientId || 
                                (p.ClientInfos != null && 
                                 p.ClientInfos.Any(c => c.ClientId == client.ClientId)) ||
                                p.OrganizationIds?.Any() == true)?
                            .SelectMany(f => f.ProcessGuideTask);
                        if (taskList != null)
                        {
                            foreach (var task in taskList)
                            {
                                var taskCompletionInfo = new TaskCompletedInfo();
                                var taskCompletionRelatedInfos = new List<TaskCompletionRelatedInfo>();
                                taskCompletionInfo.TaskTitle = task.ProcessGuideTaskTitle;

                                if (client.ControlledMembers.Any())
                                {
                                    var builder = Builders<PraxisProcessGuideAnswer>.Filter;
                                    var filter = builder.In("SubmittedBy", client.ControlledMembers.ToArray()) &
                                                 builder.Eq("ProcessGuideId", processGuideId);
                                    var answerList = processGuideAnswerRepo.Find(filter).ToList();
                                    if (answerList.Any())
                                    {
                                        var answerIds = answerList.Select(a => a.ItemId).ToList();
                                        var answerFilter = builder.Eq("ProcessGuideId", processGuideId) &
                                                           builder.Eq("ClientId", client.ClientId) &
                                                           builder.Nin("_id", answerIds.ToArray());
                                        var deletedAnswerList = processGuideAnswerRepo.Find(answerFilter).ToList();
                                        if (deletedAnswerList.Any())
                                        {
                                            answerList.AddRange(deletedAnswerList);
                                        }
                                        answerList = answerList.Where(answer => answer.Answers.Any(a => a.QuestionId == task.ProcessGuideTaskId)).ToList();
                                        foreach (var answer in answerList)
                                        {
                                            var taskCompletionRelatedInfo = new TaskCompletionRelatedInfo();
                                            var person = _repository.GetItem<Person>(p => p.ItemId == answer.SubmittedBy);
                                            if (person == null) continue;

                                            taskCompletionRelatedInfo.CompletedBy = person.DisplayName;
                                            taskCompletionRelatedInfo.DateOfCompletion = answer.CreateDate
                                                .AddMinutes(timezoneOffsetInMinutes)
                                                .ToString("dd.MM.yyyy, h:mm tt");

                                            var files = answer.Answers
                                                .Where(a => a.QuestionId == task.ProcessGuideTaskId)
                                                .SelectMany(t => t.Files) ?? Enumerable.Empty<PraxisDocument>();
                                            if (files != null)
                                            {
                                                foreach (var file in files)
                                                {
                                                    taskCompletionRelatedInfo.Attachments.Add(file.DocumentName);
                                                }
                                            }

                                            var remark = answer.Answers
                                                .Where(a => a.QuestionId == task.ProcessGuideTaskId)
                                                .Select(t => t.Remarks)
                                                .FirstOrDefault();
                                            if (remark != null)
                                            {
                                                var plainText = Regex.Replace(remark, @"<(.|\n)*?>", "");
                                                taskCompletionRelatedInfo.Remarks = plainText;
                                            }

                                            var effectiveCost = answer.Answers
                                                .Where(a => a.QuestionId == task.ProcessGuideTaskId)
                                                .Select(t => t.ActualBudget)
                                                .FirstOrDefault();
                                            if (effectiveCost != null)
                                            {
                                                taskCompletionRelatedInfo.EffectiveCost = effectiveCost;
                                            }

                                            taskCompletionRelatedInfos.Add(taskCompletionRelatedInfo);
                                        }
                                    }
                                }
                                else
                                {
                                    var answerList = _repository.GetItems<PraxisProcessGuideAnswer>(
                                            a => a.ProcessGuideId == processGuideId && a.ClientId == client.ClientId && a.Answers != null &&
                                            a.Answers.Any(answer => answer.QuestionId == task.ProcessGuideTaskId)
                                        )
                                        .ToList();
                                    foreach (var answer in answerList)
                                    {
                                        var taskCompletionRelatedInfo = new TaskCompletionRelatedInfo();
                                        var person = _repository.GetItem<Person>(p => p.ItemId == answer.SubmittedBy);
                                        if (person != null)
                                        {
                                            taskCompletionRelatedInfo.CompletedBy = person.DisplayName;
                                            taskCompletionRelatedInfo.DateOfCompletion = answer.CreateDate
                                                .AddMinutes(timezoneOffsetInMinutes)
                                                .ToString("dd.MM.yyyy, h:mm tt");
                                        }

                                        var files = answer.Answers
                                            .Where(a => a.QuestionId == task.ProcessGuideTaskId)
                                            .SelectMany(t => t.Files);
                                        if (files != null)
                                        {
                                            foreach (var file in files)
                                            {
                                                taskCompletionRelatedInfo.Attachments.Add(file.DocumentName);
                                            }
                                        }

                                        var remark = answer.Answers.Where(a => a.QuestionId == task.ProcessGuideTaskId)
                                            .Select(t => t.Remarks)
                                            .FirstOrDefault();
                                        if (remark != null)
                                        {
                                            var plainText = Regex.Replace(remark, @"<(.|\n)*?>", "");
                                            taskCompletionRelatedInfo.Remarks = plainText;
                                        }

                                        var effectiveCost = answer.Answers
                                            .Where(a => a.QuestionId == task.ProcessGuideTaskId)
                                            .Select(t => t.ActualBudget)
                                            .FirstOrDefault();
                                        if (effectiveCost != null)
                                        {
                                            taskCompletionRelatedInfo.EffectiveCost = effectiveCost;
                                        }

                                        taskCompletionRelatedInfos.Add(taskCompletionRelatedInfo);
                                    }
                                }

                                taskCompletionInfo.Budget = task.Budget.ToString();
                                taskCompletionInfo.TaskCompletionRelatedInfos = taskCompletionRelatedInfos;
                                taskCompletionInfo.AdditionalInformation = task.AdditionalInformation;
                                taskCompilationList.Add(taskCompletionInfo);
                            }
                        }
                        taskDescription.TaskCompletedInfos = taskCompilationList;
                        taskDetails.Add(taskDescription);
                    }
                }
                reportBody.TaskDescriptions = taskDetails;
                return reportBody;
            }
            catch (Exception ex)
            {
                _logger.LogInformation("Exception occured during generate process guide details report data");
                _logger.LogError(ex, "Exception message: {Message}. StackTrace: {StackTrace}", ex.Message, ex.StackTrace);
                return new ProcessGuideDetailReport();
            }
        }
        private void WriteHeader(string reportName, string dateString, string clientName)
        {
            var worksheet = _currentWorksheet;
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

                worksheet.Cells[headerRowIndex, columnIndex++].Value = GetTranslation("FORM_TITLE");
                worksheet.Cells[headerRowIndex, columnIndex++].Value = GetTranslation("TITLE");
                worksheet.Cells[headerRowIndex, columnIndex++].Value = GetTranslation("TOPIC");
                worksheet.Cells[headerRowIndex, columnIndex++].Value = GetTranslation("BIRTHDAY");
                worksheet.Cells[headerRowIndex, columnIndex++].Value = GetTranslation("PATIENT_NAME");
                worksheet.Cells[headerRowIndex, columnIndex++].Value = GetTranslation("CASE_ID");
                worksheet.Cells[headerRowIndex, columnIndex++].Value = GetTranslation("ASSIGNED_ON");
                worksheet.Cells[headerRowIndex, columnIndex++].Value = GetTranslation("DESCRIPTION"); 
                worksheet.Cells[headerRowIndex, columnIndex++].Value = GetTranslation("ATTACHMENTS");
                worksheet.Cells[headerRowIndex, columnIndex++].Value = GetTranslation("ASSIGNED_ORGANIZATION");
                worksheet.Cells[headerRowIndex, columnIndex++].Value = GetTranslation("TASK_DESCRIPTION");
                worksheet.Cells[headerRowIndex, columnIndex++].Value = GetTranslation("ADDITIONAL_INFORMATION"); 
                worksheet.Cells[headerRowIndex, columnIndex++].Value = GetTranslation("COMPLETED_BY_USER");
                worksheet.Cells[headerRowIndex, columnIndex++].Value = GetTranslation("DATE_OF_COMPLETION");
                worksheet.Cells[headerRowIndex, columnIndex++].Value = GetTranslation("COMPLETION_STATUS");
                worksheet.Cells[headerRowIndex, columnIndex++].Value = GetTranslation("OVERALL_COMPLETION");
                worksheet.Cells[headerRowIndex, columnIndex++].Value = GetTranslation("ATTACHMENT_BY_USER");
                worksheet.Cells[headerRowIndex, columnIndex++].Value = GetTranslation("REMARKS");
                worksheet.Cells[headerRowIndex, columnIndex++].Value = GetTranslation("BUDGET");
                worksheet.Cells[headerRowIndex, columnIndex++].Value = GetTranslation("EFFECTIVE_COST");
                worksheet.Cells[headerRowIndex, columnIndex].Value = GetTranslation("STATUS");

                //AddHeaderLogo(worksheet);
            }
            catch (Exception ex)
            {
                _logger.LogInformation(ex, "Exception in WriteHeader");
            }
        }

        private int WriteProcessGuideDetails(ProcessGuideDetailReport processGuideDetailReport)
        {
            var workSheetTable = _currentWorksheet;
            var rowIndex = ProcessGuideDetailReportElement.HeaderRowIndexForAllDataReport + 1;
            var initialRow = rowIndex;
            try
            {
                var taskDescriptionIndex = 0;
                var taskDescriptionMargeIndex = 0;
                var taskCompletedMargeIndex = 0;
                var totalRowsCount = 0;
                var taskCompletedInfoIndex = taskDescriptionIndex = rowIndex;
                foreach (var taskDescription in processGuideDetailReport.TaskDescriptions)
                {
                    var rowMarge = 0;
                    foreach (var taskCompletedInfo in taskDescription.TaskCompletedInfos)
                    {
                        var relatedInfoIndex = rowIndex;
                        var margeRowCount = 0;
                        double totalEffCost = 0;
                        foreach (var relatedInfo in taskCompletedInfo.TaskCompletionRelatedInfos)
                        {
                            workSheetTable.Cells[relatedInfoIndex, 13].Value = relatedInfo.CompletedBy;
                            workSheetTable.Cells[relatedInfoIndex, 13].Style.VerticalAlignment = ExcelVerticalAlignment.Center;

                            workSheetTable.Cells[relatedInfoIndex, 14].Value = relatedInfo.DateOfCompletion;
                            workSheetTable.Cells[relatedInfoIndex, 14].Style.VerticalAlignment = ExcelVerticalAlignment.Center;

                            var attachments = string.Join("\n", relatedInfo.Attachments);
                            workSheetTable.Cells[relatedInfoIndex, 17].Value = attachments;
                            workSheetTable.Cells[relatedInfoIndex, 17].Style.VerticalAlignment = ExcelVerticalAlignment.Center;

                            workSheetTable.Cells[relatedInfoIndex, 18].Value = relatedInfo.Remarks;
                            workSheetTable.Cells[relatedInfoIndex, 18].Style.VerticalAlignment = ExcelVerticalAlignment.Center;

                            workSheetTable.Cells[relatedInfoIndex, 19].Value = relatedInfo.EffectiveCost;
                            workSheetTable.Cells[relatedInfoIndex, 19].Style.VerticalAlignment = ExcelVerticalAlignment.Center;

                            totalEffCost += relatedInfo.EffectiveCost ?? 0;

                            relatedInfoIndex++;
                        }

                        workSheetTable.Cells[relatedInfoIndex, 20].Value = totalEffCost;
                        workSheetTable.Cells[relatedInfoIndex, 20].Style.Font.Bold = true;
                        workSheetTable.Cells[relatedInfoIndex, 20].Style.VerticalAlignment = ExcelVerticalAlignment.Center;

                        margeRowCount = taskCompletedInfo.TaskCompletionRelatedInfos.Count;
                        rowMarge += margeRowCount + 1;
                        rowIndex += margeRowCount + 1;
                        totalRowsCount += margeRowCount + 1;
                        taskCompletedMargeIndex = taskCompletedInfoIndex + margeRowCount;

                        workSheetTable.Cells[taskCompletedInfoIndex, 11].Value = taskCompletedInfo.TaskTitle;
                        workSheetTable.Cells[taskCompletedInfoIndex, 11].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        workSheetTable.SelectedRange[taskCompletedInfoIndex, 11, taskCompletedMargeIndex, 11].Merge = true;

                        workSheetTable.Cells[taskCompletedInfoIndex, 12].Value = taskCompletedInfo.AdditionalInformation;
                        workSheetTable.Cells[taskCompletedInfoIndex, 12].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        workSheetTable.SelectedRange[taskCompletedInfoIndex, 12, taskCompletedMargeIndex, 12].Merge = true;

                        workSheetTable.Cells[taskCompletedInfoIndex, 22].Value = taskCompletedInfo.Budget;
                        workSheetTable.Cells[taskCompletedInfoIndex, 22].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        workSheetTable.SelectedRange[taskCompletedInfoIndex, 22, taskCompletedMargeIndex, 22].Merge = true;

                        taskCompletedInfoIndex += margeRowCount;
                        taskCompletedInfoIndex++;
                    }
                    var taskCount = 0;
                    if (rowMarge == 0)
                    {
                        taskCount = taskDescription.TaskCompletedInfos.Count - 1;
                    }
                    if (taskCount == rowMarge)
                    {
                        taskDescriptionMargeIndex = taskDescriptionIndex + taskCount;
                    }
                    else
                    {
                        taskDescriptionMargeIndex = taskCount > rowMarge ? taskDescriptionIndex + taskCount : taskDescriptionIndex + rowMarge - 1;
                        _logger.LogInformation("task description merge index: {Index}", taskDescriptionMargeIndex);
                        if (taskDescriptionMargeIndex < 0) taskDescriptionMargeIndex = 0;
                    }

                    workSheetTable.Cells[taskDescriptionIndex, 10].Value = taskDescription.ClientName;
                    workSheetTable.Cells[taskDescriptionIndex, 10].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    workSheetTable.SelectedRange[taskDescriptionIndex, 10, taskDescriptionMargeIndex, 10].Merge = true;

                    workSheetTable.Cells[taskDescriptionIndex, 15].Value = taskDescription.CompletionPercentage;
                    workSheetTable.Cells[taskDescriptionIndex, 15].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    workSheetTable.SelectedRange[taskDescriptionIndex, 15, taskDescriptionMargeIndex, 15].Merge = true;

                    if (taskCount > rowMarge)
                    {
                        taskDescriptionIndex += taskCount;
                        taskDescriptionIndex++;
                    }
                    else if (rowMarge > taskCount)
                    {
                        taskDescriptionIndex += rowMarge;
                    }
                    if (taskCount == rowMarge)
                    {
                        taskDescriptionIndex++;
                    }
                }

                var mergedRowIndex = initialRow + totalRowsCount - 1;

                workSheetTable.Cells[initialRow, 1].Value = processGuideDetailReport.FormTitle;
                workSheetTable.SelectedRange[initialRow, 1, mergedRowIndex, 1].Merge = true;

                workSheetTable.Cells[initialRow, 2].Value = processGuideDetailReport.FormDescription;
                workSheetTable.SelectedRange[initialRow, 2, mergedRowIndex, 2].Merge = true;

                workSheetTable.Cells[initialRow, 3].Value = processGuideDetailReport.Topic;
                workSheetTable.SelectedRange[initialRow, 3, mergedRowIndex, 3].Merge = true;

                workSheetTable.Cells[initialRow, 4].Value = processGuideDetailReport.BirthDay;
                workSheetTable.SelectedRange[initialRow, 4, mergedRowIndex, 4].Merge = true;

                workSheetTable.Cells[initialRow, 5].Value = processGuideDetailReport.Name; // case id 1
                workSheetTable.SelectedRange[initialRow, 5, mergedRowIndex, 5].Merge = true;

                workSheetTable.Cells[initialRow, 6].Value = processGuideDetailReport.CaseNo; // case id 2
                workSheetTable.SelectedRange[initialRow, 6, mergedRowIndex, 6].Merge = true;

                workSheetTable.Cells[initialRow, 7].Value = processGuideDetailReport.AssignedOn;
                workSheetTable.SelectedRange[initialRow, 7, mergedRowIndex, 7].Merge = true;

                workSheetTable.Cells[initialRow, 8].Value = processGuideDetailReport.AdditionalDescription;
                workSheetTable.SelectedRange[initialRow, 8, mergedRowIndex, 8].Merge = true;

                workSheetTable.Cells[initialRow, 9].Value = string.Join("\n", processGuideDetailReport.Attachments);
                workSheetTable.SelectedRange[initialRow, 9, mergedRowIndex, 9].Merge = true;

                workSheetTable.Cells[initialRow, 16].Value = processGuideDetailReport.OverAllCompletion;
                workSheetTable.SelectedRange[initialRow, 16, mergedRowIndex, 16].Merge = true;

                workSheetTable.Cells[initialRow, 21].Value = processGuideDetailReport.Status;
                workSheetTable.Cells[initialRow, 21].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                workSheetTable.SelectedRange[initialRow, 21, mergedRowIndex, 21].Merge = true;

                return mergedRowIndex;
            }
            catch (Exception e)
            {
                _logger.LogInformation(e, "Exception in WriteUserList");
                return rowIndex;
            }
        }

        private void SetColumnSpecificStyle(int endRow)
        {
            var worksheet = _currentWorksheet;

            const int tableColumns = ProcessGuideDetailReportElement.ColumnsForAllDataReport;
            for (var i = 1; i <= tableColumns; i++)
            {
                var column = worksheet.Column(i);
                var headerCell = worksheet.Cells[ProcessGuideDetailReportElement.HeaderRowIndexForAllDataReport, i];
                column.AutoFit();
                column.Style.WrapText = true;
                column.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                headerCell.Style.Fill.PatternType = ExcelFillStyle.Solid;
                headerCell.Style.Font.Bold = true;
                headerCell.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                headerCell.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                headerCell.Style.Border.Right.Style = ExcelBorderStyle.Thin;
                headerCell.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                headerCell.Style.Fill.BackgroundColor.SetColor(ProcessGuideDetailReportElement.HeaderBackground);
            }

            int columnIndex = 1;
            worksheet.Column(columnIndex++).Width = 25;
            worksheet.Column(columnIndex++).Width = 15;
            worksheet.Column(columnIndex++).Width = 15;
            worksheet.Column(columnIndex++).Width = 25;
            worksheet.Column(columnIndex++).Width = 15;
            worksheet.Column(columnIndex++).Width = 15;
            worksheet.Column(columnIndex++).Width = 20;
            worksheet.Column(columnIndex++).Width = 20;
            worksheet.Column(columnIndex++).Width = 25;
            worksheet.Column(columnIndex++).Width = 25;
            worksheet.Column(columnIndex++).Width = 20;
            worksheet.Column(columnIndex++).Width = 15;
            worksheet.Column(columnIndex++).Width = 15;
            worksheet.Column(columnIndex++).Width = 25;
            worksheet.Column(columnIndex++).Width = 25;
            worksheet.Column(columnIndex++).Width = 25;
            worksheet.Column(columnIndex++).Width = 15;
            worksheet.Column(columnIndex++).Width = 15;
            worksheet.Column(columnIndex++).Width = 15;
            worksheet.Column(columnIndex++).Width = 15;
            worksheet.Column(columnIndex++).Width = 17;
            worksheet.Column(columnIndex++).Width = 15;
            worksheet.Column(columnIndex++).Width = 15;
            worksheet.Column(columnIndex).Width = 25;

            _praxisReportService.DrawBorder(
                worksheet,
                ProcessGuideDeveloperReport.HeaderRowIndexForSpecificClientReport,
                1,
                endRow,
                ProcessGuideDetailReportElement.ColumnsForAllDataReport
            );
        }

        private string GetTranslation(string key)
        {
            return _translatedStringsAsDictionary[$"APP_PROCESS_GUIDE.{key}"];
        }

        private void SetTranslation(string languageKey)
        {
            _translatedStringsAsDictionary = _uilmResourceKeyService.GetResourceValueByKeyName(
                ProcessGuideDetailReportElement.TranslationKeys,
                languageKey
            );

            _translatedStringsAsDictionary["APP_PROCESS_GUIDE.FORM_TITLE"] =
                _uilmResourceKeyService.GetResourceValueByKeyName("APP_FORM.TITLE", languageKey);

            _translatedStringsAsDictionary["APP_PROCESS_GUIDE.FORM_DESCRIPTION"] =
                _uilmResourceKeyService.GetResourceValueByKeyName("APP_FORM.FORM_DESCRIPTION", languageKey);

            _translatedStringsAsDictionary["APP_PROCESS_GUIDE.ADDITIONAL_INFORMATION"] =
                _uilmResourceKeyService.GetResourceValueByKeyName("APP_FORM.ADDITIONAL_INFORMATION", languageKey);
        }

        private void AddHeaderLogo(ExcelWorksheet worksheet)
        {
            const int logoStartColumn = ProcessGuideDetailReportElement.ColumnsForAllDataReport;
            // [firstRow, firstColumn, lastRow, LastColumn]
            worksheet.Cells[1, logoStartColumn, 2, logoStartColumn].Merge = true;
            _praxisReportService.AddLogoInExcelReport(worksheet, ProcessGuideDetailReportElement.LogoSize, logoStartColumn, rqLatestLogo);
        }

        private List<string> GetFormAttachments(AssignedTaskForm form, PraxisProcessGuide processGuide)
        {
            var userId = _securityContextProvider.GetSecurityContext().UserId;
            var userInfo = _repository.GetItem<PraxisUser>(pu => pu.UserId == userId);
            var attachments = new List<string>();
            var assignedClientIds = processGuide.Clients
                .Where(c => !c.HasSpecificControlledMembers || c.ControlledMembers.Contains(userInfo.ItemId))
                .Select(c => c.ClientId)
                .ToList();
            if (_securityHelperService.IsADepartmentLevelUser())
            {
                var loggedInUserUnit = _securityHelperService.ExtractDepartmentIdFromDepartmentLevelUser();
                assignedClientIds = new List<string> { loggedInUserUnit };
            }
            foreach (var clientSpecificCheckList in form.ProcessGuideCheckList)
            {
                var hasPermissionToRead =
                    assignedClientIds.Any(c => c == clientSpecificCheckList.ClientId) ||
                    clientSpecificCheckList.ClientInfos?.Any(c => assignedClientIds.Contains(c.ClientId)) == true ||
                    clientSpecificCheckList.OrganizationIds?.Any() == true;
                if (!hasPermissionToRead) continue;
                
                var attachmentsForClient = clientSpecificCheckList.ProcessGuideTask
                    .SelectMany(t => t.Files)
                    .Select(f => f.DocumentName);
                attachments.AddRange(attachmentsForClient);
                attachmentsForClient = clientSpecificCheckList.ProcessGuideTask
                    .SelectMany(t => t.LibraryForms)
                    .Select(f => f.LibraryFormName);
                attachments.AddRange(attachmentsForClient);
            }
            return attachments;
        }
    }
}
