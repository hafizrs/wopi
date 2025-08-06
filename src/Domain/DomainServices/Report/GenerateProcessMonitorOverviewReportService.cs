using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.Task;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.ExcelReports;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.Reports;
using Selise.Ecap.SC.PraxisMonitor.Contracts.MongoDb;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using static Selise.Ecap.SC.PraxisMonitor.Contracts.Constants.ReportConstants;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.Report
{
    public class GenerateProcessMonitorOverviewReportService : IGenerateProcessMonitorOverviewReport
    {
        private readonly IPraxisTaskService _praxisTaskService;
        private readonly ILogger<GenerateProcessMonitorOverviewReportService> _logger;
        private readonly IMongoClientRepository _mongoClientRepository;
        private readonly IPraxisReportService _praxisReportService;
        private const string ReportListSheetName = "Reporting List";
        private readonly ISecurityContextProvider _securityContextProvider;

        public GenerateProcessMonitorOverviewReportService(
            IPraxisTaskService praxisTaskService,
            ILogger<GenerateProcessMonitorOverviewReportService> logger,
            IPraxisReportService praxisReportService,
            IMongoClientRepository mongoClientRepository,
            ISecurityContextProvider securityContextProvider
        )
        {
            _praxisTaskService = praxisTaskService;
            _logger = logger;
            _praxisReportService = praxisReportService;
            _mongoClientRepository = mongoClientRepository;
            _securityContextProvider = securityContextProvider;
        }

        public async Task<bool> PrepareTaskListReport(
            string filter,
            bool enableDateRange,
            DateTime? startDate,
            DateTime? endDate,
            PraxisClient client,
            ExcelPackage excel,
            ExportTaskListReportTranslation translation,
            int timezoneOffsetInMinutes
        )
        {
            int monitoringListRowIndex = 5;
            string reportDateString = DateTime.Today.ToString("dd.MM.yyyy");

            if (enableDateRange && startDate != null && endDate != null)
            {
                reportDateString = $"{startDate.Value:dd.MM.yyyy} - {endDate.Value:dd.MM.yyyy}";
            }

            WriteHearderForOverviewReport(
                client,
                excel,
                translation.TASK_MONITOR_REPORT,
                ReportListSheetName,
                reportDateString,
                9,
                2,
                translation.REPORT_NAME,
                translation.DATE,
                translation.ORGANIZATION
            );

            var dataset = await _praxisTaskService.GetOverviewReportData(filter, "{CreateDate: -1}");

            int page = 0, pageSize = 100, totalPage = (int)Math.Ceiling((decimal)dataset.TotalRecordCount / pageSize);
            if (totalPage != 0)
            {
                for (int i = 0; i < totalPage; i++)
                {
                    var results = dataset.Results.Skip(page * pageSize).Take(pageSize).ToList();
                    WriteReportingListExcelReport(
                        results,
                        excel,
                        ReportListSheetName,
                        ref monitoringListRowIndex,
                        translation,
                        timezoneOffsetInMinutes
                    );
                    page++;
                }
            }
            else
            {
                var results = dataset.Results.Skip(page * pageSize).Take(pageSize).ToList();
                WriteReportingListExcelReport(
                    results,
                    excel,
                    ReportListSheetName,
                    ref monitoringListRowIndex,
                    translation,
                    timezoneOffsetInMinutes
                );
            }

            AddReportBorderLine(excel, monitoringListRowIndex, ReportListSheetName, 9);
            return true;
        }

        private void WriteHearderForOverviewReport(
            PraxisClient client,
            ExcelPackage excel,
            string reportName,
            string reportSheetName,
            string reportDateString,
            int logoPosition,
            int logoSize,
            string Report_NameHeader,
            string DateHeader,
            string OrganizationHeader
        )
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

                // Add Logo
                _praxisReportService.AddLogoInExcelReport(workSheetTable, logoSize, logoPosition, rqLatestLogo);
            }
            catch (Exception ex)
            {
                _logger.LogError("WriteHeader got error: {Message}", ex.Message);
            }
        }

        private void WriteReportingListExcelReport(
            List<PraxisTask> dataset,
            ExcelPackage excel,
            string reportSheetName,
            ref int rowIndex,
            ExportTaskListReportTranslation translation,
            int timezoneOffsetInMinutes
        )
        {
            try
            {
                var workSheetTable = excel.Workbook.Worksheets[reportSheetName];

                DataListHeader(workSheetTable, translation);

                foreach (PraxisTask data in dataset)
                {
                    workSheetTable.Cells[rowIndex, 9].Style.Border.Right.Style = ExcelBorderStyle.Thin;

                    try
                    {
                        workSheetTable.Cells[rowIndex, 1].Value = GetMemberNames(data.ControlledMembers.ToList());
                        workSheetTable.Cells[rowIndex, 1].Style.WrapText = true;
                        workSheetTable.Cells[rowIndex, 1].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        workSheetTable.Cells[rowIndex, 1].Style.Border.BorderAround(ExcelBorderStyle.Thin);

                        workSheetTable.Cells[rowIndex, 2].Value = data.CategoryName;
                        workSheetTable.Cells[rowIndex, 2].Style.WrapText = true;
                        workSheetTable.Cells[rowIndex, 2].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        workSheetTable.Cells[rowIndex, 2].Style.Border.BorderAround(ExcelBorderStyle.Thin);

                        workSheetTable.Cells[rowIndex, 3].Value = data.SubCategoryName;
                        workSheetTable.Cells[rowIndex, 3].Style.WrapText = true;
                        workSheetTable.Cells[rowIndex, 3].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        workSheetTable.Cells[rowIndex, 3].Style.Border.BorderAround(ExcelBorderStyle.Thin);

                        workSheetTable.Cells[rowIndex, 4].Value =
                            data.TaskSchedule.TaskDateTime.ToString("dd MMMM, yyyy ");
                        workSheetTable.Cells[rowIndex, 4].Style.WrapText = true;
                        workSheetTable.Cells[rowIndex, 4].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        workSheetTable.Cells[rowIndex, 4].Style.Border.BorderAround(ExcelBorderStyle.Thin);

                        workSheetTable.Cells[rowIndex, 5].Value = data.Task.Title;
                        workSheetTable.Cells[rowIndex, 5].Style.WrapText = true;
                        workSheetTable.Cells[rowIndex, 5].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        workSheetTable.Cells[rowIndex, 5].Style.Border.BorderAround(ExcelBorderStyle.Thin);

                        workSheetTable.Cells[rowIndex, 6].Value = data.LinearAnswer;
                        workSheetTable.Cells[rowIndex, 6].Style.Numberformat.Format = "#0\\.00%";
                        workSheetTable.Cells[rowIndex, 6].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        workSheetTable.Cells[rowIndex, 6].Style.Border.BorderAround(ExcelBorderStyle.Thin);

                        ExcelRichTextHtmlUtility.SetRichTextFromHtml(
                            workSheetTable.Cells[rowIndex, 7],
                            data.Remarks,
                            "Calibri",
                            11
                        );
                        workSheetTable.Cells[rowIndex, 7].Style.WrapText = true;
                        workSheetTable.Cells[rowIndex, 7].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        workSheetTable.Cells[rowIndex, 7].Style.Border.BorderAround(ExcelBorderStyle.Thin);

                        var submissionDate = data.TaskSchedule.SubmissionDate.AddMinutes(timezoneOffsetInMinutes);

                        workSheetTable.Cells[rowIndex, 8].Value =
                            (data.TaskSchedule.SubmissionDate == DateTime.MinValue)
                                ? "-"
                                : submissionDate.ToString("dd MMMM, yyyy") + " at " +
                                  submissionDate.ToString("hh:mm:ss tt");

                        workSheetTable.Cells[rowIndex, 8].Style.WrapText = true;
                        workSheetTable.Cells[rowIndex, 8].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        workSheetTable.Cells[rowIndex, 8].Style.Border.BorderAround(ExcelBorderStyle.Thin);

                        if (data.ReportedByUserIds != null)
                        {
                            workSheetTable.Cells[rowIndex, 9].Value = GetMemberNames(data.ReportedByUserIds.ToList());
                            workSheetTable.Cells[rowIndex, 9].Style.WrapText = true;
                        }

                        workSheetTable.Cells[rowIndex, 9].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        workSheetTable.Cells[rowIndex, 9].Style.Border.BorderAround(ExcelBorderStyle.Thin);

                        for (int i = 1; i <= 9; i++)
                        {
                            workSheetTable.Cells[rowIndex, i].Style.VerticalAlignment = ExcelVerticalAlignment.Top;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Write got error with row number: {RowNumber}", rowIndex);
                    }

                    rowIndex++;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Write got error with row number: {RowNumber}. Error message: {Message}", rowIndex, ex.Message);
            }
        }

        private static void AddReportBorderLine(
            ExcelPackage excel,
            int rowIndex,
            string reportSheetName,
            int totalColumn
        )
        {
            var workSheetTable = excel.Workbook.Worksheets[reportSheetName];

            for (int i = 1; i <= totalColumn; i++)
            {
                workSheetTable.Cells[rowIndex - 1, i].Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
            }
        }

        private void DataListHeader(ExcelWorksheet workSheetTable, ExportTaskListReportTranslation translation)
        {
            var headerRowIndex = 4;
            var headerColumnIndex = 1;

            List<string> headerColumns = new List<string>
            {
                translation.RESPONSIBLE_PERSON,
                translation.CATEGORY,
                translation.SUB_CATEGORY,
                translation.SUBMISSION_DATE,
                translation.TASK,
                translation.SCORE,
                translation.REMARK,
                translation.DATE_TIME,
                translation.CONTROLLING_PERSON
            };

            foreach (var column in headerColumns)
            {
                workSheetTable.Cells[headerRowIndex, headerColumnIndex].Value = column;
                headerColumnIndex++;
            }

            workSheetTable.Column(1).Width = 38;
            workSheetTable.Column(2).Width = 22;
            workSheetTable.Column(3).Width = 26;
            workSheetTable.Column(4).Width = 20;
            workSheetTable.Column(5).Width = 26;
            workSheetTable.Column(6).Width = 20;
            workSheetTable.Column(7).Width = 26;
            workSheetTable.Column(8).Width = 35;
            workSheetTable.Column(9).Width = 30;

            CustomBestFitColumn(workSheetTable.Column(8), 15, 35);
            CustomBestFitColumn(workSheetTable.Column(4), 40, 80);

            var headerRow = workSheetTable.Cells[$"A{headerRowIndex}:I${headerRowIndex}"];
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

        private void CustomBestFitColumn(ExcelColumn column, int minWidth, int maxWidth)
        {
            column.AutoFit();
            column.Width = Math.Min(column.Width, maxWidth);
            column.Width = Math.Max(column.Width, minWidth);
        }

        private string GetMemberNames(List<string> memberIds)
        {
            var securityContext = _securityContextProvider.GetSecurityContext();
            var removeRoleList = new List<string> { RoleNames.Admin, RoleNames.SystemAdmin, RoleNames.TaskController };
            var userRoleList = new List<string>
                { RoleNames.PowerUser, RoleNames.Leitung, RoleNames.MpaGroup1, RoleNames.MpaGroup2 };
            string memberNames = "";
            var userList = new List<PraxisUser>();

            var filter = Builders<PraxisUser>.Filter.In(member => member.ItemId, memberIds.ToArray());
            var results = _mongoClientRepository.GetCollection<PraxisUser>().Find(filter).ToList();
            if (results != null && results.Count > 0)
            {
                if (securityContext.Roles.Intersect(userRoleList).Any())
                {
                    foreach (var user in results)
                    {
                        if (!user.Roles.Intersect(removeRoleList).Any())
                        {
                            userList.Add(user);
                        }
                    }
                }

                memberNames = userList.Any()
                    ? string.Join(", ", userList.Select(user => user.DisplayName.ToString()))
                    : string.Join(", ", results.Select(user => user.DisplayName.ToString()));
            }

            return memberNames;
        }
    }
}