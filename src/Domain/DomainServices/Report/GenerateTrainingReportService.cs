using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using Selise.Ecap.Entities.PrimaryEntities.PlatformDataService;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.Training;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.Reports;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using static Selise.Ecap.SC.PraxisMonitor.Contracts.Constants.ReportConstants;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants;
using DnsClient;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Queries;
using Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.Services;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.Report
{
    public class GenerateTrainingReportService : IGenerateTrainingReport
    {
        private readonly IPraxisTrainingService _praxisTrainingService;
        private readonly ILogger<GenerateTrainingReportService> _logger;
        private readonly IUilmResourceKeyService _uilmResourceKeyService;
        private readonly IRepository _repository;
        private readonly IPraxisReportService _praxisReportService;
        private readonly IBlocksMongoDbDataContextProvider _mongoDbDataContextProvider;
        private const string ReportListSheetName = "Reporting List";
        private Dictionary<string, string> _translatedStringsAsDictionary = new Dictionary<string, string>();
        private readonly IPraxisTrainingAnswerService _praxisTrainingAnswerService;

        public GenerateTrainingReportService(
            IPraxisTrainingService praxisTrainingService,
            ILogger<GenerateTrainingReportService> logger,
            IUilmResourceKeyService uilmResourceKeyService,
            IPraxisReportService praxisReportService,
            IBlocksMongoDbDataContextProvider mongoDbDataContextProvider,
            IRepository repository,
            IPraxisTrainingAnswerService praxisTrainingAnswerService)
        {
            _praxisTrainingService = praxisTrainingService;
            _logger = logger;
            _uilmResourceKeyService = uilmResourceKeyService;
            _praxisReportService = praxisReportService;
            _mongoDbDataContextProvider = mongoDbDataContextProvider;
            _repository = repository;
            _praxisTrainingAnswerService = praxisTrainingAnswerService;
        }


        public async Task<bool> PrepareTrainingReport(string filter, PraxisClient client, ExcelPackage excel, TrainingReportTranslation translation)
        
        {
            _translatedStringsAsDictionary = _uilmResourceKeyService.GetResourceValueByKeyName(TopicTranslationKeys);

            int monitoringListRowIndex = 5;
            int LogoColumnPosition = 10;
            string reportDateString = DateTime.Today.ToString("dd.MM.yyyy");

            WriteHearderForTrainingReport(client, excel, translation.TRAINING_REPORT, ReportListSheetName,
                reportDateString, LogoColumnPosition, 2, translation.REPORT_NAME,
                translation.DATE, translation.ORGANIZATION);

            var dataset = await _praxisTrainingService.GetTrainingReportData(filter, "{CreateDate: -1}");

            int page = 0, pageSize = 100, totalPage = (int)Math.Ceiling((decimal)dataset.TotalRecordCount / pageSize);
            if (totalPage != 0)
            {
                for (int i = 0; i < totalPage; i++)
                {
                    var results = dataset.Results.Skip(page * pageSize).Take(pageSize).ToList();
                    WriteReportingTrainingExcelReport(results, excel, ReportListSheetName, ref monitoringListRowIndex, translation);
                    page++;
                }
            }
            else
            {
                var results = dataset.Results.Skip(page * pageSize).Take(pageSize).ToList();
                WriteReportingTrainingExcelReport(results, excel, ReportListSheetName, ref monitoringListRowIndex, translation);
            }

            AddReportBorderLine(excel, monitoringListRowIndex, ReportListSheetName, 10);
            return true;
        }

        private void WriteHearderForTrainingReport(PraxisClient client, ExcelPackage excel, string reportName,
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

                _praxisReportService.AddLogoInExcelReport(workSheetTable, logoSize, logoPosition, rqLatestLogo);
            }
            catch (Exception ex)
            {
                _logger.LogError("WriteHeader got error: {Message}", ex.Message);

            }
        }

        private void WriteReportingTrainingExcelReport(List<PraxisTraining> dataset, ExcelPackage excel,
            string reportSheetName, ref int rowIndex, TrainingReportTranslation trainingReportTranslation)
        {
            try
            {
                var praxisUserRepo = _mongoDbDataContextProvider.GetTenantDataContext().GetCollection<PraxisUser>("PraxisUsers");

                var workSheetTable = excel.Workbook.Worksheets[reportSheetName];
                var headerRowIndex = 4;
                var headerColumnIndex = 1;
                List<string> headerColumns = new List<string>
                {
                    trainingReportTranslation.NAME,
                    trainingReportTranslation.MAIN_GROUP,//
                    trainingReportTranslation.TOPIC,
                    trainingReportTranslation.ASSIGNED_TO,
                    trainingReportTranslation.COMPLETE,//
                    trainingReportTranslation.NUMBER_OF_ATTEMPTS,//
                    trainingReportTranslation.NOT_COMPLETED_YET,
                    trainingReportTranslation.ASSIGNED_ON,
                    trainingReportTranslation.DUE_DATE,
                    trainingReportTranslation.STATUS
                };

                foreach (var column in headerColumns)
                {
                    workSheetTable.Cells[headerRowIndex, headerColumnIndex].Value = column;
                    headerColumnIndex++;
                }
                workSheetTable.Column(1).Width = 30;
                workSheetTable.Column(2).Width = 30;//
                workSheetTable.Column(3).Width = 30;
                workSheetTable.Column(4).Width = 40;
                workSheetTable.Column(5).Width = 30;//
                workSheetTable.Column(6).Width = 30;//
                workSheetTable.Column(7).Width = 25;
                workSheetTable.Column(8).Width = 25;
                workSheetTable.Column(9).Width = 25;
                workSheetTable.Column(10).Width = 15;

                var headerRow = workSheetTable.Cells[$"A{headerRowIndex}:J{headerRowIndex}"];
                headerRow.Style.Font.Bold = true;
                headerRow.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                headerRow.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                headerRow.Style.Border.Right.Style = ExcelBorderStyle.Thin;
                headerRow.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                headerRow.Style.Fill.PatternType = ExcelFillStyle.Solid;
                headerRow.Style.Fill.BackgroundColor.SetColor(Color.LightGray);
                headerRow.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                headerRow.Style.VerticalAlignment = ExcelVerticalAlignment.Center;


                foreach (PraxisTraining data in dataset)
                {
                    var filter = Builders<PraxisUser>.Filter.Eq("ClientList.ClientId", data.ClientId) &
                    Builders<PraxisUser>.Filter.Not(Builders<PraxisUser>.Filter.AnyEq(pu => pu.Roles, RoleNames.AdminB)) &
                    Builders<PraxisUser>.Filter.In("ClientList.Roles", new[] { "mpa-group-1", "mpa-group-2" }) &
                    Builders<PraxisUser>.Filter.Eq("Active", true) &
                    Builders<PraxisUser>.Filter.Eq("IsMarkedToDelete", false);

                    var query = new GetTrainingAnswersQuery(data.ItemId, true, "{CreateDate: -1}");
                    var result = _praxisTrainingAnswerService.GetPraxisTrainingAnswerWithAssignedMembers(query).GetAwaiter().GetResult();

                    var assignedMembers = result[data.ItemId].AssignedMembers.ToList();
                    var answersSubmittedBy = result[data.ItemId].AnswersSubmittedBy.ToList();
                    var notPassedMembers = result[data.ItemId].NotPassedMembers.ToList();
                    var answersPendingBy = result[data.ItemId].AnswersPendingBy.ToList();
                    var passedMembers = assignedMembers
                        .Where(m => !notPassedMembers.Contains(m))
                        .ToList();

                    workSheetTable.Cells[rowIndex, 10].Style.Border.Right.Style = ExcelBorderStyle.Thin;

                    try
                    {
                        workSheetTable.Cells[rowIndex, 1].Value = data.Title;
                        workSheetTable.Cells[rowIndex, 1].Style.WrapText = true;
                        workSheetTable.Cells[rowIndex, 1].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        workSheetTable.Cells[rowIndex, 1].Style.Border.BorderAround(ExcelBorderStyle.Thin);

                        workSheetTable.Cells[rowIndex, 2].Value = data.FormTitle;
                        workSheetTable.Cells[rowIndex, 2].Style.WrapText = true;
                        workSheetTable.Cells[rowIndex, 2].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        workSheetTable.Cells[rowIndex, 2].Style.Border.BorderAround(ExcelBorderStyle.Thin);

                        workSheetTable.Cells[rowIndex, 3].Value = GetTopicTranslation(data.TopicValue);
                        workSheetTable.Cells[rowIndex, 3].Style.WrapText = true;
                        workSheetTable.Cells[rowIndex, 3].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        workSheetTable.Cells[rowIndex, 3].Style.Border.BorderAround(ExcelBorderStyle.Thin);

                        var assignedToPersons = GetPeronNameByIds(assignedMembers);
                        workSheetTable.Cells[rowIndex, 4].Value = assignedToPersons;
                        workSheetTable.Cells[rowIndex, 4].Style.WrapText = true;
                        workSheetTable.Cells[rowIndex, 4].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        workSheetTable.Cells[rowIndex, 4].Style.Border.BorderAround(ExcelBorderStyle.Thin);

                        var answerCompleted = GetPeronNameByIds(passedMembers);
                        workSheetTable.Cells[rowIndex, 5].Value = answerCompleted;
                        workSheetTable.Cells[rowIndex, 5].Style.WrapText = true;
                        workSheetTable.Cells[rowIndex, 5].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        workSheetTable.Cells[rowIndex, 5].Style.Border.BorderAround(ExcelBorderStyle.Thin);

                        var persons = _repository.GetItems<Person>(p => answersSubmittedBy.Contains(p.ItemId))
                            .ToDictionary(p => p.ItemId, p => p.DisplayName);

                        var answersSubmittedList = answersSubmittedBy
                            .GroupBy(id => id)
                            .Select(g => $"{persons[g.Key]} - {g.Count()}")
                            .ToList();

                        string numberOfAttempts = answersSubmittedList.Count == 1
                                ? answersSubmittedList.First()
                                : string.Join(",\n", answersSubmittedList);

                        workSheetTable.Cells[rowIndex, 6].Value = numberOfAttempts;
                        workSheetTable.Cells[rowIndex, 6].Style.WrapText = true;
                        workSheetTable.Cells[rowIndex, 6].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        workSheetTable.Cells[rowIndex, 6].Style.Border.BorderAround(ExcelBorderStyle.Thin);

                        var notCompleteYetIds = new List<string>();
                        notCompleteYetIds.AddRange(answersPendingBy);
                        notCompleteYetIds.AddRange(notPassedMembers);

                        workSheetTable.Cells[rowIndex, 7].Value = GetPeronNameByIds(notCompleteYetIds);
                        workSheetTable.Cells[rowIndex, 7].Style.WrapText = true;
                        workSheetTable.Cells[rowIndex, 7].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        workSheetTable.Cells[rowIndex, 7].Style.Border.BorderAround(ExcelBorderStyle.Thin);

                        workSheetTable.Cells[rowIndex, 8].Value = data.CreateDate.ToString("dd.MM.yyyy");
                        workSheetTable.Cells[rowIndex, 8].Style.WrapText = true;
                        workSheetTable.Cells[rowIndex, 8].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        workSheetTable.Cells[rowIndex, 8].Style.Border.BorderAround(ExcelBorderStyle.Thin);

                        workSheetTable.Cells[rowIndex, 9].Value = data.DueDate.ToString("dd.MM.yyyy");
                        workSheetTable.Cells[rowIndex, 9].Style.WrapText = true;
                        workSheetTable.Cells[rowIndex, 9].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        workSheetTable.Cells[rowIndex, 9].Style.Border.BorderAround(ExcelBorderStyle.Thin);

                        workSheetTable.Cells[rowIndex, 10].Value = data.IsActive
                            ? trainingReportTranslation.ACTIVE
                            : trainingReportTranslation.INACTIVE;
                        workSheetTable.Cells[rowIndex, 10].Style.WrapText = true;
                        workSheetTable.Cells[rowIndex, 10].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        workSheetTable.Cells[rowIndex, 10].Style.Border.BorderAround(ExcelBorderStyle.Thin);

                        for (int i = 1; i <= 11; i++)
                        {
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

        private void AddReportBorderLine(ExcelPackage excel, int rowIndex, string reportSheetName, int totalColumn)
        {
            var workSheetTable = excel.Workbook.Worksheets[reportSheetName];

            for (int i = 1; i <= totalColumn; i++)
            {
                workSheetTable.Cells[rowIndex - 1, i].Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
            }
        }

        private string GetPeronNameByIds(List<string> personIds)
        {
            var displayNameList = _repository.GetItems<Person>(p => personIds.Contains(p.ItemId))
                .Select(p => p.DisplayName).ToList();
            return string.Join('\n', displayNameList);
        }

        private string GetTopicTranslation(string key)
        {
            return _translatedStringsAsDictionary.ContainsKey(key) ? _translatedStringsAsDictionary[key] : key;
        }
    }
}
