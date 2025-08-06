using MongoDB.Driver;
using Newtonsoft.Json;
using Selise.Ecap.Entities.PrimaryEntities.PlatformDataService;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor;
using Selise.Ecap.SC.PraxisMonitor.Contracts;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.CirsReports.Create;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants.CirsReport;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.CirsScrumBoard;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.CockpitModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.UserServices;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Entities.CirsModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Entities.EquipmentModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Notifier;
using Selise.Ecap.SC.PraxisMonitor.Domain.Builders.Contracts;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.CirsScrumboard;

#nullable enable
public class CirsReportCreateService : ICirsReportCreateService
{
    private readonly ISecurityContextProvider _securityContextProvider;
    private readonly IRepository _repository;
    private readonly ISequenceNumberService _sequenceNumberService;
    private readonly ICirsPermissionService _cirsPermissionService;
    private readonly IExternalUserCreateService _externalUserCreateService;
    private readonly IEmailDataBuilder _emailDataBuilder;
    private readonly IEmailNotifierService _emailNotifierService;
    private readonly IGenericEventPublishService _genericEventPublishService;
    private readonly ICockpitSummaryCommandService _cockpitSummaryCommandService;
    public CirsReportCreateService(
        ISecurityContextProvider securityContextProvider,
        IRepository repository,
        ISequenceNumberService sequenceNumberService,
        ICirsPermissionService cirsPermissionService,
        IBlocksMongoDbDataContextProvider dbDataContextProvider,
        IExternalUserCreateService externalUserCreateService,
        IEmailDataBuilder emailDataBuilder,
        IEmailNotifierService emailNotifierService,
        IGenericEventPublishService genericEventPublishService,
        ICockpitSummaryCommandService cockpitSummaryCommandService
    )
    {
        _repository = repository;
        _securityContextProvider = securityContextProvider;
        _sequenceNumberService = sequenceNumberService;
        _cirsPermissionService = cirsPermissionService;
        _externalUserCreateService = externalUserCreateService;
        _emailDataBuilder = emailDataBuilder;
        _emailNotifierService = emailNotifierService;
        _genericEventPublishService = genericEventPublishService;
        _cockpitSummaryCommandService = cockpitSummaryCommandService;
    }

    public async Task InitiateReportCreationAsync(AbstractCreateCirsReportCommand command)
    {
        var report = await PrepareCirsCreateReportAsync(command);

        switch (command)
        {
            case CreateComplainReportCommand complainCommand:
                PrepareCirsReport(report, complainCommand);
                break;
            case CreateIdeaReportCommand ideaCommand:
                PrepareCirsReport(report, ideaCommand);
                break;
            case CreateAnotherMessageCommand anotherMessageCommand:
                PrepareCirsReport(report, anotherMessageCommand);
                break;
            case CreateIncidentReportCommand incidentCommand:
                PrepareCirsReport(report, incidentCommand);
                break;
            case CreateHintReportCommand hintCommand:
                PrepareCirsReport(report, hintCommand);
                break;
            case CreateFaultReportCommand faultCommand:
                PrepareCirsReport(report, faultCommand);
                break;
        }

        var praxisClientId = command.AffectedInvolvedParties.First().PraxisClientId;
        var client = _repository.GetItem<PraxisClient>(c => c.ItemId == praxisClientId);

        if (report.ExternalReporters?.Count > 0)
        {
            await _externalUserCreateService
                .ProcessDataForCirsReport(
                    report,
                    client,
                    EntityName.CirsGenericReport
                );
        }
        report.Rank = GetNextRankValue(praxisClientId, report.CirsDashboardName, report.Status);
        var permission = await _cirsPermissionService.GetCirsDashboardPermissionAsync(
                            praxisClientId,
                            report.CirsDashboardName, true);
        _cirsPermissionService.SetCirsReportPermission(report, permission);
        
        report.RolesDisallowedToRead = _cirsPermissionService.PrepareRolesDisallowedToRead(report.CirsDashboardName, command.ReportingVisibility, permission);
        
        var saveTask = _repository.SaveAsync(report);

        await saveTask;
        await ProcessEmailForResponsibleUsers(report, client?.ClientName, permission);
        await _cockpitSummaryCommandService.CreateSummary(report.ItemId, nameof(CockpitTypeNameEnum.CirsGenericReport));
        _genericEventPublishService.PublishDmsArtifactUsageReferenceEvent(report);
    }

    private ulong GetNextRankValue(string clientId, CirsDashboardName cirsDashboardName, string status)
    {
        ulong lastRank = _repository.GetItems<CirsGenericReport>
                (c => c.AffectedInvolvedParties != null && c.AffectedInvolvedParties.Any(a => a.PraxisClientId == clientId) && 
                c.CirsDashboardName == cirsDashboardName && c.Status == status)?.OrderByDescending(c => c.Rank)?.FirstOrDefault()?.Rank ?? 0;
        return lastRank + 1;
    }

    private async Task<CirsGenericReport> PrepareCirsCreateReportAsync(AbstractCreateCirsReportCommand command)
    {
        var securityContext = _securityContextProvider.GetSecurityContext();
        var currentTime = DateTime.UtcNow.ToLocalTime();
        var firstTag = command.Tags.First();
        var status = GenerateReportStatus(command.CirsDashboardName, firstTag);

        var praxisClientId = command.AffectedInvolvedParties.FirstOrDefault()?.PraxisClientId;
        if (string.IsNullOrWhiteSpace(praxisClientId)) throw new InvalidOperationException("AffectedInvolvedParties should have valid praxisClientId.");

        return new CirsGenericReport
        {
            ItemId = command.CirsReportId ?? Guid.NewGuid().ToString(),
            Language = securityContext.Language,
            CreateDate = currentTime,
            CreatedBy = securityContext.UserId,
            LastUpdateDate = currentTime,
            LastUpdatedBy = securityContext.UserId,
            Tags = command.Tags.ToArray(),
            TenantId = securityContext.TenantId,
            IsMarkedToDelete = false,
            RolesAllowedToRead = new string[] {},
            IdsAllowedToRead = new string[] { },
            RolesAllowedToUpdate = new string[] {},
            IdsAllowedToUpdate = new string[] {},
            RolesAllowedToDelete = GetRolesAllowedToDelete(),
            OrganizationId = command.OrganizationId,
            SequenceNumber = await GenerateCirsSequenceNumber(command.OrganizationId),
            Title = command.Title,
            Status = status,
            StatusChangeLog = GenerateCirsStatusChangeLog(status, currentTime, securityContext.UserId),
            KeyWords = command.KeyWords,
            Description = command.Description,
            AttachmentIds = command.AttachmentIds?.Distinct(),
            Remarks = command.Remarks,
            CirsDashboardName = command.CirsDashboardName,
            IsActive = true,
            ClientId = command.ClientId,
            AffectedInvolvedParties = command.AffectedInvolvedParties,
            AttachedDocuments = command.AttachedDocuments ?? new List<ReportingAttachmentFile>(),
            AttachedForm = command.AttachedForm,
            CirsEditHistory = command.CirsEditHistory 
        };
    }

    public async Task DuplicateCirsReport(CirsGenericReport cirsReport, Dictionary<string, object> cirsReportUpdates)
    {
        var securityContext = _securityContextProvider.GetSecurityContext();

        var clonedReport = JsonConvert.DeserializeObject<CirsGenericReport>(JsonConvert.SerializeObject(cirsReport));
        foreach (var (key, value) in cirsReportUpdates)
        {
            var property = clonedReport.GetType().GetProperty(key);
            if (property != null)
            {
                property.SetValue(clonedReport, value);
            }
        }
        clonedReport.ItemId = Guid.NewGuid().ToString();
        cirsReport.CreateDate = DateTime.UtcNow;
        cirsReport.CreatedBy = securityContext.UserId;
        cirsReport.LastUpdateDate = DateTime.UtcNow;
        cirsReport.LastUpdatedBy = securityContext.UserId;
        clonedReport.SequenceNumber = await GenerateCirsSequenceNumber(cirsReport.OrganizationId);
        clonedReport.Tags = new string[] { PraxisTag.IsValidDuplicatedCirsReport };
        clonedReport.Status = GenerateReportStatus(cirsReport.CirsDashboardName, PraxisTag.IsValidDuplicatedCirsReport);
        clonedReport.StatusChangeLog = GenerateCirsStatusChangeLog(clonedReport.Status, DateTime.UtcNow, securityContext.UserId);
        clonedReport.RiskManagementAttachments = new List<RiskManagementAttachment>();
        clonedReport.OpenItemAttachments = new List<OpenItemAttachment>();
        clonedReport.ProcessGuideAttachments = null;

        clonedReport.MetaData[$"{CommonCirsMetaKey.CirsParentId}"] = cirsReport.ItemId;
        var praxisClientId = clonedReport.AffectedInvolvedParties?.FirstOrDefault()?.PraxisClientId ?? string.Empty;
        var client = _repository.GetItem<PraxisClient>(c => c.ItemId == praxisClientId);
        var permission = await _cirsPermissionService.GetCirsDashboardPermissionAsync(
                                    praxisClientId,
                                    clonedReport.CirsDashboardName, true);
        clonedReport.Rank = GetNextRankValue(praxisClientId, clonedReport.CirsDashboardName, clonedReport.Status);
        _cirsPermissionService.SetCirsReportPermission(clonedReport, permission);

        await _repository.SaveAsync(clonedReport);
        await ProcessEmailForResponsibleUsers(clonedReport, client?.ClientName, permission);
        await _cockpitSummaryCommandService.CreateSummary(clonedReport.ItemId, nameof(CockpitTypeNameEnum.CirsGenericReport));
        _genericEventPublishService.PublishDmsArtifactUsageReferenceEvent(clonedReport);
    }

    private void PrepareCirsReport(
        CirsGenericReport report,
        CreateComplainReportCommand command)
    {
        report.OriginatorInfo = command.OriginatorInfo;
        var metaData = new Dictionary<string, object?>
        {
            {$"{CommonCirsMetaKey.ReportingVisibility}", command.ReportingVisibility?.ToString() },
        };

        if (command.MetaData != null)
        {
            foreach (var (key, value) in command.MetaData)
            {
                if (value != null) metaData[key] = value;
            }
        }

        report.MetaData = metaData;
    }

    private static void PrepareCirsReport(
        CirsGenericReport report,
        CreateIdeaReportCommand command)
    {
        var metaData = new Dictionary<string, object?>
        {
            {$"{IdeaMetaKey.BenefitOfIdea}", command.BenefitOfIdea},
            {$"{CommonCirsMetaKey.ReporterClientId}", command.ReporterClientId},
            {$"{IdeaMetaKey.FeasibilityAndResourceRequirements}", command.FeasibilityAndResourceRequirements},
            {$"{IdeaMetaKey.TargetGroup}", command.TargetGroup},
            {$"{IdeaMetaKey.Requirements}", command.Requirements}
        };

        if (command.MetaData != null)
        {
            foreach (var (key, value) in command.MetaData)
            {
                if (value != null) metaData[key] = value;
            }
        }

        report.MetaData = metaData;
    }

    private static void PrepareCirsReport(
        CirsGenericReport report,
        CreateAnotherMessageCommand command)
    {
        var metaData = new Dictionary<string, object?>
        {
            {$"{CommonCirsMetaKey.ReporterClientId}", command.ReporterClientId},
            {$"{CommonCirsMetaKey.ReportingVisibility}", command.ReportingVisibility?.ToString() },
            {$"{AnotherMetaKey.ImplementationProposal}", command.ImplementationProposal?.ToString() }
        };

        if (command.MetaData != null)
        {
            foreach (var (key, value) in command.MetaData)
            {
                if (value != null) metaData[key] = value;
            }
        }

        report.MetaData = metaData;

        if (command.ReportedBy != null)
        {
            report.ReportedBy = command.ReportedBy;
        }
        report.OriginatorInfo = command.OriginatorInfo;
    }

    private void PrepareCirsReport(
        CirsGenericReport report,
        CreateIncidentReportCommand command)
    {
        var metaData = new Dictionary<string, object?>
        {
            {$"{IncidentMetaKey.Topic}", command.Topic},
            {$"{IncidentMetaKey.Measures}", command.Measures},
            {$"{CommonCirsMetaKey.ReportExternalOffice}", command.ReportExternalOffice},
            {$"{CommonCirsMetaKey.ReportInternalOffice}", command.ReportInternalOffice},
            {$"{CommonCirsMetaKey.ReportingVisibility}", command.ReportingVisibility?.ToString()}
        };

        if (command.MetaData != null)
        {
            foreach (var (key, value) in command.MetaData)
            {
                if (value != null) metaData[key] = value;
            }
        }

        report.MetaData = metaData;

        report.ExternalReporters = new List<ExternalReporter>();
        if (command.ExternalReporters?.Count > 0)
        {
            foreach (var externalReporter in command.ExternalReporters)
            {
                report.ExternalReporters.Add
                (
                    new ExternalReporter()
                    {
                        SupplierInfo = externalReporter,
                        Remarks = ""
                    }
                );
            }
        }
    }

    private static void PrepareCirsReport(
        CirsGenericReport report,
        CreateHintReportCommand command)
    {
        report.ExternalReporters = new List<ExternalReporter>();
        if (command.ExternalReporters?.Count > 0)
        {
            foreach (var externalReporter in command.ExternalReporters)
            {
                report.ExternalReporters.Add
                (
                    new ExternalReporter()
                    {
                        SupplierInfo = externalReporter,
                        Remarks = ""
                    }
                );
            }
        }

        if (command.ReportedBy != null)
        {
            report.ReportedBy = command.ReportedBy;
        }

        var metaData = new Dictionary<string, object?>
        {
            {$"{CommonCirsMetaKey.ReporterClientId}", command.ReporterClientId},
            {$"{CommonCirsMetaKey.ReportExternalOffice}", command.ReportExternalOffice},
            {$"{CommonCirsMetaKey.ReportInternalOffice}", command.ReportInternalOffice},
            {$"{HintMetaKey.ReportingDate}", command.ReportingDate}
        };

        if (command.MetaData != null)
        {
            foreach (var (key, value) in command.MetaData)
            {
                if (value != null) metaData[key] = value;
            }
        }

        report.MetaData = metaData;
    }

    private void PrepareCirsReport(
        CirsGenericReport report,
        CreateFaultReportCommand command)
    {
        var metaData = new Dictionary<string, object?>
        {
            {$"{CommonCirsMetaKey.ReportingVisibility}", command.ReportingVisibility?.ToString() },
        };
        if (command.MetaData != null)
        {
            foreach (var (key, value) in command.MetaData)
            {
                if (value != null) metaData[key] = value;
            }
        }

        report.MetaData = metaData;
        
        var clientId = report.AffectedInvolvedParties?.FirstOrDefault()?.PraxisClientId ?? string.Empty;
        var equipmenId = report.MetaData["EquipmentId"]?.ToString() ?? string.Empty;
        report.EquipmentManagers = GetEquipmentManagers(clientId, equipmenId);
    }

    private async Task<string> GenerateCirsSequenceNumber(string praxisClientId)
    {
        var response = await _sequenceNumberService.GenerateNextSequenceNumber($"CIRS_FEATURE_{praxisClientId}");
        return response?.CurrentNumber.ToString() ?? string.Empty;
    }

    private static string GenerateReportStatus(CirsDashboardName dashboardName, string tag)
    {
        var enumValuesList = dashboardName.GetCirsReportStatusEnumValues();

        return (enumValuesList[0]).ToString();
    }

    private static List<StatusChangeEvent> GenerateCirsStatusChangeLog(string status, DateTime changedOn, string changedBy)
    {
        var statusChangeEvent = new List<StatusChangeEvent>()
        {
            new()
            {
                PreviousStatus = null,
                CurrentStatus = status,
                ChangedOn  = changedOn,
                ChangedBy = changedBy
            }
        };

        return statusChangeEvent;
    }

    private static string[] GetRolesAllowedToDelete()
    {
        return new[] { RoleNames.Admin };
    }

    private async Task ProcessEmailForResponsibleUsers(CirsGenericReport report, string clientName, CirsDashboardPermission permission)
    {
        
        var emailTasks = new List<Task<bool>>();
        var purposeByOffice= report.CirsDashboardName == CirsDashboardName.Hint ? EmailTemplateName.HintReported.ToString() : EmailTemplateName.CIRSReported.ToString();
        if (report.CirsDashboardName != CirsDashboardName.Incident && report?.ExternalReporters?.Count > 0)
        {
            foreach (var externalInfo in report.ExternalReporters)
            {
                if (!string.IsNullOrEmpty(externalInfo?.SupplierInfo?.SupplierEmail))
                {
                    var person = new Person()
                    {
                        DisplayName = externalInfo.SupplierInfo.SupplierName,
                        Email = externalInfo.SupplierInfo.SupplierEmail
                    };
                    var emailData = _emailDataBuilder.BuildCirsReportEmailData(report, person, clientName, externalInfo?.SupplierInfo);
                    var emailStatus = _emailNotifierService.SendEmail(
                                                                person.Email,
                                                                purposeByOffice,
                                                                emailData,
                                                                true
                                                            );
                    emailTasks.Add(emailStatus);
                }
            }
        }

        var userIds = new List<string>();
        var responsibleUserIds = new List<string>();
        var internalofficeIds = new List<string>();
        var purpose = string.Empty;

        if (report?.MetaData?.ContainsKey($"{CommonCirsMetaKey.ReportInternalOffice}") == true)
        {
            var internaloffice = report.MetaData[$"{CommonCirsMetaKey.ReportInternalOffice}"];
            if (internaloffice != null && ((bool)internaloffice))
            {
                internalofficeIds.AddRange(permission?.AdminIds?.Select(a => a.UserId)?.ToList() ?? new List<string>());
            }
        }

        if (report?.CirsDashboardName == CirsDashboardName.Hint)
        {
            purpose = EmailTemplateName.HintReceived.ToString();
            if (!string.IsNullOrEmpty(report.ReportedBy?.UserId)) responsibleUserIds.Add(report.ReportedBy.UserId);
            if (!string.IsNullOrEmpty(report.CreatedBy)) responsibleUserIds.Add(report.CreatedBy);
        }
        if (report?.CirsDashboardName == CirsDashboardName.Another)
        {
            purpose = EmailTemplateName.FeedbackReceived.ToString();
            if (!string.IsNullOrEmpty(report.ReportedBy?.UserId)) responsibleUserIds.Add(report.ReportedBy.UserId);
            if (!string.IsNullOrEmpty(report.CreatedBy)) responsibleUserIds.Add(report.CreatedBy);
        }
        userIds.AddRange(responsibleUserIds);
        userIds.AddRange(internalofficeIds);

        if (userIds.Count > 0)
        {
            userIds = userIds.Distinct().ToList();
            var praxisUsers = _repository.GetItems<PraxisUser>(x => !x.IsMarkedToDelete && userIds.Contains(x.UserId)).ToList();
            var responsibleUsers = praxisUsers.Where(pu => responsibleUserIds.Contains(pu.UserId)).ToList();
            foreach (var user in responsibleUsers)
            {
                var person = new Person()
                {
                    DisplayName = user.FirstName,
                    Email = user.Email
                };
                var emailData = _emailDataBuilder.BuildCirsReportEmailData(report, person, clientName);
                var emailStatus = _emailNotifierService.SendEmail(
                                                            person.Email,
                                                            purpose,
                                                            emailData
                                                        );
                emailTasks.Add(emailStatus);
            }
            var internaloffices = praxisUsers.Where(pu => internalofficeIds.Contains(pu.UserId)).ToList();
            foreach (var user in internaloffices)
            {
                var person = new Person()
                {
                    DisplayName = user.FirstName,
                    Email = user.Email
                };
                var emailData = _emailDataBuilder.BuildCirsReportEmailData(report, person, clientName);
                var emailStatus = _emailNotifierService.SendEmail(
                                                            person.Email,
                                                            purposeByOffice,
                                                            emailData
                                                        );
                emailTasks.Add(emailStatus);
            }
        }

        if (emailTasks.Count > 0) await Task.WhenAll(emailTasks);
    }

    private List<string> GetEquipmentManagers(string clientId, string equipmentId)
    {
        var equipmentManagers = _repository.GetItem<PraxisEquipmentRight>(x =>
            x.DepartmentId == clientId && 
            x.EquipmentId == equipmentId && 
            !x.IsOrganizationLevelRight && 
            !x.IsMarkedToDelete);
        return equipmentManagers?.AssignedAdmins?.Select(a => a.UserId).ToList() ?? new List<string>();
    }
}