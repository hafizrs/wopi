using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using Selise.Ecap.Entities.PrimaryEntities.PlatformDataService;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.Task;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.Reports;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using static Selise.Ecap.SC.PraxisMonitor.Contracts.Constants.ReportConstants;
using static Selise.Ecap.SC.PraxisMonitor.Contracts.Models.Enum;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.Report
{
    public class GenerateDistinctTaskListReportService : IGenerateDistinctTaskListReport
    {
        private readonly IPraxisTaskService _praxisTaskService;
        private readonly ILogger<GenerateDistinctTaskListReportService> _logger;
        private readonly IUilmResourceKeyService _uilmResourceKeyService;
        private Dictionary<string, string> _translatedStringsAsDictionary = new Dictionary<string, string>();
        private readonly IPraxisReportService _praxisReportService;
        private const string TaskListSheetName = "Task List";
        private readonly IRepository _repository;
        private readonly IBlocksMongoDbDataContextProvider _ecapMongoDbDataContextProvider;

        public GenerateDistinctTaskListReportService(
            IPraxisTaskService praxisTaskService,
            IUilmResourceKeyService uilmResourceKeyService,
            IPraxisReportService praxisReportService,
            ILogger<GenerateDistinctTaskListReportService> logger,
            IRepository repository,
            IBlocksMongoDbDataContextProvider ecapMongoDbDataContextProvider)
        {
            _praxisTaskService = praxisTaskService;
            _uilmResourceKeyService = uilmResourceKeyService;
            _logger = logger;
            _repository = repository;
            _ecapMongoDbDataContextProvider = ecapMongoDbDataContextProvider;
            _praxisReportService = praxisReportService;
        }


        public async Task<bool> PrepareDistinctTaskListReport(PraxisClient client, ExcelPackage excel, string filter,
            TranslationDistinctTaskList translationDistinctTaskList)
        {
            _translatedStringsAsDictionary = _uilmResourceKeyService.GetResourceValueByKeyName(
                ScheduleRecurrenceTranslationKeys.Concat(RepeatEveryTranslationKeys)
                    .Concat(RepeatEveryMonthlyTranslationKeys).Concat(WeekDaysTranslationKeys).Concat(BooleanTranslationKeys).ToList()
            );

            int taskListRowIndex = 5;
            WriteHearderForDistinctTaskListReport(client, excel, translationDistinctTaskList.DISTINCT_TASK_REPORT,
                TaskListSheetName, DateTime.Today.ToString("dd.MM.yyyy"), 12, 2, translationDistinctTaskList.REPORT_NAME,
                translationDistinctTaskList.DATE, translationDistinctTaskList.ORGANIZATION);

            var dataset = await _praxisTaskService.GetDistictTaskListReportData(filter, "{CreateDate: -1}");

            int page = 0, pageSize = 100, totalPage = (int)Math.Ceiling((decimal)dataset.TotalRecordCount / pageSize);
            if (totalPage != 0)
            {
                for (int i = 0; i < totalPage; i++)
                {
                    var results = dataset.Results.Skip(page * pageSize).Take(pageSize).ToList();
                    WriteTaskListExcelReport(results, excel, TaskListSheetName, ref taskListRowIndex,
                        translationDistinctTaskList);
                    page++;
                }
            }
            else
            {
                var results = dataset.Results.Skip(page * pageSize).Take(pageSize).ToList();
                WriteTaskListExcelReport(results, excel, TaskListSheetName, ref taskListRowIndex,
                    translationDistinctTaskList);
            }

            AddReportBorderLine(excel, taskListRowIndex, TaskListSheetName, 9);
            return true;
        }

        private void WriteHearderForDistinctTaskListReport(PraxisClient client, ExcelPackage excel, string reportName,
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

                _praxisReportService.AddLogoInExcelReport(workSheetTable, logoSize, logoPosition, rqLatestLogo);
            }
            catch (Exception ex)
            {
                _logger.LogError("WriteHeader got error: {Message}", ex.Message);
            }
        }

        private void WriteTaskListExcelReport(List<DistinctPraxisTaskDto> dataset, ExcelPackage excel,
            string reportSheetName, ref int rowIndex, TranslationDistinctTaskList translationDistinctTaskList)
        {
            try
            {
                var workSheetTable = excel.Workbook.Worksheets[reportSheetName];

                DataListHeader(workSheetTable, translationDistinctTaskList);


                foreach (DistinctPraxisTaskDto data in dataset)
                {
                    workSheetTable.Cells[rowIndex, 12].Style.Border.Right.Style = ExcelBorderStyle.Thin;

                    try
                    {
                        workSheetTable.Cells[rowIndex, 1].Value = data.CategoryName;
                        workSheetTable.Cells[rowIndex, 1].Style.WrapText = true;
                        workSheetTable.Cells[rowIndex, 1].Style.Border.BorderAround(ExcelBorderStyle.Thin);

                        workSheetTable.Cells[rowIndex, 2].Value = data.SubCategoryName;
                        workSheetTable.Cells[rowIndex, 2].Style.WrapText = true;
                        workSheetTable.Cells[rowIndex, 2].Style.Border.BorderAround(ExcelBorderStyle.Thin);

                        workSheetTable.Cells[rowIndex, 3].Value = data.Title;
                        workSheetTable.Cells[rowIndex, 3].Style.WrapText = true;
                        workSheetTable.Cells[rowIndex, 3].Style.Border.BorderAround(ExcelBorderStyle.Thin);

                        var avoidWeekEnd = data.TaskSummary.AvoidWeekEnd ? _translatedStringsAsDictionary["YES"] : _translatedStringsAsDictionary["NO"];

                        workSheetTable.Cells[rowIndex, 4].Value = avoidWeekEnd;
                        workSheetTable.Cells[rowIndex, 4].Style.WrapText = true;
                        workSheetTable.Cells[rowIndex, 4].Style.Border.BorderAround(ExcelBorderStyle.Thin);

                        var moveUncheckedTask = data.TaskSummary.IsMoveUncheckedTask ? _translatedStringsAsDictionary["YES"] : _translatedStringsAsDictionary["NO"];

                        workSheetTable.Cells[rowIndex, 5].Value = moveUncheckedTask;
                        workSheetTable.Cells[rowIndex, 5].Style.WrapText = true;
                        workSheetTable.Cells[rowIndex, 5].Style.Border.BorderAround(ExcelBorderStyle.Thin);

                        var taskRescheduled = data.TaskSummary.IsTaskReScheduled ? _translatedStringsAsDictionary["YES"] : _translatedStringsAsDictionary["NO"];

                        workSheetTable.Cells[rowIndex, 6].Value = taskRescheduled;
                        workSheetTable.Cells[rowIndex, 6].Style.WrapText = true;
                        workSheetTable.Cells[rowIndex, 6].Style.Border.BorderAround(ExcelBorderStyle.Thin);

                        var emailNotificationInfo = GetEmailNotificationInfo(data.TaskConfigId);

                        workSheetTable.Cells[rowIndex, 7].Value = string.Join('\n', emailNotificationInfo);
                        workSheetTable.Cells[rowIndex, 7].Style.WrapText = true;
                        workSheetTable.Cells[rowIndex, 7].Style.Border.BorderAround(ExcelBorderStyle.Thin);

                        workSheetTable.Cells[rowIndex, 8].Value = data.TaskSummary.StartsOnDate.HasValue ? data.TaskSummary.StartsOnDate.Value.ToString("dd.MM.yyyy") : "-";
                        workSheetTable.Cells[rowIndex, 8].Style.Border.BorderAround(ExcelBorderStyle.Thin);

                        workSheetTable.Cells[rowIndex, 9].Value = GetScheduleAndRecurrenceTranslation(
                            System.Enum.GetName(typeof(RepeatTypeEnums), data.TaskSummary.RepeatType)?.ToUpper()
                        );
                        workSheetTable.Cells[rowIndex, 9].Style.WrapText = true;
                        workSheetTable.Cells[rowIndex, 9].Style.Border.BorderAround(ExcelBorderStyle.Thin);

                        workSheetTable.Cells[rowIndex, 10].Value = GetRepeatValueOfTask(data);
                        workSheetTable.Cells[rowIndex, 10].Style.WrapText = true;
                        workSheetTable.Cells[rowIndex, 10].Style.Border.BorderAround(ExcelBorderStyle.Thin);

                        workSheetTable.Cells[rowIndex, 11].Value = data.TaskSummary.Status
                            ? translationDistinctTaskList.ACTIVE
                            : translationDistinctTaskList.INACTIVE;
                        workSheetTable.Cells[rowIndex, 11].Style.WrapText = true;
                        workSheetTable.Cells[rowIndex, 11].Style.Border.BorderAround(ExcelBorderStyle.Thin);

                        workSheetTable.Cells[rowIndex, 12].Value = (data.TaskSummary.RepeatEndDate.HasValue)
                            ? data.TaskSummary.RepeatEndDate.Value.ToString("dd.MM.yyyy")
                            : "-";
                        workSheetTable.Cells[rowIndex, 12].Style.Border.BorderAround(ExcelBorderStyle.Thin);

                        for (int i = 1; i <= 12; i++)
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

        private static void AddReportBorderLine(ExcelPackage excel, int rowIndex, string reportSheetName,
            int totalColumn)
        {
            var workSheetTable = excel.Workbook.Worksheets[reportSheetName];

            for (int i = 1; i <= totalColumn; i++)
            {
                workSheetTable.Cells[rowIndex - 1, i].Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
            }
        }

        private string GetRepeatValueOfTask(DistinctPraxisTaskDto dto)
        {
            var repeatValueString = "-";
            var taskSummary = dto.TaskSummary;

            switch (taskSummary.RepeatType)
            {
                case (int)RepeatTypeEnums.Daily:
                    repeatValueString = GetScheduleAndRecurrenceTranslation($"{taskSummary.RepeatValue}_DAY{(taskSummary.RepeatValue > 1 ? "S" : "")}");
                    break;

                case (int)RepeatTypeEnums.Weekly:
                    repeatValueString = string.Join(", ", taskSummary.RepeatOnDayOfWeeks
                        .Select(day => GetScheduleAndRecurrenceTranslation(
                            System.Enum.GetName(typeof(WeekDaysEnums), day)?.ToUpper())
                        ));
                    break;

                case (int)RepeatTypeEnums.Monthly:
                    repeatValueString = string.Join(", ",
                        taskSummary.RepeatingDates.Select(date => date.Day.ToString("D2")));
                    break;

                case (int)RepeatTypeEnums.Yearly:
                    repeatValueString = string.Join(", ",
                        taskSummary.RepeatingDates.Select(date =>
                            date.Day.ToString("D2") + "." + date.Month.ToString("D2")));
                    break;
            }

            return repeatValueString;
        }

        private void DataListHeader(ExcelWorksheet workSheetTable,
            TranslationDistinctTaskList translationDistinctTaskList)
        {
            var headerRowIndex = 4;
            var headerColumnIndex = 1;

            List<string> headerColumns = new List<string>
            {
                translationDistinctTaskList.CATEGORY, // "Category",
                translationDistinctTaskList.SUB_CATEGORY, // "Sub Category",
                translationDistinctTaskList.TASK, // "Task",
                translationDistinctTaskList.AVOID_WEEKENDS, // "Avoid Weekend",
                translationDistinctTaskList.MOVE_TASK_TO_NEXT_WORKING_DAY_IF_RECURRENCE_IS_ON_WEEKEND, // "Move unchecked task",
                translationDistinctTaskList.TASK_RESCHEDULED_NOTIFICATION, // "Task rescheduled mail",
                translationDistinctTaskList.MAIL_NOTIFICATION, // "Mail Notification",
                //translationDistinctTaskList.NOTIFY_ON, // "Notify on",
                translationDistinctTaskList.START_DATE, // "Start Date",
                translationDistinctTaskList.RECURRENCE, //"Recurrence",
                translationDistinctTaskList.REPEAT, // "Repeat",
                translationDistinctTaskList.STATUS, // "Status",
                translationDistinctTaskList.END_DATE //   "End Date"
            };

            foreach (var column in headerColumns)
            {
                workSheetTable.Cells[headerRowIndex, headerColumnIndex].Value = column;
                headerColumnIndex++;
            }

            workSheetTable.Column(1).Width = 38;
            workSheetTable.Column(2).Width = 38;
            workSheetTable.Column(3).Width = 80;
            workSheetTable.Column(4).Width = 30;
            workSheetTable.Column(5).Width = 30;
            workSheetTable.Column(5).AutoFit();
            workSheetTable.Column(6).Width = 30;
            workSheetTable.Column(7).Width = 45;
            workSheetTable.Column(8).Width = 15;
            workSheetTable.Column(9).Width = 20;
            workSheetTable.Column(10).Width = 22;
            workSheetTable.Column(11).Width = 22;
            workSheetTable.Column(12).Width = 15;

            var headerRow = workSheetTable.Cells[$"A{headerRowIndex}:L{headerRowIndex}"];

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

        private string GetScheduleAndRecurrenceTranslation(string key)
        {
            return _translatedStringsAsDictionary.ContainsKey(key) && !string.IsNullOrEmpty(key)
                ? _translatedStringsAsDictionary[key]
                : _uilmResourceKeyService.GetResourceValueByKeyName(key);
        }

        private List<string> GetEmailNotificationInfo(string taskConfigId)
        {
            var emailNotificationInfo = new List<string>();
            var personRepo = _ecapMongoDbDataContextProvider.GetTenantDataContext().GetCollection<Person>("Persons");
            var taskConfig = _repository.GetItem<PraxisTaskConfig>(c => c.ItemId == taskConfigId && !c.IsMarkedToDelete);
            if (taskConfig != null)
            {
                if (taskConfig.TaskNotification.TaskNotFullFilled.IsEnable && taskConfig.TaskNotification.TaskNotFullFilled.Members.Any())
                {
                    var filter = Builders<Person>.Filter.In("_id", taskConfig.TaskNotification.TaskNotFullFilled.Members.ToArray()) & Builders<Person>.Filter.Eq("IsMarkedToDelete", false);
                    var userList = personRepo.Find(filter).ToList();
                    var displayNames = string.Join('\n', userList.Select(u => u.DisplayName).ToList());
                    emailNotificationInfo.Add($"Task not fulfilled ({Math.Abs(taskConfig.TaskFulfillmentPercentage)}%)- Assigned members:{Environment.NewLine}{displayNames}");
                }

                if (taskConfig.TaskNotification.TaskNotCompleted.Members.Any() && taskConfig.TaskNotification.TaskNotCompleted.IsEnable)
                {
                    var filter = Builders<Person>.Filter.In("_id", taskConfig.TaskNotification.TaskNotCompleted.Members.ToArray()) & Builders<Person>.Filter.Eq("IsMarkedToDelete", false);
                    var userList = personRepo.Find(filter).ToList();
                    var displayNames = string.Join('\n', userList.Select(u => u.DisplayName).ToList());
                    emailNotificationInfo.Add($"Task not checked- Assigned members:{Environment.NewLine}{displayNames}");
                }

                if (taskConfig.TaskNotification.TaskToNextDay.IsEnable && taskConfig.TaskNotification.TaskToNextDay.Members.Any())
                {
                    var filter = Builders<Person>.Filter.In("_id", taskConfig.TaskNotification.TaskToNextDay.Members.ToArray()) & Builders<Person>.Filter.Eq("IsMarkedToDelete", false);
                    var userList = personRepo.Find(filter).ToList();
                    var displayNames = string.Join('\n', userList.Select(u => u.DisplayName).ToList());
                    emailNotificationInfo.Add($"Task rescheduled- Assigned members:{Environment.NewLine}{displayNames}");
                }
            }
            if (!emailNotificationInfo.Any())
            {
                emailNotificationInfo.Add("--");
            }
            return emailNotificationInfo;
        }
    }
}