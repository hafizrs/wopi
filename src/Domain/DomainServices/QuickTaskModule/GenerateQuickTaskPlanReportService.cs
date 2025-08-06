using Microsoft.Extensions.Logging;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.QuickTaskModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.QuickTaskModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.EntityResponse;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Infrastructure;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using static Selise.Ecap.SC.PraxisMonitor.Contracts.Constants.ReportConstants;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.LibraryModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants.ShiftPlan;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Queries.QuickTaskModule;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.QuickTaskModule
{
    public class GenerateQuickTaskPlanReportService : IGenerateQuickTaskPlanReportService
    {
        private Dictionary<string, string> _translatedStrings = new Dictionary<string, string>();

        private readonly IRepository _repository;
        private readonly IQuickTaskService _quickTaskService;
        private readonly IUilmResourceKeyService _uilmResourceKeyService;
        private readonly ISecurityHelperService _securityHelperService;
        private readonly IObjectArtifactAuthorizationCheckerService _objectArtifactAuthorizationCheckerService;
        private readonly ILogger<GenerateQuickTaskPlanReportService> _logger;

        public GenerateQuickTaskPlanReportService
        (
            IRepository repository,
            IQuickTaskService quickTaskService,
            IUilmResourceKeyService uilmResourceKeyService,
            ISecurityHelperService securityHelperService,
            IObjectArtifactAuthorizationCheckerService objectArtifactAuthorizationCheckerService,
            ILogger<GenerateQuickTaskPlanReportService> logger
        )
        {
            _repository = repository;
            _quickTaskService = quickTaskService;
            _uilmResourceKeyService = uilmResourceKeyService;
            _securityHelperService = securityHelperService;
            _objectArtifactAuthorizationCheckerService = objectArtifactAuthorizationCheckerService;
            _logger = logger;
        }

        public bool GenerateQuickTaskPlanReport(ExcelPackage excel, GenerateQuickTaskPlanReportCommand command)
        {
            try
            {
                GetTranslations(command.LanguageKey);

                var reportCreationDate = DateTime.Today;
                var worksheet = excel.Workbook.Worksheets.Add(command.FileName);

                var endDate = CalculateEndDate(command.StartDate, command.ViewMode);
                var quickTaskPlanQuery = new GetQuickTaskPlanQuery()
                {
                    StartDate = command.StartDate,
                    EndDate = endDate,
                    DepartmentId = command.ClientId
                };

                var quickTaskPlans = _quickTaskService.GetQuickTaskPlans(quickTaskPlanQuery);

                switch (command.ViewMode)
                {
                    case ShiftPlanReportType.DAILY:
                        WriteDailyReportHeader(worksheet, command, reportCreationDate);
                        WriteDailyQuickTaskPlanReport(worksheet, quickTaskPlans.Single());
                        break;
                    case ShiftPlanReportType.WEEKLY:
                        WriteRangeReportHeader(worksheet, command, endDate, reportCreationDate);
                        WriteRangeQuickTaskPlanReport(worksheet, command, endDate, quickTaskPlans);
                        break;
                    case ShiftPlanReportType.MONTHLY:
                        WriteRangeReportHeader(worksheet, command, endDate, reportCreationDate);
                        WriteRangeQuickTaskPlanReport(worksheet, command, endDate, quickTaskPlans);
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
                ShiftPlanReportType.WEEKLY => startDate.AddDays(6),
                ShiftPlanReportType.MONTHLY => startDate.AddDays(DateTime.DaysInMonth(startDate.Year, startDate.Month) - 1),
                _ => throw new ArgumentOutOfRangeException(nameof(reportType), "Unsupported DateType provided")
            };
        }

        public void SetupRolesForQuickTaskPlanReport(PraxisReport report, GenerateQuickTaskPlanReportCommand command)
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

        private void WriteReportMetadataSection(ExcelWorksheet worksheet, GenerateQuickTaskPlanReportCommand command, DateTime creationDate)
        {
            worksheet.Cells[1, 1].Value = _translatedStrings[ShiftPlanReport.LanguageKeys["DEPARTMENT_NAME"]];
            SetCellValueWithBoldStyle(worksheet, 1, 1);
            SetCellStyleWithBorderAround(worksheet, 1, 1, ExcelBorderStyle.Thin);

            var departmentDetails = GetDepartmentDetails(command.ClientId);

            worksheet.Cells[1, 2].Value = departmentDetails.ClientName;
            SetCellStyleWithBorderAround(worksheet, 1, 2, ExcelBorderStyle.Thin);

            worksheet.Cells[2, 1].Value = _translatedStrings[ShiftPlanReport.LanguageKeys["REPORT_NAME"]];
            SetCellValueWithBoldStyle(worksheet, 2, 1);
            SetCellStyleWithBorderAround(worksheet, 2, 1, ExcelBorderStyle.Thin);

            worksheet.Cells[2, 2].Value = command.FileName;
            SetCellStyleWithBorderAround(worksheet, 2, 2, ExcelBorderStyle.Thin);

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
                var appDirectoryFolderPath = Path.GetDirectoryName(AppDomain.CurrentDomain.BaseDirectory);

                if (appDirectoryFolderPath == null)
                {
                    _logger.LogError("appDirectoryFolderPath is null, logo is not found");
                    return;
                }

                var logoFilePath = @"" + appDirectoryFolderPath + rqLatestLogo;
                if (!File.Exists(logoFilePath))
                {
                    _logger.LogError("Logo is not found, {LogoFilePath} file doesn't exist", logoFilePath);
                    return;
                }

                using Stream stream = new FileStream(logoFilePath, FileMode.Open, FileAccess.Read);
                var logoImage = Image.FromStream(stream);

                stream.Close();
                stream.Dispose();
                var excelPicture = workSheetTable.Drawings.AddPicture("Logo", logoImage);
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
                return -1;
            }
            return (dateToFind - startDate).Days;
        }

        private void WriteRangeQuickTaskPlanReport(ExcelWorksheet worksheet, GenerateQuickTaskPlanReportCommand command, DateTime endDate, IReadOnlyCollection<QuickTaskPlanQueryResponse> quickTaskPlans)
        {
            try
            {
                var columnCount = (endDate - command.StartDate).Days + 1;
                var maxRowCount = 0;
                for (var selectedDate = command.StartDate; selectedDate <= endDate; selectedDate = selectedDate.AddDays(1))
                {
                    var availablePlan = quickTaskPlans.FirstOrDefault(x => x.QuickTaskDate == selectedDate);
                    if (availablePlan == null) continue;
                    if (availablePlan.QuickTaskPlans?.Count > maxRowCount)
                        maxRowCount = availablePlan.QuickTaskPlans?.Count ?? 0;
                    for (var rowIndex = 1; rowIndex <= (availablePlan.QuickTaskPlans?.Count ?? 0); rowIndex++)
                    {
                        var columnIndex = GetDateIndex(command.StartDate, endDate, availablePlan.QuickTaskDate) + 1;
                        //worksheet.Cells[ShiftPlanReport.HeaderRowIndex + rowIndex, columnIndex].Value = availablePlan.QuickTaskShift?.TaskGroupName ?? "-";
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

        private void WriteDailyQuickTaskPlanReport(ExcelWorksheet worksheet, QuickTaskPlanQueryResponse quickTaskPlan)
        {
            try
            {
                if (quickTaskPlan == null || quickTaskPlan.QuickTaskPlans?.Count <= 0)
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
                var rowCount = quickTaskPlan.QuickTaskPlans?.Count;
                for (var row = 1; row <= rowCount; row++)
                {
                    var rowIndex = ShiftPlanReport.HeaderRowIndex + row;
                    //worksheet.Cells[rowIndex, 1, rowIndex, 1].Value = quickTaskPlan.QuickTaskShift?.TaskGroupName ?? "-";
                    var mergeRange = worksheet.Cells[rowIndex, 1, rowIndex, 3];
                    mergeRange.Merge = true;
                    mergeRange.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                    mergeRange.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                    mergeRange.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    mergeRange.Style.WrapText = true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception in WriteDailyQuickTaskPlanReport: {Message}", ex.Message);
            }
        }

        private void WriteDailyReportHeader(ExcelWorksheet worksheet, GenerateQuickTaskPlanReportCommand command, DateTime creationDate)
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

        private void WriteRangeReportHeader(ExcelWorksheet worksheet, GenerateQuickTaskPlanReportCommand command, DateTime endDate, DateTime creationDate)
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
                _logger.LogError(ex, "Exception in WriteRangeReportHeader: {Message}", ex.Message);
            }
        }
    }
} 