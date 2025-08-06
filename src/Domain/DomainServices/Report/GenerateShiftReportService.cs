using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.ProcessGuide;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.ExcelReports;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.LibraryModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.Reports;
using Selise.Ecap.SC.PraxisMonitor.Contracts.EntityResponse;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Infrastructure;
using static Selise.Ecap.SC.PraxisMonitor.Contracts.Constants.ReportConstants;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.Report;

public class GenerateShiftReportService : IGenerateShiftReportService
{
    private Dictionary<string, string> _translatedStrings = new Dictionary<string, string>();

    private readonly ILogger<GenerateShiftReportService> _logger;
    private readonly IObjectArtifactAuthorizationCheckerService _objectArtifactAuthorizationCheckerService;
    private readonly ISecurityHelperService _securityHelperService;
    private readonly IUilmResourceKeyService _uilmResourceKeyService;
    private readonly IRepository _repository;
    private readonly IPraxisShiftService _praxisShiftService;
    private readonly IPraxisReportService _praxisReportService;

    public GenerateShiftReportService(
        ILogger<GenerateShiftReportService> logger,
        IObjectArtifactAuthorizationCheckerService objectArtifactAuthorizationCheckerService,
        ISecurityHelperService securityHelperService,
        IUilmResourceKeyService uilmResourceKeyService,
        IRepository repository,
        IPraxisShiftService praxisShiftService,
        IPraxisReportService praxisReportService)
    {
        _logger = logger;
        _objectArtifactAuthorizationCheckerService = objectArtifactAuthorizationCheckerService;
        _securityHelperService = securityHelperService;
        _uilmResourceKeyService = uilmResourceKeyService;
        _repository = repository;
        _praxisShiftService = praxisShiftService;
        _praxisReportService = praxisReportService;
    }
    public bool GenerateShiftReport(ExcelPackage excel, GenerateShiftReportCommand command)
    {
        _logger.LogInformation("Entered into Service: {ServiceName} with command: {Command}",
            nameof(GenerateShiftReportService), JsonConvert.SerializeObject(command));
        try
        {
            GetTranslations(command.LanguageKey);
            var worksheet = excel.Workbook.Worksheets.Add(command.FileName);
            WriteShiftReportHeader(worksheet, command);
            WriteShiftReportTableData(worksheet, command);
            return true;
        }
        catch (Exception e)
        {
            _logger.LogError("Error in Service: {ServiceName} Error Message: {Message} Error Details: {StackTrace}",
                nameof(GenerateShiftReportService), e.Message, e.StackTrace);
            return false;
        }
    }

    public void SetupRolesForShiftReport(PraxisReport report, GenerateShiftReportCommand command)
    {
        var rolesAllowedToRead = new List<string> { $"{RoleNames.Admin}", $"{RoleNames.TaskController}" };
        var rolesAllowedToDelete = new List<string> { $"{RoleNames.Admin}", $"{RoleNames.TaskController}" };

        if (_objectArtifactAuthorizationCheckerService.IsAAdminBUser())
        {
            var organizationId = command?.OrganizationId;
            if (organizationId != null)
            {
                rolesAllowedToRead.Add($"{RoleNames.AdminB_Dynamic}_{organizationId}");
            }
        }

        if (_objectArtifactAuthorizationCheckerService.IsAPowerUser() || _objectArtifactAuthorizationCheckerService.IsAManagementUser())
        {
            var departmentId = command?.ClientId ?? _securityHelperService.ExtractDepartmentIdFromDepartmentLevelUser();
            if (departmentId != null)
            {
                rolesAllowedToRead.Add($"{RoleNames.PowerUser_Dynamic}_{departmentId}");
                rolesAllowedToRead.Add($"{RoleNames.Leitung_Dynamic}_{departmentId}");
            }
        }
        report.RolesAllowedToRead = rolesAllowedToRead.ToArray();
        report.RolesAllowedToDelete = rolesAllowedToDelete.ToArray();
    }

    private void GetTranslations(string languageKey)
    {
        var keyList = new List<string>();
        keyList.AddRange(ShiftReport.LanguageKeys.Values);

        _translatedStrings = _uilmResourceKeyService.GetResourceValueByKeyName(keyList, languageKey);
    }

    private void WriteShiftReportHeader(ExcelWorksheet worksheet, GenerateShiftReportCommand command)
    {
        WriteReportMetadataSection(worksheet, command);
        SetPrimaryTableHeaderRowStyle(worksheet, 1, 4);
        const int headerRowIndex = ShiftReport.HeaderRowIndex;
        var columnIndex = 1;
        foreach (var primaryTableColumnKey in ShiftReport.PrimaryTableColumnKeys)
        {
            worksheet.Cells[headerRowIndex, columnIndex++].Value = _translatedStrings[primaryTableColumnKey.Value];
        }
        var logoColumnIndex = ShiftReport.PrimaryTableColumnKeys.Count;
        //AddHeaderLogo(worksheet, logoColumnIndex);
    }

    private void WriteShiftReportTableData(ExcelWorksheet worksheet, GenerateShiftReportCommand command)
    {
        var shifts = _praxisShiftService.GetShifts(command.ClientId);
        if (!string.IsNullOrWhiteSpace(command.SearchText))
        {
            shifts = shifts
                .Where(x => x.ShiftName.ToLower().Contains(command.SearchText.ToLower()))
                .ToList();
        }

        var rowIndex = ShiftReport.HeaderRowIndex + 1;
        foreach (var shift in shifts)
        {
            // first column
            worksheet.Cells[rowIndex, 1].Value = shift.ShiftName;
            SetHorizontalAlignmentOfCell(worksheet, rowIndex, 1, ExcelHorizontalAlignment.Left);
            SetVerticalAlignmentOfCell(worksheet,  rowIndex, 1, ExcelVerticalAlignment.Top);
            SetCellStyleWithBorderAround(worksheet, rowIndex, 1, ExcelBorderStyle.Thin);
            SetWrapText(worksheet, rowIndex, 1);
            CustomBestFitColumn(worksheet.Column(1), worksheet.Cells[rowIndex, 1], 20, 40);
            
            // second column
            worksheet.Cells[rowIndex, 2].Value = GetGuides(shift);
            SetHorizontalAlignmentOfCell(worksheet, rowIndex, 2, ExcelHorizontalAlignment.Left);
            SetVerticalAlignmentOfCell(worksheet, rowIndex, 2, ExcelVerticalAlignment.Top);
            SetCellStyleWithBorderAround(worksheet, rowIndex, 2, ExcelBorderStyle.Thin);
            SetWrapText(worksheet, rowIndex, 2);
            CustomBestFitColumn(worksheet.Column(2), worksheet.Cells[rowIndex, 2], 20, 40);

            // third column
            worksheet.Cells[rowIndex, 3].Value = GetAttachedDocuments(shift);
            SetHorizontalAlignmentOfCell(worksheet, rowIndex, 3, ExcelHorizontalAlignment.Left);
            SetVerticalAlignmentOfCell(worksheet, rowIndex, 3, ExcelVerticalAlignment.Top);
            SetCellStyleWithBorderAround(worksheet, rowIndex, 3, ExcelBorderStyle.Thin);
            SetWrapText(worksheet, rowIndex, 3);
            CustomBestFitColumn(worksheet.Column(3), worksheet.Cells[rowIndex, 3], 20, 40);

            // fourth column
            worksheet.Cells[rowIndex, 4].Value = GetAttachedForms(shift);
            SetHorizontalAlignmentOfCell(worksheet, rowIndex, 4, ExcelHorizontalAlignment.Left);
            SetVerticalAlignmentOfCell(worksheet, rowIndex, 4, ExcelVerticalAlignment.Top);
            SetCellStyleWithBorderAround(worksheet, rowIndex, 4, ExcelBorderStyle.Thin);
            SetWrapText(worksheet, rowIndex, 4);
            CustomBestFitColumn(worksheet.Column(4), worksheet.Cells[rowIndex, 4], 20, 40);
            rowIndex++;
        }
    }

    private void WriteReportMetadataSection(ExcelWorksheet worksheet, ExportReportCommand command)
    {
        // First Metadata Row
        worksheet.Cells[1, 1].Value = _translatedStrings[ShiftReport.LanguageKeys["DEPARTMENT_NAME"]];
        SetCellValueWithBoldStyle(worksheet, 1, 1);
        SetCellStyleWithBorderAround(worksheet, 1, 1, ExcelBorderStyle.Thin);
        CustomBestFitColumn(worksheet.Column(1), worksheet.Cells[1, 1], 20, 40);

        var departmentDetails = GetDepartmentDetails(command.ClientId);

        worksheet.Cells[1, 2].Value = departmentDetails.ClientName;
        SetCellStyleWithBorderAround(worksheet, 1, 2, ExcelBorderStyle.Thin);
        CustomBestFitColumn(worksheet.Column(2), worksheet.Cells[1, 2], 20, 40);

        // Second Metadata Row
        worksheet.Cells[2, 1].Value = _translatedStrings[ShiftReport.LanguageKeys["REPORT_NAME"]];
        SetCellValueWithBoldStyle(worksheet, 2, 1);
        SetCellStyleWithBorderAround(worksheet, 2, 1, ExcelBorderStyle.Thin);
        CustomBestFitColumn(worksheet.Column(1), worksheet.Cells[2, 1], 20, 40);

        worksheet.Cells[2, 2].Value = command.FileName;
        SetCellStyleWithBorderAround(worksheet, 2, 2, ExcelBorderStyle.Thin);
        CustomBestFitColumn(worksheet.Column(2), worksheet.Cells[2, 2], 20, 40);

        // Third Metadata Row
        worksheet.Cells[3, 1].Value = _translatedStrings[ShiftReport.LanguageKeys["DATE"]];
        SetCellValueWithBoldStyle(worksheet, 3, 1);
        SetCellStyleWithBorderAround(worksheet, 3, 1, ExcelBorderStyle.Thin);
        CustomBestFitColumn(worksheet.Column(1), worksheet.Cells[3, 1], 20, 40);

        var cultureInfo = new CultureInfo(command.LanguageKey);
        worksheet.Cells[3, 2].Value = DateTime.Today.ToString("MMMM dd, yyyy", cultureInfo);
        SetCellStyleWithBorderAround(worksheet, 3, 2, ExcelBorderStyle.Thin);
        CustomBestFitColumn(worksheet.Column(2), worksheet.Cells[3, 2], 20, 40);
    }

    private PraxisClient GetDepartmentDetails(string departmentId)
    {
        var praxisClient = _repository.GetItem<PraxisClient>(x => x.ItemId == departmentId);

        return praxisClient;
    }

    private static void SetCellValueWithBoldStyle(ExcelWorksheet worksheet, int row, int column)
    {
        worksheet.Cells[row, column].Style.Font.Bold = true;
    }
    private static void SetCellStyleWithBorderAround(ExcelWorksheet worksheet, int row, int column, ExcelBorderStyle excelBorderStyle)
    {
        worksheet.Cells[row, column].Style.Border.BorderAround(excelBorderStyle);
    }

    private static void SetHorizontalAlignmentOfCell(ExcelWorksheet worksheet, int row, int column, ExcelHorizontalAlignment horizontalAlignment)
    {
        worksheet.Cells[row, column].Style.HorizontalAlignment = horizontalAlignment;
    }

    private static void SetVerticalAlignmentOfCell(ExcelWorksheet worksheet, int row, int column, ExcelVerticalAlignment verticalAlignment)
    {
        worksheet.Cells[row, column].Style.VerticalAlignment = verticalAlignment;
    }

    private static void SetWrapText(ExcelWorksheet worksheet, int row, int column)
    {
        worksheet.Cells[row, column].Style.WrapText = true;
    }

    private void SetPrimaryTableHeaderRowStyle(ExcelWorksheet worksheet, int startColumn, int endColumn)
    {
        const int headerRowIndex = ShiftReport.HeaderRowIndex;
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
    private void AddHeaderLogo(ExcelWorksheet worksheet, int columnIndex)
    {
        // [firstRow, firstColumn, lastRow, LastColumn]
        worksheet.Cells[1, columnIndex, 2, columnIndex].Merge = true;
        _praxisReportService.AddLogoInExcelReport(worksheet, LibraryReport.LogoSize, columnIndex, rqLatestLogo);
    }

    private string GetGuides(RiqsShiftResponse shift)
    {
        if (shift.PraxisForms == null) return string.Empty;
        var guideNames = shift.PraxisForms.Select(f => f.Name).ToList();
        return string.Join(", ", guideNames);
    }

    private string GetAttachedDocuments(RiqsShiftResponse shift)
    {
        if (shift.Files == null) return string.Empty;
        var fileNames = shift.Files.Select(f => f.DocumentName).ToList();
        return string.Join(", ", fileNames);
    }
    private string GetAttachedForms(RiqsShiftResponse shift)
    {
        if (shift.LibraryForms == null) return string.Empty;
        var formNames = shift.LibraryForms.Select(f => f.LibraryFormName).ToList();
        return string.Join(", ", formNames);
    }
    private void CustomBestFitColumn(ExcelColumn column, ExcelRange cell, int minWidth, int maxWidth)
    {
        var width = (cell?.Value?.ToString()?.Length ?? 0) * 1.2;
        column.Width = Math.Max(column.Width, width);
        column.Width = Math.Min(column.Width, maxWidth);
        column.Width = Math.Max(column.Width, minWidth);
    }
}