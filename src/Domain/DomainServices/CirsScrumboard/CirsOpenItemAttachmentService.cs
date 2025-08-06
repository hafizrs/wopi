using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using Selise.Ecap.Entities.PrimaryEntities.Dms;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.OpenItem;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.Shared;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.CirsScrumBoard;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.CockpitModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Entities.CirsModule;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.CirsScrumboard;

public class CirsOpenItemAttachmentService : ICirsOpenItemAttachmentService
{
    private readonly ILogger<CirsOpenItemAttachmentService> _logger;
    private readonly IRepository _repository;
    private readonly ISecurityContextProvider _securityContextProvider;
    private readonly IChangeLogService _changeLogService;
    private readonly ICirsPermissionService _cirsPermissionService;
    private readonly ICockpitSummaryCommandService _cockpitSummaryCommandService;

    public CirsOpenItemAttachmentService(
        ILogger<CirsOpenItemAttachmentService> logger,
        IRepository repository,
        ISecurityContextProvider securityContextProvider,
        IChangeLogService changeLogService,
        ICirsPermissionService cirsPermissionService,
        ICockpitSummaryCommandService cockpitSummaryCommandService
    )
    {
        _logger = logger;
        _repository = repository;
        _securityContextProvider = securityContextProvider;
        _changeLogService = changeLogService;
        _cirsPermissionService = cirsPermissionService;
        _cockpitSummaryCommandService = cockpitSummaryCommandService;
    }
    public async Task UpdateCirsOnOpenItemCreate(string cirsReportId, string openItemId)
    {
        _logger.LogInformation("Entered into the Method: {MethodName} with CirsReportId: {CirsReportId}", 
            nameof(UpdateCirsOnOpenItemCreate), cirsReportId);
        try
        {
            var openItem = await GetPraxisOpenItem(openItemId);
            if (openItem == null)
            {
                _logger.LogWarning("Method : {MethodName}  OpenItem Attachment can't create. OpenItem not found with OpenItemId: {OpenItemId}",
                    nameof(UpdateCirsOnOpenItemCreate), openItemId);
                return ;
            }
            var assignedUsers = openItem.ControlledMembers?.ToList() ?? new List<string>();
            var assignedGroup = openItem.ControlledGroups?.ToList() ?? new List<string>();

            if (assignedUsers.Count == 0 && assignedGroup.Count == 0)
            {
                assignedGroup.Add($"{RoleNames.PowerUser_Dynamic}_{openItem.ClientId}");
                assignedGroup.Add($"{RoleNames.Leitung_Dynamic}_{openItem.ClientId}");
                assignedGroup.Add($"{RoleNames.MpaGroup_Dynamic}_{openItem.ClientId}");
            }
            var userIds = new List<string>();
            if (!string.IsNullOrEmpty(openItem.CreatedBy)) userIds.Add(openItem.CreatedBy);
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
            var openItemAttachment = new OpenItemAttachment
            {
                OpenItemId = openItem.ItemId,
                OpenItemName = openItem.Title,
                CompletionStatus = openItem.OverAllCompletionStatus.Value,
                AssignedBy = involvedUsers.Find(i => i.UserId == openItem.CreatedBy),
                DueDate = openItem.TaskSchedule?.ToDateTime,
                AssignedGroup = assignedGroup,
                AssignedUsers = involvedUsers.Where(i => assignedUsers.Contains(i.PraxisUserId)).ToList()
            };
            await UpdateOpenItemAttachment(cirsReportId, openItemAttachment);
        }
        catch (Exception e)
        {
            _logger.LogError("Exception in Method: {MethodName}. Error Message: {Message}. Error Details: {StackTrace}",
                nameof(UpdateCirsOnOpenItemCreate), e.Message, e.StackTrace);
        }
    }

    public async Task UpdateCirsOpenItemCompletionStatus(string openItemId, PraxisKeyValue completionStatus)
    {
        _logger.LogInformation("Entered into the Method: {MethodName} with OpenItemId: {OpenItemId}", 
            nameof(UpdateCirsOpenItemCompletionStatus), openItemId);
        try
        {
            var openItem = await GetPraxisOpenItem(openItemId);
            if (openItem == null)
            {
                _logger.LogWarning("Method : {MethodName}. Completion Status can't update. OpenItem not found with OpenItemId: {OpenItemId}",
                    nameof(UpdateCirsOpenItemCompletionStatus), openItemId);
                return ;
            }

            var cirsReportId = openItem.TaskReferenceId;
            if (string.IsNullOrEmpty(cirsReportId))
            {
                _logger.LogWarning("Method : {MethodName}. Completion Status can't update. CirsReportId not found for OpenItemId: {OpenItemId}",
                    nameof(UpdateCirsOpenItemCompletionStatus), openItemId);
                return ;
            }

            await UpdateCompletionStatus(cirsReportId, openItemId, completionStatus.Value);

        }
        catch (Exception e)
        {
            _logger.LogError("Exception in Method: {MethodName}. Error Message: {Message}. Error Details: {StackTrace}",
                nameof(UpdateCirsOpenItemCompletionStatus), e.Message, e.StackTrace);
        }
    }

    private async Task UpdateOpenItemAttachment(string cirsReportId, OpenItemAttachment openItemAttachment)
    {
        _logger.LogInformation("Entered into the Method: {MethodName} with CirsReportId: {CirsReportId}", 
            nameof(UpdateOpenItemAttachment), cirsReportId);
        try
        {
            var cirsReport = await GetCirsGenericReport(cirsReportId);
            
            if (cirsReport == null)
            {
                _logger.LogWarning("Method : {MethodName}  OpenItem Attachment can't create. CirsReport not found with CirsReportId: {CirsReportId}",
                    nameof(UpdateOpenItemAttachment), cirsReportId);
                return ;
            }

            await UpdateRepository(cirsReportId, openItemAttachment);
        }
        catch (Exception e)
        {
            _logger.LogError("Exception in Method: {MethodName}. Error Message: {Message}. Error Details: {StackTrace}",
                nameof(UpdateOpenItemAttachment), e.Message, e.StackTrace);
        }
    }

    private async Task UpdateCompletionStatus(string cirsReportId, string openItemId, string completionStatus) 
    {
        _logger.LogInformation("Entered into the Method: {MethodName} with CirsReportId: {CirsReportId}", 
            nameof(UpdateCompletionStatus), cirsReportId);
        try
        {
            var cirsReport = await GetCirsGenericReport(cirsReportId);
            if (cirsReport == null)
            {
                _logger.LogWarning("Method : {MethodName}. CirsReport not found with CirsReportId: {CirsReportId}",
                    nameof(UpdateCompletionStatus), cirsReportId);
                return ;
            }
            var openItemAttachment = cirsReport?.OpenItemAttachments?.Find(o => o.OpenItemId == openItemId);
            if (openItemAttachment == null)
            {
                _logger.LogWarning("Method : {MethodName}  OpenItemAttachment not found for CirsReportId: {CirsReportId}",
                    nameof(UpdateCompletionStatus), cirsReportId);
                return ;
            }

            openItemAttachment.CompletionStatus = completionStatus;
            await UpdateRepository(cirsReportId, openItemAttachment);
        }
        catch (Exception e)
        {
            _logger.LogError("Exception in Method: {MethodName}. Error Message: {Message}. Error Details: {StackTrace}",
                nameof(UpdateCompletionStatus), e.Message, e.StackTrace);
        
        }
    }

    private async Task UpdateRepository(string cirsReportId, OpenItemAttachment attachment)
    {
        var updatedBy = _securityContextProvider.GetSecurityContext().UserId;
        var localTime = DateTime.UtcNow.ToLocalTime();
        var cirsReport = await _repository.GetItemAsync<CirsGenericReport>(r => r.ItemId == cirsReportId);
        if (cirsReport == null) return;

        var openItemAttachments = cirsReport.OpenItemAttachments ?? new List<OpenItemAttachment>();
        var existingIndex = openItemAttachments.FindIndex(o => o.OpenItemId == attachment?.OpenItemId && !string.IsNullOrEmpty(o.OpenItemId));

        if (existingIndex == -1) openItemAttachments.Add(attachment);
        else openItemAttachments[existingIndex] = attachment;

        cirsReport.OpenItemAttachments = openItemAttachments;
        _cirsPermissionService.SetCirsReportPermission(cirsReport);

        var updates = new Dictionary<string, object>
        {
            { nameof(CirsGenericReport.OpenItemAttachments), openItemAttachments },
            { nameof(CirsGenericReport.LastUpdateDate), localTime },
            { nameof(CirsGenericReport.LastUpdatedBy), updatedBy },
            { nameof(CirsGenericReport.RolesAllowedToRead), cirsReport.RolesAllowedToRead },
            { nameof(CirsGenericReport.IdsAllowedToRead), cirsReport.IdsAllowedToRead }
        };
        var builder = Builders<BsonDocument>.Filter;
        var updateFilters = builder.Eq("_id", cirsReportId);

        _ = await _changeLogService.UpdateChange(nameof(CirsGenericReport), updateFilters, updates);
        // await _cockpitSummaryCommandService.CreateSummary(cirsReportId, nameof(CirsGenericReport), true);
    }
    private async Task<PraxisOpenItem>GetPraxisOpenItem(string openItemId)
    {
        return await _repository.GetItemAsync<PraxisOpenItem>(oa => oa.ItemId.Equals(openItemId) && oa.IsMarkedToDelete == false);
    }
    private async Task<CirsGenericReport> GetCirsGenericReport(string cirsReportId)
    {
        return await _repository.GetItemAsync<CirsGenericReport>(cr => cr.ItemId.Equals(cirsReportId) && cr.IsMarkedToDelete == false);
    }
}