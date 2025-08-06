using System;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Driver;
using Selise.Ecap.Entities.PrimaryEntities.PlatformDataService;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor;

using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Entities.CirsModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.CirsScrumBoard;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.CirsReports;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants.CirsReport;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.CirsScrumboard.CirsPermissions;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.Client;
using Microsoft.Extensions.Logging;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Queries.CirsReports;
using SeliseBlocks.Genesis.Framework.Events;
using Selise.Ecap.Entities.PrimaryEntities.SLPC;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Entities.EquipmentModule;
using Selise.Ecap.Entities.PrimaryEntities.DWT;
using SeliseBlocks.Entities.PrimaryEntities.EnterpriseCRM;
using System.Security;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models.CirsReport;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.CirsScrumboard;

#nullable enable
public class CirsPermissionService : ICirsPermissionService
{
    private readonly ILogger<CirsPermissionService> _logger;
    private readonly ISecurityContextProvider _securityContextProvider;
    private readonly IRepository _repository;
    private readonly IBlocksMongoDbDataContextProvider _ecapMongoDbDataContextProvider;
    private readonly IServiceClient _serviceClient;
    private readonly IPermissionsFactoryService _permissionsFactoryService;
    private readonly ISecurityHelperService _securityHelperService;

    public CirsPermissionService(
        ILogger<CirsPermissionService> logger,
        ISecurityContextProvider securityContextProvider,
        IRepository repository,
        IBlocksMongoDbDataContextProvider ecapMongoDbDataContextProvider,
        IServiceClient serviceClient,
        IPermissionsFactoryService permissionsFactoryService,
        ISecurityHelperService securityHelperService
    )
    {
        _logger = logger;
        _repository = repository;
        _securityContextProvider = securityContextProvider;
        _ecapMongoDbDataContextProvider = ecapMongoDbDataContextProvider;
        _serviceClient = serviceClient;
        _permissionsFactoryService = permissionsFactoryService;
        _securityHelperService = securityHelperService;
    }

    public async Task InitiateAssignCirsAdminsAsync(AssignCirsAdminsCommand command)
    {
        var dashboardPermission = await GetOrCreateDashboardPermissionAsync(command);
        if (dashboardPermission == null) return;

        var addedPraxisUserIds = command.AddedIds.Where(id => !string.IsNullOrWhiteSpace(id)).Distinct().ToArray();
        var removedPraxisUserIds = command.RemovedIds.Where(id => !string.IsNullOrWhiteSpace(id)).Distinct().ToArray();

        var addedPraxisUsers = GetPraxisUsers(addedPraxisUserIds);
        var removedPraxisUsers = GetPraxisUsers(removedPraxisUserIds);

        var userEmails = addedPraxisUsers.Select(x => x.Email).ToArray();

        var isCirsAdminsAssigned = await UpdateCirsDashboardPermission(
            dashboardPermission.ItemId,
            dashboardPermission.AdminIds,
            removedPraxisUserIds,
            addedPraxisUsers);

        if (isCirsAdminsAssigned)
        {
            PublishCirsAdminAssignedEvent(dashboardPermission.ItemId);
        }
    }

    public async Task<CirsDashboardPermission?> GetCirsDashboardPermissionAsync(
        string praxisClientId,
        CirsDashboardName dashboardName,
        bool returnDefaultValue = false)
    {
        if (string.IsNullOrEmpty(praxisClientId)) return null;

        var permission = await _repository.GetItemAsync<CirsDashboardPermission>(i =>
            !i.IsMarkedToDelete &&
            i.PraxisClientId == praxisClientId &&
            i.CirsDashboardName == dashboardName) ?? null;

        if (permission == null && returnDefaultValue)
        {
            var client = await _repository.GetItemAsync<PraxisClient>(c => c.ItemId == praxisClientId && !c.IsMarkedToDelete);
            permission = new CirsDashboardPermission()
            {
                OrganizationId = client?.ParentOrganizationId,
                PraxisClientId = praxisClientId,
                CirsDashboardName = dashboardName,
                AdminIds = new List<PraxisIdDto>(),
                AssignmentLevel = GetAssignmentLevelByDashboardName(dashboardName, client?.CirsReportConfig) ?? AssignmentLevel.None
            };
        }

        return permission;
    }

    public AssignmentLevel? GetAssignmentLevelByDashboardName(CirsDashboardName dashboardName, CirsReportConfigModel? cirsReportConfig)
    {
        var levelString = dashboardName switch
        {
            CirsDashboardName.Incident => cirsReportConfig?.Cirs,
            CirsDashboardName.Complain => cirsReportConfig?.ComplainManagement,
            CirsDashboardName.Idea => cirsReportConfig?.IdeaManagement,
            CirsDashboardName.Hint => cirsReportConfig?.HintManagement,
            CirsDashboardName.Another => cirsReportConfig?.AnotherMessage,
            CirsDashboardName.Fault => cirsReportConfig?.FaultManagement,
            _ => null,
        };

        if (int.TryParse(levelString, out int levelInt))
        {
            if (Enum.IsDefined(typeof(AssignmentLevel), levelInt))
            {
                return (AssignmentLevel)levelInt;
            }
        }
        return null;
    }

    public async Task<CirsDashboardPermission?> GetCirsDashboardPermissionAsync(CirsGenericReport cirsGenericReport)
    {
        var praxisClientId = cirsGenericReport.AffectedInvolvedParties?.FirstOrDefault()?.PraxisClientId;
        if (string.IsNullOrWhiteSpace(praxisClientId))
        {
            return null;
        }

        return await GetCirsDashboardPermissionAsync(
            praxisClientId,
            cirsGenericReport.CirsDashboardName);
    }

    public async Task<bool> IsACirsAdminAsync(string praxisClientId, CirsDashboardName dashboardName, PraxisClient? praxisClient = null, CirsDashboardPermission? dashboardPermission = null)
    {
        praxisClient ??= (await GetPraxisClientsAsync(praxisClientId)).FirstOrDefault();
        if (praxisClient == null)
        {
            throw new InvalidOperationException($"PraxisClient with ID: {praxisClientId} not found.");
        }

        var securityContext = _securityContextProvider.GetSecurityContext();
        dashboardPermission ??= await GetCirsDashboardPermissionAsync(praxisClientId, dashboardName);
        var cirsAppointedAdminIds = dashboardPermission?.AdminIds?.Select(ad => ad.UserId).ToList() ?? new List<string>();
        var isACirsAdmin = IsAAdminOrTaskController() ||
                           cirsAppointedAdminIds.Contains(securityContext.UserId);
        var isEquipmentOfficer = dashboardName == CirsDashboardName.Fault &&
                                 await IsEquipmentOfficer(praxisClientId, dashboardPermission?.OrganizationId ?? string.Empty, securityContext.UserId);
        return isACirsAdmin || isEquipmentOfficer;
    }

    private async Task<bool> IsEquipmentOfficer(string clientId, string organizationId, string userId)
    {
        var equipmentRight = await _repository.GetItemAsync<PraxisEquipmentRight>(er =>
            !er.IsMarkedToDelete &&
            er.IsOrganizationLevelRight &&
            er.DepartmentId == clientId);
        return equipmentRight?.AssignedAdmins?.Any(f => f.UserId == userId) == true;
    }

    public Dictionary<string, bool> GetPermissionsByDashBoardName(CirsDashboardName dashboardName, CirsDashboardPermission dashboardPermissions)
    {
        var permissionService = _permissionsFactoryService.GetPermissionsService(dashboardName);
        return permissionService != null ? 
            permissionService.GetPermissions(dashboardPermissions) : new Dictionary<string, bool>();
    }

    public bool IsAAdminOrTaskController()
    {
        var securityContext = _securityContextProvider.GetSecurityContext();
        var adminAndTaskControllerRoles = new string[] { RoleNames.Admin, RoleNames.TaskController };
        var isAAdminOrTaskController = securityContext.Roles.Any(role => adminAndTaskControllerRoles.Contains(role));
        return isAAdminOrTaskController;
    }

    public void SetCirsReportPermission(CirsGenericReport report, CirsDashboardPermission? permission = null)
    {
        if (report == null) return;

        var clientId = report?.ClientId ?? string.Empty;
        if (report?.AffectedInvolvedParties != null && report.AffectedInvolvedParties.Count() > 0)
        {
            clientId = report.AffectedInvolvedParties.ElementAt(0).PraxisClientId;
        }

        if (permission == null) permission = GetCirsDashboardPermissionAsync(
                            clientId,
                            report.CirsDashboardName).GetAwaiter().GetResult();
        report.RolesAllowedToRead = GetRolesAllowedToRead(report, clientId);
        report.IdsAllowedToRead = GetIdsAllowedToRead(report, permission);
    }

    public async Task<bool> HaveAllUnitViewPermissions(string userId, CirsDashboardName? dashboardName)
    {
        if (string.IsNullOrEmpty(userId) || !_securityHelperService.IsADepartmentLevelUser() || dashboardName == null) return false;
        var deptId = _securityHelperService.ExtractDepartmentIdFromDepartmentLevelUser();

        return await _repository.ExistsAsync<CirsDashboardPermission>(
            x => x.AssignmentLevel == AssignmentLevel.Organizational &&
                x.CirsDashboardName != CirsDashboardName.Hint &&
                x.CirsDashboardName == dashboardName &&
                x.PraxisClientId == deptId &&
                x.AdminIds.Any(y => y.UserId.Equals(userId))
        );
    }

    public bool checkDirectVisibilityPermission(Dictionary<string, bool> loggedInUserPermission, bool isActive, CirsDashboardName? dashboardName)
    {
        if (dashboardName == null) return false;

        string key = isActive ? CirsPermissionValue.CanSeeActiveCards : CirsPermissionValue.CanSeeInactiveCards;
        bool havePermission = loggedInUserPermission?.ContainsKey(key) == true ? loggedInUserPermission[key] : false;
        if (havePermission) return true;

        return !(dashboardName == CirsDashboardName.Hint || dashboardName == CirsDashboardName.Idea);
    }

    private string[] GetRolesAllowedToRead(CirsGenericReport report, string clientId)
    {
        var roles = new List<string>() { RoleNames.Admin, RoleNames.TaskController };
        
        try
        {
            if (!string.IsNullOrEmpty(clientId))
            {
                var permissionService = _permissionsFactoryService.GetPermissionsService(report.CirsDashboardName);
                var clientRoles = permissionService.GetRolesAllowedToRead(clientId);

                if (report?.MetaData != null && report.MetaData.ContainsKey($"{CommonCirsMetaKey.ReportingVisibility}"))
                {
                    var visibility = report.MetaData[$"{CommonCirsMetaKey.ReportingVisibility}"]?.ToString();
                    if (visibility == ReportingVisibility.Management.ToString())
                    {
                        clientRoles = new List<string>()
                        {
                            ($"{RoleNames.PowerUser_Dynamic}_{clientId}"),
                            ($"{RoleNames.Leitung_Dynamic}_{clientId}")
                        };
                    }
                    else if (visibility == ReportingVisibility.Officer.ToString())
                    {
                        clientRoles = new List<string>();
                    }
                }
                roles.AddRange(clientRoles);

                var todoGroup = report?.OpenItemAttachments?
                        .Where(a => a.AssignedGroup != null)?
                        .SelectMany(a => a.AssignedGroup)?
                        .Where(i => !string.IsNullOrEmpty(i))?
                        .ToList() ?? new List<string>();

                roles.AddRange(todoGroup);

                var pgGroup = report?.ProcessGuideAttachments?
                        .Where(a => a.AssignedGroup != null)?
                        .SelectMany(a => a.AssignedGroup)?
                        .Where(i => !string.IsNullOrEmpty(i))?
                        .ToList() ?? new List<string>();

                roles.AddRange(pgGroup);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"Exception occured in GetRolesAllowedToRead -> Message: {ex.Message}");
        }
        return roles.Distinct().ToArray();
    }

    private string[] GetIdsAllowedToRead(CirsGenericReport report, CirsDashboardPermission permission)
    {
        var ids = new List<string>();
        if (!string.IsNullOrEmpty(report?.CreatedBy))
        {
            ids.Add(report.CreatedBy);
        }
        if (permission?.AdminIds?.Count() > 0)
        {
            ids.AddRange(permission.AdminIds.Select(a => a.UserId).ToList());
        }

        var involvedParties = report?.AffectedInvolvedParties?
                    .Where(a => a.InvolvedUsers != null)?
                    .SelectMany(a => a.InvolvedUsers)?
                    .Where(i => !string.IsNullOrEmpty(i.UserId))?
                    .Select(i => i.UserId)?
                    .ToList() ?? new List<string>();

        ids.AddRange(involvedParties);

        var responsibleUsers = report?.ResponsibleUsers?
                    .Where(i => !string.IsNullOrEmpty(i.UserId))?
                    .Select(i => i.UserId)?
                    .ToList() ?? new List<string>();

        ids.AddRange(responsibleUsers);

        var todoUsers = report?.OpenItemAttachments?
                    .Where(a => a.AssignedUsers != null)?
                    .SelectMany(a => a.AssignedUsers)?
                    .Where(i => !string.IsNullOrEmpty(i.UserId))?
                    .Select(i => i.UserId)?
                    .ToList() ?? new List<string>();

        ids.AddRange(todoUsers);

        var pgUsers = report?.ProcessGuideAttachments?
                    .Where(a => a.AssignedUsers != null)?
                    .SelectMany(a => a.AssignedUsers)?
                    .Where(i => !string.IsNullOrEmpty(i.UserId))?
                    .Select(i => i.UserId)?
                    .ToList() ?? new List<string>();

        ids.AddRange(pgUsers);

        var riskOwners = report?.RiskManagementAttachments?
                    .Where(a => a.RiskOwners != null)?
                    .SelectMany(a => a.RiskOwners)?
                    .Where(i => !string.IsNullOrEmpty(i.UserId))?
                    .Select(i => i.UserId)?
                    .Distinct()
                    .ToList() ?? new List<string>();
        ids.AddRange(riskOwners);

        var riskProfessionals = report?.RiskManagementAttachments?
                    .Where(a => a.RiskProfessionals != null)?
                    .SelectMany(a => a.RiskProfessionals)?
                    .Where(i => !string.IsNullOrEmpty(i.UserId))?
                    .Select(i => i.UserId)?
                    .Distinct()
                    .ToList() ?? new List<string>();
        ids.AddRange(riskProfessionals);

        var equipmentId = report?.MetaData?.ContainsKey("EquipmentId") == true ? report?.MetaData["EquipmentId"] as string : null;

        if (report?.EquipmentManagers.Count > 0)
        {
            ids.AddRange(report.EquipmentManagers);
        }


        return ids.Distinct().ToArray();
    }

    private async Task<List<PraxisClient>> GetPraxisClientsAsync(params string[] praxisClientIds)
    {
        var collection = _ecapMongoDbDataContextProvider.GetTenantDataContext().GetCollection<PraxisClient>("PraxisClients");
        var filter = Builders<PraxisClient>.Filter.In(client => client.ItemId, praxisClientIds);
        var projection = Builders<PraxisClient>.Projection
            .Include(client => client.ItemId)
            .Include(client => client.ParentOrganizationId)
            .Include(client => client.ClientName);

        return await collection.Find(filter).Project<PraxisClient>(projection).ToListAsync();
    }

    private void PublishCirsAdminAssignedEvent(string dashboardPermissionId)
    {
        var cirsAdminAssignedEvent = new GenericEvent
        {
            EventType = PraxisEventType.CirsAdminAssignedEvent,
            JsonPayload = JsonConvert.SerializeObject(dashboardPermissionId)
        };

        _serviceClient.SendToQueue<GenericEvent>(PraxisConstants.GetPraxisQueueName(), cirsAdminAssignedEvent);
    }

    private async Task<CirsDashboardPermission?> GetOrCreateDashboardPermissionAsync(AssignCirsAdminsCommand command)
    {
        return (await GetCirsDashboardPermissionAsync(command.PraxisClientId, command.DashboardNameEnum)
            ) ?? await SaveNewDashboardPermissionAsync(command);
    }

    private static List<string> GetCirsDefaultAdmins(string organizationId)
    {
        var roles = new List<string> { RoleNames.Admin, RoleNames.TaskController };
        if (!string.IsNullOrWhiteSpace(organizationId))
        {
            roles.Add($"{RoleNames.AdminB_Dynamic}_{organizationId}");
        }
        return roles;
    }

    private async Task<CirsDashboardPermission?> SaveNewDashboardPermissionAsync(AssignCirsAdminsCommand command)
    {
        var client = await _repository.GetItemAsync<PraxisClient>(i =>
                        !i.IsMarkedToDelete &&
                        i.ItemId == command.PraxisClientId);
        if (client == null) return null;

        var securityContext = _securityContextProvider.GetSecurityContext();
        var currentTime = DateTime.UtcNow.ToLocalTime();

        var cirsDashboardPermission = new CirsDashboardPermission
        {
            ItemId = Guid.NewGuid().ToString(),
            PraxisClientId = client.ItemId,
            Language = securityContext.Language,
            CreateDate = currentTime,
            CreatedBy = securityContext.UserId,
            LastUpdateDate = currentTime,
            LastUpdatedBy = securityContext.UserId,
            Tags = client.Tags,
            TenantId = securityContext.TenantId,
            IsMarkedToDelete = false,
            RolesAllowedToRead = client.RolesAllowedToRead,
            IdsAllowedToRead = client.IdsAllowedToRead,
            RolesAllowedToUpdate = client.RolesAllowedToUpdate,
            IdsAllowedToUpdate = client.IdsAllowedToUpdate,
            RolesAllowedToDelete = client.RolesAllowedToDelete,
            OrganizationId = client.ParentOrganizationId,
            CirsDashboardName = command.DashboardNameEnum,
            AssignmentLevel = client.CirsReportConfig.GetAssignmentLevel(command.DashboardNameEnum),
            AdminIds = new List<PraxisIdDto>(),
        };

        await _repository.SaveAsync(cirsDashboardPermission);
        return cirsDashboardPermission;
    }

    private List<PraxisUser> GetPraxisUsers(string[] ids)
    {
        var securityContext = _securityContextProvider.GetSecurityContext();

        return
            _repository
            .GetItems<PraxisUser>(i =>
            ids.Contains(i.ItemId) &&
            !i.IsMarkedToDelete)
            .Select(pu => new PraxisUser
            {
                ItemId = pu.ItemId,
                UserId = pu.UserId,
                Email = pu.Email
            })
            .ToList();
    }

    private List<Person> GetPersonsForPraxisUsers(string[] emails)
    {
        var securityContext = _securityContextProvider.GetSecurityContext();

        var persons = _repository
            .GetItems<Person>(i =>
            emails.Contains(i.Email) &&
            !i.IsMarkedToDelete)
            .Select(p => new Person
            {
                ItemId = p.ItemId,
                Email = p.Email
            })
            .ToList();

        return persons;
    }

    private async Task<bool> UpdateCirsDashboardPermission(
        string dashboardPermissionId,
        IEnumerable<PraxisIdDto> currentAdmins,
        string[] removedAdminIds,
        List<PraxisUser> praxisUsers)
    {
        var securityContext = _securityContextProvider.GetSecurityContext();

        var collection = _ecapMongoDbDataContextProvider.GetTenantDataContext()
            .GetCollection<CirsDashboardPermission>("CirsDashboardPermissions");

        var builder = Builders<CirsDashboardPermission>.Filter;
        var filter = builder.Eq(po => po.ItemId, dashboardPermissionId);

        var updates = Builders<CirsDashboardPermission>.Update
            .Set(po => po.AdminIds, PrepareCirsAdmins(currentAdmins, removedAdminIds, praxisUsers))
            .Set(po => po.LastUpdateDate, DateTime.UtcNow.ToLocalTime())
            .Set(po => po.LastUpdatedBy, securityContext.UserId);

        await collection.UpdateOneAsync(filter, updates);

        return true;
    }

    private IEnumerable<PraxisIdDto> PrepareCirsAdmins(
        IEnumerable<PraxisIdDto>? currentAdmins,
        string[] removedAdminIds,
        List<PraxisUser> praxisUsers)
    {
        currentAdmins ??= new List<PraxisIdDto>();
        var admins = currentAdmins.Where(a => !removedAdminIds.Contains(a.PraxisUserId))
            .Concat(PrepareNewCirsAdminIds(praxisUsers));
        return admins.DistinctBy(u => u.PraxisUserId).ToList();
    }

    private static List<PraxisIdDto> PrepareNewCirsAdminIds(List<PraxisUser> praxisUsers)
    {
        var cirsAdminIds = new List<PraxisIdDto>();
        praxisUsers.ForEach(pu =>
        {
            var cirsAdminInfo = new PraxisIdDto
            {
                UserId = pu.UserId,
                PraxisUserId = pu.ItemId
            };
            cirsAdminIds.Add(cirsAdminInfo);
        });

        return cirsAdminIds;
    }

    public Task<FilterDefinition<CirsGenericReport>> GetPermissionFilter(GetPermissionFilterModel model)
    {
        var securityContext = _securityContextProvider.GetSecurityContext();
        var clientId = model.ClientId;
        var clientOrganizationId = model.ClientOrganizationId;
        var isActive = model.IsActive;
        var cirsReportId = model.CirsReportId;
        var dashboardName = model.DashboardName;
        var loggedInUserPermission = model.LoggedInUserPermission;
        var praxisClient = model.PraxisClient;
        var haveOfficerPermission = model.HaveOfficerPermission;
        var dashboardPermission = model.DashboardPermission;
        var isACirsAdmin = model.IsACirsAdmin;

        var builder = Builders<CirsGenericReport>.Filter;
        var filter = builder.Empty;
        
        if (securityContext.Roles.Contains(RoleNames.ExternalUser))
        {
            filter &= builder.Where(r => r.ExternalReporters != null && r.ExternalReporters.Any(e => e.SupplierInfo.ExternalUserId == securityContext.UserId));
        }
        else if (!(dashboardPermission?.AssignmentLevel == AssignmentLevel.Organizational && haveOfficerPermission && dashboardPermission?.CirsDashboardName != CirsDashboardName.Hint))
        {
            if (!isACirsAdmin || (_securityHelperService.IsAAdmin() && dashboardPermission?.CirsDashboardName == CirsDashboardName.Hint))
            {
                var orPermissions = builder.Where(r =>
                    (r.RolesAllowedToRead != null && r.RolesAllowedToRead.Any(e => securityContext.Roles.Contains(e)))
                ||
                (r.IdsAllowedToRead != null && r.IdsAllowedToRead.Contains(securityContext.UserId))
                );
                var haveDirectVisibilityPermission = checkDirectVisibilityPermission(loggedInUserPermission, isActive, dashboardPermission?.CirsDashboardName);
                if (!haveDirectVisibilityPermission)
                {
                    orPermissions = builder.Where(r =>
                        (r.IdsAllowedToRead != null && r.IdsAllowedToRead.Contains(securityContext.UserId))
                    );
                }
                filter &= orPermissions;
                filter &= builder.Or(
                    builder.Eq(r => r.RolesDisallowedToRead, null),
                    builder.Size(r => r.RolesDisallowedToRead, 0),
                    builder.Nin(nameof(CirsGenericReport.RolesDisallowedToRead), securityContext.Roles));
            }
        }


        if (!string.IsNullOrEmpty(cirsReportId))
        {
            filter &= builder.Eq(r => r.ItemId, cirsReportId);
        }

        if (!_securityHelperService.IsAAdminOrTaskConrtroller())
        {
            filter &= builder.Eq(r => r.OrganizationId, clientOrganizationId);
        }

        if (loggedInUserPermission?.ContainsKey(CirsPermissionValue.HideToBeApprovedColumn) == true && loggedInUserPermission[CirsPermissionValue.HideToBeApprovedColumn])
        {
            switch (dashboardPermission?.CirsDashboardName)
            {
                case CirsDashboardName.Incident:
                    filter &= builder.Not(builder.Eq(r => r.Status, $"{CirsIncidentStatusEnum.TO_BE_APPROVED}"));
                    break;
            }
        }
        return Task.FromResult(filter);
    }

    public List<string> PrepareRolesDisallowedToRead(CirsDashboardName dashboardName, ReportingVisibility? reportingVisibility, CirsDashboardPermission? dashboardPermission)
    {
        if (dashboardPermission == null || dashboardPermission.AssignmentLevel != AssignmentLevel.UnitWithoutInsight)
        {
            return new List<string>();
        }

        if (reportingVisibility == null || reportingVisibility == ReportingVisibility.All) return new List<string>();
        
        return dashboardName switch
        {
            CirsDashboardName.Complain => new List<string> { RoleNames.AdminB, RoleNames.GroupAdmin },
            CirsDashboardName.Incident => new List<string> { RoleNames.AdminB, RoleNames.GroupAdmin},
            CirsDashboardName.Hint => new List<string>(),
            CirsDashboardName.Another => new List<string>(),
            CirsDashboardName.Idea => new List<string>(),
            CirsDashboardName.Fault => new List<string> { RoleNames.GroupAdmin, RoleNames.AdminB },
            _ => new List<string>()
        };
    }
}