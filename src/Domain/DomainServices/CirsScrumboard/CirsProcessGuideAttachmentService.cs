using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.ProcessGuide;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.Task;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.CirsScrumBoard;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Entities.CirsModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.CirsScrumboard;

#nullable enable
public class CirsProcessGuideAttachmentService : ICirsProcessGuideAttachmentService
{
    private readonly ISecurityContextProvider _securityContextProvider;
    private readonly IRepository _repository;
    private readonly IBlocksMongoDbDataContextProvider _mongoDbDataContextProvider;
    private readonly ILogger<CirsProcessGuideAttachmentService> _logger;
    private readonly IAuthUtilityService _authUtilityService;
    private readonly IServiceClient _serviceClient;
    private readonly ICirsPermissionService _cirsPermissionService;
    private readonly IChangeLogService _changeLogService;

    public CirsProcessGuideAttachmentService(
        ISecurityContextProvider securityContextProvider,
        IRepository repository,
        IBlocksMongoDbDataContextProvider mongoDbDataContextProvider,
        IAuthUtilityService authUtilityService,
        IServiceClient serviceClient,
        IConfiguration configuration,
        ILogger<CirsProcessGuideAttachmentService> logger,
        ICirsPermissionService cirsPermissionService,
        IChangeLogService changeLogService)
    {
        _repository = repository;
        _securityContextProvider = securityContextProvider;
        _mongoDbDataContextProvider = mongoDbDataContextProvider;
        _authUtilityService = authUtilityService;
        _serviceClient = serviceClient;
        _logger = logger;
        _cirsPermissionService = cirsPermissionService;
        _changeLogService = changeLogService;
    }

    public async Task UpdateOnProcessGuideCreatedAsync(string cirsReportId, string processGuideId)
    {
        var processGuide = await GetProjectedProcessGuideAsync(processGuideId);
        if (processGuide == null) return;

        var relatedEntityId = processGuide.RelatedEntityId;
        var cirsReport = GetProjectedCirsReport(relatedEntityId);
        if (cirsReport == null) return;

        var clientId = cirsReport.AffectedInvolvedParties?.FirstOrDefault()?.PraxisClientId;

        var assignedUsers = processGuide.ControlledMembers?.ToList() ?? new List<string>();
        var assignedGroup = new List<string>();

        if (assignedUsers.Count == 0 && assignedGroup.Count == 0)
        {
            assignedGroup.Add($"{RoleNames.PowerUser_Dynamic}_{clientId}");
            assignedGroup.Add($"{RoleNames.Leitung_Dynamic}_{clientId}");
            assignedGroup.Add($"{RoleNames.MpaGroup_Dynamic}_{clientId}");
        }
        var userIds = new List<string>();
        if (!string.IsNullOrEmpty(processGuide.CreatedBy)) userIds.Add(processGuide.CreatedBy);
        var involvedUsers = _repository.GetItems<PraxisUser>
                                (pu => assignedUsers.Contains(pu.ItemId) || userIds.Contains(pu.UserId))?
                                    .Select(pu => new InvolvedUser()
                                    {
                                        PraxisUserId = pu.ItemId,
                                        UserId = pu.UserId,
                                        Image = pu.Image,
                                        DisplayName = pu.DisplayName,
                                        Email = pu.Email
                                    })
                                    .ToList() ?? new List<InvolvedUser>();
        var processGuideAttachment =
            new ProcessGuideAttachment
            {
                ProcessGuideId = processGuide.ItemId,
                ProcessGuideTitle = processGuide.FormName,
                ProcessGuideDescription = processGuide.Description,
                FormId = processGuide.FormId,
                AssignedBy = involvedUsers.Find(i => i.UserId == processGuide.CreatedBy),
                DueDate = processGuide.TaskSchedule?.ToDateTime,
                AssignedGroup = assignedGroup,
                AssignedUsers = involvedUsers.Where(i => assignedUsers.Contains(i.PraxisUserId ?? "")).ToList(),
            };

        await UpdateProcessGuideAttachmentAsync(cirsReportId, processGuideAttachment);
    }

    public async Task UpdateProcessGuideCompletionStatus(string processGuideId, int completionStatus)
    {
        _logger.LogInformation("Entered in Method : {MethodName} with ProcessGuideId: {ProcessGuideId}",
            nameof(UpdateProcessGuideCompletionStatus), processGuideId);
        try
        {
            var processGuide = await GetProjectedProcessGuideAsync(processGuideId);
            if (processGuide == null)
            {
                _logger.LogWarning("Method : {MethodName}. Completion Status can't update. ProcessGuide not found with ProcessGuideId: {ProcessGuideId}",
                    nameof(UpdateProcessGuideCompletionStatus), processGuideId);
                return;
            }

            var cirsReportId = processGuide.RelatedEntityId;
            var cirsReport = GetProjectedCirsReport(cirsReportId);
            if (cirsReport == null)
            {
                _logger.LogWarning("Method : {MethodName}. Completion Status can't update. CirsReport not found with CirsReportId: {CirsReportId}",
                    nameof(UpdateProcessGuideCompletionStatus), cirsReportId);
                return;
            }
            var processGuideAttachment = cirsReport.ProcessGuideAttachments?.Find(pg => pg.ProcessGuideId == processGuideId);
            if (processGuideAttachment == null)
            {
                _logger.LogWarning("Method : {MethodName}. Completion Status can't update. ProcessGuideAttachment not found with CirsReportId: {CirsReportId}",
                    nameof(UpdateProcessGuideCompletionStatus), cirsReportId);
                return;
            }

            processGuideAttachment.CompletionStatus = completionStatus;
            await UpdateProcessGuideAttachmentAsync(cirsReportId, processGuideAttachment);

        }
        catch (Exception e)
        {
            _logger.LogError("Exception in Method: {MethodName}. Error Message: {Message}. Error Details: {STackTrace}",
                nameof(UpdateProcessGuideCompletionStatus), e.Message, e.StackTrace);
        }
    }

    private async Task<PraxisProcessGuide?> GetProjectedProcessGuideAsync(string processGuideId)
    {
        var filter = Builders<PraxisProcessGuide>.Filter.Where(guide =>
                    guide.ItemId == processGuideId &&
                    guide.IsMarkedToDelete == false);
        var collection = _mongoDbDataContextProvider.GetTenantDataContext().GetCollection<PraxisProcessGuide>("PraxisProcessGuides");
        var documents = await collection
            .Find(filter)
            .ToListAsync();
        var processGuide = documents.Any()
            ? documents.FirstOrDefault()
            : null;
        return processGuide;
    }

    private CirsGenericReport? GetProjectedCirsReport(string cirsId)
    {
        var builder = Builders<CirsGenericReport>.Filter;
        var filter = builder.Eq(guide => guide.ItemId, cirsId) &
                     builder.Eq(guide => guide.IsMarkedToDelete, false);

        var collection = _mongoDbDataContextProvider.GetTenantDataContext().GetCollection<CirsGenericReport>("CirsGenericReports");
        var document = collection?
            .Find(filter)?
            .FirstOrDefault();
        var report = document != null
            ? (document)
            : null;
        return report;
    }

    private async Task UpdateProcessGuideAttachmentAsync(
        string cirsReportId,
        ProcessGuideAttachment? processGuideAttachment)
    {
        var updatedBy = _securityContextProvider.GetSecurityContext().UserId;
        var cirsReport = await _repository.GetItemAsync<CirsGenericReport>(c => c.ItemId == cirsReportId);
        if (cirsReport == null || processGuideAttachment == null) return;
        var pgAttachments = cirsReport.ProcessGuideAttachments ?? new List<ProcessGuideAttachment>();
        var existingIndex = pgAttachments.FindIndex(o => o.ProcessGuideId == processGuideAttachment?.ProcessGuideId && !string.IsNullOrEmpty(o.ProcessGuideId));

        if (existingIndex == -1) pgAttachments.Add(processGuideAttachment);
        else pgAttachments[existingIndex] = processGuideAttachment;

        cirsReport.ProcessGuideAttachments = pgAttachments;
        _cirsPermissionService.SetCirsReportPermission(cirsReport);

        var updates = new Dictionary<string, object>
        {
            { nameof(CirsGenericReport.ProcessGuideAttachments), pgAttachments },
            { nameof(CirsGenericReport.LastUpdateDate), DateTime.UtcNow.ToLocalTime() },
            { nameof(CirsGenericReport.LastUpdatedBy), updatedBy },
            { nameof(CirsGenericReport.RolesAllowedToRead), cirsReport.RolesAllowedToRead },
            { nameof(CirsGenericReport.IdsAllowedToRead), cirsReport.IdsAllowedToRead }
        };
        var builder = Builders<BsonDocument>.Filter;
        var updateFilters = builder.Eq("_id", cirsReportId);

        _ = await _changeLogService.UpdateChange(nameof(CirsGenericReport), updateFilters, updates);
    }

    private async Task<PraxisProcessGuideConfig> SaveNewProcessGuideConfigAsync(CirsGenericReport cirsReport, PraxisForm praxisForm)
    {
        var assignedclient = await GetAssignedClientInfo(cirsReport);

        var taskTimeTable = new PraxisTaskTimetable()
        {
            SubmissionDates = new List<DateTime> { cirsReport.DueDate.Date }
        };

        var securityContext = _securityContextProvider.GetSecurityContext();

        var newPgConfig = new PraxisProcessGuideConfig()
        {
            ItemId = Guid.NewGuid().ToString(),
            CreateDate = DateTime.UtcNow,
            FormId = praxisForm.ItemId,
            TopicKey = praxisForm.TopicKey,
            TopicValue = praxisForm.TopicValue,
            Title = praxisForm.Title,
            TaskTimetable = taskTimeTable,
            Clients = new ProcessGuideClientInfo[] { assignedclient },
            ControlledMembers = assignedclient.ControlledMembers,
            DueDate = cirsReport.DueDate.Date,
            RolesAllowedToRead = new string[] { RoleNames.Admin, RoleNames.AppUser },
            RolesAllowedToUpdate = new string[] { RoleNames.Admin, RoleNames.TaskController, RoleNames.PowerUser, RoleNames.Leitung },
            RolesAllowedToDelete = new string[] { RoleNames.Admin },
            IdsAllowedToDelete = new string[] { securityContext.UserId }
        };

        await _repository.SaveAsync(newPgConfig);

        return newPgConfig;
    }

    private async Task<ProcessGuideClientInfo> GetAssignedClientInfo(CirsGenericReport cirsReport)
    {
        var assignedPraxisUserIds = GetAssignedPraxisUserIds(cirsReport);
        var departmentId = cirsReport.AffectedInvolvedParties?.FirstOrDefault()?.PraxisClientId;
        var userClient = await _repository.GetItemAsync<PraxisClient>(c => c.ItemId == departmentId);

        return new ProcessGuideClientInfo
        {
            ClientId = userClient?.ItemId,
            ClientName = userClient?.ClientName,
            CategoryId = string.Empty,
            CategoryName = string.Empty,
            SubCategoryId = string.Empty,
            SubCategoryName = string.Empty,
            ControlledMembers = assignedPraxisUserIds,
            HasSpecificControlledMembers = assignedPraxisUserIds?.Count > 0
        };
    }

    private RelatedEntityObject GetRelatedEntityObject(
        PraxisProcessGuideConfig processGuideConfig,
        string formName,
        CirsGenericReport cirsReport)
    {
        return new RelatedEntityObject()
        {
            ItemId = Guid.NewGuid().ToString(),
            FormId = processGuideConfig.FormId,
            FormName = formName,
            Title = processGuideConfig.Title,
            Tags = new[] { PraxisTag.IsValidPraxisProcessGuide },
            Language = "en-US",
            TopicKey = processGuideConfig.TopicKey,
            TopicValue = processGuideConfig.TopicValue,
            Description = processGuideConfig.Title,
            IsActive = true,
            ControlledMembers = processGuideConfig.ControlledMembers,
            Clients = processGuideConfig.Clients,
            ClientId = processGuideConfig.Clients.FirstOrDefault()?.ClientId,
            ClientName = processGuideConfig.Clients.FirstOrDefault()?.ClientName,
            DueDate = processGuideConfig.DueDate?.ToString("yyyy-MM-dd"),
            PraxisProcessGuideConfigId = processGuideConfig.ItemId,
            RelatedEntityId = cirsReport.ItemId,
            RelatedEntityName = nameof(CirsGenericReport)
        };
    }

    private async Task UpdateProcessGuideAsync(PraxisProcessGuide processGuide, CirsGenericReport cirsReport)
    {
        var controllMembers = GetAssignedPraxisUserIds(cirsReport);

        if (processGuide.Clients?.Any() == true)
        {
            processGuide.Clients.First().ControlledMembers = controllMembers;
            processGuide.Clients.First().CategoryId = string.Empty;
            processGuide.Clients.First().CategoryName = string.Empty;
            processGuide.Clients.First().SubCategoryId = string.Empty;
            processGuide.Clients.First().SubCategoryName = string.Empty;
        }

        processGuide.ControlledMembers = controllMembers;
        processGuide.DueDate = cirsReport.DueDate.Date;
        if (processGuide.TaskSchedule != null)
        {
            var fromDate = cirsReport.DueDate.Date;
            var toDate = cirsReport.DueDate;
            processGuide.TaskSchedule.TaskDateTime = fromDate;
            processGuide.TaskSchedule.FromDateTime = fromDate;
            processGuide.TaskSchedule.ToDateTime = toDate;
        }

        await _repository.UpdateAsync(pg => pg.ItemId == processGuide.ItemId, processGuide);
    }

    private List<string> GetAssignedPraxisUserIds(CirsGenericReport cirsReport)
    {
        var allusers = new List<string>();
        var responsibleUsers = cirsReport?.ResponsibleUsers?
            .Select(ru => ru.UserId)?
            .ToList() ?? new List<string>();
        var invovledUsers = cirsReport?.AffectedInvolvedParties?
            .Where(party => party.InvolvedUsers != null)?
            .SelectMany(party => party.InvolvedUsers!.Select(user => user.UserId))?
            .ToList() ?? new List<string>();

        allusers.AddRange(responsibleUsers);
        allusers.AddRange(invovledUsers);

        allusers = allusers.Distinct().ToList();
        return _repository.GetItems<PraxisUser>(pu => allusers.Contains(pu.UserId)).Select(pu => pu.ItemId).ToList();
    }

    private CreateTaskScheduleRequestModel GetTaskSchedulerModel(
        PraxisProcessGuideConfig processGuideConfig,
        string formName,
        CirsGenericReport cirsReport)
    {
        var taskDatas = new List<TaskData>
        {
            new()
            {
                HasRelatedEntity = true,
                HasTaskScheduleIntoRelatedEntity = true,
                RelatedEntityName = EntityName.PraxisProcessGuide,
                TaskSummaryId = Guid.NewGuid().ToString(),
                Title = processGuideConfig.Title,
                RelatedEntityObject = GetRelatedEntityObject(processGuideConfig, formName, cirsReport)
            }
        };

        var taskScheduleDetails = new TaskScheduleDetails()
        {
            HasToMoveNextDay = true,
            IsRepeat = false,
            SubmissionDates = new List<string?> { processGuideConfig.DueDate?.ToString("yyyy-MM-dd") }
        };

        return new CreateTaskScheduleRequestModel()
        {
            TaskScheduleDetails = taskScheduleDetails,
            TaskDatas = taskDatas,
            AssignMembers = new List<object>()
        };
    }
}