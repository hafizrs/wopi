using Microsoft.Extensions.Logging;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using Selise.Ecap.Entities;
using Selise.Ecap.Entities.PrimaryEntities.Dms;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.ExcelReports;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Dms;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.LibraryModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.Reports;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.ResponseModels.LibraryModule;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Entities.LibraryModule;
using static Selise.Ecap.SC.PraxisMonitor.Contracts.Constants.ReportConstants;
using SharedOrganizationInfo = Selise.Ecap.Entities.PrimaryEntities.Dms.SharedOrganizationInfo;
using Aspose.Words.Tables;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.Report
{
    public class GenerateLibraryReportService : IGenerateLibraryReportService
    {
        private Dictionary<string, string> _allViewReportTranslatedStrings = new Dictionary<string, string>();
        private Dictionary<string, string> _approvalViewReportTranslatedStrings = new Dictionary<string, string>();
        private Dictionary<string, string> _wordFilesViewReportTranslatedStrings = new Dictionary<string, string>();
        private Dictionary<string, string> _formFilesViewReportTranslatedStrings = new Dictionary<string, string>();
        private Dictionary<string, string> _folderStructureReportTranslatedStrings = new Dictionary<string, string>();
        private List<int> _folderStructureColumnWiseMaxWidth = new List<int> { 40, 40, 40, 20, 30, 30, 30, 30, 20};

        private readonly IObjectArtifactSearchService _objectArtifactSearchService;
        private readonly IUilmResourceKeyService _uilmResourceKeyService;
        private readonly IPraxisReportService _praxisReportService;
        private readonly IObjectArtifactUtilityService _objectArtifactUtilityService;
        private readonly IObjectArtifactAuthorizationCheckerService _objectArtifactAuthorizationCheckerService;
        private readonly IObjectArtifactReportsSharedDataResponseGeneratorService _objectArtifactReportsSharedDataResponseGeneratorService;
        private readonly ISecurityHelperService _securityHelperService;
        private readonly ILogger<GenerateLibraryReportService> _logger;
        private readonly IBlocksMongoDbDataContextProvider _mongoDbDataContextProvider;

        public GenerateLibraryReportService
        (
            IObjectArtifactSearchService objectArtifactSearchService,
            IUilmResourceKeyService uilmResourceKeyService,
            IPraxisReportService praxisReportService,
            IObjectArtifactUtilityService objectArtifactUtilityService,
            IObjectArtifactAuthorizationCheckerService objectArtifactAuthorizationCheckerService,
            IObjectArtifactReportsSharedDataResponseGeneratorService objectArtifactReportsSharedDataResponseGeneratorService,
            ISecurityHelperService securityHelperService,
            ILogger<GenerateLibraryReportService> logger,
            IBlocksMongoDbDataContextProvider mongoDbDataContextProvider)
        {
            _objectArtifactSearchService = objectArtifactSearchService;
            _uilmResourceKeyService = uilmResourceKeyService;
            _praxisReportService = praxisReportService;
            _objectArtifactUtilityService = objectArtifactUtilityService;
            _objectArtifactAuthorizationCheckerService = objectArtifactAuthorizationCheckerService;
            _objectArtifactReportsSharedDataResponseGeneratorService = objectArtifactReportsSharedDataResponseGeneratorService;
            _securityHelperService = securityHelperService;
            _logger = logger;
            _mongoDbDataContextProvider = mongoDbDataContextProvider;
        }

        public bool GenerateLibraryReport(ExcelPackage excel, GenerateLibraryReportCommand command)
        {
            try
            {
                var viewMode = GetViewMode(command.Type);

                switch (viewMode)
                {
                    case LibraryViewModeEnum.ALL:
                        WriteFolderStructureLibraryReport(excel, command);
                        break;
                    case LibraryViewModeEnum.APPROVAL_VIEW:

                        WriteApprovalViewLibraryReport(excel, command, LibraryViewModeEnum.APPROVAL_VIEW);
                        break;
                    case LibraryViewModeEnum.DOCUMENT:

                        WriteWordFilesViewLibraryReport(excel, command, LibraryViewModeEnum.DOCUMENT);
                        break;
                    case LibraryViewModeEnum.FORM:

                        WriteFormFilesViewLibraryReport(excel, command, LibraryViewModeEnum.FORM);
                        break;
                    default:
                        throw new ArgumentException($"Unsupported view mode: {viewMode}");
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError("Exception occured during Library Report generation");
                _logger.LogError("Exception Message: {Message}. Exception Details: {StackTrace}.", ex.Message, ex.StackTrace);
                return false;
            }
        }

        public bool GenerateLibraryFolderStructureReport(ExcelPackage excel, GenerateLibraryReportCommand command)
        {
            try
            {
                WriteFolderStructureLibraryReport(excel, command);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError("Exception occured during Library Folder Structure Report generation");
                _logger.LogError("Exception Message: {Message}. Exception Details: {StackTrace}.", ex.Message, ex.StackTrace);
                return false;
            }
        }

        public void SetupRolesForLibraryReport(PraxisReport report, GenerateLibraryReportCommand command)
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
                string organizationId = command?.OrganizationId;

                if (organizationId != null)
                {
                    var adminBRole = $"{RoleNames.AdminB_Dynamic}_{organizationId}";
                    rolesAllowedToRead.Add(adminBRole);
                }
            }

            if (_objectArtifactAuthorizationCheckerService.IsAPowerUser() || _objectArtifactAuthorizationCheckerService.IsAManagementUser())
            {
                string clientId = command?.ClientId;
                if (clientId == null)
                {
                    clientId = _securityHelperService.ExtractDepartmentIdFromDepartmentLevelUser();
                }

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

        private SearchResult GetAllObjectArtifacts(GenerateLibraryReportCommand command, LibraryViewModeEnum viewMode)
        {
            var allResults = new List<dynamic>();
            var pageSize = 100;

            // Get all the folder type object artifacts
            if ((command.ArtifactType == null || command.ArtifactType == ArtifactTypeEnum.Folder) && viewMode == LibraryViewModeEnum.ALL)
            {
                var currentPageNumber = 1;
                while (true)
                {
                    var artifactSearchQuery = new ObjectArtifactSearchCommand()
                    {
                        OrganizationId = command.OrganizationId,
                        DepartmentId = command.ClientId,
                        Type = command.Type,
                        ArtifactType = ArtifactTypeEnum.Folder,
                        Text = command.Text,
                        ParentId = command.ParentId,
                        Filter = command.SearchFilter,
                        PageSize = pageSize,
                        PageNumber = currentPageNumber
                    };

                    var searchResults = _objectArtifactSearchService.InitiateSearchObjectArtifact(artifactSearchQuery);

                    ProcessData(searchResults);
                    allResults.AddRange(searchResults?.Data ?? new List<dynamic>());
                    currentPageNumber++;  // increment page number for the next iteration

                    if (searchResults == null || searchResults?.Data?.Count() < pageSize)
                        break;  // exit loop if no results, or results are less than the page size
                }
            }


            // Get all the file type object artifacts
            if (command.ArtifactType == null || command.ArtifactType == ArtifactTypeEnum.File)
            {
                var currentPageNumber = 1;
                while (true)
                {
                    var artifactSearchQuery = new ObjectArtifactSearchCommand()
                    {
                        OrganizationId = command.OrganizationId,
                        DepartmentId = command.ClientId,
                        Type = command.Type,
                        ArtifactType = ArtifactTypeEnum.File,
                        Text = command.Text,
                        ParentId = command.ParentId,
                        Filter = command.SearchFilter,
                        PageSize = pageSize,
                        PageNumber = currentPageNumber
                    };

                    var searchResults = _objectArtifactSearchService.InitiateSearchObjectArtifact(artifactSearchQuery);

                    ProcessData(searchResults);
                    allResults.AddRange(searchResults?.Data);
                    currentPageNumber++;  // increment page number for the next iteration


                    if (searchResults == null || searchResults?.Data?.Count() < pageSize)
                        break;  // exit loop if no results, or results are less than the page size
                }
            }

            var searchResult = new SearchResult(allResults, null);

            return searchResult;
        }

        private void ProcessData(SearchResult objectArtifactSearchResult)
        {
            var parentObjectArtifactIds = GetParentObjectArtifactIds(objectArtifactSearchResult.Data);
            var parentObjectArtifacts = _objectArtifactUtilityService.GetObjectArtifactNames(parentObjectArtifactIds);
            foreach (var objectArtifact in objectArtifactSearchResult.Data)
            {
                var organizationId = GetSearchResultDictionaryValue(objectArtifact, nameof(RiqsObjectArtifact.OrganizationId)) as string;
                var metaData = GetSearchResultDictionaryValue(objectArtifact, nameof(RiqsObjectArtifact.MetaData)) as IDictionary<string, MetaValuePair>;
                var sharedOrganizationList = GetSearchResultDictionaryValue(objectArtifact, nameof(RiqsObjectArtifact.SharedOrganizationList)) as List<SharedOrganizationInfo>;

                objectArtifact[nameof(RiqsObjectArtifact.ReportAssigneeDetail)] =
                    _objectArtifactReportsSharedDataResponseGeneratorService.GetObjectArtifactAssigneeDetailResponse(
                        organizationId, metaData, sharedOrganizationList);
                var parentObjectArtifactId = GetSearchResultDictionaryValue(objectArtifact, nameof(RiqsObjectArtifact.ParentId)) as string;
                objectArtifact[nameof(RiqsObjectArtifact.ParentName)] = GetObjectArtifactNameFromAList(parentObjectArtifactId, parentObjectArtifacts);
            }
        }

        private string[] GetParentObjectArtifactIds(IEnumerable<dynamic> objectArtifacts)
        {
            var parentIds = new List<string>();
            foreach (var objectArtifact in objectArtifacts)
            {
                var parentId = GetSearchResultDictionaryValue(objectArtifact, nameof(ObjectArtifact.ParentId)) as string;
                parentIds.Add(parentId);
            }
            return parentIds.ToArray();
        }

        private string GetObjectArtifactNameFromAList(string artifactId, List<ObjectArtifact> parentObjectArtifacts)
        {
            return parentObjectArtifacts.FirstOrDefault(p => p.ItemId == artifactId)?.Name ?? string.Empty;
        }

        private SearchResult GetWholeDepartmentObjectArtifacts(GenerateLibraryReportCommand command)
        {
            var artifactList = new List<dynamic>();
            void GetArtifacts(string parentId, string path = "")
            {
                var newCommand = new GenerateLibraryReportCommand
                {
                    OrganizationId = command.OrganizationId,
                    ClientId = command.ClientId,
                    Type = command.Type,
                    ArtifactType = ArtifactTypeEnum.Folder,
                    Text = command.Text,
                    ParentId = parentId,
                    SearchFilter = command.SearchFilter
                };
                var localFolders = GetAllObjectArtifacts(newCommand, LibraryViewModeEnum.ALL).Data;
                newCommand.ArtifactType = ArtifactTypeEnum.File;
                var localFiles = GetAllObjectArtifacts(newCommand, LibraryViewModeEnum.ALL).Data;
                foreach (var item in localFolders)
                {
                    item.Add("RelativePath", path);
                }

                foreach (var item in localFiles)
                {
                    item.Add("RelativePath", path);
                }

                foreach (var folder in localFolders)
                {
                    artifactList.Add(folder);
                    var folderId = GetSearchResultDictionaryValue(folder, nameof(RiqsObjectArtifact.ItemId)) as string;
                    var folderName = GetSearchResultDictionaryValue(folder, nameof(RiqsObjectArtifact.Name)) as string;
                    var relativePath = string.IsNullOrEmpty(path) ? $"{folderName}" : $"{path} / {folderName}";
                    GetArtifacts(folderId, relativePath);
                }
                artifactList.AddRange(localFiles);
            }
            GetArtifacts(null);
            var serachResult = new SearchResult(artifactList, null);
            return serachResult;
        }

        private void WriteAllViewLibraryReport(ExcelPackage excel, GenerateLibraryReportCommand command, LibraryViewModeEnum viewMode)
        {
            GetAllViewReportTranslations(command.LanguageKey);

            var reportCreationDate = DateTime.Today;
            var worksheet = excel.Workbook.Worksheets.Add(command.FileName);

            var searchResults = GetAllObjectArtifacts(command, viewMode);

            WriteAllViewLibraryReportHeader(worksheet, command, reportCreationDate);

            WriteAllViewPrimaryTable(worksheet, searchResults);

        }

        private void WriteApprovalViewLibraryReport(ExcelPackage excel, GenerateLibraryReportCommand command, LibraryViewModeEnum viewMode)
        {
            GetApprovalViewReportTranslations(command.LanguageKey);

            var reportCreationDate = DateTime.Today;
            var worksheet = excel.Workbook.Worksheets.Add(command.FileName);

            var searchResults = GetAllObjectArtifacts(command, viewMode);

            WriteApprovalViewLibraryReportHeader(worksheet, command, reportCreationDate);

            WriteApprovalViewPrimaryTable(worksheet, searchResults);
        }

        private void WriteWordFilesViewLibraryReport(ExcelPackage excel, GenerateLibraryReportCommand command, LibraryViewModeEnum viewMode)
        {
            GetWordFilesViewReportTranslations(command.LanguageKey);

            var reportCreationDate = DateTime.Today;
            var worksheet = excel.Workbook.Worksheets.Add(command.FileName);

            var searchResults = GetAllObjectArtifacts(command, viewMode);

            WriteWordFilesViewLibraryReportHeader(worksheet, command, reportCreationDate);

            WriteWordFilesViewPrimaryTable(worksheet, searchResults);
        }

        private void WriteFormFilesViewLibraryReport(ExcelPackage excel, GenerateLibraryReportCommand command,
            LibraryViewModeEnum viewMode)
        {
            GetFormFilesViewReportTranslations(command.LanguageKey);

            var reportCreationDate = DateTime.Today;
            var worksheet = excel.Workbook.Worksheets.Add(command.FileName);

            var searchResults = GetAllObjectArtifacts(command, viewMode);

            WriteFormFilesViewLibraryReportHeader(worksheet, command, reportCreationDate);

            WriteFormFilesViewPrimaryTable(worksheet, searchResults);
        }

        private void WriteFolderStructureLibraryReport(ExcelPackage excel, GenerateLibraryReportCommand command)
        {
            GetFolderStructureReportTranslations(command.LanguageKey);
            var reportCreationDate = DateTime.Today;
            var worksheet = excel.Workbook.Worksheets.Add(command.FileName);

            var searchResults = GetWholeDepartmentObjectArtifacts(command);

            WriteFolderStructureLibraryReportHeader(worksheet, command, reportCreationDate);

            WriteFolderStructurePrimaryTable(worksheet, searchResults);
        }

        private void GetAllViewReportTranslations(string languageKey)
        {
            var translationKeys = new List<string>();
            translationKeys.AddRange(LibraryReport.MetadataKeys.Values);
            translationKeys.AddRange(LibraryReport.AllViewPrimaryTableColumnKeys.Values);

            _allViewReportTranslatedStrings = _uilmResourceKeyService.GetResourceValueByKeyName(translationKeys, languageKey);
        }

        private void GetApprovalViewReportTranslations(string languageKey)
        {
            var translationKeys = new List<string>();
            translationKeys.AddRange(LibraryReport.MetadataKeys.Values);
            translationKeys.AddRange(LibraryReport.ApprovalViewPrimaryTableColumnKeys.Values);

            _approvalViewReportTranslatedStrings = _uilmResourceKeyService.GetResourceValueByKeyName(translationKeys, languageKey);
        }

        private void GetWordFilesViewReportTranslations(string languageKey)
        {
            var translationKeys = new List<string>();
            translationKeys.AddRange(LibraryReport.MetadataKeys.Values);
            translationKeys.AddRange(LibraryReport.WordFilesViewPrimaryTableColumnKeys.Values);

            _wordFilesViewReportTranslatedStrings = _uilmResourceKeyService.GetResourceValueByKeyName(translationKeys, languageKey);
        }

        private void GetFormFilesViewReportTranslations(string languageKey)
        {
            var translationKeys = new List<string>();
            translationKeys.AddRange(LibraryReport.MetadataKeys.Values);
            translationKeys.AddRange(LibraryReport.FormViewPrimaryTableColumnKeys.Values);

            _formFilesViewReportTranslatedStrings = _uilmResourceKeyService.GetResourceValueByKeyName(translationKeys, languageKey);
        }

        private void GetFolderStructureReportTranslations(string languageKey)
        {
            var translationKeys = new List<string>();
            translationKeys.AddRange(LibraryReport.MetadataKeys.Values);
            translationKeys.AddRange(LibraryReport.FolderStructurePrimaryTableColumnKeys.Values);

            _folderStructureReportTranslatedStrings = _uilmResourceKeyService.GetResourceValueByKeyName(translationKeys, languageKey);
        }

        private void WriteAllViewLibraryReportMetadataSection(ExcelWorksheet worksheet, GenerateLibraryReportCommand command, DateTime creationDate)
        {
            // First Metadata Row
            worksheet.Cells[1, 1].Value = ToTitleCase(_allViewReportTranslatedStrings[LibraryReport.MetadataKeys["REPORT_NAME"]]);
            SetCellValueWithBoldStyle(worksheet, 1, 1);
            SetCellStyleWithBorderAround(worksheet, 1, 1, ExcelBorderStyle.Thin);

            worksheet.Cells[1, 2].Value = command.FileName;
            SetCellStyleWithBorderAround(worksheet, 1, 2, ExcelBorderStyle.Thin);

            // Second Metadata Row
            worksheet.Cells[2, 1].Value = ToTitleCase(_allViewReportTranslatedStrings[LibraryReport.MetadataKeys["DATE"]]);
            SetCellValueWithBoldStyle(worksheet, 2, 1);
            SetCellStyleWithBorderAround(worksheet, 2, 1, ExcelBorderStyle.Thin);

            CultureInfo cultureInfo = new CultureInfo(command.LanguageKey);
            worksheet.Cells[2, 2].Value = creationDate.ToString("MMMM dd, yyyy", cultureInfo);
            SetCellStyleWithBorderAround(worksheet, 2, 2, ExcelBorderStyle.Thin);
        }

        private void WriteApprovalViewLibraryReportMetadataSection(ExcelWorksheet worksheet, GenerateLibraryReportCommand command, DateTime creationDate)
        {
            // First Metadata Row
            worksheet.Cells[1, 1].Value = ToTitleCase(_approvalViewReportTranslatedStrings[LibraryReport.MetadataKeys["REPORT_NAME"]]);
            SetCellValueWithBoldStyle(worksheet, 1, 1);
            SetCellStyleWithBorderAround(worksheet, 1, 1, ExcelBorderStyle.Thin);

            worksheet.Cells[1, 2].Value = command.FileName;
            SetCellStyleWithBorderAround(worksheet, 1, 2, ExcelBorderStyle.Thin);

            // Second Metadata Row
            worksheet.Cells[2, 1].Value = ToTitleCase(_approvalViewReportTranslatedStrings[LibraryReport.MetadataKeys["DATE"]]);
            SetCellValueWithBoldStyle(worksheet, 2, 1);
            SetCellStyleWithBorderAround(worksheet, 2, 1, ExcelBorderStyle.Thin);

            CultureInfo cultureInfo = new CultureInfo(command.LanguageKey);
            worksheet.Cells[2, 2].Value = creationDate.ToString("MMMM dd, yyyy", cultureInfo);
            SetCellStyleWithBorderAround(worksheet, 2, 2, ExcelBorderStyle.Thin);
        }

        private void WriteWordFilesViewLibraryReportMetadataSection(ExcelWorksheet worksheet, GenerateLibraryReportCommand command, DateTime creationDate)
        {
            // First Metadata Row
            worksheet.Cells[1, 1].Value = ToTitleCase(_wordFilesViewReportTranslatedStrings[LibraryReport.MetadataKeys["REPORT_NAME"]]);
            SetCellValueWithBoldStyle(worksheet, 1, 1);
            SetCellStyleWithBorderAround(worksheet, 1, 1, ExcelBorderStyle.Thin);

            worksheet.Cells[1, 2].Value = command.FileName;
            SetCellStyleWithBorderAround(worksheet, 1, 2, ExcelBorderStyle.Thin);

            // Second Metadata Row
            worksheet.Cells[2, 1].Value = ToTitleCase(_wordFilesViewReportTranslatedStrings[LibraryReport.MetadataKeys["DATE"]]);
            SetCellValueWithBoldStyle(worksheet, 2, 1);
            SetCellStyleWithBorderAround(worksheet, 2, 1, ExcelBorderStyle.Thin);

            CultureInfo cultureInfo = new CultureInfo(command.LanguageKey);
            worksheet.Cells[2, 2].Value = creationDate.ToString("MMMM dd, yyyy", cultureInfo);
            SetCellStyleWithBorderAround(worksheet, 2, 2, ExcelBorderStyle.Thin);
        }

        private void WriteFormFilesViewLibraryReportMetadataSection(ExcelWorksheet worksheet, GenerateLibraryReportCommand command, DateTime creationDate)
        {
            // First Metadata Row
            worksheet.Cells[1, 1].Value = ToTitleCase(_formFilesViewReportTranslatedStrings[LibraryReport.MetadataKeys["REPORT_NAME"]]);
            SetCellValueWithBoldStyle(worksheet, 1, 1);
            SetCellStyleWithBorderAround(worksheet, 1, 1, ExcelBorderStyle.Thin);

            worksheet.Cells[1, 2].Value = command.FileName;
            SetCellStyleWithBorderAround(worksheet, 1, 2, ExcelBorderStyle.Thin);

            // Second Metadata Row
            worksheet.Cells[2, 1].Value = ToTitleCase(_formFilesViewReportTranslatedStrings[LibraryReport.MetadataKeys["DATE"]]);
            SetCellValueWithBoldStyle(worksheet, 2, 1);
            SetCellStyleWithBorderAround(worksheet, 2, 1, ExcelBorderStyle.Thin);

            CultureInfo cultureInfo = new CultureInfo(command.LanguageKey);
            worksheet.Cells[2, 2].Value = creationDate.ToString("MMMM dd, yyyy", cultureInfo);
            SetCellStyleWithBorderAround(worksheet, 2, 2, ExcelBorderStyle.Thin);
        }

        private void WriteFolderStructureLibraryReportMetadataSection(ExcelWorksheet worksheet, GenerateLibraryReportCommand command, DateTime creationDate)
        {
            // First Metadata Row
            worksheet.Cells[1, 1].Value = ToTitleCase(_folderStructureReportTranslatedStrings[LibraryReport.MetadataKeys["REPORT_NAME"]]);
            SetCellValueWithBoldStyle(worksheet, 1, 1);
            SetCellStyleWithBorderAround(worksheet, 1, 1, ExcelBorderStyle.Thin);
            worksheet.Cells[1, 1].Style.VerticalAlignment = ExcelVerticalAlignment.Top;
            CustomBestFitColumn(worksheet.Column(1), worksheet.Cells[1, 1], 20, _folderStructureColumnWiseMaxWidth[0]);

            worksheet.Cells[1, 2].Value = command.FileName;
            SetCellStyleWithBorderAround(worksheet, 1, 2, ExcelBorderStyle.Thin);
            worksheet.Cells[1, 2].Style.VerticalAlignment = ExcelVerticalAlignment.Top;
            CustomBestFitColumn(worksheet.Column(2), worksheet.Cells[1, 2], 20, _folderStructureColumnWiseMaxWidth[1]);

            // Second Metadata Row
            worksheet.Cells[2, 1].Value = ToTitleCase(_folderStructureReportTranslatedStrings[LibraryReport.MetadataKeys["DATE"]]);
            SetCellValueWithBoldStyle(worksheet, 2, 1);
            SetCellStyleWithBorderAround(worksheet, 2, 1, ExcelBorderStyle.Thin);
            worksheet.Cells[2, 1].Style.VerticalAlignment = ExcelVerticalAlignment.Top;
            CustomBestFitColumn(worksheet.Column(1), worksheet.Cells[2, 1], 20, _folderStructureColumnWiseMaxWidth[0]);

            CultureInfo cultureInfo = new CultureInfo(command.LanguageKey);
            worksheet.Cells[2, 2].Value = creationDate.ToString("MMMM dd, yyyy", cultureInfo);
            SetCellStyleWithBorderAround(worksheet, 2, 2, ExcelBorderStyle.Thin);
            worksheet.Cells[2, 2].Style.VerticalAlignment = ExcelVerticalAlignment.Top;
            CustomBestFitColumn(worksheet.Column(2), worksheet.Cells[2, 2], 20, _folderStructureColumnWiseMaxWidth[1]);
        }

        private void WriteAllViewLibraryReportHeader(ExcelWorksheet worksheet, GenerateLibraryReportCommand command, DateTime creationDate)
        {
            try
            {
                WriteAllViewLibraryReportMetadataSection(worksheet, command, creationDate);

                int headerRowIndex = LibraryReport.HeaderRowIndex;
                var primaryTableHeaderBackgroundColor = LibraryReport.HeaderBackground;

                int colIndex = 1;
                foreach (var currentColumn in LibraryReport.AllViewPrimaryTableColumnKeys)
                {
                    var selectedColumn = worksheet.Column(colIndex);
                    var cell = worksheet.Cells[headerRowIndex, colIndex];
                    cell.Value = ToTitleCase(_allViewReportTranslatedStrings[currentColumn.Value]);
                    cell.Style.Font.Bold = true;
                    cell.Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                    cell.Style.VerticalAlignment = OfficeOpenXml.Style.ExcelVerticalAlignment.Center;
                    cell.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                    cell.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    cell.Style.Fill.BackgroundColor.SetColor(primaryTableHeaderBackgroundColor);

                    CustomBestFitColumn(selectedColumn, cell, 20, 50);

                    colIndex++;
                }

                int logoColumnIndex = LibraryReport.AllViewPrimaryTableColumnKeys.Count;
                //AddHeaderLogo(worksheet, logoColumnIndex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception in WriteDailyReportHeader: {Message}", ex.Message);
            }
        }

        private void WriteApprovalViewLibraryReportHeader(ExcelWorksheet worksheet, GenerateLibraryReportCommand command, DateTime creationDate)
        {
            try
            {
                WriteApprovalViewLibraryReportMetadataSection(worksheet, command, creationDate);

                int headerRowIndex = LibraryReport.HeaderRowIndex;
                var primaryTableHeaderBackgroundColor = LibraryReport.HeaderBackground;

                int colIndex = 1;
                foreach (var currentColumn in LibraryReport.ApprovalViewPrimaryTableColumnKeys)
                {
                    var selectedColumn = worksheet.Column(colIndex);
                    var cell = worksheet.Cells[headerRowIndex, colIndex];
                    cell.Value = ToTitleCase(_approvalViewReportTranslatedStrings[currentColumn.Value]);
                    cell.Style.Font.Bold = true;
                    cell.Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                    cell.Style.VerticalAlignment = OfficeOpenXml.Style.ExcelVerticalAlignment.Center;
                    cell.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                    cell.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    cell.Style.Fill.BackgroundColor.SetColor(primaryTableHeaderBackgroundColor);

                    CustomBestFitColumn(selectedColumn, cell, 20, 50);

                    colIndex++;
                }

                int logoColumnIndex = LibraryReport.ApprovalViewPrimaryTableColumnKeys.Count;
                //AddHeaderLogo(worksheet, logoColumnIndex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception in WriteDailyReportHeader: {Message}", ex);
            }
        }

        private void WriteWordFilesViewLibraryReportHeader(ExcelWorksheet worksheet, GenerateLibraryReportCommand command, DateTime creationDate)
        {
            try
            {
                WriteWordFilesViewLibraryReportMetadataSection(worksheet, command, creationDate);

                int headerRowIndex = LibraryReport.HeaderRowIndex;
                var primaryTableHeaderBackgroundColor = LibraryReport.HeaderBackground;

                int colIndex = 1;
                foreach (var currentColumn in LibraryReport.WordFilesViewPrimaryTableColumnKeys)
                {
                    var selectedColumn = worksheet.Column(colIndex);
                    var cell = worksheet.Cells[headerRowIndex, colIndex];
                    cell.Value = ToTitleCase(_wordFilesViewReportTranslatedStrings[currentColumn.Value]);
                    cell.Style.Font.Bold = true;
                    cell.Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                    cell.Style.VerticalAlignment = OfficeOpenXml.Style.ExcelVerticalAlignment.Center;
                    cell.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                    cell.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    cell.Style.Fill.BackgroundColor.SetColor(primaryTableHeaderBackgroundColor);

                    CustomBestFitColumn(selectedColumn, cell, 20, 50);

                    colIndex++;
                }

                int logoColumnIndex = LibraryReport.WordFilesViewPrimaryTableColumnKeys.Count;
                //AddHeaderLogo(worksheet, logoColumnIndex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception in WriteDailyReportHeader");
            }
        }

        private void WriteFormFilesViewLibraryReportHeader(ExcelWorksheet worksheet, GenerateLibraryReportCommand command, DateTime creationDate)
        {
            try
            {
                WriteFormFilesViewLibraryReportMetadataSection(worksheet, command, creationDate);

                int headerRowIndex = LibraryReport.HeaderRowIndex;
                var primaryTableHeaderBackgroundColor = LibraryReport.HeaderBackground;

                int colIndex = 1;
                foreach (var currentColumn in LibraryReport.FormViewPrimaryTableColumnKeys)
                {
                    var selectedColumn = worksheet.Column(colIndex);
                    var cell = worksheet.Cells[headerRowIndex, colIndex];
                    cell.Value = ToTitleCase(_formFilesViewReportTranslatedStrings[currentColumn.Value]);
                    cell.Style.Font.Bold = true;
                    cell.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    cell.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    cell.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                    cell.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    cell.Style.Fill.BackgroundColor.SetColor(primaryTableHeaderBackgroundColor);

                    CustomBestFitColumn(selectedColumn, cell, 20, 50);

                    colIndex++;
                }

                int logoColumnIndex = LibraryReport.FormViewPrimaryTableColumnKeys.Count;
                //AddHeaderLogo(worksheet, logoColumnIndex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception in WriteFormFilesViewLibraryReportHeader");
            }
        }

        private void WriteFolderStructureLibraryReportHeader(ExcelWorksheet worksheet, GenerateLibraryReportCommand command, DateTime creationDate)
        {
            try
            {
                WriteFolderStructureLibraryReportMetadataSection(worksheet, command, creationDate);

                int headerRowIndex = LibraryReport.HeaderRowIndex;
                var primaryTableHeaderBackgroundColor = LibraryReport.HeaderBackground;

                int colIndex = 1;
                foreach (var currentColumn in LibraryReport.FolderStructurePrimaryTableColumnKeys)
                {
                    var selectedColumn = worksheet.Column(colIndex);
                    var cell = worksheet.Cells[headerRowIndex, colIndex];
                    cell.Value = ToTitleCase(_folderStructureReportTranslatedStrings[currentColumn.Value]);
                    cell.Style.Font.Bold = true;
                    cell.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    cell.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    cell.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                    cell.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    cell.Style.Fill.BackgroundColor.SetColor(primaryTableHeaderBackgroundColor);

                    CustomBestFitColumn(selectedColumn, cell, 20, _folderStructureColumnWiseMaxWidth[colIndex - 1]);

                    colIndex++;
                }

                int logoColumnIndex = LibraryReport.FolderStructurePrimaryTableColumnKeys.Count;
                //AddHeaderLogo(worksheet, logoColumnIndex);
            }
            catch (Exception ex)
            {
                _logger.LogError("Exception in WriteFolderStructureLibraryReportHeader, {Exception}", ex.Message);
            }
        }

        private void WriteAllViewPrimaryTable(ExcelWorksheet worksheet, SearchResult objectArtifactSearchResult)
        {
            var searchResultData = objectArtifactSearchResult.Data as List<dynamic>;

            for (int column = 1; column <= LibraryReport.AllViewPrimaryTableColumnKeys.Count; column++)
            {
                var selectedColumn = worksheet.Column(column);
                selectedColumn.Style.WrapText = true;
            }

            const int startRow = LibraryReport.HeaderRowIndex;
            for (int index = 1; index <= objectArtifactSearchResult.Data.Count(); index++)
            {
                var selectedResult = searchResultData[index - 1];
                int colIndex = 1;
                int rowIndex = startRow + index;

                var artifactName = GetSearchResultDictionaryValue(selectedResult, nameof(RiqsObjectArtifact.Name)) as string;

                worksheet.Cells[rowIndex, colIndex].Value = artifactName;
                worksheet.Cells[rowIndex, colIndex].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                worksheet.Cells[rowIndex, colIndex].Style.Border.BorderAround(ExcelBorderStyle.Thin);
                CustomBestFitColumn(worksheet.Column(colIndex), worksheet.Cells[rowIndex, colIndex], 20, 50);
                colIndex++;

                var parentName = GetSearchResultDictionaryValue(selectedResult, nameof(RiqsObjectArtifact.ParentName)) as string;
                worksheet.Cells[rowIndex, colIndex].Value = parentName;
                worksheet.Cells[rowIndex, colIndex].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                worksheet.Cells[rowIndex, colIndex].Style.Border.BorderAround(ExcelBorderStyle.Thin);
                CustomBestFitColumn(worksheet.Column(colIndex), worksheet.Cells[rowIndex, colIndex], 20, 50);
                colIndex++;

                var fileVersion = GetSearchResultDictionaryValue(selectedResult, nameof(RiqsObjectArtifact.Version)) as string;
                worksheet.Cells[rowIndex, colIndex].Value = fileVersion;
                worksheet.Cells[rowIndex, colIndex].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                worksheet.Cells[rowIndex, colIndex].Style.Border.BorderAround(ExcelBorderStyle.Thin);
                CustomBestFitColumn(worksheet.Column(colIndex), worksheet.Cells[rowIndex, colIndex], 20, 50);
                colIndex++;

                var assignDetails = GetSearchResultDictionaryValue(selectedResult, nameof(RiqsObjectArtifact.ReportAssigneeDetail)) as LibraryReportAssigneeDetail;
                worksheet.Cells[rowIndex, colIndex].Value = ConcatenateWithDelimiter(
                    GetDateValue(assignDetails?.AssignedOn),
                    ConcatenateAssigneeNames(assignDetails, selectedResult["ItemId"]), Environment.NewLine);
                worksheet.Cells[rowIndex, colIndex].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                worksheet.Cells[rowIndex, colIndex].Style.Border.BorderAround(ExcelBorderStyle.Thin);
                CustomBestFitColumn(worksheet.Column(colIndex), worksheet.Cells[rowIndex, colIndex], 20, 50);
                colIndex++;

                var keywords = GetSearchResultDictionaryValue(selectedResult, nameof(RiqsObjectArtifact.Keywords)) as string[];
                worksheet.Cells[rowIndex, colIndex].Value = string.Join(", ", keywords);
                worksheet.Cells[rowIndex, colIndex].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                worksheet.Cells[rowIndex, colIndex].Style.Border.BorderAround(ExcelBorderStyle.Thin);
                CustomBestFitColumn(worksheet.Column(colIndex), worksheet.Cells[rowIndex, colIndex], 20, 50);
                colIndex++;

                var uploadDetails =
                    GetSearchResultDictionaryValue(selectedResult, nameof(RiqsObjectArtifact.UploadDetail)) as
                        ActionDetail;
                var uploadedOn = uploadDetails?.DateTime;
                var uploadedBy = uploadDetails?.Name ?? string.Empty;

                worksheet.Cells[rowIndex, colIndex].Value =
                    ConcatenateWithDelimiter(GetDateValue(uploadedOn), uploadedBy, Environment.NewLine);
                worksheet.Cells[rowIndex, colIndex].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                worksheet.Cells[rowIndex, colIndex].Style.Border.BorderAround(ExcelBorderStyle.Thin);
                CustomBestFitColumn(worksheet.Column(colIndex), worksheet.Cells[rowIndex, colIndex], 20, 50);
                colIndex++;

                var approvalDetailList =
                    GetSearchResultDictionaryValue(selectedResult, nameof(RiqsObjectArtifact.ApprovalDetails)) as
                        List<ActionDetail>;

                worksheet.Cells[rowIndex, colIndex].Value = GetApprovalDetailsValue(approvalDetailList);
                worksheet.Cells[rowIndex, colIndex].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                worksheet.Cells[rowIndex, colIndex].Style.Border.BorderAround(ExcelBorderStyle.Thin);
                CustomBestFitColumn(worksheet.Column(colIndex), worksheet.Cells[rowIndex, colIndex], 20, 50);
                colIndex++;

                var activeStatus = GetSearchResultDictionaryValue(selectedResult, nameof(RiqsObjectArtifact.Status)) as string;
                worksheet.Cells[rowIndex, colIndex].Value = GetActiveStatusString(activeStatus);
                worksheet.Cells[rowIndex, colIndex].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                worksheet.Cells[rowIndex, colIndex].Style.Border.BorderAround(ExcelBorderStyle.Thin);
                CustomBestFitColumn(worksheet.Column(colIndex), worksheet.Cells[rowIndex, colIndex], 20, 50);
            }
        }

        private void WriteApprovalViewPrimaryTable(ExcelWorksheet worksheet, SearchResult objectArtifactSearchResult)
        {
            var searchResultData = objectArtifactSearchResult.Data as List<dynamic>;

            for (int column = 1; column <= LibraryReport.ApprovalViewPrimaryTableColumnKeys.Count; column++)
            {
                var selectedColumn = worksheet.Column(column);
                selectedColumn.Style.WrapText = true;
            }

            const int startRow = LibraryReport.HeaderRowIndex;
            for (int index = 1; index <= objectArtifactSearchResult.Data.Count(); index++)
            {
                var selectedResult = searchResultData[index - 1];
                int colIndex = 1;
                int rowIndex = startRow + index;

                var artifactName = GetSearchResultDictionaryValue(selectedResult, nameof(RiqsObjectArtifact.Name)) as string;
                worksheet.Cells[rowIndex, colIndex].Value = artifactName;
                worksheet.Cells[rowIndex, colIndex].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                worksheet.Cells[rowIndex, colIndex].Style.Border.BorderAround(ExcelBorderStyle.Thin);
                CustomBestFitColumn(worksheet.Column(colIndex), worksheet.Cells[rowIndex, colIndex], 20, 50);
                colIndex++;

                var parentName = GetSearchResultDictionaryValue(selectedResult, nameof(RiqsObjectArtifact.ParentName)) as string;
                worksheet.Cells[rowIndex, colIndex].Value = parentName;
                worksheet.Cells[rowIndex, colIndex].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                worksheet.Cells[rowIndex, colIndex].Style.Border.BorderAround(ExcelBorderStyle.Thin);
                CustomBestFitColumn(worksheet.Column(colIndex), worksheet.Cells[rowIndex, colIndex], 20, 50);
                colIndex++;

                var version = GetSearchResultDictionaryValue(selectedResult, nameof(RiqsObjectArtifact.Version)) as string;
                worksheet.Cells[rowIndex, colIndex].Value = version;
                worksheet.Cells[rowIndex, colIndex].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                worksheet.Cells[rowIndex, colIndex].Style.Border.BorderAround(ExcelBorderStyle.Thin);
                CustomBestFitColumn(worksheet.Column(colIndex), worksheet.Cells[rowIndex, colIndex], 20, 50);
                colIndex++;

                var uploadDetails = GetSearchResultDictionaryValue(selectedResult, nameof(RiqsObjectArtifact.UploadDetail)) as ActionDetail;
                var uploadedOn = uploadDetails?.DateTime;
                var uploadedBy = uploadDetails?.Name ?? string.Empty;
                worksheet.Cells[rowIndex, colIndex].Value = ConcatenateWithDelimiter(GetDateValue(uploadedOn), uploadedBy, Environment.NewLine);
                worksheet.Cells[rowIndex, colIndex].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                worksheet.Cells[rowIndex, colIndex].Style.Border.BorderAround(ExcelBorderStyle.Thin);
                CustomBestFitColumn(worksheet.Column(colIndex), worksheet.Cells[rowIndex, colIndex], 20, 50);
                colIndex++;

                var approvalDetailList = GetSearchResultDictionaryValue(selectedResult, nameof(RiqsObjectArtifact.ApprovalDetails)) as List<ActionDetail>;
                worksheet.Cells[rowIndex, colIndex].Value = GetApprovalDetailsValue(approvalDetailList);
                worksheet.Cells[rowIndex, colIndex].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                worksheet.Cells[rowIndex, colIndex].Style.Border.BorderAround(ExcelBorderStyle.Thin);
                CustomBestFitColumn(worksheet.Column(colIndex), worksheet.Cells[rowIndex, colIndex], 20, 50);
                colIndex++;

                var reApprovalDetailList = GetSearchResultDictionaryValue(selectedResult, nameof(RiqsObjectArtifact.ReapprovalDetails)) as List<
                    ReapproveDetail>;
                worksheet.Cells[rowIndex, colIndex].Value = GetApprovalDetailsValue(reApprovalDetailList);
                worksheet.Cells[rowIndex, colIndex].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                worksheet.Cells[rowIndex, colIndex].Style.Border.BorderAround(ExcelBorderStyle.Thin);
                CustomBestFitColumn(worksheet.Column(colIndex), worksheet.Cells[rowIndex, colIndex], 20, 50);
                colIndex++;

                var keywords = GetSearchResultDictionaryValue(selectedResult, nameof(RiqsObjectArtifact.Keywords)) as string[];
                worksheet.Cells[rowIndex, colIndex].Value = string.Join(", ", keywords);
                worksheet.Cells[rowIndex, colIndex].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                worksheet.Cells[rowIndex, colIndex].Style.Border.BorderAround(ExcelBorderStyle.Thin);
                CustomBestFitColumn(worksheet.Column(colIndex), worksheet.Cells[rowIndex, colIndex], 20, 50);
                colIndex++;

                var approvalStatus = GetSearchResultDictionaryValue(selectedResult, nameof(RiqsObjectArtifact.ApprovalStatus)) as string;
                worksheet.Cells[rowIndex, colIndex].Value = GetApprovalStatusString(approvalStatus);
                worksheet.Cells[rowIndex, colIndex].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                worksheet.Cells[rowIndex, colIndex].Style.Border.BorderAround(ExcelBorderStyle.Thin);
                CustomBestFitColumn(worksheet.Column(colIndex), worksheet.Cells[rowIndex, colIndex], 20, 50);
                colIndex++;

                var activeStatus = GetSearchResultDictionaryValue(selectedResult, nameof(RiqsObjectArtifact.Status)) as string;
                worksheet.Cells[rowIndex, colIndex].Value = GetActiveStatusString(activeStatus);
                worksheet.Cells[rowIndex, colIndex].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                worksheet.Cells[rowIndex, colIndex].Style.Border.BorderAround(ExcelBorderStyle.Thin);
                CustomBestFitColumn(worksheet.Column(colIndex), worksheet.Cells[rowIndex, colIndex], 20, 50);
            }
        }

        private void WriteWordFilesViewPrimaryTable(ExcelWorksheet worksheet, SearchResult objectArtifactSearchResult)
        {
            var searchResultData = objectArtifactSearchResult.Data as List<dynamic>;

            for (int column = 1; column <= LibraryReport.WordFilesViewPrimaryTableColumnKeys.Count; column++)
            {
                var selectedColumn = worksheet.Column(column);
                //CustomBestFitColumn(selectedColumn, 20, 50);
                selectedColumn.Style.WrapText = true;
            }

            const int startRow = LibraryReport.HeaderRowIndex;
            for (int index = 1; index <= objectArtifactSearchResult.Data.Count(); index++)
            {
                var selectedResult = searchResultData[index - 1];
                int colIndex = 1;
                int rowIndex = startRow + index;

                var artifactName = GetSearchResultDictionaryValue(selectedResult, nameof(RiqsObjectArtifact.Name)) as string;
                worksheet.Cells[rowIndex, colIndex].Value = artifactName;
                worksheet.Cells[rowIndex, colIndex].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                worksheet.Cells[rowIndex, colIndex].Style.Border.BorderAround(ExcelBorderStyle.Thin);
                CustomBestFitColumn(worksheet.Column(colIndex), worksheet.Cells[rowIndex, colIndex], 20, 50);
                colIndex++;

                var parentName = GetSearchResultDictionaryValue(selectedResult, nameof(RiqsObjectArtifact.ParentName)) as string;
                worksheet.Cells[rowIndex, colIndex].Value = parentName;
                worksheet.Cells[rowIndex, colIndex].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                worksheet.Cells[rowIndex, colIndex].Style.Border.BorderAround(ExcelBorderStyle.Thin);
                CustomBestFitColumn(worksheet.Column(colIndex), worksheet.Cells[rowIndex, colIndex], 20, 50);
                colIndex++;

                var version = GetSearchResultDictionaryValue(selectedResult, nameof(RiqsObjectArtifact.Version)) as string;
                worksheet.Cells[rowIndex, colIndex].Value = version;
                worksheet.Cells[rowIndex, colIndex].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                worksheet.Cells[rowIndex, colIndex].Style.Border.BorderAround(ExcelBorderStyle.Thin);
                CustomBestFitColumn(worksheet.Column(colIndex), worksheet.Cells[rowIndex, colIndex], 20, 50);
                colIndex++;

                var uploadDetails = GetSearchResultDictionaryValue(selectedResult, nameof(RiqsObjectArtifact.UploadDetail)) as ActionDetail;
                worksheet.Cells[rowIndex, colIndex].Value = ConcatenateWithDelimiter(GetDateValue(uploadDetails?.DateTime), uploadDetails?.Name ?? string.Empty, Environment.NewLine);
                worksheet.Cells[rowIndex, colIndex].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                worksheet.Cells[rowIndex, colIndex].Style.Border.BorderAround(ExcelBorderStyle.Thin);
                CustomBestFitColumn(worksheet.Column(colIndex), worksheet.Cells[rowIndex, colIndex], 20, 50);
                colIndex++;

                var approvalDetailList = GetSearchResultDictionaryValue(selectedResult, nameof(RiqsObjectArtifact.ApprovalDetails)) as List<ActionDetail>;
                worksheet.Cells[rowIndex, colIndex].Value = GetApprovalDetailsValue(approvalDetailList);
                worksheet.Cells[rowIndex, colIndex].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                worksheet.Cells[rowIndex, colIndex].Style.Border.BorderAround(ExcelBorderStyle.Thin);
                CustomBestFitColumn(worksheet.Column(colIndex), worksheet.Cells[rowIndex, colIndex], 20, 50);
                colIndex++;

                var editDetails = GetSearchResultDictionaryValue(selectedResult, nameof(RiqsObjectArtifact.EditDetail)) as ActionDetail;
                worksheet.Cells[rowIndex, colIndex].Value = ConcatenateWithDelimiter(GetDateValue(editDetails?.DateTime), editDetails?.Name ?? string.Empty, Environment.NewLine);
                worksheet.Cells[rowIndex, colIndex].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                worksheet.Cells[rowIndex, colIndex].Style.Border.BorderAround(ExcelBorderStyle.Thin);
                CustomBestFitColumn(worksheet.Column(colIndex), worksheet.Cells[rowIndex, colIndex], 20, 50);
                colIndex++;

                var assignDetails = GetSearchResultDictionaryValue(selectedResult, nameof(RiqsObjectArtifact.ReportAssigneeDetail)) as LibraryReportAssigneeDetail;
                worksheet.Cells[rowIndex, colIndex].Value = ConcatenateWithDelimiter(GetDateValue(assignDetails?.AssignedOn), ConcatenateAssigneeNames(assignDetails, selectedResult["ItemId"]), Environment.NewLine);
                worksheet.Cells[rowIndex, colIndex].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                worksheet.Cells[rowIndex, colIndex].Style.Border.BorderAround(ExcelBorderStyle.Thin);
                CustomBestFitColumn(worksheet.Column(colIndex), worksheet.Cells[rowIndex, colIndex], 20, 50);
            }
        }

        private void WriteFormFilesViewPrimaryTable(ExcelWorksheet worksheet, SearchResult objectArtifactSearchResult)
        {
            var searchResultData = objectArtifactSearchResult.Data as List<dynamic>;

            for (int column = 1; column <= LibraryReport.FormViewPrimaryTableColumnKeys.Count; column++)
            {
                var selectedColumn = worksheet.Column(column);
                //CustomBestFitColumn(selectedColumn, 20, 50);
                selectedColumn.Style.WrapText = true;
            }

            const int startRow = LibraryReport.HeaderRowIndex;
            for (int index = 1; index <= objectArtifactSearchResult.Data.Count(); index++)
            {
                var selectedResult = searchResultData[index - 1];
                int colIndex = 1;
                int rowIndex = startRow + index;

                var artifactName = GetSearchResultDictionaryValue(selectedResult, nameof(RiqsObjectArtifact.Name)) as string;
                worksheet.Cells[rowIndex, colIndex].Value = artifactName;
                worksheet.Cells[rowIndex, colIndex].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                worksheet.Cells[rowIndex, colIndex].Style.Border.BorderAround(ExcelBorderStyle.Thin);
                CustomBestFitColumn(worksheet.Column(colIndex), worksheet.Cells[rowIndex, colIndex], 20, 50);
                colIndex++;

                var parentName = GetSearchResultDictionaryValue(selectedResult, nameof(RiqsObjectArtifact.ParentName)) as string;
                worksheet.Cells[rowIndex, colIndex].Value = parentName;
                worksheet.Cells[rowIndex, colIndex].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                worksheet.Cells[rowIndex, colIndex].Style.Border.BorderAround(ExcelBorderStyle.Thin);
                CustomBestFitColumn(worksheet.Column(colIndex), worksheet.Cells[rowIndex, colIndex], 20, 50);
                colIndex++;

                var version = GetSearchResultDictionaryValue(selectedResult, nameof(RiqsObjectArtifact.Version)) as string;
                worksheet.Cells[rowIndex, colIndex].Value = version;
                worksheet.Cells[rowIndex, colIndex].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                worksheet.Cells[rowIndex, colIndex].Style.Border.BorderAround(ExcelBorderStyle.Thin);
                CustomBestFitColumn(worksheet.Column(colIndex), worksheet.Cells[rowIndex, colIndex], 20, 50);
                colIndex++;

                var assignDetails = GetSearchResultDictionaryValue(selectedResult, nameof(RiqsObjectArtifact.ReportAssigneeDetail)) as LibraryReportAssigneeDetail;
                worksheet.Cells[rowIndex, colIndex].Value = ConcatenateWithDelimiter(GetDateValue(assignDetails?.AssignedOn), ConcatenateAssigneeNames(assignDetails, selectedResult["ItemId"]), Environment.NewLine);
                worksheet.Cells[rowIndex, colIndex].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                worksheet.Cells[rowIndex, colIndex].Style.Border.BorderAround(ExcelBorderStyle.Thin);
                CustomBestFitColumn(worksheet.Column(colIndex), worksheet.Cells[rowIndex, colIndex], 20, 50);
                colIndex++;

                var completedDetails = GetSearchResultDictionaryValue(selectedResult, nameof(RiqsObjectArtifact.FormFilledBy)) as
                    AssignedMemberDetail;
                worksheet.Cells[rowIndex, colIndex].Value = ConcatenateAssigneeNames(completedDetails);
                worksheet.Cells[rowIndex, colIndex].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                worksheet.Cells[rowIndex, colIndex].Style.Border.BorderAround(ExcelBorderStyle.Thin);
                CustomBestFitColumn(worksheet.Column(colIndex), worksheet.Cells[rowIndex, colIndex], 20, 50);
                colIndex++;

                var pendingDetails = GetSearchResultDictionaryValue(selectedResult, nameof(RiqsObjectArtifact.FormFillPendingBy)) as
                    AssignedMemberDetail;
                worksheet.Cells[rowIndex, colIndex].Value = ConcatenateAssigneeNames(pendingDetails);
                worksheet.Cells[rowIndex, colIndex].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                worksheet.Cells[rowIndex, colIndex].Style.Border.BorderAround(ExcelBorderStyle.Thin);
                CustomBestFitColumn(worksheet.Column(colIndex), worksheet.Cells[rowIndex, colIndex], 20, 50);
                colIndex++;

                var departmentName = GetSearchResultDictionaryValue(selectedResult, nameof(RiqsObjectArtifact.Department)) as string;
                worksheet.Cells[rowIndex, colIndex].Value = departmentName;
                worksheet.Cells[rowIndex, colIndex].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                worksheet.Cells[rowIndex, colIndex].Style.Border.BorderAround(ExcelBorderStyle.Thin);
                CustomBestFitColumn(worksheet.Column(colIndex), worksheet.Cells[rowIndex, colIndex], 20, 50);
            }
        }

        private void WriteFolderStructurePrimaryTable(ExcelWorksheet worksheet, SearchResult objectArtifactSearchResult)
        {
            var searchResultData = objectArtifactSearchResult.Data as List<dynamic>;

            for (int column = 1; column <= LibraryReport.AllViewPrimaryTableColumnKeys.Count; column++)
            {
                var selectedColumn = worksheet.Column(column);
                 //CustomBestFitColumn(selectedColumn, 20, 40);
                selectedColumn.Style.WrapText = true;
            }

            const int startRow = LibraryReport.HeaderRowIndex;
            for (int index = 1; index <= objectArtifactSearchResult.Data.Count(); index++)
            {
                var selectedResult = searchResultData[index - 1];
                int colIndex = 1;
                int rowIndex = startRow + index;

                var relativeFolderPath = GetSearchResultDictionaryValue(selectedResult, "RelativePath") as string;

                worksheet.Cells[rowIndex, colIndex].Value = relativeFolderPath;
                worksheet.Cells[rowIndex, colIndex].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                worksheet.Cells[rowIndex, colIndex].Style.Border.BorderAround(ExcelBorderStyle.Thin);
                CustomBestFitColumn(worksheet.Column(colIndex), worksheet.Cells[rowIndex, colIndex], 20, 
                    _folderStructureColumnWiseMaxWidth[colIndex - 1]);
                colIndex++;
                var artifactType = GetSearchResultDictionaryValue(selectedResult, nameof(ObjectArtifact.ArtifactType)) ;
                var name = GetSearchResultDictionaryValue(selectedResult, nameof(ObjectArtifact.Name)) as string;

                worksheet.Cells[rowIndex, colIndex].Value = artifactType == ArtifactTypeEnum.Folder ? name : "";
                worksheet.Cells[rowIndex, colIndex].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                worksheet.Cells[rowIndex, colIndex].Style.Border.BorderAround(ExcelBorderStyle.Thin);
                CustomBestFitColumn(worksheet.Column(colIndex), worksheet.Cells[rowIndex, colIndex], 20, 
                    _folderStructureColumnWiseMaxWidth[colIndex - 1]);
                colIndex++;

                worksheet.Cells[rowIndex, colIndex].Value = artifactType == ArtifactTypeEnum.File ? name : "";
                worksheet.Cells[rowIndex, colIndex].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                worksheet.Cells[rowIndex, colIndex].Style.Border.BorderAround(ExcelBorderStyle.Thin);
                CustomBestFitColumn(worksheet.Column(colIndex), worksheet.Cells[rowIndex, colIndex], 20,
                    _folderStructureColumnWiseMaxWidth[colIndex - 1]);
                colIndex++;

                var fileVersion = GetSearchResultDictionaryValue(selectedResult, nameof(RiqsObjectArtifact.Version)) as string;
                worksheet.Cells[rowIndex, colIndex].Value = fileVersion;
                worksheet.Cells[rowIndex, colIndex].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                worksheet.Cells[rowIndex, colIndex].Style.Border.BorderAround(ExcelBorderStyle.Thin);
                CustomBestFitColumn(worksheet.Column(colIndex), worksheet.Cells[rowIndex, colIndex], 20,
                    _folderStructureColumnWiseMaxWidth[colIndex - 1]);
                colIndex++;

                var assignDetails = GetSearchResultDictionaryValue(selectedResult, nameof(RiqsObjectArtifact.ReportAssigneeDetail)) as LibraryReportAssigneeDetail;
                worksheet.Cells[rowIndex, colIndex].Value = ConcatenateWithDelimiter(
                    GetDateValue(assignDetails?.AssignedOn),
                    ConcatenateAssigneeNames(assignDetails, selectedResult["ItemId"]), Environment.NewLine);
                worksheet.Cells[rowIndex, colIndex].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                worksheet.Cells[rowIndex, colIndex].Style.Border.BorderAround(ExcelBorderStyle.Thin);
                CustomBestFitColumn(worksheet.Column(colIndex), worksheet.Cells[rowIndex, colIndex], 20, 
                    _folderStructureColumnWiseMaxWidth[colIndex - 1]);
                colIndex++;

                var keywords = GetSearchResultDictionaryValue(selectedResult, nameof(RiqsObjectArtifact.Keywords)) as string[];
                worksheet.Cells[rowIndex, colIndex].Value = string.Join(", ", keywords);
                worksheet.Cells[rowIndex, colIndex].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                worksheet.Cells[rowIndex, colIndex].Style.Border.BorderAround(ExcelBorderStyle.Thin);
                CustomBestFitColumn(worksheet.Column(colIndex), worksheet.Cells[rowIndex, colIndex], 20,
                    _folderStructureColumnWiseMaxWidth[colIndex - 1]);
                colIndex++;

                var uploadDetails =
                    GetSearchResultDictionaryValue(selectedResult, nameof(RiqsObjectArtifact.UploadDetail)) as
                        ActionDetail;
                var uploadedOn = uploadDetails?.DateTime;
                var uploadedBy = uploadDetails?.Name ?? string.Empty;

                worksheet.Cells[rowIndex, colIndex].Value =
                    ConcatenateWithDelimiter(GetDateValue(uploadedOn), uploadedBy, Environment.NewLine);
                worksheet.Cells[rowIndex, colIndex].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                worksheet.Cells[rowIndex, colIndex].Style.Border.BorderAround(ExcelBorderStyle.Thin);
                CustomBestFitColumn(worksheet.Column(colIndex), worksheet.Cells[rowIndex, colIndex], 20,
                    _folderStructureColumnWiseMaxWidth[colIndex - 1]);
                colIndex++;

                var approvalDetailList =
                    GetSearchResultDictionaryValue(selectedResult, nameof(RiqsObjectArtifact.ApprovalDetails)) as
                        List<ActionDetail>;

                worksheet.Cells[rowIndex, colIndex].Value = GetApprovalDetailsValue(approvalDetailList);
                worksheet.Cells[rowIndex, colIndex].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                worksheet.Cells[rowIndex, colIndex].Style.Border.BorderAround(ExcelBorderStyle.Thin);
                CustomBestFitColumn(worksheet.Column(colIndex), worksheet.Cells[rowIndex, colIndex], 20,
                    _folderStructureColumnWiseMaxWidth[colIndex - 1]);
                colIndex++;

                var activeStatus = GetSearchResultDictionaryValue(selectedResult, nameof(RiqsObjectArtifact.Status)) as string;
                worksheet.Cells[rowIndex, colIndex].Value = GetActiveStatusString(activeStatus);
                worksheet.Cells[rowIndex, colIndex].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                worksheet.Cells[rowIndex, colIndex].Style.Border.BorderAround(ExcelBorderStyle.Thin);
                CustomBestFitColumn(worksheet.Column(colIndex), worksheet.Cells[rowIndex, colIndex], 20, 
                    _folderStructureColumnWiseMaxWidth[colIndex - 1]);
            }
        }

        private dynamic GetSearchResultDictionaryValue(dynamic searchResult, string key)
        {
            if (searchResult.TryGetValue(key, out object objectValue))
            {
                return objectValue;
            }
            else
            {
                return null;
            }
        }

        private static string ConcatenateWithDelimiter(string str1, string str2, string delimiter = ", ")
        {
            if (!string.IsNullOrEmpty(str1) && !string.IsNullOrEmpty(str2))
            {
                return str1 + delimiter + str2;
            }
            else if (!string.IsNullOrEmpty(str1))
            {
                return str1;
            }
            else if (!string.IsNullOrEmpty(str2))
            {
                return str2;
            }
            else
            {
                return string.Empty;
            }
        }

        private string ConcatenateAssigneeNames(LibraryReportAssigneeDetail detail, string objectArtifactId)
        {
            var names = new List<string>();

            if (detail?.AssignedDepartmentList == null)
            {
                return "";
            }

            var documentReadInfo = _objectArtifactUtilityService
                .GetDocumentsMarkAsReadByArtifactId(objectArtifactId)?
                .GetAwaiter()
                .GetResult() ?? new List<DocumentsMarkedAsRead>();

            for (var i = 0; i < detail?.AssignedDepartmentList?.Count; i++)
            {
                var dept = detail.AssignedDepartmentList[i];

                names.Add(dept.Name);

                if (dept.Assignees != null)
                {
                    names.AddRange(dept.Assignees.Select(a => GetNameWithReadInfo(a, documentReadInfo)));
                }
            }

            return string.Join(", ", names);
        }

        private string GetNameWithReadInfo(AssigneeSummary assignee, List<DocumentsMarkedAsRead> readInfo)
        {
            var isExist = readInfo.Exists(r => r.ReadByUserId == assignee.Id && r.ReadByUserName == assignee.Name);
            return assignee.Name + (isExist ? " (Read)" : "");
        }

        private static string ConcatenateAssigneeNames(AssignedMemberDetail detail)
        {
            var names = new List<string>();

            if (detail?.Members == null)
            {
                return "";
            }

            for (var i = 0; i < detail?.Members?.Count; i++)
            {
                var member = detail.Members[i];
                var dateValue = GetDateValue(member?.Time);
                if (string.IsNullOrWhiteSpace(dateValue))
                {
                    names.Add(member?.Name);
                }
                else
                {
                    names.Add(ConcatenateWithDelimiter(
                        dateValue,
                        member?.Name)
                    );
                }
            }

            return string.Join(Environment.NewLine, names);
        }

        private LibraryViewModeEnum GetViewMode(string type)
        {
            if (string.IsNullOrWhiteSpace(type))
            {
                return LibraryViewModeEnum.ALL;
            }

            return type switch
            {
                "approval-view" => LibraryViewModeEnum.APPROVAL_VIEW,
                "document" => LibraryViewModeEnum.DOCUMENT,
                "form" => LibraryViewModeEnum.FORM,
                _ => throw new ArgumentException($"Unsupported view mode: {type}"),
            };
        }

        private string GetApprovalStatusString(string statusString)
        {
            if (string.IsNullOrWhiteSpace(statusString))
                return string.Empty;

            bool success = Enum.TryParse<LibraryFileApprovalStatusEnum>(statusString, true, out var result);

            if (success)
            {
                switch (result)
                {
                    case LibraryFileApprovalStatusEnum.PENDING:

                        return "Pending";
                    case LibraryFileApprovalStatusEnum.APPROVED:

                        return "Approved";
                    default:
                        break;
                }
            }

            return string.Empty;
        }

        private string GetActiveStatusString(string statusString)
        {
            if (string.IsNullOrWhiteSpace(statusString))
                return string.Empty;

            bool success = Enum.TryParse<LibraryFileStatusEnum>(statusString, true, out var result);

            if (success)
            {
                switch (result)
                {
                    case LibraryFileStatusEnum.INACTIVE:

                        return "Inactive";
                    case LibraryFileStatusEnum.ACTIVE:

                        return "Active";
                    default:
                        break;
                }
            }

            return string.Empty;
        }

        private void AddHeaderLogo(ExcelWorksheet worksheet, int columnIndex)
        {
            // [firstRow, firstColumn, lastRow, LastColumn]
            worksheet.Cells[1, columnIndex, 2, columnIndex].Merge = true;
            _praxisReportService.AddLogoInExcelReport(worksheet, LibraryReport.LogoSize, columnIndex, rqLatestLogo);
        }

        private void SetCellValueWithBoldStyle(ExcelWorksheet worksheet, int row, int column)
        {
            worksheet.Cells[row, column].Style.Font.Bold = true;
        }

        private void SetCellStyleWithBorderAround(ExcelWorksheet worksheet, int row, int column, ExcelBorderStyle excelBorderStyle)
        {
            worksheet.Cells[row, column].Style.Border.BorderAround(excelBorderStyle);
        }

        private static string GetDateValue(DateTime? dateTime)
        {
            if (dateTime == null)
            {
                return "";
            }

            var formattedDate = dateTime.Value.ToString("MMM dd, yyyy");
            return formattedDate.Equals("Jan 01, 0001") ? "" : formattedDate;
        }

        private static string GetApprovalDetailsValue(IEnumerable<ActionDetail> list)
        {
            var names = new List<string>();

            if (list == null)
            {
                return "";
            }

            foreach (var approvalDetail in list)
            {
                var approvedOn = GetDateValue(approvalDetail?.DateTime);
                var approvedBy = approvalDetail?.Name ?? string.Empty;

                if (string.IsNullOrWhiteSpace(approvedOn))
                {
                    names.Add(approvedBy);
                }
                else
                {
                    names.Add(ConcatenateWithDelimiter(
                        approvedOn,
                        approvedBy,
                        Environment.NewLine)
                    );
                }
            }

            return string.Join(Environment.NewLine, names);
        }

        static string ToTitleCase(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return input;
            }

            // Create a TextInfo object for the current culture to get proper casing rules
            var textInfo = CultureInfo.CurrentCulture.TextInfo;

            // Use ToTitleCase method to convert the input string to title case
            var titleCase = textInfo.ToTitleCase(input.ToLower());

            return titleCase;
        }
        private void CustomBestFitColumn(ExcelColumn column, ExcelRange cell, int minWidth, int maxWidth)
        {
            var width = (cell?.Value?.ToString()?.Length ?? 0) * 1.2;
            column.Width = Math.Max(column.Width, width);
            column.Width = Math.Min(column.Width, maxWidth);
            column.Width = Math.Max(column.Width, minWidth);
        }

    }
}
