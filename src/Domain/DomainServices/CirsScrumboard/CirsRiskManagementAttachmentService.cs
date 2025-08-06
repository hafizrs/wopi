using System;
using System.Collections.Generic;
using System.Linq;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.Risk;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.CirsScrumBoard;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using Newtonsoft.Json;
using Selise.Ecap.Entities.PrimaryEntities.Dms;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.OpenItem;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Entities.CirsModule;
using Selise.Ecap.Entities.PrimaryEntities.SKO;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.CirsScrumboard;

public class CirsRiskManagementAttachmentService : ICirsRiskManagementAttachmentService
{
    private readonly ILogger<CirsRiskManagementAttachmentService> _log;
    private readonly IRepository _repository;
    private readonly IChangeLogService _changeLogService;
    private readonly ISecurityContextProvider _securityContextProvider;
    private readonly ICirsPermissionService _cirsPermissionService;

    public CirsRiskManagementAttachmentService(
        ILogger<CirsRiskManagementAttachmentService> log,
        IRepository repository, 
        IChangeLogService changeLogService, 
        ISecurityContextProvider securityContextProvider,
        ICirsPermissionService cirsPermissionService)
    {
        _log = log;
        _repository = repository;
        _changeLogService = changeLogService;
        _securityContextProvider = securityContextProvider;
        _cirsPermissionService = cirsPermissionService;
    }
    public async Task AddRiskManagementAttachment(List<ReportingInfo> reportingInfos, PraxisRisk risk)
    {
        foreach (var report in reportingInfos)
        {
            await AttachRisk(report?.ReportingId, risk);
        }
    }

    private async Task AttachRisk(string reportId, PraxisRisk risk)
    {
        try
        {
            var model = PrepareRiskAttachmentUpdates(reportId, risk);
            var filter = Builders<BsonDocument>.Filter.Eq("_id", reportId);
            await _changeLogService.UpdateChange(nameof(CirsGenericReport), filter, model);
        }
        catch (Exception e)
        {
            _log.LogError("Exception in Method: {MethodName}. Error Message: {Message}. Error Details: {StackTrace}", 
                nameof(AttachRisk), e.Message, e.StackTrace);
        }
    }

    private CirsGenericReport GetReport(string reportId)
    {
        return reportId == null 
            ? null 
            : _repository.GetItem<CirsGenericReport>(r => r.ItemId == reportId && !r.IsMarkedToDelete);
    }

    private Dictionary<string, object?> PrepareRiskAttachmentUpdates(string reportId, PraxisRisk risk)
    {
        try
        {
            var report = GetReport(reportId);
            if (report == null) return new Dictionary<string, object>();

            report.RiskManagementAttachments ??= new List<RiskManagementAttachment>();

            FilterOutExistingRiskAttachment(report, risk.ItemId);

            report.RiskManagementAttachments.Add(new RiskManagementAttachment
            {
                RiskItemId = risk.ItemId,
                RiskName = risk.Reference,
                IsResolved = risk.IsResolved,
                RiskOwners = GetInvolvedUsersByPraxisUsers(risk.RiskOwners?.ToList()),
                RiskProfessionals = GetInvolvedUsersByPraxisUsers(risk.RiskProfessionals?.ToList())
            });

            _cirsPermissionService.SetCirsReportPermission(report);

            var securityContext = _securityContextProvider.GetSecurityContext().UserId;
            var currentDate = DateTime.UtcNow.ToLocalTime();

            var updates = new Dictionary<string, object?>
            {
                { nameof(CirsGenericReport.RiskManagementAttachments), report.RiskManagementAttachments },
                { nameof(CirsGenericReport.LastUpdateDate), currentDate },
                { nameof(CirsGenericReport.LastUpdatedBy), securityContext },
                { nameof(CirsGenericReport.IdsAllowedToRead), report.IdsAllowedToRead }
            };
            
            return updates;
        }
        catch (Exception e)
        {
            _log.LogError("Exception in Method: {MethodName}. Error Message: {Message}. Error Details: {StackTrace}",
                nameof(PrepareRiskAttachmentUpdates), e.Message, e.StackTrace);
            return new Dictionary<string, object?>();
        }
    }

    private List<InvolvedUser> GetInvolvedUsersByPraxisUsers(List<string> praxisUserIds)
    {
        if (praxisUserIds == null || praxisUserIds.Count == 0)
        {
            return new List<InvolvedUser>();
        }
        var users = _repository
            .GetItems<PraxisUser>(u => praxisUserIds.Contains(u.ItemId))?
            .Select(u => new InvolvedUser
            {
                DisplayName = u.DisplayName,
                Email = u.Email,
                PraxisUserId = u.ItemId,
                UserId = u.UserId
            })
            .ToList() ?? new List<InvolvedUser>();
        return users;
    }
    private void FilterOutExistingRiskAttachment(CirsGenericReport report, string riskId)
    {
        report.RiskManagementAttachments = report.RiskManagementAttachments?
            .Where(r => r.RiskItemId != riskId)
            .ToList();
    }
}