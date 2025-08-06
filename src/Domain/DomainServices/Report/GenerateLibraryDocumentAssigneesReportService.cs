using System;
using OfficeOpenXml;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.Reports;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Selise.Ecap.Entities.PrimaryEntities.Dms;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.ExcelReports;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.LibraryModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Entities.LibraryModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Queries.LibraryModule;
using static Selise.Ecap.SC.PraxisMonitor.Contracts.Constants.ReportConstants;
using OfficeOpenXml.Style;
using SeliseBlocks.Genesis.Framework.PDS.Entity;
using Selise.Ecap.SC.PraxisMonitor.Contracts.ResponseModels.LibraryModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.Report;

public class GenerateLibraryDocumentAssigneesReportService : IGenerateLibraryDocumentAssigneesReportService
{
    private Dictionary<string, string> _translatedStrings = new Dictionary<string, string>();

    private readonly ILogger<GenerateLibraryDocumentAssigneesReportService> _logger;
    private readonly IUilmResourceKeyService _uilmResourceKeyService;
    private readonly IRepository _repository;
    private readonly ILibraryDocumentAssigneeService _libraryDocumentAssigneeService;
    private readonly IPraxisReportService _praxisReportService;

    public GenerateLibraryDocumentAssigneesReportService(
        ILogger<GenerateLibraryDocumentAssigneesReportService> logger,
        IUilmResourceKeyService uilmResourceKeyService,
        IRepository repository,
        ILibraryDocumentAssigneeService libraryDocumentAssigneeService,
        IPraxisReportService praxisReportService)
    {
        _logger = logger;
        _uilmResourceKeyService = uilmResourceKeyService;
        _repository = repository;
        _libraryDocumentAssigneeService = libraryDocumentAssigneeService;
        _praxisReportService = praxisReportService;
    }
    public async Task<bool> GenerateLibraryDocumentAssigneesReportAsync(ExcelPackage excel, ExportLibraryDocumentAssigneesReportCommand command)
    {
        try
        {
            GetTranslations(command.LanguageKey);
            var reportSheet = excel.Workbook.Worksheets.Add(command.FileName);
            var assignees = await _libraryDocumentAssigneeService.GetPurposeWiseLibraryAssignees(
                new LibraryDocumentAssigneeQuery
                {
                    ObjectArtifactId = command.ObjectArtifactId,
                    Purpose = command.Purpose
                });
            assignees = GetFullAssignedDepartments(assignees);
            var objectArtifact =
                await _repository.GetItemAsync<ObjectArtifact>(c => c.ItemId == command.ObjectArtifactId);
            WriteReportMetaData(reportSheet, command.FileNameWithExtension, objectArtifact);
            AddLogoToWorksheet(reportSheet, 1, 4);
            WriteReportHeader(reportSheet, command);
            WriteReportData(reportSheet, assignees, command.ObjectArtifactId);
            return true;
        }
        catch (Exception e)
        {
            _logger.LogError("Exception occurred during generating library document assignees report.");
            _logger.LogError("Exception Message: {Message} Exception Details: {StackTrace}", e.Message, e.StackTrace);
            return false;
        }
    }

    public IRowLevelSecurity PrepareRowLevelSecurity(List<string> clientIds)
    {
        IRowLevelSecurity rowLevelSecurity = new PraxisReport();
        var allowedRoles = _praxisReportService.GetDynamicRolesForPraxisReportFromCLietIds(clientIds).ToArray();
        rowLevelSecurity.RolesAllowedToRead = allowedRoles;
        return rowLevelSecurity;
    }

    public async Task UpdateClientsInReport(string reportFileId, List<string> clientIds)
    {
        var report = await _repository.GetItemAsync<PraxisReport>(p => p.ReportFileId.Equals(reportFileId));
        report.ClientIds = clientIds;
        await _repository.UpdateAsync(p => p.ReportFileId.Equals(reportFileId), report);
    }

    private void WriteReportHeader(ExcelWorksheet reportSheet, ExportLibraryDocumentAssigneesReportCommand command)
    {
        try
        {
            const int rowIndex = LibraryDocumentAssigneeReport.HeaderRowIndex;
            SetPrimaryTableHeaderRowStyle(reportSheet);

            reportSheet.Cells[rowIndex, 1].Value =
                _translatedStrings[LibraryDocumentAssigneeReport.LanguageKeys[nameof(LibraryDocumentAssigneeReport.LanguageKeyEnum.UNIT)]];
            reportSheet.Cells[rowIndex, 2].Value = _translatedStrings[LibraryDocumentAssigneeReport.LanguageKeys[nameof(LibraryDocumentAssigneeReport.LanguageKeyEnum.ASSIGNED_TO)]];
            reportSheet.Cells[rowIndex, 3].Value =
                _translatedStrings[LibraryDocumentAssigneeReport.LanguageKeys[nameof(LibraryDocumentAssigneeReport.LanguageKeyEnum.READ_UNREAD_STATUS)]];
            reportSheet.Cells[rowIndex, 4].Value =
                _translatedStrings[LibraryDocumentAssigneeReport.LanguageKeys[nameof(LibraryDocumentAssigneeReport.LanguageKeyEnum.READ_DATE)]];
        }
        catch (Exception e)
        {
            _logger.LogError("Exception occurred during writing report header.");
            _logger.LogError("Exception Message: {Message} Exception Details: {StackTrace}", e.Message, e.StackTrace);
        }
    }

    private void WriteReportMetaData(ExcelWorksheet reportSheet, string reportName, ObjectArtifact artifact)
    {
        try
        {
            reportSheet.Cells[1, 1].Value = _translatedStrings[LibraryDocumentAssigneeReport.LanguageKeys[nameof(LibraryDocumentAssigneeReport.LanguageKeyEnum.REPORT_NAME)]];
            reportSheet.Cells[1, 2].Value = reportName;

            reportSheet.Cells[2, 1].Value = _translatedStrings[LibraryDocumentAssigneeReport.LanguageKeys[nameof(LibraryDocumentAssigneeReport.LanguageKeyEnum.DATE)]];
            reportSheet.Cells[2, 2].Value = DateTime.Today.ToString("dd/MM/yyyy");

            reportSheet.Cells[3, 1].Value = _translatedStrings[LibraryDocumentAssigneeReport.LanguageKeys[nameof(LibraryDocumentAssigneeReport.LanguageKeyEnum.FILE_NAME)]];
            reportSheet.Cells[3, 2].Value = artifact.Name;

            reportSheet.Cells[4, 1].Value = _translatedStrings[LibraryDocumentAssigneeReport.LanguageKeys[nameof(LibraryDocumentAssigneeReport.LanguageKeyEnum.VERSION)]];
            reportSheet.Cells[4, 2].Value = GetMetaDataValue(artifact.MetaData, "Version")?.Value ?? "-";

            reportSheet.Cells[5, 1].Value = _translatedStrings[LibraryDocumentAssigneeReport.LanguageKeys[nameof(LibraryDocumentAssigneeReport.LanguageKeyEnum.KEYWORDS)]];
            reportSheet.Cells[5, 2].Value = string.Join(", ", GetMetaDataValue(artifact.MetaData, "KeyWords")?.Value);

            reportSheet.Cells[6, 1].Value = _translatedStrings[LibraryDocumentAssigneeReport.LanguageKeys[nameof(LibraryDocumentAssigneeReport.LanguageKeyEnum.UPLOADED_BY)]];
            reportSheet.Cells[6, 2].Value =
                string.Join(Environment.NewLine, artifact.OwnerName, DateToString(artifact.CreateDate));

            reportSheet.Cells[7, 1].Value = _translatedStrings[LibraryDocumentAssigneeReport.LanguageKeys[nameof(LibraryDocumentAssigneeReport.LanguageKeyEnum.APPROVED_BY)]];
            reportSheet.Cells[7, 2].Value = GetApproverInfo(artifact.ItemId);

            reportSheet.Cells[8, 1].Value = _translatedStrings[LibraryDocumentAssigneeReport.LanguageKeys[nameof(LibraryDocumentAssigneeReport.LanguageKeyEnum.ACTIVE)]];
            reportSheet.Cells[8, 2].Value = GetMetaDataValue(artifact.MetaData, "Status")?.Value == "1" ? "Yes" : "No";
        }
        catch (Exception e)
        {
            _logger.LogError("Exception occurred during writing report meta data.");
            _logger.LogError("Exception Message: {Message} Exception Details: {StackTrace}", e.Message, e.StackTrace);
        }
    }

    private void WriteReportData(ExcelWorksheet reportSheet, List<AssignedDepartment> assignees, string objectArtifactId) 
    {
        try
        {
            var rowIndex = LibraryDocumentAssigneeReport.HeaderRowIndex + 1;
            foreach (var unit in assignees)
            {
                reportSheet.Cells[rowIndex, 1].Value = unit.Name;
                var startRow = rowIndex;
                var readIds = unit.Assignees.Where(a => a.IsMarkedAsRead).Select(a => a.Id).ToList();
                var markedAsReads = GetDocumentsMarkedAsReads(readIds, objectArtifactId);
                foreach (var assignee in unit.Assignees)
                {
                    reportSheet.Cells[rowIndex, 2].Value = assignee.Name;
                    reportSheet.Cells[rowIndex, 3].Value = assignee.IsMarkedAsRead ? "Read" : "Unread";
                    reportSheet.Cells[rowIndex, 4].Value = markedAsReads.LastOrDefault(a => a.ReadByUserId == assignee.Id)?.ReadOn.ToString("dd.MM.yyyy hh:mm tt") ?? string.Empty;
                    ++rowIndex;
                }

                var currentBlocks = reportSheet.Cells[startRow, 1, rowIndex - 1, 4];
                currentBlocks.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                reportSheet.Cells[rowIndex, 1, rowIndex, 4].Merge = true;
                ++rowIndex;
            }
        }
        catch (Exception e)
        {
            _logger.LogError("Exception occurred during writing report data.");
            _logger.LogError("Exception Message: {Message} Exception Details: {StackTrace}", e.Message, e.StackTrace);
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
            // Add Logo
            var appDirectoryFolderPath = Path.GetDirectoryName(AppDomain.CurrentDomain.BaseDirectory);

            if (appDirectoryFolderPath == null)
            {
                _logger.LogError("App directory folder path is null, logo is not found.");
                return;
            }

            var logoFilePath = @"" + appDirectoryFolderPath + rqLatestLogo;
            //Pass the filepath and filename to the StreamWriter Constructor
            if (!File.Exists(logoFilePath))
            {
                _logger.LogError("Logo is not found. File '{LogoFilePath}' doesn't exist.", logoFilePath);
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
            _logger.LogError("Error while adding logo to excel, Error: {ErrorMessage}. Exception Details: {StackTrace}", ex.Message, ex.StackTrace);
        }
        finally
        {
            GC.Collect();
        }
    }

    private void SetPrimaryTableHeaderRowStyle(ExcelWorksheet worksheet, int startColumn = 1, int endColumn = 4)
    {
        const int headerRowIndex = LibraryDocumentAssigneeReport.HeaderRowIndex;
        var widths = new List<int> { 0, 30, 30, 25, 28 };
        for (var col = startColumn; col <= endColumn; col++)
        {
            var cell = worksheet.Cells[headerRowIndex, col];
            worksheet.Column(col).Width = widths[col];
            SetStyleForSpecificCell(cell);
        }
    }

    private void SetStyleForSpecificCell(ExcelRange cell)
    {
        var primaryTableHeaderBackgroundColor = Color.FromArgb(222, 222, 222);
        cell.Style.Font.Bold = true;
        cell.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
        cell.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
        cell.Style.Border.BorderAround(ExcelBorderStyle.Thin);
        cell.Style.Fill.PatternType = ExcelFillStyle.Solid;
        cell.Style.Fill.BackgroundColor.SetColor(primaryTableHeaderBackgroundColor);
    }

    private void GetTranslations(string languageKey)
    {
        var keyList = new List<string>();
        keyList.AddRange(LibraryDocumentAssigneeReport.LanguageKeys.Values);

        _translatedStrings = _uilmResourceKeyService.GetResourceValueByKeyName(keyList, languageKey);
    }

    private MetaValuePair GetMetaDataValue(IDictionary<string, MetaValuePair> metaData, string key)
    {
        return metaData.TryGetValue(key, out var val) ? val : null;
    }

    private string GetApproverInfo(string objectArtifactId)
    {
        var history = _repository.GetItem<RiqsObjectArtifactMapping>(r =>
            r.ObjectArtifactId == objectArtifactId);
        var approver = history?.ApproverInfos?
            .Select(a => string.Join(Environment.NewLine, a.ApproverName, DateToString(a.ApprovedDate)))
            .ToList();
        return approver != null ? string.Join(Environment.NewLine, approver) : "-";
    }

    private string DateToString(DateTime date) => date.ToString("MMMM dd, yyyy", CultureInfo.InvariantCulture);

    private List<AssignedDepartment> GetFullAssignedDepartments(List<AssignedDepartment> assignees)
    {
        foreach (var assignedDepartment in assignees)
        {
            if (assignedDepartment.Assignees?.Count == 0)
            {
                var clientId = assignedDepartment.Id;
                var users = GetUsersByClientId(clientId);
                assignedDepartment.Assignees = users.Select(u => new AssigneeSummary
                {
                    Id = u.ItemId,
                    Name = u.DisplayName,
                }).ToList();
            }
        }
        return assignees;
    }

    private List<PraxisUser> GetUsersByClientId(string clientId)
    {
        var users = _repository.GetItems<PraxisUser>(u => 
            !u.IsMarkedToDelete &&
            u.ClientList != null &&
            u.ClientList.Any(c => c.ClientId == clientId) &&
            !(u.Roles != null && u.Roles.Contains(RoleNames.GroupAdmin)))
            .ToList();
        return users;
    }

    private List<DocumentsMarkedAsRead> GetDocumentsMarkedAsReads(List<string> readByUserIds, string objectArtifactId)
    {
        var documentsMarkedAsReads = _repository.GetItems<DocumentsMarkedAsRead>(
            o => readByUserIds.Contains(o.ReadByUserId) && o.ObjectArtifactId == objectArtifactId
        )?.ToList();

        return documentsMarkedAsReads != null
            ? documentsMarkedAsReads
            : new List<DocumentsMarkedAsRead>();
    }
}