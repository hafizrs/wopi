using Microsoft.Extensions.Logging;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.Equipment;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.Shared;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.EquipmentModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.Reports;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Entities.EquipmentModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.EntityResponse;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Queries.EquipmentModule;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using static Selise.Ecap.SC.PraxisMonitor.Contracts.Constants.ReportConstants;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.Report
{
    public class GenerateEquipmentReportService : IGenerateEquipmentReport
    {
        private const int logoPosition = 26;
        private readonly IPraxisReportService _praxisReportService;
        private readonly ILogger<GenerateEquipmentReportService> _logger;
        private readonly IUilmResourceKeyService _uilmResourceKeyService;
        private const string ReportListSheetName = "Reporting List";
        private Dictionary<string, string> _maintenanceStatusTranslation = new Dictionary<string, string>();
        private readonly IRepository _repository;
        private readonly IPraxisFileService _praxisFileService;
        private readonly ISecurityContextProvider _securityContextProvider;
        private readonly IPraxisEquipmentQueryService _praxisEquipmentQueryService;
        private readonly IPraxisRoomService _praxisRoomService;
        public GenerateEquipmentReportService(
            IPraxisReportService praxisReportService,
            ILogger<GenerateEquipmentReportService> logger,
            IUilmResourceKeyService uilmResourceKeyService,
            IRepository repository,
            IPraxisFileService praxisFileService,
            ISecurityContextProvider securityContextProvider,
            IPraxisEquipmentQueryService praxisEquipmentQueryService,
            IPraxisRoomService praxisRoomService
        )
        {
            _praxisReportService = praxisReportService;
            _logger = logger;
            _uilmResourceKeyService = uilmResourceKeyService;
            _repository = repository;
            _praxisFileService = praxisFileService;
            _securityContextProvider = securityContextProvider;
            _praxisEquipmentQueryService = praxisEquipmentQueryService;
            _praxisRoomService = praxisRoomService;
        }

        public async Task<bool> PrepareEquipmentListReport(string filter, bool enableDateRange, PraxisClient client, ExcelPackage excel, TranslationEpuimentList translationEpuimentList)
        {
            _maintenanceStatusTranslation = _uilmResourceKeyService
                .GetResourceValueByKeyName(new List<string> { "DONE", "PENDING", "SUBMITTED", "YES", "NO", "IN_PROGRESS" });
            int monitoringListRowIndex = 5;
            var isMultiClient = client == null;
            string reportDateString = DateTime.Today.ToString("dd.MM.yyyy");

            if (enableDateRange)
            {
                reportDateString = DateTime.Now.ToString("dd.MM.yyyy");
            }

            WriteHeaderForEquipmentReport(client, excel, translationEpuimentList.EQUIPMENT_MANAGEMENT_REPORT,
                ReportListSheetName, reportDateString, translationEpuimentList.REPORT_NAME,
                translationEpuimentList.DATE, translationEpuimentList.ORGANIZATION, isMultiClient);
            var query = new GetEquipementQuery
            {
                FilterString = filter,
                PageNumber = 0,
                PageSize = 100,
                SortBy = "{Name: 1}"
            };
            const int safeExitPoint = 50;
            while (query.PageNumber < safeExitPoint)
            {
                try
                {
                    var results = await _praxisEquipmentQueryService.GetPraxisEquipments(query);
                    if (results?.Results == null || !results.Results.Any())
                    {
                        break;
                    }

                    var totalData = results.Results.ToList();
                    WriteEquipmentListExcelReport(totalData, excel, ReportListSheetName, ref monitoringListRowIndex,
                        translationEpuimentList, isMultiClient);
                    query.PageNumber++;
                }
                catch (Exception e)
                {
                    _logger.LogError("Exception in Service: {ServiceName} Error Message: {Message}. Details: {StackTrace}",
                        nameof(GenerateEquipmentReportService), e.Message, e.StackTrace);

                }
            }

            AddReportBorderLine(excel, monitoringListRowIndex, ReportListSheetName, 26 + (isMultiClient ? 1 : 0));
            return true;
        }

        private void WriteHeaderForEquipmentReport(PraxisClient client, ExcelPackage excel, string reportName, string reportSheetName, string reportDateString, string Report_NameHeader, string DateHeader, string OrganizationHeader, bool isMultiClient = false)
        {
            try
            {
                excel.Workbook.Worksheets.Add(reportSheetName);
                var offset = isMultiClient ? 1 : 0;
                var rows = 3 - offset;
                // Target a worksheet
                var workSheetTable = excel.Workbook.Worksheets[reportSheetName];

                workSheetTable.Cells[1, 1].Value = Report_NameHeader;
                workSheetTable.Cells[2, 1].Value = DateHeader;
                if (!isMultiClient) workSheetTable.Cells[3, 1].Value = OrganizationHeader;

                if (!isMultiClient) workSheetTable.Cells["A1:AC4"].Style.Font.Bold = true;
                else workSheetTable.Cells["A1:AB4"].Style.Font.Bold = true;
                // [firstRow, firstColumn, lastRow, LastColumn]
                workSheetTable.Cells[1, 1, rows, rows].Style.Border.Top.Style = ExcelBorderStyle.Thin;
                workSheetTable.Cells[1, 1, rows, rows].Style.Border.Left.Style = ExcelBorderStyle.Thin;
                workSheetTable.Cells[1, 1, rows, rows].Style.Border.Right.Style = ExcelBorderStyle.Thin;
                workSheetTable.Cells[1, 1, rows, rows].Style.Border.Bottom.Style = ExcelBorderStyle.Thin;

                workSheetTable.Cells["B1:C1"].Merge = true;
                workSheetTable.Cells["B1:C1"].Value = reportName;
                workSheetTable.Cells["B1:C1"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                workSheetTable.Cells["B2:C2"].Merge = true;
                workSheetTable.Cells["B2:C2"].Value = reportDateString;
                workSheetTable.Cells["B2:C2"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                if (client == null) return;

                workSheetTable.Cells["B3:C3"].Merge = true;
                workSheetTable.Cells["B3:C3"].Value = client.ClientName;
                workSheetTable.Cells["B3:C3"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                workSheetTable.Cells[1, logoPosition, 2, logoPosition].Merge = true;
                //_praxisReportService.AddLogoInExcelReport(workSheetTable, 2, logoPosition, rqLatestLogo);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Exception occured during equipment report header generation process.");
                _logger.LogError("Exception Message: {Message}. Exception Details: {StackTrace}.", ex.Message, ex.StackTrace);
            }
        }

        private void WriteEquipmentListExcelReport(List<ProjectedEquipmentResponse> dataset, ExcelPackage excel, string reportSheetName, ref int rowIndex, TranslationEpuimentList translationEquipmentList, bool isMultiClient = false)
        {
            try
            {
                var workSheetTable = excel.Workbook.Worksheets[reportSheetName];
                var offset = isMultiClient ? 1 : 0;

                SetDataListHeader(workSheetTable, translationEquipmentList, isMultiClient);

                var roomIds = dataset?.Select(r => r.RoomId)?.ToList() ?? new List<string>();
                var praxisRooms = _praxisRoomService.GetPraxisRoomsByIds(roomIds);

                foreach (var data in dataset)
                {
                    workSheetTable.Cells[rowIndex, offset + 28].Style.Border.Right.Style = ExcelBorderStyle.Thin;

                    if (isMultiClient)
                    {
                        workSheetTable.Cells[rowIndex, offset].Value = data?.ClientName ?? string.Empty;
                        workSheetTable.Cells[rowIndex, offset].Style.WrapText = true;
                        workSheetTable.Cells[rowIndex, offset].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        workSheetTable.Cells[rowIndex, offset].Style.Border.BorderAround(ExcelBorderStyle.Thin);
                    }

                    workSheetTable.Cells[rowIndex, offset + 1].Value = data?.RoomName ?? string.Empty;
                    workSheetTable.Cells[rowIndex, offset + 1].Style.WrapText = true;
                    workSheetTable.Cells[rowIndex, offset + 1].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    workSheetTable.Cells[rowIndex, offset + 1].Style.Border.BorderAround(ExcelBorderStyle.Thin);

                    workSheetTable.Cells[rowIndex, offset + 2].Value = praxisRooms?.FirstOrDefault(r => r.ItemId == data?.RoomId)?.Address?.FullAddress ?? string.Empty;
                    workSheetTable.Cells[rowIndex, offset + 2].Style.WrapText = true;
                    workSheetTable.Cells[rowIndex, offset + 2].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    workSheetTable.Cells[rowIndex, offset + 2].Style.Border.BorderAround(ExcelBorderStyle.Thin);

                    workSheetTable.Cells[rowIndex, offset + 3].Value = data?.MetaValues?.FirstOrDefault(x => x.Key == "ExactLocation")?.Value ?? string.Empty;
                    workSheetTable.Cells[rowIndex, offset + 3].Style.WrapText = true;
                    workSheetTable.Cells[rowIndex, offset + 3].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    workSheetTable.Cells[rowIndex, offset + 3].Style.Border.BorderAround(ExcelBorderStyle.Thin);

                    workSheetTable.Cells[rowIndex, offset + 4].Value = data?.Name ?? string.Empty;
                    workSheetTable.Cells[rowIndex, offset + 4].Style.WrapText = true;
                    workSheetTable.Cells[rowIndex, offset + 4].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    workSheetTable.Cells[rowIndex, offset + 4].Style.Border.BorderAround(ExcelBorderStyle.Thin);

                    workSheetTable.Cells[rowIndex, offset + 5].Value = !string.IsNullOrWhiteSpace(data.Manufacturer) ? data.Manufacturer : "-";
                    workSheetTable.Cells[rowIndex, offset + 5].Style.WrapText = true;
                    workSheetTable.Cells[rowIndex, offset + 5].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    workSheetTable.Cells[rowIndex, offset + 5].Style.Border.BorderAround(ExcelBorderStyle.Thin);

                    var serialNumber = data?.MetaDataList?.FirstOrDefault(x => x.Key == EquipmentMetaDataKeys.SerialNumber)?.MetaData?.Value;
                    workSheetTable.Cells[rowIndex, offset + 6].Value = serialNumber ?? data.SerialNumber;
                    workSheetTable.Cells[rowIndex, offset + 6].Style.WrapText = true;
                    workSheetTable.Cells[rowIndex, offset + 6].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    workSheetTable.Cells[rowIndex, offset + 6].Style.Border.BorderAround(ExcelBorderStyle.Thin);

                    workSheetTable.Cells[rowIndex, offset + 7].Value = data?.SupplierName ?? string.Empty;
                    workSheetTable.Cells[rowIndex, offset + 7].Style.WrapText = true;
                    workSheetTable.Cells[rowIndex, offset + 7].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    workSheetTable.Cells[rowIndex, offset + 7].Style.Border.BorderAround(ExcelBorderStyle.Thin);


                    workSheetTable.Cells[rowIndex, offset + 8].Value = (data?.DateOfPurchase != null && data.DateOfPurchase == DateTime.MinValue) 
                        || (data?.DateOfPurchase != null && data.DateOfPurchase.Value.Date.Year < 2000) ? "-"
                            : data?.DateOfPurchase != null ? data.DateOfPurchase.Value.ToString("dd.MM.yyyy") : "-";
                    workSheetTable.Cells[rowIndex, offset + 8].Style.WrapText = true;
                    workSheetTable.Cells[rowIndex, offset + 8].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    workSheetTable.Cells[rowIndex, offset + 8].Style.Border.BorderAround(ExcelBorderStyle.Thin);

                    workSheetTable.Cells[rowIndex, offset + 9].Value = (data?.DateOfPlacingInService != null && data.DateOfPlacingInService == DateTime.MinValue) 
                        || (data?.DateOfPlacingInService != null &&  data.DateOfPlacingInService.Date.Year < 2000) ? "-"
                          : data?.DateOfPlacingInService != null ? data.DateOfPlacingInService.ToString("dd.MM.yyyy") : "-";
                    workSheetTable.Cells[rowIndex, offset + 9].Style.WrapText = true;
                    workSheetTable.Cells[rowIndex, offset + 9].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    workSheetTable.Cells[rowIndex, offset + 9].Style.Border.BorderAround(ExcelBorderStyle.Thin);


                    workSheetTable.Cells[rowIndex, offset + 10].Value = data?.MaintenanceMode ?? false
                        ? _maintenanceStatusTranslation["YES"]
                        : _maintenanceStatusTranslation["NO"];
                    workSheetTable.Cells[rowIndex, offset + 10].Style.WrapText = true;
                    workSheetTable.Cells[rowIndex, offset + 10].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    workSheetTable.Cells[rowIndex, offset + 10].Style.Border.BorderAround(ExcelBorderStyle.Thin);

                    List<MaintenanceDateProp> maintenanceDates = null;
                    MaintenanceDateProp lastMaintenanceDate = null;
                    MaintenanceDateProp nextMaintenanceDate = null;
                    if (data?.MaintenanceDates != null)
                    {
                        maintenanceDates = data.MaintenanceDates.ToList();
                        lastMaintenanceDate = maintenanceDates.LastOrDefault(o => o.Date.Value.Date < DateTime.UtcNow.Date);
                        nextMaintenanceDate = maintenanceDates.FirstOrDefault(o => o.Date.Value.Date >= DateTime.UtcNow.Date);
                    }

                    if (lastMaintenanceDate == null)
                    {
                        workSheetTable.Cells[rowIndex, offset + 11].Value = "-";
                        workSheetTable.Cells[rowIndex, offset + 12].Value = "-";
                    }
                    else
                    {
                        workSheetTable.Cells[rowIndex, offset + 11].Value =
                            lastMaintenanceDate.Date.Value.ToString("dd.MM.yyyy");
                        workSheetTable.Cells[rowIndex, offset + 12].Value =
                            _maintenanceStatusTranslation[lastMaintenanceDate.CompletionStatus.Value];
                    }
                    workSheetTable.Cells[rowIndex, offset + 11].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    workSheetTable.Cells[rowIndex, offset + 11].Style.Border.BorderAround(ExcelBorderStyle.Thin);
                    workSheetTable.Cells[rowIndex, offset + 12].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    workSheetTable.Cells[rowIndex, offset + 12].Style.Border.BorderAround(ExcelBorderStyle.Thin);


                    if (nextMaintenanceDate == null)
                    {
                        workSheetTable.Cells[rowIndex, offset + 13].Value = "-";
                        workSheetTable.Cells[rowIndex, offset + 14].Value = "-";
                    }
                    else
                    {
                        workSheetTable.Cells[rowIndex, offset + 13].Value =
                            nextMaintenanceDate.Date.Value.ToString("dd.MM.yyyy");
                        workSheetTable.Cells[rowIndex, offset + 14].Value =
                            _maintenanceStatusTranslation[nextMaintenanceDate.CompletionStatus.Value];
                    }

                    workSheetTable.Cells[rowIndex, offset + 13].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    workSheetTable.Cells[rowIndex, offset + 13].Style.Border.BorderAround(ExcelBorderStyle.Thin);
                    workSheetTable.Cells[rowIndex, offset + 14].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    workSheetTable.Cells[rowIndex, offset + 14].Style.Border.BorderAround(ExcelBorderStyle.Thin);

                    workSheetTable.Cells[rowIndex, offset + 11, rowIndex, offset + 14].Style.WrapText = true;
                    var additionalInfos = data.PraxisUserAdditionalInformationTitles?
                                    .Where(title => !string.IsNullOrEmpty(title?.Title))
                                    .Select(t => t.Title)
                                    .ToList() ?? new List<string>();
                    workSheetTable.Cells[rowIndex, offset + 15].Value = string.Join(", ", additionalInfos) ?? "-";
                    workSheetTable.Cells[rowIndex, offset + 15].Style.WrapText = true;
                    workSheetTable.Cells[rowIndex, offset + 15].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    workSheetTable.Cells[rowIndex, offset + 15].Style.Border.BorderAround(ExcelBorderStyle.Thin);

                    var infoList = data?.AdditionalInfos?.Select(additionalInfo =>
                        $"{translationEquipmentList.TITLE}: {additionalInfo?.Title}\n" +
                        $"{translationEquipmentList.DESCRIPTION}: {additionalInfo?.Description}\n"
                    ).ToList() ?? new List<string>();
                    workSheetTable.Cells[rowIndex, offset + 16].Value = infoList.Count > 0 ? string.Join("", infoList) : "-";
                    workSheetTable.Cells[rowIndex, offset + 16].Style.WrapText = true;
                    workSheetTable.Cells[rowIndex, offset + 16].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    workSheetTable.Cells[rowIndex, offset + 16].Style.Border.BorderAround(ExcelBorderStyle.Thin);

                    workSheetTable.Cells[rowIndex, offset + 17].Value = !string.IsNullOrWhiteSpace(data.Company) ? data.Company : "-";
                    workSheetTable.Cells[rowIndex, offset + 17].Style.WrapText = true;
                    workSheetTable.Cells[rowIndex, offset + 17].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    workSheetTable.Cells[rowIndex, offset + 17].Style.Border.BorderAround(ExcelBorderStyle.Thin);

                    workSheetTable.Cells[rowIndex, offset + 18].Value = !string.IsNullOrWhiteSpace(data.ContactPerson) ? data.ContactPerson : "-";
                    workSheetTable.Cells[rowIndex, offset + 18].Style.WrapText = true;
                    workSheetTable.Cells[rowIndex, offset + 18].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    workSheetTable.Cells[rowIndex, offset + 18].Style.Border.BorderAround(ExcelBorderStyle.Thin);

                    workSheetTable.Cells[rowIndex, offset + 19].Value = !string.IsNullOrWhiteSpace(data.Email) ? data.Email : "-";
                    workSheetTable.Cells[rowIndex, offset + 19].Style.WrapText = true;
                    workSheetTable.Cells[rowIndex, offset + 19].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    workSheetTable.Cells[rowIndex, offset + 19].Style.Border.BorderAround(ExcelBorderStyle.Thin);

                    workSheetTable.Cells[rowIndex, offset + 20].Value = !string.IsNullOrWhiteSpace(data.PhoneNumber) ? data.PhoneNumber : "-";
                    workSheetTable.Cells[rowIndex, offset + 20].Style.WrapText = true;
                    workSheetTable.Cells[rowIndex, offset + 20].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    workSheetTable.Cells[rowIndex, offset + 20].Style.Border.BorderAround(ExcelBorderStyle.Thin);

                    workSheetTable.Cells[rowIndex, offset + 21].Value = !string.IsNullOrWhiteSpace(data.CategoryName) ? data.CategoryName : "-";
                    workSheetTable.Cells[rowIndex, offset + 21].Style.WrapText = true;
                    workSheetTable.Cells[rowIndex, offset + 21].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    workSheetTable.Cells[rowIndex, offset + 21].Style.Border.BorderAround(ExcelBorderStyle.Thin);

                    workSheetTable.Cells[rowIndex, offset + 22].Value = !string.IsNullOrWhiteSpace(data.SubCategoryName) ? data.SubCategoryName : "-";
                    workSheetTable.Cells[rowIndex, offset + 22].Style.WrapText = true;
                    workSheetTable.Cells[rowIndex, offset + 22].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    workSheetTable.Cells[rowIndex, offset + 22].Style.Border.BorderAround(ExcelBorderStyle.Thin);

                    workSheetTable.Cells[rowIndex, offset + 23].Value = string.Join(
                        '\n', GetPhotoNames(data.Photos ?? new List<PraxisImage>()));
                    workSheetTable.Cells[rowIndex, offset + 23].Style.WrapText = true;
                    workSheetTable.Cells[rowIndex, offset + 23].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    workSheetTable.Cells[rowIndex, offset + 23].Style.Border.BorderAround(ExcelBorderStyle.Thin);

                    workSheetTable.Cells[rowIndex, offset + 24].Value = string.Join(
                        '\n', GetFileNames(data.Files ?? new List<PraxisDocument>()));
                    workSheetTable.Cells[rowIndex, offset + 24].Style.WrapText = true;
                    workSheetTable.Cells[rowIndex, offset + 24].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    workSheetTable.Cells[rowIndex, offset + 24].Style.Border.BorderAround(ExcelBorderStyle.Thin);

                    workSheetTable.Cells[rowIndex, offset + 25].Value =
                        HtmlToTextAgilityPackage.ExtractStyledText(data?.Remarks);
                    workSheetTable.Cells[rowIndex, offset + 25].Style.WrapText = true;
                    workSheetTable.Cells[rowIndex, offset + 25].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    workSheetTable.Cells[rowIndex, offset + 25].Style.Border.BorderAround(ExcelBorderStyle.Thin);

                    var internalNumber = data?.MetaDataList?.FirstOrDefault(x => x.Key == EquipmentMetaDataKeys.InternalNumber)?.MetaData?.Value;
                    workSheetTable.Cells[rowIndex, offset + 26].Value = internalNumber ?? string.Empty;
                    workSheetTable.Cells[rowIndex, offset + 26].Style.WrapText = true;
                    workSheetTable.Cells[rowIndex, offset + 26].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    workSheetTable.Cells[rowIndex, offset + 26].Style.Border.BorderAround(ExcelBorderStyle.Thin);

                    var installationNumber = data?.MetaDataList?.FirstOrDefault(x => x.Key == EquipmentMetaDataKeys.InstallationNumber)?.MetaData?.Value;
                    workSheetTable.Cells[rowIndex, offset + 27].Value = installationNumber ?? string.Empty;
                    workSheetTable.Cells[rowIndex, offset + 27].Style.WrapText = true;
                    workSheetTable.Cells[rowIndex, offset + 27].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    workSheetTable.Cells[rowIndex, offset + 27].Style.Border.BorderAround(ExcelBorderStyle.Thin);

                    var UDINumber = data?.MetaDataList?.FirstOrDefault(x => x.Key == EquipmentMetaDataKeys.UDINumber)?.MetaData?.Value;
                    workSheetTable.Cells[rowIndex, offset + 28].Value = UDINumber ?? string.Empty;
                    workSheetTable.Cells[rowIndex, offset + 28].Style.WrapText = true;
                    workSheetTable.Cells[rowIndex, offset + 28].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    workSheetTable.Cells[rowIndex, offset + 28].Style.Border.BorderAround(ExcelBorderStyle.Thin);

                    for (var i = 1; i <= (offset + 28); i++)
                    {
                        workSheetTable.Cells[rowIndex, i].Style.VerticalAlignment = ExcelVerticalAlignment.Top;
                    }

                    rowIndex++;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Exception occured during equipment report data write process.");
                _logger.LogError("Exception Message: {Message}. Exception Details: {StackTrace}.", ex.Message, ex.StackTrace);
            }
        }

        private void SetDataListHeader(ExcelWorksheet workSheetTable, TranslationEpuimentList translationEpuimentList, bool isMultiClient = false)
        {
            var headerRowIndex = 4;
            var headerColumnIndex = 1;
            var offset = isMultiClient ? 1 : 0;
            List<string> headerColumns = new List<string>
                {
                   translationEpuimentList.LOCATION,//  "Location",1
                   translationEpuimentList.LOCATION_ADDRESS,//2
                   translationEpuimentList.EXACT_LOCATION,//3
                   translationEpuimentList.NAME,//4
                   translationEpuimentList.MANUFACTURER,// "Manufacturer",5
                   translationEpuimentList.SERIAL_NUMBER,//"Serial Number",6
                   translationEpuimentList.SUPPLIER,// "Supplier",7
                   translationEpuimentList.PURCHASE,//8
                   translationEpuimentList.PLACING_IN_SERVICE,// "Placing in Service",9
                   translationEpuimentList.MAINTENANCE,//"Maintenance",10
                   translationEpuimentList.LAST_MAINTENANCE,// "Last maintenance",11
                   translationEpuimentList.LAST_MAINTENANCE_STATUS,//12
                   translationEpuimentList.NEXT_MAINTENANCE, //  "Next maintenance",13    
                   translationEpuimentList.NEXT_MAINTENANCE_STATUS,//14
                   translationEpuimentList.USER_ADDITIONAL_INFORMATION,// "User Additional Information", 15
                   translationEpuimentList.ADDITIONAL_INFORMATION,// "Additional Information", 16
                   translationEpuimentList.MAINTENANCE_BY,// "Maintenance By",17
                   translationEpuimentList.CONTACT_PERSON,//18
                   translationEpuimentList.E_MAIL,// "E-Mail",19
                   translationEpuimentList.PHONE,// "Phone",20
                   translationEpuimentList.CATEGORY,// "Category",21
                   translationEpuimentList.SUB_CATEGORY,// "Sub-Category",22
                   translationEpuimentList.PHOTOS, // "Photos", 23
                   translationEpuimentList.ATTACHMENT, // "Attachment", 24
                   translationEpuimentList.REMARKS,// "Remarks", 25
                   translationEpuimentList.INTERNAL_NUMBER,// "Internal Number", 26
                   translationEpuimentList.INSTALLATION_NUMBER,// "Installation Number", 27
                   translationEpuimentList.UDI_NUMBER,// "UDI Number", 28
                };
            if (isMultiClient)
            {
                headerColumns.Insert(0, translationEpuimentList.UNIT);
            }
            foreach (var column in headerColumns)
            {
                workSheetTable.Cells[headerRowIndex, headerColumnIndex].Value = column;
                headerColumnIndex++;
            }

            var widthList = new List<int>
            {
                38,46,46, 22, 26, 46, 16, 40, 26, 26, 40, 25, 26, 30, 25, 40, 40, 36, 28, 26, 35, 34, 34, 34, 34, 46, 46, 46
            };
            if (isMultiClient)
            {
                widthList.Insert(0, 46);
            }
            for (var i = 1; i <= widthList.Count; i++)
            {
                workSheetTable.Column(i).Width = widthList[i - 1];
            }
            CustomBestFitColumn(workSheetTable.Column(28 + offset), 15, 40);

            var headerRow = isMultiClient
                ? workSheetTable.Cells[$"A{headerRowIndex}:Z{headerRowIndex}"]
                : workSheetTable.Cells[$"A{headerRowIndex}:AB{headerRowIndex}"];
            headerRow.Style.Font.Bold = true;
            headerRow.Style.Border.Top.Style = ExcelBorderStyle.Thin;
            headerRow.Style.Border.Left.Style = ExcelBorderStyle.Thin;
            headerRow.Style.Border.Right.Style = ExcelBorderStyle.Thin;
            headerRow.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
            headerRow.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
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
        private static void AddReportBorderLine(ExcelPackage excel, int rowIndex, string reportSheetName, int totalColumn)
        {
            var workSheetTable = excel.Workbook.Worksheets[reportSheetName];

            for (int i = 1; i <= totalColumn; i++)
            {
                workSheetTable.Cells[rowIndex - 1, i].Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
            }
        }

        private List<string> GetPhotoNames(IEnumerable<PraxisImage> photos)
        {
            return photos
                .Select(photo => _praxisFileService.GetFileInformation(photo.FileId))
                .Where(file => file != null)
                .Select(file => file.Name)
                .ToList();
        }

        private List<string> GetFileNames(IEnumerable<PraxisDocument> files)
        {
            return files
                .Select(file => file.DocumentName)
                .ToList();
        }

        public List<string> GetEquipmentAssignedOrgAdmin(string organizationId)
        {
            var rights = _repository
                .GetItems<PraxisEquipmentRight>(p =>
                    !p.IsMarkedToDelete &&
                    p.IsOrganizationLevelRight == true &&
                    p.OrganizationId == organizationId)?
                .ToList() ?? new List<PraxisEquipmentRight>();
            var adminIds = rights
                .Where(p => p.AssignedAdmins != null)?
                .SelectMany(x => x.AssignedAdmins.Select(u => u.UserId))
                .ToList() ?? new List<string>();
            return adminIds;
        }
    }
}
