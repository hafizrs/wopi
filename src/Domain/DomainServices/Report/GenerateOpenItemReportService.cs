using Microsoft.Extensions.Logging;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.OpenItem;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.Reports;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static Selise.Ecap.SC.PraxisMonitor.Contracts.Constants.ReportConstants;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.Report
{
    public class GenerateOpenItemReportService : IGenerateOpenItemReport
    {
        private readonly IPraxisOpenItemService _praxisOpenItemService;
        private readonly ILogger<GenerateOpenItemReportService> _logger;
        private readonly IRepository _repository;
        private readonly IPraxisReportService _praxisReportService;
        private const string OpenItemSheetName = "To-Do Reports";

        public GenerateOpenItemReportService(
            IPraxisOpenItemService praxisOpenItemService,
            ILogger<GenerateOpenItemReportService> logger,
            IPraxisReportService praxisReportService,
            IRepository repository
        )
        {
            _praxisOpenItemService = praxisOpenItemService;
            _logger = logger;
            _praxisReportService = praxisReportService;
            _repository = repository;
        }

        public async Task<bool> PrepareOpenItemReport(string filter, bool enableDateRange, DateTime? startDate,
            DateTime? endDate, PraxisClient client, ExcelPackage excel, TranslationOpenItem translation, int timezoneOffsetInMinutes)
        {
            int openItemReportRowIndex = 5;
            int logoColumnPosition = 12;
            string reportDateString = DateTime.Today.ToString("dd.MM.yyyy");

            if (enableDateRange && startDate != null && endDate != null)
            {
                reportDateString = $"{startDate.Value:dd.MM.yyyy} - {endDate.Value:dd.MM.yyyy}";
            }

            WriteHeaderForOpenItem(client, excel, translation.TO_DO_REPORT, OpenItemSheetName, reportDateString,
                logoColumnPosition, 2, translation.REPORT_NAME, translation.DATE, translation.ORGANIZATION);

            var dataset = await _praxisOpenItemService.GetPraxisOpenReportData(filter, "{CreateDate: -1}");

            int page = 0, pageSize = 100, totalPage = (int)Math.Ceiling((decimal)dataset.TotalRecordCount / pageSize);
            if (totalPage != 0)
            {
                for (int i = 0; i < totalPage; i++)
                {
                    var results = dataset.Results.Skip(page * pageSize).Take(pageSize).ToList();
                    WriteOpenItemExcelReport(results, excel, OpenItemSheetName, ref openItemReportRowIndex, translation, timezoneOffsetInMinutes);
                    page++;
                }
            }
            else
            {
                var results = dataset.Results/*.Skip(page * pageSize)*/.Take(pageSize).ToList();
                WriteOpenItemExcelReport(results, excel, OpenItemSheetName, ref openItemReportRowIndex, translation, timezoneOffsetInMinutes);
            }

            SetDataListHeader(excel.Workbook.Worksheets[OpenItemSheetName], translation);

            return true;
        }

        private void WriteHeaderForOpenItem(PraxisClient client, ExcelPackage excel, string reportName,
            string reportSheetName, string reportDateString, int logoPosition, int logoSize, string reportNameHeader,
            string dateHeader, string organizationHeader)
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
                workSheetTable.Cells["B3:C3"].Value = client.ClientName.ToString();
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

        private void WriteOpenItemExcelReport(
            List<PraxisOpenItem> dataset,
            ExcelPackage excel,
            string reportSheetName,
            ref int rowIndex,
            TranslationOpenItem translation,
            int timezoneOffsetInMinutes
        )
        {
            try
            {

                var workSheetTable = excel.Workbook.Worksheets[reportSheetName];

                foreach (PraxisOpenItem openItem in dataset)
                {
                    var columnIndex = 1;
                    try
                    {
                        var budget = translation.ACTUAL_BUDGET + ": " + openItem.ActualBudget.ToString("F") + "\n" +
                                     translation.BUDGET + ": " + openItem.PlannedBudget.ToString("F");

                        var remarks = Regex.Replace(openItem.Remarks.Trim(), "<.*?>", string.Empty).Replace("\t", "");
                        var notCompletedMembers = string.Join(",", _praxisOpenItemService.GetNotCompletedMembers(openItem.ItemId, openItem.ControlledMembers.ToList()));

                        workSheetTable.Cells[rowIndex, columnIndex++].Value = openItem.TaskReference.Value;
                        workSheetTable.Cells[rowIndex, columnIndex++].Value = openItem.CategoryName;
                        workSheetTable.Cells[rowIndex, columnIndex++].Value = openItem.SubCategoryName;
                        workSheetTable.Cells[rowIndex, columnIndex++].Value = openItem.Title;
                        workSheetTable.Cells[rowIndex, columnIndex++].Value = GetMemberNames(openItem);
                        workSheetTable.Cells[rowIndex, columnIndex++].Value = !(openItem.ControlledGroups?.Count() > 0)
                            ? "-"
                            : string.Join(", ", openItem.ControlledGroups);
                        workSheetTable.Cells[rowIndex, columnIndex++].Value = openItem.ResponseByAllMember ?? false
                            ? translation.RESPONSE_BY_ALL_MEMBER
                            : translation.RESPONSE_BY_SINGLE_MEMBER;
                        workSheetTable.Cells[rowIndex, columnIndex++].Value = openItem.TaskSchedule.TaskDateTime.ToString("dd.MM.yyyy");
                        workSheetTable.Cells[rowIndex, columnIndex++].Value = budget;
                        workSheetTable.Cells[rowIndex, columnIndex++].Value = remarks;
                        workSheetTable.Cells[rowIndex, columnIndex++].Value = notCompletedMembers;
                        if (!openItem.OverAllCompletionStatus.Key.Equals("pending"))
                        {
                            workSheetTable.Cells[rowIndex, columnIndex++].Value = translation.OPEN;
                            columnIndex++;
                        }
                        else
                        {
                            workSheetTable.Cells[rowIndex, columnIndex++].Value = translation.CLOSED;

                            workSheetTable.Cells[rowIndex, columnIndex++].Value =
                                GetOpenItemCompletionDate(openItem.ItemId)
                                    .AddMinutes(timezoneOffsetInMinutes)
                                    .ToString("dd.MM.yyyy, h:mm tt");
                        }

                        for (int i = 1; i < columnIndex; i++)
                        {
                            workSheetTable.Cells[rowIndex, i].Style.WrapText = true;
                            workSheetTable.Cells[rowIndex, i].Style.Border.BorderAround(ExcelBorderStyle.Thin);
                            workSheetTable.Cells[rowIndex, i].Style.VerticalAlignment = ExcelVerticalAlignment.Top;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Write got error with row number: {RowIndex}", rowIndex);
                    }

                    rowIndex++;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Write got error with row number: {RowNumber}. Error message: {Message}", rowIndex, ex.Message);
            }
        }

        private string GetMemberNames(PraxisOpenItem openItem)
        {
            var controlledMembers = _praxisOpenItemService.GetAggregatedControlledMembers(openItem);
            string memberNames = "";
            var praxisUsers = _repository.GetItems<PraxisUser>(praxisUser => 
                controlledMembers.Contains(praxisUser.ItemId)).ToList();

            if (praxisUsers.Count > 0)
            {
                memberNames = string.Join("\n", praxisUsers.Select(user => user.DisplayName.ToString()));
            }

            return memberNames;
        }

        private void SetDataListHeader(ExcelWorksheet workSheetTable, TranslationOpenItem translation)
        {
            var headerRowIndex = 4;
            var headerColumnIndex = 1;

            List<string> headerColumns = new List<string>
                {
                   translation.TASK_TYPE,// "Task Type",
                   translation.CATEGORY, // "Category",
                   translation.SUB_CATEGORY,// "Sub category",
                   translation.TASK,// "Task",
                   translation.RESPONSIBLE_PERSON,// "Responsible Person",
                   translation.RESPONSIBLE_GROUP, // "Responsible Group",
                   translation.COMPLETION_CRITERIA,// "Completion Criteria"
                   translation.DUE_DATE,// "Due Date",
                   translation.BUDGET,// "Budget",
                   translation.REMARKS,// "Remarks",
                   translation.PENDING,// "Pending",
                   translation.OPEN_OR_CLOSED,//  "Open/Completed",
                   translation.COMPLETION_TIME//Completion Time
                };

            foreach (var column in headerColumns)
            {
                workSheetTable.Cells[headerRowIndex, headerColumnIndex].Value = column;
                headerColumnIndex++;
            }

            var columnIndex = 1;
            workSheetTable.Column(columnIndex++).Width = 38;
            workSheetTable.Column(columnIndex++).Width = 22;
            workSheetTable.Column(columnIndex++).Width = 26;
            workSheetTable.Column(columnIndex++).Width = 50;
            workSheetTable.Column(columnIndex++).Width = 32;
            workSheetTable.Column(columnIndex++).Width = 32;
            workSheetTable.Column(columnIndex++).Width = 30;
            workSheetTable.Column(columnIndex++).Width = 30;
            workSheetTable.Column(columnIndex++).Width = 30;
            workSheetTable.Column(columnIndex++).Width = 32;
            workSheetTable.Column(columnIndex++).Width = 34;
            workSheetTable.Column(columnIndex++).Width = 20;

            CustomBestFitColumn(workSheetTable.Column(columnIndex), 25, 35);

            var headerRow = workSheetTable.Cells[headerRowIndex, 1, headerRowIndex, columnIndex];

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

        private DateTime GetOpenItemCompletionDate(string praxisOpenItemId)
        {
            var openItemCompletionInfos = _repository.GetItems<PraxisOpenItemCompletionInfo>(
                completionInfo => completionInfo.PraxisOpenItemId.Equals(praxisOpenItemId) &&
                completionInfo.Completion != null && completionInfo.Completion.Key.Equals("done")
            ).ToList();
            if (openItemCompletionInfos.Count > 1)
            {
                openItemCompletionInfos = openItemCompletionInfos.OrderByDescending(
                    completionInfo => new DateTime(
                        Math.Max(completionInfo.ApprovedTime.Ticks, completionInfo.ReportedTime.Ticks)
                    )
                ).ToList();
            }

            var latestCompletionInfo = openItemCompletionInfos[0];
            return new DateTime(
                Math.Max(latestCompletionInfo.ApprovedTime.Ticks, latestCompletionInfo.ReportedTime.Ticks)
            );
        }

        private void CustomBestFitColumn(ExcelColumn column, int minWidth, int maxWidth)
        {
            column.AutoFit(minWidth, maxWidth);
        }
    }
}
