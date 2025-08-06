using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.Reports;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.Reports.DeveloperReport;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using Selise.Ecap.SC.PraxisMonitor.Contracts.MongoDb;
using Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.Services;

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using static Selise.Ecap.SC.PraxisMonitor.Contracts.Constants.ReportConstants;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.Report.DeveloperReport
{
    public class GenerateDeveloperReportForAllData : IDeveloperReportGenerate
    {
        private readonly ILogger<GenerateDeveloperReportForAllData> _logger;
        private readonly IMongoClientRepository _mongoClientRepository;
        private readonly IUilmResourceKeyService _uilmResourceKeyService;
        private readonly IPraxisFormService _praxisFormService;
        private readonly IPraxisReportService _praxisReportService;
        private const string ReportListSheetName = "Reporting List";

        private Dictionary<string, string> _translatedStringsAsDictionary = new Dictionary<string, string>();

        public GenerateDeveloperReportForAllData(
            ILogger<GenerateDeveloperReportForAllData> logger,
            IMongoClientRepository mongoClientRepository,
            IUilmResourceKeyService uilmResourceKeyService,
            IPraxisReportService praxisReportService,
            IPraxisFormService praxisFormService)
        {
            _logger = logger;
            _mongoClientRepository = mongoClientRepository;
            _uilmResourceKeyService = uilmResourceKeyService;
            _praxisReportService = praxisReportService;
            _praxisFormService = praxisFormService;
        }

        public async Task<bool> GenerateReport(string filter, PraxisClient client, ExcelPackage excel, DeveloperReportTranslation developerReportTranslation, string reportFileId)
        {
            _translatedStringsAsDictionary = _uilmResourceKeyService.GetResourceValueByKeyName(
                TopicTranslationKeys.Concat(FormPurposeTranslationKeys).ToList()
            );
            var monitoringListRowIndex = PraxisDeveloperReport.HeaderRowIndexForAllDataReport + 1;
            int LogoColumnPosition = 6;
            string reportDateString = DateTime.Today.ToString("dd.MM.yyyy");

            WriteHeader(excel, developerReportTranslation.DEVELOPER_REPORT, ReportListSheetName,
                reportDateString, LogoColumnPosition, PraxisDeveloperReport.LogoSize, developerReportTranslation);

            var dataset = await _praxisFormService.GetDeveloperReportData(string.Empty, "{CreateDate: -1}");

            var clientIds = dataset.Results.SelectMany(
                praxisForm =>
                {
                    var formSpecificClientIds = new List<string> { praxisForm.ClientId };
                    formSpecificClientIds.AddRange(
                        (praxisForm.Clients ?? new List<ClientAssociated>()).Select(c => c.ClientId)
                    );
                    formSpecificClientIds.AddRange(
                        (praxisForm.ProcessGuideCheckList ?? new List<ClientSpecificCheckList>()).Select(
                            cl => cl.ClientId
                        )
                    );
                    formSpecificClientIds.AddRange(
                        (praxisForm.ProcessGuideCheckList ?? new List<ClientSpecificCheckList>()).Select(
                            cl => cl.ClientInfos
                        )
                        .Where(cl => cl != null)
                        .SelectMany(cl => cl.Select(c => c.ClientId))
                    );
                    return formSpecificClientIds;
                }
            );
            await _praxisReportService.AddClientIdsToPraxisReport(reportFileId, clientIds.Distinct());

            int page = 0, pageSize = 100, totalPage = (int)Math.Ceiling((decimal)dataset.TotalRecordCount / pageSize);
            if (totalPage != 0)
            {
                for (int i = 0; i < totalPage; i++)
                {
                    var results = dataset.Results.Skip(page * pageSize).Take(pageSize).ToList();
                    WriteReportingDeveloperExcelReport(results, excel, ReportListSheetName, ref monitoringListRowIndex);
                    page++;
                }
            }
            else
            {
                var results = dataset.Results.Skip(page * pageSize).Take(pageSize).ToList();
                WriteReportingDeveloperExcelReport(results, excel, ReportListSheetName, ref monitoringListRowIndex);
            }

            AddReportBorderLine(excel, monitoringListRowIndex, ReportListSheetName, 6);
            return true;
        }

        private static void AddReportBorderLine(ExcelPackage excel, int lastRowIndex, string reportSheetName, int totalColumn)
        {
            var workSheetTable = excel.Workbook.Worksheets[reportSheetName];

            for (int i = 1; i <= totalColumn; i++)
            {
                workSheetTable.Cells[lastRowIndex - 1, i].Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
            }
        }

        private void WriteHeader(ExcelPackage excel, string reportName, string reportSheetName, string reportDateString,
            int logoPosition, int logoSize, DeveloperReportTranslation developerReportTranslation)
        {
            try
            {
                excel.Workbook.Worksheets.Add(reportSheetName);

                // Target a worksheet
                var workSheetTable = excel.Workbook.Worksheets[reportSheetName];

                workSheetTable.Cells[1, 1].Value = developerReportTranslation.REPORT_NAME;
                workSheetTable.Cells[2, 1].Value = developerReportTranslation.DATE;

                workSheetTable.Cells["A1:A2"].Style.Font.Bold = true;
                // [firstRow, firstColumn, lastRow, LastColumn]
                workSheetTable.Cells[1, 1, 2, 2].Style.Border.Top.Style = ExcelBorderStyle.Thin;
                workSheetTable.Cells[1, 1, 2, 2].Style.Border.Left.Style = ExcelBorderStyle.Thin;
                workSheetTable.Cells[1, 1, 2, 3].Style.Border.Right.Style = ExcelBorderStyle.Thin;
                workSheetTable.Cells[1, 1, 2, 3].Style.Border.Bottom.Style = ExcelBorderStyle.Thin;

                workSheetTable.Cells["B1:C1"].Merge = true;
                workSheetTable.Cells["B1:C1"].Value = reportName;
                workSheetTable.Cells["B1:C1"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                workSheetTable.Cells["B2:C2"].Merge = true;
                workSheetTable.Cells["B2:C2"].Value = reportDateString;
                workSheetTable.Cells["B2:C2"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                const int headerRowIndex = PraxisDeveloperReport.HeaderRowIndexForAllDataReport;
                var headerColumnIndex = 1;
                List<string> headerColumns = new List<string>
                {
                    developerReportTranslation.NAME,
                    developerReportTranslation.TOPIC,
                    developerReportTranslation.PURPOSE,
                    developerReportTranslation.CREATED_BY,
                    developerReportTranslation.ORGANIZATION,
                    developerReportTranslation.LAST_UPDATE_ON
                };

                foreach (var column in headerColumns)
                {
                    workSheetTable.Cells[headerRowIndex, headerColumnIndex].Value = column;
                    headerColumnIndex++;
                }
                workSheetTable.Column(1).Width = 40;
                workSheetTable.Column(2).Width = 30;
                workSheetTable.Column(3).Width = 30;
                workSheetTable.Column(4).Width = 35;
                workSheetTable.Column(5).Width = 35;
                workSheetTable.Column(6).Width = 40;

                var headerRow = workSheetTable.Cells["A3:F3"];
                headerRow.Style.Font.Bold = true;
                headerRow.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                headerRow.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                headerRow.Style.Border.Right.Style = ExcelBorderStyle.Thin;
                headerRow.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                headerRow.Style.Fill.PatternType = ExcelFillStyle.Solid;
                headerRow.Style.Fill.BackgroundColor.SetColor(Color.LightGray);
                headerRow.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                headerRow.Style.VerticalAlignment = ExcelVerticalAlignment.Center;

                workSheetTable.Cells[1, logoPosition, 2, logoPosition].Merge = true;
                _praxisReportService.AddLogoInExcelReport(workSheetTable, logoSize, logoPosition, rqLatestLogo);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "WriteHeader Got Error. Exception message: {Message}. Exception Details: {StackTrace}", ex.Message, ex.StackTrace);
            }
        }

        private void WriteReportingDeveloperExcelReport(List<PraxisForm> dataset, ExcelPackage excel, string reportSheetName, ref int rowIndex)
        {
            try
            {
                var workSheetTable = excel.Workbook.Worksheets[reportSheetName];

                foreach (PraxisForm data in dataset)
                {
                    workSheetTable.Cells[rowIndex, 6].Style.Border.Right.Style = ExcelBorderStyle.Thin;

                    try
                    {
                        workSheetTable.Cells[rowIndex, 1].Value = data.Title;
                        workSheetTable.Cells[rowIndex, 1].Style.WrapText = true;
                        workSheetTable.Cells[rowIndex, 1].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        workSheetTable.Cells[rowIndex, 1].Style.Border.BorderAround(ExcelBorderStyle.Thin);

                        workSheetTable.Cells[rowIndex, 2].Value = GetTopicAndPurposeTranslation(data.TopicValue);
                        workSheetTable.Cells[rowIndex, 2].Style.WrapText = true;
                        workSheetTable.Cells[rowIndex, 2].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        workSheetTable.Cells[rowIndex, 2].Style.Border.BorderAround(ExcelBorderStyle.Thin);

                        workSheetTable.Cells[rowIndex, 3].Value = GetTopicAndPurposeTranslation(data.PurposeOfFormValue);
                        workSheetTable.Cells[rowIndex, 3].Style.WrapText = true;
                        workSheetTable.Cells[rowIndex, 3].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        workSheetTable.Cells[rowIndex, 3].Style.Border.BorderAround(ExcelBorderStyle.Thin);

                        if (data.CreatedBy != string.Empty)
                        {
                            var projection = Builders<PraxisUser>.Projection
                            .Include("_id")
                            .Include("DisplayName");
                            var filter = Builders<PraxisUser>.Filter.Eq(p => p.UserId, data.CreatedBy) & Builders<PraxisUser>.Filter.Eq(p => p.IsMarkedToDelete, false);
                            var result = _mongoClientRepository.GetCollection<PraxisUser>().Find(filter).Project(projection).FirstOrDefault();

                            if (result != null)
                            {
                                var dataResultList = BsonSerializer.Deserialize<FilterPraxisUser>(result.ToJson());

                                workSheetTable.Cells[rowIndex, 4].Value = dataResultList.DisplayName;
                                workSheetTable.Cells[rowIndex, 4].Style.WrapText = true;
                                workSheetTable.Cells[rowIndex, 4].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                                workSheetTable.Cells[rowIndex, 4].Style.Border.BorderAround(ExcelBorderStyle.Thin);
                            }
                            else
                            {
                                workSheetTable.Cells[rowIndex, 4].Value = "n/a";
                                workSheetTable.Cells[rowIndex, 4].Style.WrapText = true;
                                workSheetTable.Cells[rowIndex, 4].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                                workSheetTable.Cells[rowIndex, 4].Style.Border.BorderAround(ExcelBorderStyle.Thin);
                            }
                        }
                        else
                        {
                            workSheetTable.Cells[rowIndex, 4].Value = "n/a";
                            workSheetTable.Cells[rowIndex, 4].Style.WrapText = true;
                            workSheetTable.Cells[rowIndex, 4].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                            workSheetTable.Cells[rowIndex, 4].Style.Border.BorderAround(ExcelBorderStyle.Thin);
                        }

                        var cProjection = Builders<PraxisClient>.Projection
                            .Include("_id")
                            .Include("ClientName");
                        var cFilter = Builders<PraxisClient>.Filter.Eq(c => c.ItemId, data.ClientId) & Builders<PraxisClient>.Filter.Eq(c => c.IsMarkedToDelete, false);
                        var clientJson = _mongoClientRepository.GetCollection<PraxisClient>().Find(cFilter).Project(cProjection).FirstOrDefault();

                        var clientInfo = BsonSerializer.Deserialize<PraxisClient>(clientJson.ToJson());

                        if (clientInfo != null)
                        {
                            workSheetTable.Cells[rowIndex, 5].Value = clientInfo.ClientName;
                            workSheetTable.Cells[rowIndex, 5].Style.WrapText = true;
                        }
                        workSheetTable.Cells[rowIndex, 5].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        workSheetTable.Cells[rowIndex, 5].Style.Border.BorderAround(ExcelBorderStyle.Thin);

                        workSheetTable.Cells[rowIndex, 6].Value = data.LastUpdateDate.ToString("dd.MM.yyyy");
                        workSheetTable.Cells[rowIndex, 6].Style.WrapText = true;
                        workSheetTable.Cells[rowIndex, 6].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        workSheetTable.Cells[rowIndex, 6].Style.Border.BorderAround(ExcelBorderStyle.Thin);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Write Got Error with row number: {RowNumber}. Exception message: {Message}. Exception Details: {StackTrace}", rowIndex, ex.Message, ex.StackTrace);
                    }

                    rowIndex++;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Write Got Error with row number: {RowNumber}. Exception message: {Message}. Exception Details: {StackTrace}", rowIndex, ex.Message, ex.StackTrace);
            }
        }

        private string GetTopicAndPurposeTranslation(string key)
        {
            return _translatedStringsAsDictionary.ContainsKey(key) ? _translatedStringsAsDictionary[key] : key;
        }
    }
}
