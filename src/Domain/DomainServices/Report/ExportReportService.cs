using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using OfficeOpenXml;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.Training;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.ExcelReports;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.Reports;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.Reports.DeveloperReport;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.Reports.EquipmentReport;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.Reports.PraxisUserReport;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.Reports.RiskOverviewReport;
using Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.Report;
using static Selise.Ecap.SC.PraxisMonitor.Contracts.Constants.ReportConstants;
using IGenerateEquipmentReport = Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.Reports.IGenerateEquipmentReport;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.QuickTaskModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.QuickTaskModule;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.Services
{
    public class ExportReportService : IExportReportService
    {
        private readonly IRepository _repository;
        private readonly ILogger<ExportReportService> _logger;
        private readonly IStorageDataService _storageDataService;
        private readonly IDeveloperReportGenerateStrategy _developerReportGenarateStrategy;
        private readonly IGenerateCategoryReport _generateCategoryReportService;
        private readonly IGenerateTrainingReport _generateTrainingReportService;
        private readonly IGenerateTrainingDetailsReport _generateTrainingDetailsReportService;
        private readonly IGenerateEquipmentMaintenanceListReport _generateEquipmentMaintenanceListReportService;
        private readonly IGenerateProcessMonitorOverviewReport _generateProcessMonitorOverviewReportService;
        private readonly IGenerateDistinctTaskListReport _generateDistinctTaskListReportService;
        private readonly IGenerateOpenItemReport _generateOpenItemReportService;
        private readonly IPraxisUserListReportGenerateStrategy _praxisUserListReportGenerateStrategyService;
        private readonly IRiskOverviewReportGenerateStrategy _riskOverviewReportGenerateStrategyService;
        private readonly IDocGenerationService _docGenerationService;
        private readonly IPraxisFileService _fileService;
        private readonly IPraxisReportService _praxisReportService;
        private readonly ICirsReportGenerationService _cirsReportGenerationService;
        private readonly IGenerateShiftPlanReportService _generateShiftPlanReportService;
        private readonly IGenerateLibraryReportService _generateLibraryReportService;
        private readonly IGenerateSuppliersReport _generateSuppliersReport;
        private readonly IEquipmentReportGenerationStrategyService _equipmentReportGenerationStrategyService;
        private readonly IGenerateLibraryDocumentAssigneesReportService _generateLibraryDocumentAssigneesReportService;
        private readonly IGenerateShiftReportService _generateShiftReportService;
        private readonly IGenerateQuickTaskPlanReportService _generateQuickTaskPlanReportService;
        private readonly IGenerateQuickTaskReportService _generateQuickTaskReportService;
        public ExportReportService(
            IRepository repo,
            ILogger<ExportReportService> logger,
            IStorageDataService storageDataService,
            IDeveloperReportGenerateStrategy developerReportGenarateStrategy,
            IGenerateCategoryReport generateCategoryReportService,
            IGenerateTrainingReport generateTrainingReportService,
            IGenerateTrainingDetailsReport generateTrainingDetailsReportService,
            IGenerateProcessMonitorOverviewReport generateProcessMonitorOverviewReportService,
            IGenerateDistinctTaskListReport generateDistinctTaskListReportService,
            IGenerateOpenItemReport generateOpenItemReportService,
            IPraxisUserListReportGenerateStrategy praxisUserListReportGenerateStrategyService,
            IRiskOverviewReportGenerateStrategy riskOverviewReportGenerateStrategyService,
            IDocGenerationService docGenerationService,
            IPraxisFileService fileService,
            IPraxisReportService praxisReportService,
            ICirsReportGenerationService cirsReportGenerationService,
            IGenerateShiftPlanReportService generateShiftPlanReportService,
            IGenerateLibraryReportService generateLibraryReportService,
            IGenerateEquipmentMaintenanceListReport generateEquipmentMaintenanceListReportService,
            IGenerateSuppliersReport generateSuppliersReport,
            IEquipmentReportGenerationStrategyService equipmentReportGenerationStrategyService,
            IGenerateLibraryDocumentAssigneesReportService generateLibraryDocumentAssigneesReportService,
            IGenerateShiftReportService generateShiftReportService,
            IGenerateQuickTaskPlanReportService generateQuickTaskPlanReportService,
            IGenerateQuickTaskReportService generateQuickTaskReportService
        )
        {
            _repository = repo;
            _logger = logger;
            _storageDataService = storageDataService;
            _developerReportGenarateStrategy = developerReportGenarateStrategy;
            _generateCategoryReportService = generateCategoryReportService;
            _generateTrainingReportService = generateTrainingReportService;
            _generateTrainingDetailsReportService = generateTrainingDetailsReportService;
            _generateProcessMonitorOverviewReportService = generateProcessMonitorOverviewReportService;
            _generateDistinctTaskListReportService = generateDistinctTaskListReportService;
            _generateOpenItemReportService = generateOpenItemReportService;
            _praxisUserListReportGenerateStrategyService = praxisUserListReportGenerateStrategyService;
            _riskOverviewReportGenerateStrategyService = riskOverviewReportGenerateStrategyService;
            _docGenerationService = docGenerationService;
            _fileService = fileService;
            _praxisReportService = praxisReportService;
            _cirsReportGenerationService = cirsReportGenerationService;
            _generateShiftPlanReportService = generateShiftPlanReportService;
            _generateLibraryReportService = generateLibraryReportService;
            _generateEquipmentMaintenanceListReportService = generateEquipmentMaintenanceListReportService;
            _generateSuppliersReport = generateSuppliersReport;
            _equipmentReportGenerationStrategyService = equipmentReportGenerationStrategyService;
            _generateLibraryDocumentAssigneesReportService = generateLibraryDocumentAssigneesReportService;
            _generateShiftReportService = generateShiftReportService;
            _generateQuickTaskPlanReportService = generateQuickTaskPlanReportService;
            _generateQuickTaskReportService = generateQuickTaskReportService;
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        }

        public bool Export(Guid fileId, string fileNameWithExtension, string filter)
        {
            throw new NotImplementedException();
        }

        public async Task<bool> ExportTaskListReport(ExportTaskListReportCommand command)
        {
            try
            {

                var client = await _repository.GetItemAsync<PraxisClient>(
                    pc => pc.ItemId.Equals(command.ClientId) && !pc.IsMarkedToDelete
                );

                if (client == null)
                {
                    return false;
                }

                using var excel = new ExcelPackage();

                await _praxisReportService.UpdatePraxisReportStatus(command.ReportFileId, PraxisReportProgress.InProgress);

                bool isReportPrepared = await _generateProcessMonitorOverviewReportService.PrepareTaskListReport(
                    command.FilterString,
                    command.EnableDateRange,
                    command.StartDate,
                    command.EndDate,
                    client,
                    excel,
                    command.Translation,
                    command.TimezoneOffsetInMinutes ?? 0
                );

                if (!isReportPrepared)
                {
                    return false;
                }

                var isSuccess = await _storageDataService.UploadFileAsync(
                    command.ReportFileId,
                    command.FileNameWithExtension,
                    excel.GetAsByteArray()
                );

                _logger.LogInformation("ExportTaskListReport upload to storage success: {IsSuccess}", isSuccess);

                return isSuccess;

            }
            catch (Exception ex)
            {
                _logger.LogError("ExportTaskListReport got error: {ErrorMessage}", ex.Message);
            }

            return false;
        }

        public async Task<bool> ExportDistinctTaskListReport(ExportDistinctTaskListReportCommand command)
        {
            try
            {

                PraxisClient client = _repository.GetItem<PraxisClient>(
                    pc => pc.ItemId.Equals(command.ClientId) && !pc.IsMarkedToDelete
                );

                if (client == null)
                {
                    return false;
                }

                using var excel = new ExcelPackage();
                await _praxisReportService.UpdatePraxisReportStatus(command.ReportFileId, PraxisReportProgress.InProgress);

                bool isReportPrepared = await _generateDistinctTaskListReportService.PrepareDistinctTaskListReport(
                    client,
                    excel,
                    command.FilterString,
                    command.Translation
                );

                if (!isReportPrepared) return false;

                var isSuccess = await _storageDataService.UploadFileAsync(
                    command.ReportFileId,
                    command.FileNameWithExtension,
                    excel.GetAsByteArray()
                );

                _logger.LogInformation("ExportDistinctTaskListReport upload to storage success: {IsSuccess}", isSuccess);
                return isSuccess;
            }
            catch (Exception ex)
            {
                _logger.LogError("ExportDistinctTaskListReport got error: {ErrorMessage}", ex.Message);
            }

            return false;
        }

        public async Task<bool> ExportOpenItemReport(ExportOpenItemReportCommand command)
        {
            try
            {
                PraxisClient client = _repository.GetItem<PraxisClient>(
                    pc => pc.ItemId.Equals(command.ClientId) && !pc.IsMarkedToDelete
                );

                if (client == null) return false;

                using var excel = new ExcelPackage();
                await _praxisReportService.UpdatePraxisReportStatus(command.ReportFileId, PraxisReportProgress.InProgress);
                var isReportPrepared = await _generateOpenItemReportService.PrepareOpenItemReport(
                    command.FilterString,
                    command.EnableDateRange,
                    command.StartDate,
                    command.EndDate,
                    client,
                    excel,
                    command.Translation,
                    command.TimezoneOffsetInMinutes ?? 0
                );

                if (isReportPrepared)
                {
                    var isSuccess = await _storageDataService.UploadFileAsync(
                        command.ReportFileId,
                        command.FileNameWithExtension,
                        excel.GetAsByteArray()
                    );

                    _logger.LogInformation("ExportOpenItemReport upload to storage success: {IsSuccess}", isSuccess);

                    return isSuccess;
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError("ExportOpenItemReport got error: {ErrorMessage}", ex.Message);
            }

            return false;
        }

        public async Task<bool> ExportEquipmentListReport(ExportEquipmentListReportCommand command)
        {
            try
            {
                using var excel = new ExcelPackage();

                await _praxisReportService.UpdatePraxisReportStatus(command.ReportFileId, PraxisReportProgress.InProgress);

                var reportType = _equipmentReportGenerationStrategyService.GetReportType(string.IsNullOrEmpty(command.ClientId));

                var isReportPrepared = await reportType.GenerateReport(excel, command);

                if (!isReportPrepared) return false;
                
                var isSuccess = await _storageDataService.UploadFileAsync(
                    command.ReportFileId,
                    command.FileNameWithExtension,
                    await excel.GetAsByteArrayAsync()
                );

                _logger.LogInformation("ExportEquipmentListReport upload to storage success: {IsSuccess}", isSuccess);

                return isSuccess;

            }
            catch (Exception ex)
            {
                _logger.LogError("ExportEquipmentListReport got error: {ErrorMessage}", ex.Message);
            }

            return false;
        }

        public async Task<bool> ExportEquipmentMaintenanceListReport(
            ExportEquipmentMaintenanceListReportCommand command)
        {
            try
            {
                PraxisClient client = _repository.GetItem<PraxisClient>(
                    pc => pc.ItemId.Equals(command.ClientId) && !pc.IsMarkedToDelete
                );

                if (client == null)
                {
                    return false;
                }

                using var excel = new ExcelPackage();

                await _praxisReportService.UpdatePraxisReportStatus(command.ReportFileId, PraxisReportProgress.InProgress);
                var isValidationReport = command.ScheduleType == "VALIDATION";
                var isReportPrepared = await _generateEquipmentMaintenanceListReportService.PrepareEquipmentMaintenanceListReport(
                    command.FilterString,
                    client,
                    excel,
                    command.Translation,
                    isValidationReport
                );
                if (isReportPrepared)
                {
                    var isSuccess = await _storageDataService.UploadFileAsync(
                        command.ReportFileId,
                        command.FileNameWithExtension,
                        await excel.GetAsByteArrayAsync()
                    );

                    _logger.LogInformation("ExportEquipmentMaintenanceListReport upload to storage success: {IsSuccess}", isSuccess);

                    return isSuccess;
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError("ExportEquipmentMaintenanceListReport got error: {ErrorMessage}", ex.Message);
            }

            return false;
        }

        public async Task<bool> ExportCategoryReport(ExportCategoryReportCommand command)
        {
            try
            {
                var client = _repository.GetItem<PraxisClient>(
                    pc => pc.ItemId.Equals(command.ClientId) && !pc.IsMarkedToDelete
                );

                if (client == null)
                {
                    return false;
                }

                using ExcelPackage excel = new ExcelPackage();
                await _praxisReportService.UpdatePraxisReportStatus(command.ReportFileId, PraxisReportProgress.InProgress);

                bool isReportPrepared =
                    await _generateCategoryReportService.PrepareCategoryReport(
                        command.FilterString,
                        client,
                        excel,
                        command.Translation
                    );

                if (!isReportPrepared) return false;


                var isSuccess = await _storageDataService.UploadFileAsync(
                    command.ReportFileId,
                    command.FileNameWithExtension,
                    excel.GetAsByteArray()
                );

                _logger.LogInformation("ExportCategoryReport upload to storage success: {IsSuccess}", isSuccess);

                return isSuccess;
            }
            catch (Exception ex)
            {
                _logger.LogInformation("Exception occurred during export category report. Exception Message: {ErrorMessage}. Exception Details: {StackTrace}.", ex.Message, ex.StackTrace);
                return false;
            }
        }

        public async Task<bool> ExportTrainingReport(ExportTrainingReportCommand command)
        {
            try
            {
                var client =
                    _repository.GetItem<PraxisClient>(pc => pc.ItemId.Equals(command.ClientId) && !pc.IsMarkedToDelete);

                if (client == null)
                {
                    return false;
                }

                using var excel = new ExcelPackage();
                await _praxisReportService.UpdatePraxisReportStatus(command.ReportFileId, PraxisReportProgress.InProgress);
                bool isReportPrepared =
                    await _generateTrainingReportService.PrepareTrainingReport(
                        command.FilterString,
                        client,
                        excel,
                        command.Translation
                    );

                if (isReportPrepared)
                {
                    var isSuccess = await _storageDataService.UploadFileAsync(
                        command.ReportFileId,
                        command.FileNameWithExtension,
                        excel.GetAsByteArray()
                    );

                    _logger.LogInformation("ExportCategoryReport upload to storage success: {IsSuccess}", isSuccess);

                    return isSuccess;
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogInformation("Exception occurred during export category report. Exception Message: {ErrorMessage}. Exception Details: {StackTrace}.", ex.Message, ex.StackTrace);
                return false;
            }
        }

        public async Task<bool> ExportTrainingDetailsReport(ExportTrainingDetailsReportCommand command)
        {
            try
            {
                var client =
                    _repository.GetItem<PraxisClient>(pc => pc.ItemId.Equals(command.ClientId) && !pc.IsMarkedToDelete);

                if (client == null || command.TrainingId == null)
                {
                    return false;
                }
                var training = _repository.GetItem<PraxisTraining>(
                    t => t.ItemId == command.TrainingId && !t.IsMarkedToDelete
                );

                using var excel = new ExcelPackage();
                bool isReportPrepared =
                    await _generateTrainingDetailsReportService.PrepareTrainingDetailsReport(
                        command.FilterString,
                        client,
                        training,
                        excel,
                        command.Translation
                    );

                if (isReportPrepared)
                {
                    var isSuccess = await _storageDataService.UploadFileAsync(
                        command.ReportFileId,
                        command.FileNameWithExtension,
                        excel.GetAsByteArray()
                    );

                    _logger.LogInformation("ExportCategoryReport upload to storage success: {IsSuccess}", isSuccess);

                    return isSuccess;
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogInformation("Exception occurred during export category report. Exception Message: {ErrorMessage}. Exception Details: {StackTrace}.", ex.Message, ex.StackTrace);
                return false;
            }
        }

        public async Task<bool> ExportDeveloperReport(ExportDeveloperReportCommand command)
        {
            try
            {
                var client = new PraxisClient();
                if (!command.IsReportForAllData)
                {
                    client = _repository.GetItem<PraxisClient>(
                        pc => pc.ItemId.Equals(command.ClientId) && !pc.IsMarkedToDelete
                    );

                    if (client == null)
                    {
                        return false;
                    }
                }

                using var excel = new ExcelPackage();

                await _praxisReportService.UpdatePraxisReportStatus(command.ReportFileId, PraxisReportProgress.InProgress);

                var service = _developerReportGenarateStrategy.GetReportType(command.IsReportForAllData);

                bool isReportPrepared = await service.GenerateReport(
                    command.FilterString,
                    client,
                    excel,
                    command.Translation,
                    command.ReportFileId
                );

                if (isReportPrepared)
                {
                    var isSuccess = await _storageDataService.UploadFileAsync(
                        command.ReportFileId,
                        command.FileNameWithExtension,
                        excel.GetAsByteArray()
                    );

                    _logger.LogInformation("ExportCategoryReport upload to storage success: {IsSuccess}", isSuccess);

                    return isSuccess;
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError("Exception occured during export developer/praxis form report");
                _logger.LogError("Exception occurred: {ErrorMessage} StackTrace: {StackTrace}", ex.Message, ex.StackTrace);
                return false;
            }
        }

        #region PraxisUserListReport

        public async Task<bool> ExportPraxisUserListReport(ExportPraxisUserListReportCommand command)
        {
            try
            {
                using var excel = new ExcelPackage();
                await _praxisReportService.UpdatePraxisReportStatus(command.ReportFileId, PraxisReportProgress.InProgress);
                var service = _praxisUserListReportGenerateStrategyService
                    .GetReportType(string.IsNullOrEmpty(command.ClientId));

                var isReportPrepared = await service.GenerateReport(excel, command);

                if (!isReportPrepared) return false;

                var isSuccess = await _storageDataService.UploadFileAsync(
                    command.ReportFileId,
                    command.FileNameWithExtension,
                    excel.GetAsByteArray()
                );

                _logger.LogInformation("ExportUserListReport upload to storage success: {IsSuccess}", isSuccess);

                return isSuccess;
            }
            catch (Exception ex)
            {
                _logger.LogError("Exception occured during Exporting UserListReport");
                _logger.LogError("Exception occurred: {ErrorMessage} StackTrace: {StackTrace}", ex.Message, ex.StackTrace);
                return false;
            }
        }

        public async Task<bool> ExportRiskOverviewReport(ExportRiskOverviewReportCommand command)
        {
            try
            {
                using var excel = new ExcelPackage();
                await _praxisReportService.UpdatePraxisReportStatus(command.ReportFileId, PraxisReportProgress.InProgress);
                var reportType = _riskOverviewReportGenerateStrategyService.GetReportType(
                    string.IsNullOrEmpty(command.ClientId)
                );

                var isReportPrepared = await reportType.GenerateReport(excel, command);

                if (!isReportPrepared) return false;

                var excelAsByteArray = excel.GetAsByteArray();

                var isSuccess = await _storageDataService.UploadFileAsync(
                    command.ReportFileId,
                    command.FileNameWithExtension,
                    excelAsByteArray
                );

                _logger.LogInformation("ExportRiskOverviewReport upload to storage success: {IsSuccess}", isSuccess);
                return isSuccess;
            }
            catch (Exception ex)
            {
                _logger.LogError("Exception occured during Exporting RiskOverviewReport");
                _logger.LogError("Exception occurred: {ErrorMessage} StackTrace: {StackTrace}", ex.Message, ex.StackTrace);
                return false;
            }
        }

        public Task<bool> ExportProcessGuideDeveloperReport(ExportProcessGuideReportForDeveloperCommand command)
        {
            return Task.FromResult(true);
        }

        #endregion PraxisUserListReport

        public async Task<bool> ExportPhotoDocumentationReport(
            string htmlFileId,
            string reportFileId,
            string reportFileName
        )
        {
            try
            {
                var fileInfo = await _fileService.GetFileInfoFromStorage(htmlFileId);
                if (string.IsNullOrEmpty(fileInfo?.Url)) return false;

                await using var content = _storageDataService.GetFileContentStream(fileInfo.Url);
                if (content == null) return false;

                var success = false;
                var docByteData = _docGenerationService.PrepareDocumentFromHtmlStream(content);
                if (docByteData != null && docByteData.Length != 0)
                {
                    success = await _storageDataService.UploadFileAsync(reportFileId, reportFileName, docByteData);
                }

                return success;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error occurred while generation docx from html");
                _logger.LogError("Exception occurred: {ErrorMessage} StackTrace: {StackTrace}", ex.Message, ex.StackTrace);
                return false;
            }
        }

        public async Task<bool> ExportCirsReport(ExportReportCommand command)
        {
            try
            {
                await _praxisReportService.CreatePraxisReport(command, "CIRS");

                using var excel = new ExcelPackage();
                await _praxisReportService.UpdatePraxisReportStatus(command.ReportFileId, PraxisReportProgress.InProgress);

                var isReportPrepared = await _cirsReportGenerationService.GenerateReport(excel, command);

                if (!isReportPrepared) return false;

                var isSuccess = await _storageDataService.UploadFileAsync(
                    command.ReportFileId,
                    command.FileNameWithExtension,
                    excel.GetAsByteArray());

                _logger.LogInformation("ExportIncidentReport upload to storage success: {IsSuccess}", isSuccess);


                await _praxisReportService.UpdatePraxisReportStatus(
                    command.ReportFileId,
                    isSuccess ? PraxisReportProgress.Complete : PraxisReportProgress.Failed);

                return isSuccess;
            }
            catch (Exception ex)
            {
                _logger.LogError("Exception occured during exporting incident report");
                _logger.LogError("Exception occurred: {ErrorMessage} StackTrace: {StackTrace}", ex.Message, ex.StackTrace);
                return false;
            }
        }

        public async Task<bool> GenerateShiftPlanReportAsync(GenerateShiftPlanReportCommand command)
        {
            try
            {
                var praxisReport = await _praxisReportService.CreatePraxisReportWithExportReportCommand(command, "SHIFT_PLAN");
                _generateShiftPlanReportService.SetupRolesForShiftPlanReport(praxisReport, command);
                await _praxisReportService.InsertOrUpdatePraxisReport(praxisReport, command.ReportFileId);

                using var excel = new ExcelPackage();
                await _praxisReportService.UpdatePraxisReportStatus(command.ReportFileId, PraxisReportProgress.InProgress);

                var isReportPrepared = _generateShiftPlanReportService.GenerateShiftPlanReport(excel, command);
                if (!isReportPrepared)
                    return false;

                var isSuccess = await _storageDataService.UploadFileAsync(
                    command.ReportFileId,
                    command.FileNameWithExtension,
                    await excel.GetAsByteArrayAsync()
                );

                await _praxisReportService.UpdatePraxisReportStatus(
                    command.ReportFileId,
                    isSuccess ? PraxisReportProgress.Complete : PraxisReportProgress.Failed
                );

                _logger.LogInformation("Shift plan report upload to storage success: {IsSuccess}", isSuccess);
                return isSuccess;
            }
            catch (Exception ex)
            {
                _logger.LogError("Exception occurred during exporting incident report");
                _logger.LogError("Exception occurred: {ErrorMessage} StackTrace: {StackTrace}", ex.Message, ex.StackTrace);
                return false;
            }
        }

        public async Task<bool> GenerateShiftReportAsync(GenerateShiftReportCommand command)
        {
            try
            {
                var praxisReport = await _praxisReportService.CreatePraxisReportWithExportReportCommand(command, "SHIFT");
                _generateShiftReportService.SetupRolesForShiftReport(praxisReport, command);
                await _praxisReportService.InsertOrUpdatePraxisReport(praxisReport, command.ReportFileId);

                using var excel = new ExcelPackage();
                await _praxisReportService.UpdatePraxisReportStatus(command.ReportFileId, PraxisReportProgress.InProgress);

                var isReportPrepared = _generateShiftReportService.GenerateShiftReport(excel, command);
                if (!isReportPrepared) return false;

                var isSuccess = await _storageDataService.UploadFileAsync(
                    command.ReportFileId,
                    command.FileNameWithExtension,
                    await excel.GetAsByteArrayAsync()
                );

                await _praxisReportService.UpdatePraxisReportStatus(
                    command.ReportFileId,
                    isSuccess ? PraxisReportProgress.Complete : PraxisReportProgress.Failed
                );

                _logger.LogInformation("Shift report upload to storage success status: {isSuccess}", isSuccess);
                return isSuccess;
            }
            catch (Exception ex)
            {
                _logger.LogError("Exception occurred during exporting shift report");
                _logger.LogError("Exception Message: {Message}. Exception Details: {StackTrace}.", ex.Message, ex.StackTrace);
                return false;
            }
        }

        public async Task<bool> GenerateLibraryReportAsync(GenerateLibraryReportCommand command)
        {
            try
            {
                var praxisReport = await _praxisReportService.CreatePraxisReportWithExportReportCommand(command, "LIBRARY");
                _generateLibraryReportService.SetupRolesForLibraryReport(praxisReport, command);
                await _praxisReportService.InsertOrUpdatePraxisReport(praxisReport, command.ReportFileId);

                using var excel = new ExcelPackage();
                await _praxisReportService.UpdatePraxisReportStatus(command.ReportFileId, PraxisReportProgress.InProgress);

                var isReportPrepared = _generateLibraryReportService.GenerateLibraryReport(excel, command);
                if (!isReportPrepared)
                    return false;

                var isSuccess = await _storageDataService.UploadFileAsync(
                    command.ReportFileId,
                    command.FileNameWithExtension,
                    excel.GetAsByteArray()
                );

                await _praxisReportService.UpdatePraxisReportStatus(
                    command.ReportFileId,
                    isReportPrepared ? PraxisReportProgress.Complete : PraxisReportProgress.Failed
                );

                _logger.LogInformation("Shift plan report upload to storage success: {IsSuccess}", isSuccess);
                return isSuccess;
            }
            catch (Exception ex)
            {
                _logger.LogError("Exception occurred during exporting incident report");
                _logger.LogError("Exception occurred: {ErrorMessage} StackTrace: {StackTrace}", ex.Message, ex.StackTrace);
                return false;
            }
        }

        public async Task<bool> ExportSuppliersReportAsync(ExportSuppliersReportCommand command)
        {
            try
            {
                var client =
                     _repository.GetItem<PraxisClient>(pc => pc.ItemId.Equals(command.ClientId) && !pc.IsMarkedToDelete);

                if (client == null || client?.AdditionalInfos==null) return false;

                using var excel = new ExcelPackage();

                bool isReportPrepared = await _generateSuppliersReport.PrepareSuppliersReport(null, client, excel, command.Translation, command.SupplierKeyNameTranslation);
                if (!isReportPrepared) return false;

                var isSuccess = await _storageDataService.UploadFileAsync(
                    command.ReportFileId,
                    command.FileNameWithExtension,
                    excel.GetAsByteArray()
                );

                _logger.LogInformation("ExportSuppliersReportAsync upload to storage success -> {IsSuccess}", isSuccess);

                return isSuccess;
            }
            catch (Exception ex)
            {
                _logger.LogError("Exception occurred during exporting ExportSuppliersReportAsync report");
                _logger.LogError("Exception Message: {Message}. Exception Details: {StackTrace}.", ex.Message, ex.StackTrace);
                return false;
            }
        }

        public async Task<bool> ExportLibraryDocumentAssigneesReportAsync(
            ExportLibraryDocumentAssigneesReportCommand command)
        {
            try
            {
                using var excel = new ExcelPackage();
                await _praxisReportService.UpdatePraxisReportStatus(command.ReportFileId, PraxisReportProgress.InProgress);
                var isReportPrepared = await _generateLibraryDocumentAssigneesReportService.GenerateLibraryDocumentAssigneesReportAsync(excel, command);

                if (!isReportPrepared) return false;

                var isSuccess = await _storageDataService.UploadFileAsync(
                    command.ReportFileId,
                    command.FileNameWithExtension,
                    await excel.GetAsByteArrayAsync()
                );

                var rowLevelSecurity = _generateLibraryDocumentAssigneesReportService.PrepareRowLevelSecurity(command.ClientIds);
                await _praxisReportService.InsertOrUpdateRowLevelSecurity(rowLevelSecurity, command.ReportFileId);
                await _generateLibraryDocumentAssigneesReportService.UpdateClientsInReport(command.ReportFileId, command.ClientIds);

                _logger.LogInformation("ExportLibraryDocumentAssigneesReportAsync upload to storage success -> {IsSuccess}", isSuccess);

                return isSuccess;
            }
            catch (Exception ex)
            {
                _logger.LogError("Exception occurred during exporting ExportLibraryDocumentAssigneesReportAsync report");
                _logger.LogError("Exception Message: {Message}. Exception Details: {StackTrace}.", ex.Message, ex.StackTrace);
                return false;
            }
        }

        public async Task<bool> GenerateQuickTaskPlanReportAsync(GenerateQuickTaskPlanReportCommand command)
        {
            try
            {
                var praxisReport = await _praxisReportService.CreatePraxisReportWithExportReportCommand(command, "QUICK_TASK_PLAN");
                _generateQuickTaskPlanReportService.SetupRolesForQuickTaskPlanReport(praxisReport, command);
                await _praxisReportService.InsertOrUpdatePraxisReport(praxisReport, command.ReportFileId);

                using var excel = new ExcelPackage();
                await _praxisReportService.UpdatePraxisReportStatus(command.ReportFileId, PraxisReportProgress.InProgress);

                var isReportPrepared = _generateQuickTaskPlanReportService.GenerateQuickTaskPlanReport(excel, command);
                if (!isReportPrepared)
                    return false;

                var isSuccess = await _storageDataService.UploadFileAsync(
                    command.ReportFileId,
                    command.FileNameWithExtension,
                    await excel.GetAsByteArrayAsync()
                );

                await _praxisReportService.UpdatePraxisReportStatus(
                    command.ReportFileId,
                    isSuccess ? PraxisReportProgress.Complete : PraxisReportProgress.Failed
                );

                _logger.LogInformation("QuickTask plan report upload to storage success: {IsSuccess}", isSuccess);
                return isSuccess;
            }
            catch (Exception ex)
            {
                _logger.LogError("Exception occurred during exporting quick task plan report");
                _logger.LogError("Exception occurred: {ErrorMessage} StackTrace: {StackTrace}", ex.Message, ex.StackTrace);
                return false;
            }
        }

        public async Task<bool> GenerateQuickTaskReportAsync(GenerateQuickTaskReportCommand command)
        {
            try
            {
                var praxisReport = await _praxisReportService.CreatePraxisReportWithExportReportCommand(command, "QUICK_TASK");
                _generateQuickTaskReportService.SetupRolesForQuickTaskReport(praxisReport, command);
                await _praxisReportService.InsertOrUpdatePraxisReport(praxisReport, command.ReportFileId);

                using var excel = new ExcelPackage();
                await _praxisReportService.UpdatePraxisReportStatus(command.ReportFileId, PraxisReportProgress.InProgress);

                var isReportPrepared = _generateQuickTaskReportService.GenerateQuickTaskReport(excel, command);
                if (!isReportPrepared)
                    return false;

                var isSuccess = await _storageDataService.UploadFileAsync(
                    command.ReportFileId,
                    command.FileNameWithExtension,
                    await excel.GetAsByteArrayAsync()
                );

                await _praxisReportService.UpdatePraxisReportStatus(
                    command.ReportFileId,
                    isSuccess ? PraxisReportProgress.Complete : PraxisReportProgress.Failed
                );

                _logger.LogInformation("QuickTask report upload to storage success: {IsSuccess}", isSuccess);
                return isSuccess;
            }
            catch (Exception ex)
            {
                _logger.LogError("Exception occurred during exporting quick task report");
                _logger.LogError("Exception occurred: {ErrorMessage} StackTrace: {StackTrace}", ex.Message, ex.StackTrace);
                return false;
            }
        }
    }

    public class FilterPraxisUser
    {
        public string _id { get; set; }
        public string DisplayName { get; set; }
    }
}