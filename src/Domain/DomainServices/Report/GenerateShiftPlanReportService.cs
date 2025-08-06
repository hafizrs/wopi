using Microsoft.Extensions.Logging;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants.ShiftPlan;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.LibraryModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.Reports;
using Selise.Ecap.SC.PraxisMonitor.Contracts.EntityResponse;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Queries;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.ExcelReports;
using static Selise.Ecap.SC.PraxisMonitor.Contracts.Constants.ReportConstants;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.LibraryModule;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.Shared;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.Report
{
    public class GenerateShiftPlanReportService : IGenerateShiftPlanReportService
    {
        private Dictionary<string, string> _translatedStrings = new Dictionary<string, string>();

        private readonly IRepository _repository;
        private readonly IPraxisShiftService _praxisShiftService;
        private readonly IUilmResourceKeyService _uilmResourceKeyService;
        private readonly ISecurityHelperService _securityHelperService;
        private readonly IObjectArtifactAuthorizationCheckerService _objectArtifactAuthorizationCheckerService;
        private readonly ILogger<GenerateShiftPlanReportService> _logger;

        public GenerateShiftPlanReportService
        (
            IRepository repository,
            IPraxisShiftService praxisShiftService,
            IUilmResourceKeyService uilmResourceKeyService,
            ISecurityHelperService securityHelperService,
            IObjectArtifactAuthorizationCheckerService objectArtifactAuthorizationCheckerService,
            ILogger<GenerateShiftPlanReportService> logger
        )
        {
            _repository = repository;
            _praxisShiftService = praxisShiftService;
            _uilmResourceKeyService = uilmResourceKeyService;
            _securityHelperService = securityHelperService;
            _objectArtifactAuthorizationCheckerService = objectArtifactAuthorizationCheckerService;
            _logger = logger;
        }

        public bool GenerateShiftPlanReport(ExcelPackage excel, GenerateShiftPlanReportCommand command)
        {
            try
            {
                GetTranslations(command.LanguageKey);

                var reportCreationDate = DateTime.Today;
                var worksheet = excel.Workbook.Worksheets.Add(command.FileName);

                var endDate = CalculateEndDate(command.StartDate, command.ViewMode);
                var shiftPlanQuery = new GetShiftPlanQuery()
                {
                    StartDate = command.StartDate,
                    EndDate = endDate,
                    DepartmentId = command.ClientId
                };

                var shiftPlans = _praxisShiftService.GetShiftPlans(shiftPlanQuery);

                switch (command.ViewMode)
                {
                    case ShiftPlanReportType.DAILY:
                        WriteDailyReportHeader(worksheet, command, reportCreationDate);
                        WriteDailyShiftPlanReport(worksheet, shiftPlans.Single());
                        break;
                    case ShiftPlanReportType.WEEKLY:
                        WriteRangeReportHeader(worksheet, command, endDate, reportCreationDate);
                        WriteRangeShiftPlanReport(worksheet, command, endDate, shiftPlans);
                        break;
                    case ShiftPlanReportType.MONTHLY:
                        WriteRangeReportHeader(worksheet, command, endDate, reportCreationDate);
                        WriteRangeShiftPlanReport(worksheet, command, endDate, shiftPlans);
                        break;
                    default:
                        throw new ArgumentException("Unsupported report type.");
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError("Exception occured during report generation");
                _logger.LogError("Exception occurred. Message: {Message}. Details: {StackTrace}", ex.Message, ex.StackTrace);
                return false;
            }
        }

        private static DateTime CalculateEndDate(DateTime startDate, ShiftPlanReportType reportType)
        {
            return reportType switch
            {
                ShiftPlanReportType.DAILY => startDate,
                ShiftPlanReportType.WEEKLY => startDate.AddDays(6), // For Weekly, add 6 day to start date
                ShiftPlanReportType.MONTHLY => startDate.AddDays(DateTime.DaysInMonth(startDate.Year, startDate.Month) - 1),
                _ => throw new ArgumentOutOfRangeException(nameof(reportType), "Unsupported DateType provided")
            };
        }

        public void SetupRolesForShiftPlanReport(PraxisReport report, GenerateShiftPlanReportCommand command)
        {
            var rolesAllowedToRead = new List<string>();
            var rolesAllowedToDelete = new List<string>();

            var adminRole = $"{RoleNames.Admin}";
            rolesAllowedToRead.Add(adminRole);
            rolesAllowedToDelete.Add(adminRole);

            var taskControllerRole = $"{RoleNames.TaskController}";
            rolesAllowedToRead.Add(taskControllerRole);
            rolesAllowedToDelete.Add(taskControllerRole);

            if (_objectArtifactAuthorizationCheckerService.IsAAdminBUser())
            {
                var organizationId = command?.OrganizationId;

                if (organizationId != null)
                {
                    var adminBRole = $"{RoleNames.AdminB_Dynamic}_{organizationId}";
                    rolesAllowedToRead.Add(adminBRole);
                }
            }

            if (_objectArtifactAuthorizationCheckerService.IsAPowerUser() || _objectArtifactAuthorizationCheckerService.IsAManagementUser())
            {
                var clientId = command?.ClientId ?? _securityHelperService.ExtractDepartmentIdFromDepartmentLevelUser();

                if (clientId != null)
                {
                    var poweruserRole = $"{RoleNames.PowerUser_Dynamic}_{clientId}";
                    rolesAllowedToRead.Add(poweruserRole);

                    var managementRole = $"{RoleNames.Leitung_Dynamic}_{clientId}";
                    rolesAllowedToRead.Add(managementRole);
                }
            }

            report.RolesAllowedToRead = rolesAllowedToRead.ToArray();
            report.RolesAllowedToDelete = rolesAllowedToDelete.ToArray();
        }

        private void GetTranslations(string languageKey)
        {
            var keyList = new List<string>();
            keyList.AddRange(ShiftPlanReport.LanguageKeys.Values);

            _translatedStrings = _uilmResourceKeyService.GetResourceValueByKeyName(keyList, languageKey);
        }

        private void WriteReportMetadataSection(ExcelWorksheet worksheet, ExportReportCommand command, DateTime creationDate)
        {
            // First Metadata Row
            worksheet.Cells[1, 1].Value = _translatedStrings[ShiftPlanReport.LanguageKeys["DEPARTMENT_NAME"]];
            SetCellValueWithBoldStyle(worksheet, 1, 1);
            SetCellStyleWithBorderAround(worksheet, 1, 1, ExcelBorderStyle.Thin);

            var departmentDetails = GetDepartmentDetails(command.ClientId);

            worksheet.Cells[1, 2].Value = departmentDetails.ClientName;
            SetCellStyleWithBorderAround(worksheet, 1, 2, ExcelBorderStyle.Thin);

            // Second Metadata Row
            worksheet.Cells[2, 1].Value = _translatedStrings[ShiftPlanReport.LanguageKeys["REPORT_NAME"]];
            SetCellValueWithBoldStyle(worksheet, 2, 1);
            SetCellStyleWithBorderAround(worksheet, 2, 1, ExcelBorderStyle.Thin);

            worksheet.Cells[2, 2].Value = command.FileName;
            SetCellStyleWithBorderAround(worksheet, 2, 2, ExcelBorderStyle.Thin);

            // Third Metadata Row
            worksheet.Cells[3, 1].Value = _translatedStrings[ShiftPlanReport.LanguageKeys["DATE"]];
            SetCellValueWithBoldStyle(worksheet, 3, 1);
            SetCellStyleWithBorderAround(worksheet, 3, 1, ExcelBorderStyle.Thin);

            var cultureInfo = new CultureInfo(command.LanguageKey);
            worksheet.Cells[3, 2].Value = creationDate.ToString("MMMM dd, yyyy", cultureInfo);
            SetCellStyleWithBorderAround(worksheet, 3, 2, ExcelBorderStyle.Thin);
        }

        private PraxisClient GetDepartmentDetails(string departmentId)
        {
            var praxisClient = _repository.GetItem<PraxisClient>(x => x.ItemId == departmentId);

            return praxisClient;
        }

        private void SetPrimaryTableHeaderRowStyle(ExcelWorksheet worksheet, int startColumn, int endColumn)
        {
            const int headerRowIndex = 4;
            var primaryTableHeaderBackgroundColor = Color.FromArgb(222, 222, 222);

            for (var col = startColumn; col <= endColumn; col++)
            {
                var cell = worksheet.Cells[headerRowIndex, col];
                cell.Style.Font.Bold = true;
                cell.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                cell.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                cell.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                cell.Style.Fill.PatternType = ExcelFillStyle.Solid;
                cell.Style.Fill.BackgroundColor.SetColor(primaryTableHeaderBackgroundColor);
            }
        }

        private void AddLogoToWorksheet(ExcelWorksheet worksheet, int startRow, int startColumn)
        {
            var mergeRange = worksheet.Cells[startRow, startColumn, startRow + 1, startColumn];
            mergeRange.Merge = true;

            AddLogoInExcelReport(worksheet, 2, startRow, startColumn);
        }

        private void WriteRangeReportHeader(ExcelWorksheet worksheet, GenerateShiftPlanReportCommand command, DateTime endDate, DateTime creationDate)
        {
            try
            {
                WriteReportMetadataSection(worksheet, command, creationDate);

                var duration = endDate - command.StartDate;

                var numberOfDays = duration.Days;
                SetPrimaryTableHeaderRowStyle(worksheet, 1, numberOfDays);

                var columnIndex = 1;
                const int headerRowIndex = 4;
                for (var selectedDate = command.StartDate; selectedDate <= endDate; selectedDate = selectedDate.AddDays(1))
                {
                    var weekdayName = selectedDate.DayOfWeek.ToString().ToUpper();
                    worksheet.Cells[headerRowIndex, columnIndex++].Value = _translatedStrings[ShiftPlanReport.LanguageKeys[weekdayName]] + " " + selectedDate.ToString("M/d");
                }

                var logoStartColumn = (endDate - command.StartDate).Days + 1;
                const int logoStartRow = 2;
                AddLogoToWorksheet(worksheet, logoStartRow, logoStartColumn);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception in WriteWeeklyReportHeader: {Message}", ex.Message);
            }
        }

        private void WriteDailyReportHeader(ExcelWorksheet worksheet, GenerateShiftPlanReportCommand command, DateTime creationDate)
        {
            try
            {
                WriteReportMetadataSection(worksheet, command, creationDate);

                var primaryTableHeaderBackgroundColor = Color.FromArgb(222, 222, 222);

                var monthName = command.StartDate.ToString("MMMM").ToUpperInvariant();
                worksheet.Cells[4, 1, 4, 1].Value = _translatedStrings[ShiftPlanReport.LanguageKeys[monthName]] + " " + command.StartDate.ToString("dd, yyyy");

                var mergeRange = worksheet.Cells[4, 1, 4, 3];
                mergeRange.Merge = true;
                mergeRange.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                mergeRange.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                mergeRange.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                mergeRange.Style.Font.Bold = true;
                mergeRange.Style.Fill.PatternType = ExcelFillStyle.Solid;
                mergeRange.Style.Fill.BackgroundColor.SetColor(primaryTableHeaderBackgroundColor);

                const int logoStartColumn = 3;
                const int logoStartRow = 2;
                AddLogoToWorksheet(worksheet, logoStartRow, logoStartColumn);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception in WriteDailyReportHeader: {Message}", ex.Message);
            }
        }

        private static void SetCellValueWithBoldStyle(ExcelWorksheet worksheet, int row, int column)
        {
            worksheet.Cells[row, column].Style.Font.Bold = true;
        }

        private static void SetCellStyleWithBorderAround(ExcelWorksheet worksheet, int row, int column, ExcelBorderStyle excelBorderStyle)
        {
            worksheet.Cells[row, column].Style.Border.BorderAround(excelBorderStyle);
        }

        private static void SetWrapText(ExcelWorksheet worksheet, int row, int column)
        {
            worksheet.Cells[row, column].Style.WrapText = true;
        }

        private static void SetVerticalAlignmentOfCell(ExcelWorksheet worksheet, int row, int column, ExcelVerticalAlignment verticalAlignment)
        {
            worksheet.Cells[row, column].Style.VerticalAlignment = verticalAlignment;
        }

        private static void SetHorizontalAlignmentOfCell(ExcelWorksheet worksheet, int row, int column, ExcelHorizontalAlignment horizontalAlignment)
        {
            worksheet.Cells[row, column].Style.HorizontalAlignment = horizontalAlignment;
        }

        private static int GetDateIndex(DateTime startDate, DateTime endDate, DateTime dateToFind)
        {
            if (dateToFind < startDate || dateToFind > endDate)
            {
                return -1; // the given date is outside the range
            }

            return (dateToFind - startDate).Days;
        }

        private void WriteRangeShiftPlanReport(ExcelWorksheet worksheet, GenerateShiftPlanReportCommand command, DateTime endDate, IReadOnlyCollection<ShiftPlanQueryResponse> shiftPlans)
        {
            try
            {
                var columnCount = (endDate - command.StartDate).Days + 1;

                var maxRowCount = 0;
                for (var selectedDate = command.StartDate; selectedDate <= endDate; selectedDate = selectedDate.AddDays(1))
                {
                    var availablePlan = shiftPlans.FirstOrDefault(x => x.ShiftDate == selectedDate);

                    if (availablePlan == null)
                    {
                        continue;
                    }

                    if (availablePlan.ShiftPlans?.Count > maxRowCount)
                    {
                        maxRowCount = availablePlan.ShiftPlans?.Count ?? 0;
                    }

                    for (var rowIndex = 1; rowIndex <= availablePlan.ShiftPlans?.Count; rowIndex++)
                    {
                        var selectedPlan = availablePlan.ShiftPlans[rowIndex - 1];

                        var columnIndex = GetDateIndex(command.StartDate, endDate, selectedPlan.ShiftDate) + 1;

                        SetPlanCellText(worksheet.Cells[ShiftPlanReport.HeaderRowIndex + rowIndex, columnIndex], selectedPlan);

                        var height  = worksheet.Row(rowIndex).Height;
                        worksheet.Row(rowIndex).Height = worksheet.Row(rowIndex).Height;

                        SetHorizontalAlignmentOfCell(worksheet, ShiftPlanReport.HeaderRowIndex + rowIndex, columnIndex, ExcelHorizontalAlignment.Left);
                        SetVerticalAlignmentOfCell(worksheet, ShiftPlanReport.HeaderRowIndex + rowIndex, columnIndex, ExcelVerticalAlignment.Top);
                        SetCellStyleWithBorderAround(worksheet, ShiftPlanReport.HeaderRowIndex + rowIndex, columnIndex, ExcelBorderStyle.Thin);
                        SetWrapText(worksheet, ShiftPlanReport.HeaderRowIndex + rowIndex, columnIndex);
                    }
                }

                for (var row = 1; row <= maxRowCount; row++)
                {
                    for (var column = 1; column <= columnCount; column++)
                    {
                        var selectedColumn = worksheet.Column(column);
                        selectedColumn.AutoFit(20, 50);

                        var selectedCell = worksheet.Cells[ShiftPlanReport.HeaderRowIndex + row, column];
                        selectedCell.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                        selectedCell.Style.WrapText = true;

                        if (column == columnCount)
                            selectedColumn.Width = Math.Max(selectedColumn.Width, 20);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception in WriteUserList: {Message}", ex.Message);
            }
        }

        private void WriteDailyShiftPlanReport(ExcelWorksheet worksheet, ShiftPlanQueryResponse shiftPlan)
        {
            try
            {
                if (shiftPlan?.ShiftPlans?.Count <= 0)
                {
                    return;
                }

                const int startColumn = 1;
                const int endColumn = 3;
                for (var i = startColumn; i <= endColumn; i++)
                {
                    var column = worksheet.Column(i);
                    column.AutoFit(20, 50);

                    if (i == endColumn)
                        column.Width = Math.Max(column.Width, 20);
                }

                var rowCount = shiftPlan?.ShiftPlans?.Count;
                for (var row = 1; row <= rowCount; row++)
                {
                    var rowIndex = ShiftPlanReport.HeaderRowIndex + row;

                    var selectedPlan = shiftPlan.ShiftPlans[row - 1];

                    SetPlanCellText(worksheet.Cells[rowIndex, 1, rowIndex, 1], selectedPlan);

                    var mergeRange = worksheet.Cells[rowIndex, 1, rowIndex, 3];
                    mergeRange.Merge = true;
                    mergeRange.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                    mergeRange.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                    mergeRange.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    mergeRange.Style.WrapText = true;

                    //worksheet.Row(rowIndex).Height = worksheet.Row(rowIndex).Height;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception in WriteDailyShiftPlanReport: {Message}", ex.Message);
            }
        }

        private void SetPlanCellText(ExcelRange cell, RiqsShiftPlanResponse selectedPlan)
        {
            cell.Value = "";
            cell.IsRichText = true;
            var taskPart = cell.RichText.Add(_translatedStrings[ShiftPlanReport.LanguageKeys["TASK"]] + ": ");
            taskPart.Bold = true;
            var shiftName = cell.RichText.Add(selectedPlan.Shift.ShiftName + Environment.NewLine);
            shiftName.Bold = false;
            var assignToPart = cell.RichText.Add(_translatedStrings[ShiftPlanReport.LanguageKeys["ASSIGNED_TO"]] + ": ");
            assignToPart.Bold = true;
            var persons = cell.RichText.Add(GetNames(selectedPlan.PraxisPersons) + Environment.NewLine);
            persons.Bold = false;
            var attachmentPart = cell.RichText.Add(_translatedStrings[ShiftPlanReport.LanguageKeys["ATTACHMENT"]] + ": ");
            attachmentPart.Bold = true;
            var attach = cell.RichText.Add(GetAttachmentNames(selectedPlan));
            attach.Bold = false;
        }

        private void AddLogoInExcelReport(
            ExcelWorksheet workSheetTable,
            int logoSize,
            int rowPosition,
            int columnPosition,
            int columnOffsetPixel = 0
        )
        {
            try
            {
                // Add Logo
                var appDirectoryFolderPath = Path.GetDirectoryName(AppDomain.CurrentDomain.BaseDirectory);

                if (appDirectoryFolderPath == null)
                {
                    _logger.LogError("appDirectoryFolderPath is null, logo is not found");
                    return;
                }

                var logoFilePath = @"" + appDirectoryFolderPath + rqLatestLogo;
                //Pass the filepath and filename to the StreamWriter Constructor
                if (!File.Exists(logoFilePath))
                {
                    _logger.LogError("Logo is not found, {LogoFilePath} file doesn't exist", logoFilePath);
                    return;
                }

                using Stream stream = new FileStream(logoFilePath, FileMode.Open, FileAccess.Read);
                var logoImage = Image.FromStream(stream);

                stream.Close();
                stream.Dispose();
                // Set Logo Image To Sheet1
                var excelPicture = workSheetTable.Drawings.AddPicture("Logo", logoImage);
                //add the image to row 1, column F
                excelPicture.SetPosition(rowPosition - 1, 0, columnPosition - 1, columnOffsetPixel);

                excelPicture.SetSize(logoSize);
            }
            catch (Exception ex)
            {
                _logger.LogError("Error while adding logo to Excel. Error: {Message}. Exception Details: {StackTrace}", ex.Message, ex.StackTrace);
            }
            finally
            {
                GC.Collect();
            }
        }
        private string GetNames(List<PraxisUser> praxisPersons)
        {
            var names = string.Join(", ", praxisPersons.Select(x => x.DisplayName));
            return string.IsNullOrEmpty(names) ? "-" : names;
        }

        private string GetAttachmentNames(RiqsShiftPlanResponse selectedPlan)
        {
            var files = selectedPlan.Shift.Files ?? new List<PraxisDocument>();
            var libraryForms = selectedPlan.Shift.LibraryForms ?? new List<PraxisLibraryEntityDetail>();
            var fileNames = string.Join(", ", files.Select(x => x.DocumentName));
            var formNames = string.Join(", ", libraryForms.Select(x => x.LibraryFormName));
            var names = (fileNames + (files.Count > 0 ? "," : "") + formNames);
            return string.IsNullOrEmpty(names) ? "-" : names;
        }
    }
}
