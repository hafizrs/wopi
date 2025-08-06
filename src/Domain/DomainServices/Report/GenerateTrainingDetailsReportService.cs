using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.Training;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.Reports;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using Selise.Ecap.SC.PraxisMonitor.Contracts.MongoDb;
using Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.Services;

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using static Selise.Ecap.SC.PraxisMonitor.Contracts.Constants.ReportConstants;
using GetTrainingAnswersQuery = Selise.Ecap.SC.PraxisMonitor.Contracts.Queries.GetTrainingAnswersQuery;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.Report
{
    public class GenerateTrainingDetailsReportService : IGenerateTrainingDetailsReport
    {
        private const string ReportListSheetName = "Reporting List";
        private readonly ILogger<GenerateTrainingDetailsReportService> _logger;
        private readonly IMongoClientRepository _mongoClientRepository;
        private readonly IPraxisTrainingAnswerService _praxisTrainingAnswerService;
        private readonly IPraxisReportService _praxisReportService;

        public GenerateTrainingDetailsReportService(
            IPraxisTrainingAnswerService praxisTrainingAnswerService,
            ILogger<GenerateTrainingDetailsReportService> logger,
            IMongoClientRepository mongoClientRepository,
            IPraxisReportService praxisReportService
        )
        {
            _praxisTrainingAnswerService = praxisTrainingAnswerService;
            _logger = logger;
            _mongoClientRepository = mongoClientRepository;
            _praxisReportService = praxisReportService;
        }

        public async Task<bool> PrepareTrainingDetailsReport(string filter, PraxisClient client,
            PraxisTraining training, ExcelPackage excel, TrainingDetailsTranslation translation)
        {
            var monitoringListRowIndex = 9;
            const int logoColumnPosition = 3;
            var reportDateString = DateTime.Today.ToString("dd.MM.yyyy");

            WriteHeaderForTrainingDetailsReport(client, training, excel, translation.TRAINING_STATISTIC_REPORT,
                ReportListSheetName, reportDateString, logoColumnPosition, 2, translation);

            var trainingAnswers = (await _praxisTrainingAnswerService.GetPraxisTrainingAnswerWithAssignedMembers(
                new GetTrainingAnswersQuery(training.ItemId, true, "{CreateDate: -1}")
            ))[training.ItemId].TrainingAnswers.ToList();

            WriteTrainingDetailsExcelReport(trainingAnswers, training, excel, ReportListSheetName,
                ref monitoringListRowIndex, translation);
            AddReportBorderLine(excel, monitoringListRowIndex, ReportListSheetName, 3);
            return true;
        }


        private void WriteHeaderForTrainingDetailsReport(PraxisClient client, PraxisTraining training,
            ExcelPackage excel, string reportName, string reportSheetName, string reportDateString, int logoPosition,
            int logoSize, TrainingDetailsTranslation translation)
        {
            try
            {
                excel.Workbook.Worksheets.Add(reportSheetName);

                // Target a worksheet
                var workSheetTable = excel.Workbook.Worksheets[reportSheetName];

                workSheetTable.Cells[1, 1].Value = translation.REPORT_NAME;
                workSheetTable.Cells[2, 1].Value = translation.DATE;
                workSheetTable.Cells[3, 1].Value = translation.ORGANIZATION;
                workSheetTable.Cells[4, 1].Value = translation.TRAINING_MODULE;
                workSheetTable.Cells[5, 1].Value = translation.NAME;
                workSheetTable.Cells[6, 1].Value = translation.CRITERIA_OF_COMPLETION;
                workSheetTable.Cells[7, 1].Value = translation.SUBMISSION_DATE;

                workSheetTable.Cells["A1:A7"].Style.Font.Bold = true;
                // [firstRow, firstColumn, lastRow, LastColumn]
                workSheetTable.Cells[1, 1, 7, 2].Style.Border.Top.Style = ExcelBorderStyle.Thin;
                workSheetTable.Cells[1, 1, 7, 2].Style.Border.Left.Style = ExcelBorderStyle.Thin;
                workSheetTable.Cells[1, 1, 7, 2].Style.Border.Right.Style = ExcelBorderStyle.Thin;
                workSheetTable.Cells[1, 1, 7, 2].Style.Border.Bottom.Style = ExcelBorderStyle.Thin;

                workSheetTable.Cells["B1"].Value = reportName;
                workSheetTable.Cells["B1"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                workSheetTable.Cells["B2"].Value = reportDateString;
                workSheetTable.Cells["B2"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                workSheetTable.Cells["B3"].Value = client.ClientName;
                workSheetTable.Cells["B3"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                workSheetTable.Cells["B4"].Value = training.FormTitle;
                workSheetTable.Cells["B4"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                workSheetTable.Cells["B5"].Value = training.Title;
                workSheetTable.Cells["B5"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                workSheetTable.Cells["B6"].Value = training.Qualification;
                workSheetTable.Cells["B6"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                workSheetTable.Cells["B7"].Value = training.DueDate.ToString("dd.MM.yyyy");
                workSheetTable.Cells["B7"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                workSheetTable.Cells[1, logoPosition, 2, logoPosition].Merge = true;

                _praxisReportService.AddLogoInExcelReport(workSheetTable, logoSize, logoPosition, rqLatestLogo, 25);

                const int headerRowIndex = 8;
                var headerColumnIndex = 1;
                var headerColumns = new List<string>
                {
                    translation.NAME_OF_RESPONDENT,
                    translation.RESPONSE_SCORE,
                    translation.STATUS
                };

                foreach (var column in headerColumns)
                {
                    var headerCell = workSheetTable.Cells[headerRowIndex, headerColumnIndex];
                    headerCell.Value = column;
                    headerCell.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                    headerColumnIndex++;
                }

                workSheetTable.Column(1).Width = 45;
                workSheetTable.Column(2).Width = 45;
                workSheetTable.Column(3).Width = 20;

                var headerRow = workSheetTable.Cells[$"A{headerRowIndex}:C{headerRowIndex}"];
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
            catch (Exception ex)
            {
                _logger.LogError("WriteHeader got error: {Message}", ex.Message);
            }
        }

        private void WriteTrainingDetailsExcelReport(List<PraxisTrainingAnswer> dataset, PraxisTraining training,
            ExcelPackage excel, string reportSheetName, ref int rowIndex,
            TrainingDetailsTranslation trainingDetailsReportTranslation)
        {
            try
            {
                var workSheetTable = excel.Workbook.Worksheets[reportSheetName];

                var assignedToIds = new List<string>();

                if (training.SpecificControllingMembers.Any())
                    assignedToIds.AddRange(training.SpecificControllingMembers);
                if (training.SpecificControlledMembers.Any())
                    assignedToIds.AddRange(training.SpecificControlledMembers);

                if (dataset.Any()) assignedToIds.AddRange(dataset.Select(trainingAnswer => trainingAnswer.PersonId));

                var assignMembers = GetMemberInfos(assignedToIds.Distinct().ToList());

                foreach (var assignMember in assignMembers)
                {
                    workSheetTable.Cells[rowIndex, 3].Style.Border.Right.Style = ExcelBorderStyle.Thin;

                    try
                    {
                        workSheetTable.Cells[rowIndex, 1].Value = assignMember.DisplayName;
                        workSheetTable.Cells[rowIndex, 1].Style.WrapText = true;
                        workSheetTable.Cells[rowIndex, 1].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        workSheetTable.Cells[rowIndex, 1].Style.Border.BorderAround(ExcelBorderStyle.Thin);

                        if (dataset.Count > 0 && dataset.Any(d => d.PersonId == assignMember._id))
                        {
                            var answerResults = dataset.Where(d => d.PersonId == assignMember._id).ToList();
                            var responses = answerResults.Select(trainingAnswer =>
                                $"{trainingDetailsReportTranslation.SUBMISSION}: {trainingAnswer.Score}%"
                            ).ToList();

                            workSheetTable.Cells[rowIndex, 2].Value = string.Join('\n', responses);
                            workSheetTable.Cells[rowIndex, 2].Style.WrapText = true;
                            workSheetTable.Cells[rowIndex, 2].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                            workSheetTable.Cells[rowIndex, 2].Style.Border.BorderAround(ExcelBorderStyle.Thin);

                            workSheetTable.Row(rowIndex).Height = 15 * responses.Count;
                            workSheetTable.Cells[rowIndex, 3].Value =
                                answerResults.Any(a => a.Score >= training.Qualification)
                                    ? trainingDetailsReportTranslation.COMPLETE
                                    : trainingDetailsReportTranslation.PENDING;
                            workSheetTable.Cells[rowIndex, 3].Style.WrapText = true;
                            workSheetTable.Cells[rowIndex, 3].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                            workSheetTable.Cells[rowIndex, 3].Style.Border.BorderAround(ExcelBorderStyle.Thin);
                        }
                        else
                        {
                            workSheetTable.Cells[rowIndex, 2].Value = string.Empty;
                            workSheetTable.Cells[rowIndex, 2].Style.WrapText = true;
                            workSheetTable.Cells[rowIndex, 2].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                            workSheetTable.Cells[rowIndex, 2].Style.Border.BorderAround(ExcelBorderStyle.Thin);

                            workSheetTable.Cells[rowIndex, 3].Value = trainingDetailsReportTranslation.PENDING;
                            workSheetTable.Cells[rowIndex, 3].Style.WrapText = true;
                            workSheetTable.Cells[rowIndex, 3].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                            workSheetTable.Cells[rowIndex, 3].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                            workSheetTable.Cells[rowIndex, 3].Style.Border.BorderAround(ExcelBorderStyle.Thin);
                        }

                        for (var i = 1; i <= 3; i++)
                            workSheetTable.Cells[rowIndex, i].Style.VerticalAlignment = ExcelVerticalAlignment.Top;
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

        private static void AddReportBorderLine(ExcelPackage excel, int rowIndex, string reportSheetName,
            int totalColumn)
        {
            var workSheetTable = excel.Workbook.Worksheets[reportSheetName];

            for (var i = 1; i <= totalColumn; i++)
                workSheetTable.Cells[rowIndex - 1, i].Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
        }

        private List<FilterPraxisUser> GetMemberInfos(List<string> memberIds)
        {
            var projection = Builders<PraxisUser>.Projection
                .Include("_id")
                .Include("DisplayName");
            var filter = Builders<PraxisUser>.Filter.In(member => member.ItemId, memberIds.ToArray());
            var results = _mongoClientRepository.GetCollection<PraxisUser>().Find(filter).Project(projection).ToList();

            var dataResultList = BsonSerializer.Deserialize<List<FilterPraxisUser>>(results.ToJson());

            return dataResultList;
        }
    }
}