using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Driver;
using Microsoft.Extensions.Logging;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.Entities.PrimaryEntities.Dms;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.LibraryModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.ResponseModels.LibraryModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Queries.LibraryModule;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants;
using System.Linq.Expressions;
using System;
using MongoDB.Bson.Serialization;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Entities.LibraryModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Entities.CockpitModule;

namespace Selise.Ecap.SC.PraxisMonitor.Domain
{
    public class LibraryDocumentAssigneeService : ILibraryDocumentAssigneeService
    {
        private readonly ISecurityContextProvider _securityContextProvider;
        private readonly ISecurityHelperService _securityHelperService;
        private readonly IRepository _repository;
        private readonly IObjectArtifactUtilityService _objectArtifactUtilityService;
        private readonly IBlocksMongoDbDataContextProvider _ecapMongoDbDataContextProvider;
        private readonly IRiqsPediaViewControlService _riqsPediaViewControlService;

        public LibraryDocumentAssigneeService(
            ISecurityContextProvider securityContextProvider,
            ISecurityHelperService securityHelperService,
            IRepository repository,
            IObjectArtifactUtilityService objectArtifactUtilityService,
            IBlocksMongoDbDataContextProvider ecapMongoDbDataContextProvider,
            IRiqsPediaViewControlService riqsPediaViewControlService
            )
        {
            _securityContextProvider = securityContextProvider;
            _securityHelperService = securityHelperService;
            _repository = repository;
            _objectArtifactUtilityService = objectArtifactUtilityService;
            _ecapMongoDbDataContextProvider = ecapMongoDbDataContextProvider;
            _riqsPediaViewControlService = riqsPediaViewControlService;
        }

        public async Task<List<AssignedDepartment>> GetPurposeWiseLibraryAssignees(LibraryDocumentAssigneeQuery query)
        {
            var result = new List<AssignedDepartment>();
            var objectArtifact = _objectArtifactUtilityService.GetObjectArtifactSecuredById(query.ObjectArtifactId);

            if (objectArtifact != null)
            {
                result = query.Purpose switch
                {
                    LibraryAssignedMemberType.ASSIGNED_TO => GetObjectArtifactAssigneeDetailResponse(objectArtifact),
                    LibraryAssignedMemberType.FORM_FILLED_BY => GetFormFilledSummary(objectArtifact.ItemId),
                    LibraryAssignedMemberType.FORM_FILL_PENDING_BY => GetFormFillPendingSummary(objectArtifact),
                    _ => new List<AssignedDepartment>(),
                };
            }

            return await Task.FromResult(result);
        }

        #region Assigned to response block

        private List<AssignedDepartment> GetObjectArtifactAssigneeDetailResponse(ObjectArtifact objectArtifact)
        {
            if (objectArtifact.SharedOrganizationList != null && !_objectArtifactUtilityService.IsAGeneralForm(objectArtifact.MetaData))
            {
                var sharedOrganizations = GetGroupedSharedOrganizations(objectArtifact.SharedOrganizationList);
                var riqsViewControl = _riqsPediaViewControlService.GetRiqsPediaViewControl().GetAwaiter().GetResult();
                var sharedDepartments = (_securityHelperService.IsADepartmentLevelUser() && riqsViewControl?.IsAdminViewEnabled != true) ?
                    PrepareSharedOwnDepartmentData(sharedOrganizations, objectArtifact.OrganizationId) :
                    PrepareSharedDepartmentData(sharedOrganizations, objectArtifact.OrganizationId);
                var praxisUsers = GetPraxisUsers(sharedDepartments, isActiveUserOnly: true);
                var responseList = GetDepartmentWiseAssignees(sharedDepartments, praxisUsers);
                return UpdateMarkedAsReadStatus(responseList, objectArtifact.ItemId).GetAwaiter().GetResult();
            }

            return null;
        }

        private List<string> GetSharedRolesWithOrganization(List<string> tags)
        {
            return tags?.Where(t => t == RoleNames.PowerUser)?.ToList() ?? new List<string>();
        }

        private List<SharedOrganizationInfo> GetGroupedSharedOrganizations(List<SharedOrganizationInfo> sharedData)
        {
            var sharedOrganizations = sharedData?
                                    .GroupBy(i => i.OrganizationId)?
                                    .Select(g => new SharedOrganizationInfo
                                    {
                                        OrganizationId = g.Key,
                                        Tags = g.SelectMany(gi => gi.Tags).Distinct().ToArray(),
                                        SharedPersonList = g.SelectMany(gi => gi.SharedPersonList).Distinct().ToList()
                                    })?.ToList() ?? new List<SharedOrganizationInfo>();

            return sharedOrganizations;
        }

        private SharedOrganizationInfo GetSharedOrganizationData(List<SharedOrganizationInfo> sharedOrganizations, string organizationId)
        {
            return sharedOrganizations?.FirstOrDefault(i => i.OrganizationId == organizationId);
        }

        private List<SharedOrganizationInfo> PrepareSharedOwnDepartmentData(List<SharedOrganizationInfo> sharedOrganizations, string organizationId)
        {
            var assignedDepartments = new List<SharedOrganizationInfo>();

            var departmentId = _securityHelperService.ExtractDepartmentIdFromDepartmentLevelUser();

            var sharedOrganization = GetSharedOrganizationData(sharedOrganizations, organizationId);
            var sharedDepartment = GetSharedOrganizationData(sharedOrganizations, departmentId);

            if (sharedDepartment != null || sharedOrganization != null)
            {
                var userGroups =
                    sharedOrganization == null ? sharedDepartment?.Tags : _securityHelperService.GetAllDepartmentLevelStaticRoles();

                var orgGroup = GetSharedRolesWithOrganization(sharedOrganization?.Tags?.ToList());
                if (orgGroup.Count > 0)
                {
                    userGroups = orgGroup.ToArray();
                }

                var members =
                    sharedOrganization == null ? sharedDepartment?.SharedPersonList : new List<string>();

                assignedDepartments.Add(new SharedOrganizationInfo
                {
                    OrganizationId = departmentId,
                    Tags = userGroups,
                    SharedPersonList = members
                });
            }

            return assignedDepartments;
        }

        private List<SharedOrganizationInfo> PrepareSharedDepartmentData(List<SharedOrganizationInfo> sharedOrganizations, string organizationId)
        {
            var sharedDepartments = sharedOrganizations?.Where(i => i.OrganizationId != organizationId)?.ToList() ?? new List<SharedOrganizationInfo>();
            return sharedDepartments;
        }

        private List<PraxisUser> GetPraxisUsers(List<SharedOrganizationInfo> sharedDepartments, bool isExcludeAdminB = true, bool hideGroupAdmin = true, bool isActiveUserOnly = false)
        {
            var roles = sharedDepartments?
                .SelectMany(d => d.Tags?
                    .Select(t => $"{LibraryModuleConstants.StaticRoleDynamicRolePrefixMap[t]}_{d.OrganizationId}")
                )?.Distinct()?.ToArray() ?? new string[] {};
            var praxisUserIds = sharedDepartments?
                .SelectMany(d => d.SharedPersonList?.Count > 0 ? d.SharedPersonList : new List<string>())?
                .Distinct()?.ToArray() ?? new string[] {};

            if (roles?.Count() > 0 || praxisUserIds?.Count() > 0)
            {
                var praxisUsers = GetPraxisUsersByIdsOrRoles(praxisUserIds, roles, null, isExcludeAdminB, hideGroupAdmin, isActiveUserOnly);
                return praxisUsers;
            }

            return null;
        }

        private List<AssigneeSummary> PrepareDepartmentAssigneeList(List<PraxisUser> praxisUsers, List<RiqsActivitySummaryModel> formFillSummary = null)
        {
            var assignees =
                praxisUsers.Select(pu =>
                new AssigneeSummary
                {
                    Id = pu.ItemId,
                    Name = pu.DisplayName,
                    Time = formFillSummary?.Find(f => f.PerformedBy == pu.ItemId)?.PerformedOn,
                    Logo = pu.Image?.FileId
                })
                .OrderBy(pu => pu.Name)
                .ToList();

            return assignees;
        }

        #endregion

        #region Form filled response block

        private List<AssignedDepartment> GetFormFilledSummary(string objectArtifactId)
        {
            var formFillSummary = _objectArtifactUtilityService.GetFormCompletionSummary(objectArtifactId);
            var groupedFormFillSummary = GetGroupedActivitySummary(formFillSummary);

            var praxisUsers = GetPraxisUsers(groupedFormFillSummary, false);
            return GetFormFilledAssignees(groupedFormFillSummary, praxisUsers, formFillSummary);
        }

        private List<AssignedDepartment> GetFormFilledAssignees
        (
            List<SharedOrganizationInfo> sharedDepartments, 
            List<PraxisUser> praxisUsers, 
            List<RiqsActivitySummaryModel> formFillSummary
        )
        {
            var assignedDepartmentList = new List<AssignedDepartment>();
            sharedDepartments ??= new List<SharedOrganizationInfo>();

            foreach (var item in sharedDepartments)
            {
                var formFilledUsers = praxisUsers.Where(m => item.SharedPersonList.Contains(m.ItemId)).ToList();
                var departmentName = _objectArtifactUtilityService.GetDepartmentById(item.OrganizationId)?.ClientName;
                var organizationName = !string.IsNullOrWhiteSpace(departmentName) ? departmentName : _objectArtifactUtilityService.GetOrganizationById(item.OrganizationId)?.ClientName;

                var assignedDepartment = new AssignedDepartment()
                {
                    Id = item.OrganizationId,
                    Name = organizationName,
                    Assignees = PrepareDepartmentAssigneeList(formFilledUsers, formFillSummary)
                };
                assignedDepartmentList.Add(assignedDepartment);
            }

            return assignedDepartmentList;
        }

        #endregion

        #region Form fill pending response block

        private List<AssignedDepartment> GetFormFillPendingSummary(ObjectArtifact objectArtifact)
        {
            var sharedOrganizations = GetGroupedSharedOrganizations(objectArtifact.SharedOrganizationList);

            var sharedOrganization = GetSharedOrganizationData(sharedOrganizations, objectArtifact.OrganizationId);
            if (sharedOrganization == null)
            {
                var formFillSummary = _objectArtifactUtilityService.GetFormCompletionSummary(objectArtifact.ItemId);
                var groupedFormFillSummary = GetGroupedActivitySummary(formFillSummary);
                var sharedDepartments = PrepareSharedDepartmentData(sharedOrganizations, objectArtifact.OrganizationId);

                var roles = sharedDepartments?
                .SelectMany(d => d.Tags?
                    .Select(t => $"{LibraryModuleConstants.StaticRoleDynamicRolePrefixMap[t]}_{d.OrganizationId}")
                ).Distinct().ToArray();
                var includingIds = sharedDepartments?
                    .SelectMany(d => d.SharedPersonList?.Count > 0 ? d.SharedPersonList : new List<string>())
                    .Distinct().ToArray();
                var excludingIds = groupedFormFillSummary?
                    .SelectMany(d => d.SharedPersonList?.Count > 0 ? d.SharedPersonList : new List<string>())
                    .Distinct().ToArray();

                if (roles?.Count() > 0 || includingIds?.Count() > 0 || excludingIds?.Count() > 0)
                {
                    var praxisUsers = GetPraxisUsersByIdsOrRoles(includingIds, roles, excludingIds);
                    return GetDepartmentWiseAssignees(sharedDepartments, praxisUsers);
                }
            }

            return null;
        }

        #endregion

        private List<SharedOrganizationInfo> GetGroupedActivitySummary(List<RiqsActivitySummaryModel> activitySummary)
        {
            var groupedActivitySummary = activitySummary?
                                    .GroupBy(i => i.OrganizationId)
                                    .Select(g => new SharedOrganizationInfo
                                    {
                                        OrganizationId = g.Key,
                                        SharedPersonList = g.Select(gi => gi.PerformedBy).Distinct().ToList(),
                                        Tags = new string[] { }
                                    }).ToList();

            return groupedActivitySummary;
        }

        private List<AssignedDepartment> GetDepartmentWiseAssignees(List<SharedOrganizationInfo> sharedDepartments, List<PraxisUser> praxisUsers)
        {
            var assignedDepartmentList = new List<AssignedDepartment>();
            sharedDepartments ??= new List<SharedOrganizationInfo>();

            foreach (var item in sharedDepartments)
            {
                var departmentPraxisUsers = praxisUsers?.Where(m => m.ClientList.Any(c => c.ClientId == item.OrganizationId))?.ToList() ?? new List<PraxisUser>();
                var assignedDepartment = new AssignedDepartment()
                {
                    Id = item.OrganizationId,
                    Name = _objectArtifactUtilityService.GetDepartmentById(item.OrganizationId)?.ClientName,
                    Assignees = PrepareDepartmentAssigneeList(departmentPraxisUsers)
                };
                assignedDepartmentList.Add(assignedDepartment);
            }

            return assignedDepartmentList;
        }

        private List<PraxisUser> GetPraxisUsersByIdsOrRoles(
            string[] includingIds, string[] roles = null, string[] excludingIds = null, bool isExcludeAdminB = true, bool hideGroupAdmin = true, bool isActiveUserOnly = false)
        {
            roles ??= new string[] { };
            excludingIds ??= new string[] { };
            var hideRoles = new List<string>() {};
            if (isExcludeAdminB) hideRoles.Add(RoleNames.AdminB);
            if (hideGroupAdmin) hideRoles.Add(RoleNames.GroupAdmin);

            Expression<Func<PraxisUser, bool>> filter =
                pu => !excludingIds.Contains(pu.ItemId) &&
                !pu.Roles.Any(r => hideRoles.Contains(r)) && !pu.IsMarkedToDelete &&
                (includingIds.Contains(pu.ItemId) || pu.Roles.Any(r => roles.Contains(r))) && (pu.Active || !isActiveUserOnly);

            return
                _repository.GetItems(filter)?
                    .Select(pu => new PraxisUser()
                    {
                        ItemId = pu.ItemId,
                        DisplayName = pu.DisplayName,
                        ClientList = pu.ClientList,
                        Roles = pu.Roles ?? new List<string>(),
                        Active = pu.Active,
                        Image = pu.Image,
                    })?
                    .ToList();
        }
        private async Task<List<AssignedDepartment>> UpdateMarkedAsReadStatus(List<AssignedDepartment> assignedDepartments, string objectArtifactId)
        {
            var usersWithReadNotificationEnabled = GetUsersWithReadNotificationEnabled(assignedDepartments, objectArtifactId);
            if (usersWithReadNotificationEnabled?.Count == 0) return assignedDepartments;
            
            var usersReadCurrentArtifact = await GetDocumentsMarkedAsReadByArtifactId(objectArtifactId);

            var enabledUsersSet = new HashSet<string>(usersWithReadNotificationEnabled);
            var readByUsersSet = usersReadCurrentArtifact != null
                ? new HashSet<string>(usersReadCurrentArtifact.Select(u => u.ReadByUserId))
                : new HashSet<string>();

            foreach (var assignee in assignedDepartments.SelectMany(department => department.Assignees))
            {
                assignee.IsReadNotificationEnabled = enabledUsersSet.Contains(assignee.Id);
                assignee.IsMarkedAsRead = readByUsersSet.Contains(assignee.Id);
            }

            return assignedDepartments;
        }

        private List<string> GetUsersWithReadNotificationEnabled(List<AssignedDepartment> assignedDepartments, string objectArtifactId)
        {
            if (assignedDepartments == null || !assignedDepartments.Any() || string.IsNullOrEmpty(objectArtifactId))
                return new List<string>();

            var clientIds = new HashSet<string>(assignedDepartments.Select(a => a.Id));
            var assignees = new HashSet<string>(assignedDepartments.SelectMany(a => a.Assignees?.Select(u => u.Id) ?? Enumerable.Empty<string>()));
            
            if (!clientIds.Any() || !assignees.Any())
                return new List<string>();
            
            var cockpitObjectArtifactSummaries = _repository.GetItems<CockpitObjectArtifactSummary>(c => 
                    c.ObjectArtifactId == objectArtifactId && !c.IsMarkedToDelete && c.IsActive)
                ?.ToList() ?? new List<CockpitObjectArtifactSummary>();
            
            if (!cockpitObjectArtifactSummaries.Any())
                return new List<string>();
            
            var summaryIds = new HashSet<string>(cockpitObjectArtifactSummaries.Select(c => c.ItemId));
            
            var usersWithReadNotification = _repository.GetItems<CockpitDocumentActivityMetrics>(a =>
                    clientIds.Contains(a.DepartmentId) && 
                    a.ActivityKey == nameof(CockpitDocumentActivityEnum.DOCUMENTS_ASSIGNED) &&
                    assignees.Contains(a.PraxisUserId) &&
                    a.CockpitObjectArtifactSummaryIds.Any(id => summaryIds.Contains(id)) &&
                    !a.IsMarkedToDelete)
                ?.Select(a => a.PraxisUserId)
                .Distinct()
                .ToList() ?? new List<string>();
            
            return usersWithReadNotification;
        }

        private async Task<List<DocumentsMarkedAsRead>> GetDocumentsMarkedAsReadByArtifactId(string objectArtifactId)
        {
            var builder = Builders<DocumentsMarkedAsRead>.Filter;
            var filter = builder.Eq(d => d.ObjectArtifactId, objectArtifactId);
            var projection = Builders<DocumentsMarkedAsRead>.Projection
                .Include(d => d.ReadByUserId);
            var collection = _ecapMongoDbDataContextProvider
                .GetTenantDataContext()
                .GetCollection<DocumentsMarkedAsRead>($"{nameof(DocumentsMarkedAsRead)}s");
            var documentsMarkedAsRead = await collection
                .Find(filter)
                .Project(projection)
                .ToListAsync();
            return documentsMarkedAsRead?
                .Select(i => BsonSerializer.Deserialize<DocumentsMarkedAsRead>(i))
                .ToList() ?? new List<DocumentsMarkedAsRead>();
        }
    }
}