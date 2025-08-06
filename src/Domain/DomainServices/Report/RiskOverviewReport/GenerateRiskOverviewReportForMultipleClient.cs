using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.Risk;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.ExcelReports;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.Reports;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.Reports.RiskOverviewReport;
using static Selise.Ecap.SC.PraxisMonitor.Contracts.Constants.ReportConstants;
using GetCompletionListQuery = Selise.Ecap.SC.PraxisMonitor.Contracts.Queries.GetCompletionListQuery;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.Report.RiskOverviewReport
{
    public class GenerateRiskOverviewReportForMultipleClient : IGenerateRiskOverviewReport
    {
        private readonly IRepository _repository;
        private readonly ILogger<GenerateRiskOverviewReportForMultipleClient> _logger;
        private readonly IUilmResourceKeyService _uilmResourceKeyService;
        private readonly IPraxisReportService _praxisReportService;
        private readonly ICommonUtilService _commonUtilService;
        private readonly ISecurityContextProvider _securityContextProvider;
        private readonly IPraxisOpenItemService _openItemService;
        private readonly IPraxisRiskService _riskService;
        private Dictionary<string, string> _translatedStringsAsDictionary;
        private readonly List<string> _translationKeys;

        public GenerateRiskOverviewReportForMultipleClient(
            IRepository repository,
            ILogger<GenerateRiskOverviewReportForMultipleClient> logger,
            IUilmResourceKeyService uilmResourceKeyService,
            IPraxisReportService praxisReportService,
            ICommonUtilService commonUtilService,
            ISecurityContextProvider securityContextProvider,
            IPraxisOpenItemService openItemService,
            IPraxisRiskService riskService
        )
        {
            _repository = repository;
            _logger = logger;
            _uilmResourceKeyService = uilmResourceKeyService;
            _praxisReportService = praxisReportService;
            _commonUtilService = commonUtilService;
            _securityContextProvider = securityContextProvider;
            _openItemService = openItemService;
            _riskService = riskService;

            _translationKeys = PraxisRiskOverviewReport.TranslationKeys;
            _translationKeys.AddRange(TopicTranslationKeys);
            _translationKeys = _translationKeys.Distinct().ToList();
        }

        public async Task<bool> GenerateReport(ExcelPackage excel, ExportRiskOverviewReportCommand command)
        {
            try
            {
                var reportDateString = DateTime.Today.ToString("dd.MM.yyyy");
                var languageKey = _securityContextProvider.GetSecurityContext().Language;
                _translatedStringsAsDictionary = _uilmResourceKeyService
                    .GetResourceValueByKeyName(_translationKeys, languageKey);

                var worksheet = excel.Workbook.Worksheets.Add(command.ReportHeader);

                var praxisRisks = (await _commonUtilService.GetEntityQueryResponse<PraxisRisk>(
                        command.FilterString,
                        "{Reference: 1}",
                        "",
                        false,
                        1,
                        100,
                        true,
                        false
                    ))
                    .Results;
                var praxisRiskIds = new List<string>();
                var praxisClientIds = new List<string>();

                foreach (var praxisRisk in praxisRisks)
                {
                    praxisRiskIds.Add(praxisRisk.ItemId);
                    praxisClientIds.Add(praxisRisk.ClientId);
                }

                praxisClientIds = praxisClientIds.Distinct().ToList();

                await _praxisReportService.AddClientIdsToPraxisReport(command.ReportFileId, praxisClientIds);

                var praxisAssessments = _repository
                    .GetItems<PraxisAssessment>(assessment => praxisRiskIds.Contains(assessment.RiskId))
                    .ToList();

                var clientList = _repository.GetItems<PraxisClient>(
                        client =>
                            praxisClientIds.Contains(client.ItemId)
                    )
                    .OrderBy(x => x.ClientName)
                    .ToList();

                var startRow = PraxisRiskOverviewReport.HeaderRowIndexForMultipleClientReport + 2;

                foreach (var client in clientList)
                {
                    var rowsTakenForClient = await WriteRiskList(
                        worksheet,
                        praxisRisks.Where(risk => risk.ClientId.Equals(client.ItemId)).ToList(),
                        praxisAssessments,
                        client.ClientName,
                        startRow
                    );
                    if (rowsTakenForClient > 1)
                    {
                        worksheet.Cells[startRow, 1, startRow + rowsTakenForClient - 1, 1].Merge = true;
                    }

                    startRow += rowsTakenForClient;
                }

                WriteHeader(worksheet, command.ReportHeader, reportDateString);
                SetColumnSpecificStyle(worksheet);

                return true;
            }
            catch (Exception e)
            {
                _logger.LogError("Exception occurred while generating Risk overview excel report for multiple clients. Exception message: {Message}. Full stacktrace: {StackTrace}", e.Message, e.StackTrace);
                return false;
            }
        }

        private async Task<int> WriteRiskList(
            ExcelWorksheet worksheet,
            List<PraxisRisk> praxisRisks,
            List<PraxisAssessment> praxisAssessments,
            string clientName,
            int startRow
        )
        {
            try
            {
                var currentRow = startRow;
                foreach (var praxisRisk in praxisRisks)
                {
                    var riskStartRow = currentRow;
                    var columnIndex = 1;
                    var assessments = praxisAssessments
                        .Where(assessment => assessment.RiskId.Equals(praxisRisk.ItemId))
                        .ToList();

                    var completionDetails = await GetCompletionDetails(praxisRisk.ItemId);

                    worksheet.Cells[currentRow, columnIndex++].Value = clientName;
                    worksheet.Cells[currentRow, columnIndex++].Value = praxisRisk.Reference;
                    worksheet.Cells[currentRow, columnIndex++].Value = GetTranslation(praxisRisk.TopicValue, false);
                    worksheet.Cells[currentRow, columnIndex++].Value = praxisRisk.CategoryName;
                    worksheet.Cells[currentRow, columnIndex++].Value = praxisRisk.SubCategoryName;
                    worksheet.Cells[currentRow, columnIndex++].Value = GetUserNameByIds(praxisRisk.RiskOwners.ToList());
                    worksheet.Cells[currentRow, columnIndex++].Value = GetUserNameByIds(praxisRisk.RiskProfessionals.ToList());
                    ExcelRichTextHtmlUtility.SetRichTextFromHtml(
                        worksheet.Cells[currentRow, columnIndex++],
                        praxisRisk.Event,
                        PraxisRiskOverviewReport.DefaultFontName,
                        PraxisRiskOverviewReport.DefaultFontSize
                    );
                    ExcelRichTextHtmlUtility.SetRichTextFromHtml(
                        worksheet.Cells[currentRow, columnIndex++],
                        praxisRisk.Damages,
                        PraxisRiskOverviewReport.DefaultFontName,
                        PraxisRiskOverviewReport.DefaultFontSize
                    );
                    ExcelRichTextHtmlUtility.SetRichTextFromHtml(
                        worksheet.Cells[currentRow, columnIndex++],
                        praxisRisk.Causes,
                        PraxisRiskOverviewReport.DefaultFontName,
                        PraxisRiskOverviewReport.DefaultFontSize
                    );
                    ExcelRichTextHtmlUtility.SetRichTextFromHtml(
                        worksheet.Cells[currentRow, columnIndex++],
                        praxisRisk.Remarks,
                        PraxisRiskOverviewReport.DefaultFontName,
                        PraxisRiskOverviewReport.DefaultFontSize
                    );

                    var assessmentColumnIndex = columnIndex;
                    for (var i = 0; i < assessments.Count; i++)
                    {
                        var assessment = assessments[i];
                        var cell = worksheet.Cells[currentRow + i, columnIndex];
                        GetAssessmentCellText(cell, assessment);
                    }

                    columnIndex++;

                    if (praxisRisk.RecentAssessment != null)
                    {
                        var recentAssessment = praxisRisk.RecentAssessment;
                        worksheet.Cells[currentRow, columnIndex++].Value = GetTranslation(recentAssessment.Impact);
                        worksheet.Cells[currentRow, columnIndex++].Value = GetTranslation(recentAssessment.Probability);
                        worksheet.Cells[currentRow, columnIndex++].Value = GetTranslation(recentAssessment.Assessment);
                        worksheet.Cells[currentRow, columnIndex++].Value =
                            GetTranslation(recentAssessment.Measure.Value, false);
                        worksheet.Cells[currentRow, columnIndex++].Value =
                            _riskService.GetCurrentRiskValue(recentAssessment);
                        worksheet.Cells[currentRow, columnIndex++].Value = recentAssessment.RiskAssessmentValue;
                    }
                    else
                    {
                        columnIndex += 6;
                    }

                    worksheet.Cells[currentRow, columnIndex++].Value = completionDetails.MeasuresTakenCount;
                    worksheet.Cells[currentRow, columnIndex++].Value =
                        string.Join(",\n", completionDetails.MeasuresTaken);
                    worksheet.Cells[currentRow, columnIndex++].Value = completionDetails.MeasuresPendingCount;
                    worksheet.Cells[currentRow, columnIndex++].Value =
                        string.Join(",\n", completionDetails.MeasuresPending);

                    currentRow += Math.Max(assessments.Count, 1);

                    if (currentRow - riskStartRow == 1) continue;
                    
                    for (var i = 1; i < columnIndex; i++)
                    {
                        if (i == assessmentColumnIndex) continue;
                        worksheet.Cells[riskStartRow, i, currentRow - 1, i].Merge = true;
                    }
                }

                worksheet.Cells[1, 1, currentRow, PraxisRiskOverviewReport.ColumnsForMultipleClientReport]
                    .Style.VerticalAlignment = ExcelVerticalAlignment.Top;
                return currentRow - startRow;
            }
            catch (Exception e)
            {
                _logger.LogError("Exception: {Message}", e.Message);
                return 0;
            }
        }

        private void GetAssessmentCellText(ExcelRange cell, PraxisAssessment assessment)
        {
            cell.IsRichText = true;
            cell.RichText.Add($"{assessment.CreateDate:dd MMM yyyy}\n").Bold = false;
            cell.RichText.Add($"{assessment.CreateDate:h:mm tt}\n").Bold = false;
            cell.RichText.Add($"{GetTranslation("ASSESSMENT")}: ").Bold = true;
            cell.RichText.Add($"{GetTranslation(assessment.Assessment)}\n").Bold = false;
            cell.RichText.Add($"{GetTranslation("IMPACT")}: ").Bold = true;
            cell.RichText.Add($"{GetTranslation(assessment.Impact)}\n").Bold = false;
            cell.RichText.Add($"{GetTranslation("PROBABILITY")}: ").Bold = true;
            cell.RichText.Add($"{GetTranslation(assessment.Probability)}\n").Bold = false;
            cell.RichText.Add($"{GetTranslation("MEASURE")}: ").Bold = true;
            cell.RichText.Add($"{GetTranslation(assessment.Measure.Value, false)}\n").Bold = false;
            cell.RichText.Add($"{GetTranslation("TARGET_VALUE")}: ").Bold = true;
            cell.RichText.Add($"{assessment.RiskAssessmentValue}").Bold = false;
        }

        private async Task<dynamic> GetCompletionDetails(string praxisRiskItemId)
        {
            var measuresTaken = new List<string>();
            var measuresTakenCount = 0;
            var measuresPending = new List<string>();
            var measuresPendingCount = 0;

            try
            {
                var completionDetails = await _openItemService.GetOpenItemCompletionDetails(
                    new GetCompletionListQuery { TaskReferenceId = praxisRiskItemId }
                );
                var openItemCompletionHistory = completionDetails.CompletionHistory.ToList();


                foreach (var completionHistory in openItemCompletionHistory)
                {
                    int doneCount = 0, pendingCount = 0;
                    foreach (var status in completionHistory.CompletionStatus.Select(cs => cs.Status))
                    {
                        if (status.Equals("done", StringComparison.CurrentCultureIgnoreCase))
                        {
                            doneCount++;
                        }
                        else if (status.Equals("pending", StringComparison.CurrentCultureIgnoreCase))
                        {
                            pendingCount++;
                        }
                    }

                    if (doneCount > 0)
                    {
                        measuresTaken.Add(
                            doneCount > 1
                                ? $"{completionHistory.TaskTitle} ({doneCount})"
                                : completionHistory.TaskTitle
                        );
                    }

                    if (pendingCount > 0)
                    {
                        measuresPending.Add(
                            pendingCount > 1
                                ? $"{completionHistory.TaskTitle}, ({pendingCount})"
                                : completionHistory.TaskTitle
                        );
                    }

                    measuresTakenCount += doneCount;
                    measuresPendingCount += pendingCount;
                }
            }
            catch (Exception e)
            {
                _logger.LogError("Error occurred while generating completion details for risk with ItemId: {ItemId}. StackTrace: {StackTrace}", praxisRiskItemId, e.StackTrace);
            }
            return new
            {
                MeasuresTaken = measuresTaken,
                MeasuresTakenCount = measuresTakenCount,
                MeasuresPending = measuresPending,
                MeasuresPendingCount = measuresPendingCount
            };
        }

        private void WriteHeader(ExcelWorksheet worksheet, string reportName, string dateString)
        {
            try
            {
                worksheet.Cells[1, 1].Value = GetTranslation("REPORT_NAME");
                worksheet.Cells[1, 1].Style.Font.Bold = true;
                worksheet.Cells[1, 2].Value = reportName;

                worksheet.Cells[2, 1].Value = GetTranslation("DATE", false);
                worksheet.Cells[2, 1].Style.Font.Bold = true;
                worksheet.Cells[2, 2].Value = dateString;

                var columnIndex = 1;
                const int headerRowIndex = PraxisRiskOverviewReport.HeaderRowIndexForMultipleClientReport;

                worksheet.Cells[headerRowIndex, columnIndex++].Value = GetTranslation("ORGANIZATION", false);
                worksheet.Cells[headerRowIndex, columnIndex++].Value = GetTranslation("RISK_NAME");
                worksheet.Cells[headerRowIndex, columnIndex++].Value = GetTranslation("TOPIC");
                worksheet.Cells[headerRowIndex, columnIndex++].Value = GetTranslation("CATEGORY");
                worksheet.Cells[headerRowIndex, columnIndex++].Value = GetTranslation("SUB_CATEGORY");
                worksheet.Cells[headerRowIndex, columnIndex++].Value = GetTranslation("RISK_OWNERS");
                worksheet.Cells[headerRowIndex, columnIndex++].Value = GetTranslation("RISK_PROFESSIONALS");
                worksheet.Cells[headerRowIndex, columnIndex++].Value = GetTranslation("EVENT");
                worksheet.Cells[headerRowIndex, columnIndex++].Value = GetTranslation("IMPACT");
                worksheet.Cells[headerRowIndex, columnIndex++].Value = GetTranslation("CAUSES");
                worksheet.Cells[headerRowIndex, columnIndex++].Value = GetTranslation("REMARKS");
                worksheet.Cells[headerRowIndex, columnIndex++].Value = GetTranslation("RISK_ASSESSMENT");
                var lastAssessmentColumnIndex = columnIndex;
                worksheet.Cells[headerRowIndex, columnIndex].Value = GetTranslation("LAST_ASSESSMENT");
                worksheet.Cells[headerRowIndex + 1, columnIndex++].Value = GetTranslation("IMPACT");
                worksheet.Cells[headerRowIndex + 1, columnIndex++].Value = GetTranslation("PROBABILITY");
                worksheet.Cells[headerRowIndex + 1, columnIndex++].Value = GetTranslation("ASSESSMENT");
                worksheet.Cells[headerRowIndex + 1, columnIndex++].Value = GetTranslation("MEASURE");
                worksheet.Cells[headerRowIndex + 1, columnIndex++].Value = GetTranslation("VALUE");
                worksheet.Cells[headerRowIndex + 1, columnIndex++].Value = GetTranslation("TARGET_VALUE");
                worksheet.Cells[headerRowIndex, columnIndex++].Value = GetTranslation("NUMBER_OF_MEASURES_TAKEN");
                worksheet.Cells[headerRowIndex, columnIndex++].Value = GetTranslation("MEASURES_TAKEN");
                worksheet.Cells[headerRowIndex, columnIndex++].Value = GetTranslation("NUMBER_OF_MEASURES_PENDING");
                worksheet.Cells[headerRowIndex, columnIndex++].Value = GetTranslation("MEASURES_PENDING");

                for (int i = 1; i < columnIndex; i++)
                {
                    if (i < lastAssessmentColumnIndex || i > lastAssessmentColumnIndex + 5)
                        worksheet.Cells[headerRowIndex, i, headerRowIndex + 1, i].Merge = true;
                }

                worksheet.Cells[
                        headerRowIndex,
                        lastAssessmentColumnIndex,
                        headerRowIndex,
                        lastAssessmentColumnIndex + 5
                    ]
                    .Merge = true;

                worksheet.Row(headerRowIndex).Style.Font.Bold = true;
                worksheet.Row(headerRowIndex).Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                worksheet.Row(headerRowIndex).Style.VerticalAlignment = ExcelVerticalAlignment.Top;

                var headerRange = worksheet.Cells[headerRowIndex, 1, headerRowIndex + 1, columnIndex - 1];
                headerRange.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                headerRange.Style.WrapText = false;
                headerRange.Style.Fill.PatternType = ExcelFillStyle.Solid;
                headerRange.Style.Fill.BackgroundColor.SetColor(PraxisRiskOverviewReport.HeaderBackground);

                //AddHeaderLogo(worksheet);
            }
            catch (Exception ex)
            {
                _logger.LogError("Exception: {Message}", ex.Message);
            }
        }

        private void AddHeaderLogo(ExcelWorksheet worksheet)
        {
            const int logoStartColumn = PraxisRiskOverviewReport.ColumnsForMultipleClientReport;
            // [firstRow, firstColumn, lastRow, LastColumn]
            worksheet.Cells[1, logoStartColumn, 2, logoStartColumn].Merge = true;
            _praxisReportService.AddLogoInExcelReport(
                worksheet,
                PraxisRiskOverviewReport.LogoSize,
                logoStartColumn,
                rqLatestLogo
            );
        }

        private void SetColumnSpecificStyle(ExcelWorksheet worksheet)
        {
            const int tableColumns = PraxisRiskOverviewReport.ColumnsForMultipleClientReport;
            const int headerRowIndex = PraxisRiskOverviewReport.HeaderRowIndexForMultipleClientReport;
            try
            {
                for (var i = 1; i <= tableColumns; i++)
                {
                    var column = worksheet.Column(i);
                    column.Style.WrapText = true;
                    if (i <= 2 || i >= 8 && i <= 12)
                    {
                        column.Width = 30;
                    }
                    else if (i <= 7)
                    {
                        column.Width = 25;
                    }
                    else if (i == 19 || i == 21)
                    {
                        column.Width = 18;
                        column.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                        worksheet.Cells[headerRowIndex, i].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    }
                    else if (i == 20 || i == 22)
                    {
                        column.Width = 22;
                    }
                    else
                    {
                        worksheet.Cells[headerRowIndex, i, headerRowIndex + 1, i].Style.WrapText = false;
                        worksheet.Cells[headerRowIndex, i, headerRowIndex + 1, i].Style.HorizontalAlignment =
                            ExcelHorizontalAlignment.Center;
                        column.AutoFit();
                        column.Width = Math.Max(column.Width, 15);
                    }

                    worksheet.Cells[headerRowIndex, i].Style.Border.BorderAround(ExcelBorderStyle.Thin);
                    worksheet.Cells[headerRowIndex + 1, i].Style.Border.BorderAround(ExcelBorderStyle.Thin);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Exception: {Message}", ex.Message);
            }
        }

        private string GetTranslation(string key, bool withPrefix = true)
        {
            if (withPrefix)
            {
                key = $"APP_RISK_MANAGEMENT.{key}";
            }

            return _translatedStringsAsDictionary.ContainsKey(key) ? _translatedStringsAsDictionary[key] : key;
        }

        private string GetUserNameByIds(List<string> userIds)
        {
            var displayNameList = _repository.GetItems<PraxisUser>(p => userIds.Contains(p.ItemId))
                .Select(p => p.DisplayName).ToList();
            return string.Join('\n', displayNameList);
        }
    }
}