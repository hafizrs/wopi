using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.LibraryModule;
using Selise.Ecap.Entities.PrimaryEntities.Dms;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants;
using Selise.Ecap.SC.PraxisMonitor.Contracts.ResponseModels.LibraryModule;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Entities.LibraryModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;

namespace Selise.Ecap.SC.PraxisMonitor.Domain
{
    public class ObjectArtifactSharedDataResponseGeneratorService : IObjectArtifactSharedDataResponseGeneratorService
    {
        private readonly IObjectArtifactUtilityService _objectArtifactUtilityService;
        private readonly ISecurityHelperService _securityHelperService;
        private readonly ILogger<ObjectArtifactSharedDataResponseGeneratorService> _logger;

        public ObjectArtifactSharedDataResponseGeneratorService(
            IObjectArtifactUtilityService objectArtifactUtilityService,
            ISecurityHelperService securityHelperService,
            ILogger<ObjectArtifactSharedDataResponseGeneratorService> logger)
        {
            _objectArtifactUtilityService = objectArtifactUtilityService;
            _securityHelperService = securityHelperService;
            _logger = logger;
        }

        #region Public methods

        public SharedObjectArtifactResponse GetSharedObjectArtifactResponse(
            ObjectArtifact objectArtifact, 
            List<PraxisUser> praxisUsers = null, 
            List<PraxisClient> praxisClients = null, 
            List<PraxisOrganization> praxisOrganizations = null,
            RiqsPediaViewControlResponse riqsViewControl = null
        )
        {
            var response = new SharedObjectArtifactResponse();

            if (objectArtifact.SharedOrganizationList != null)
            {
                var sharedPersonIdList = objectArtifact.SharedOrganizationList.SelectMany(s => s.SharedPersonList).Distinct().ToArray();
                var praxisUserList = _objectArtifactUtilityService.GetPraxisUsersByIdsOrRoles(sharedPersonIdList, null, null, false, praxisUsers);
                var sharedOrganization = PrepareSharedOrganizationData(objectArtifact.OrganizationId, objectArtifact.SharedOrganizationList, praxisOrganizations);
                response.SharedOrganization = _securityHelperService.IsADepartmentLevelUser() && riqsViewControl?.IsAdminViewEnabled != true ?
                    null : sharedOrganization;
                response.SharedDepartmentList = _securityHelperService.IsADepartmentLevelUser() && riqsViewControl?.IsAdminViewEnabled != true ?
                    PrepareRestrictedSharedDepartmentData(sharedOrganization, objectArtifact.SharedOrganizationList, praxisUserList, praxisClients) :
                    PrepareCompleteSharedDepartmentData(objectArtifact.OrganizationId, objectArtifact.SharedOrganizationList, praxisUserList, praxisClients);
            }

            return response;
        }

        public AssigneeDetail GetObjectArtifactAssigneeDetailResponse(
            ObjectArtifact objectArtifact,
            List<PraxisUser> praxisUsers = null,
            List<PraxisOrganization> praxisOrganizations = null,
            RiqsPediaViewControlResponse riqsViewControl = null
        )
        {
            var response = new AssigneeDetail();
            if (objectArtifact.SharedOrganizationList != null && !_objectArtifactUtilityService.IsAGeneralForm(objectArtifact.MetaData))
            {
                var sharedOrganizations = GetGroupedSharedOrganizations(objectArtifact.SharedOrganizationList);
                var sharedOrganization = GetSharedOrganizationData(sharedOrganizations, objectArtifact.OrganizationId);
                if (sharedOrganization != null && (!_securityHelperService.IsADepartmentLevelUser() || riqsViewControl?.IsAdminViewEnabled == true))
                {
                    response.AssignedOrganization = PrepareAssignedOrganizationData(sharedOrganization, praxisOrganizations);
                }
                else
                {
                    var sharedDepartments = _securityHelperService.IsADepartmentLevelUser() && riqsViewControl?.IsAdminViewEnabled != true ?
                        PrepareSharedOwnDepartmentData(sharedOrganizations, objectArtifact.OrganizationId) :
                        PrepareSharedDepartmentData(sharedOrganizations, objectArtifact.OrganizationId);
                    response.AssignedMembers = PrepareAssignedMembersData(sharedDepartments, true, praxisUsers);
                }
            }
            return response;
        }

        public (AssignedMemberDetail, AssignedMemberDetail) GetFormFillActionDetails(ObjectArtifact objectArtifact, List<PraxisUser> praxisUsers = null, List<RiqsObjectArtifactMapping> artifactMappingDatas = null)
        {
            return (GetFormFilledSummary(objectArtifact.ItemId, praxisUsers, artifactMappingDatas), GetFormFillPendingSummary(objectArtifact, praxisUsers, artifactMappingDatas));
        }

        public (AssignedMemberDetail, AssignedMemberDetail) GetFormFillActionDetailsForFilledForm(ObjectArtifact objectArtifact, List<PraxisUser> praxisUsers = null, List<RiqsObjectArtifactMapping> artifactMappingDatas = null)
        {
            return (GetFilledFormSummary(objectArtifact.ItemId, praxisUsers, artifactMappingDatas), GetFilledFormPendingSummary(objectArtifact, praxisUsers, artifactMappingDatas));
        }

        #endregion

        #region Form completion block

        private AssignedMemberDetail GetFormFilledSummary(string objectArtifactId, List<PraxisUser> praxisUsers = null, List<RiqsObjectArtifactMapping> artifactMappingDatas = null)
        {
            var formFillSummary = _objectArtifactUtilityService.GetFormCompletionSummary(objectArtifactId, artifactMappingDatas);
            var groupedFormFillSummary = GetGroupedActivitySummary(formFillSummary);

            return PrepareAssignedMembersData(groupedFormFillSummary, false, praxisUsers, formFillSummary);
        }

        private AssignedMemberDetail GetFormFillPendingSummary(ObjectArtifact objectArtifact, List<PraxisUser> praxisUsers = null, List<RiqsObjectArtifactMapping> artifactMappingDatas = null)
        {
            var sharedOrganizations = GetGroupedSharedOrganizations(objectArtifact.SharedOrganizationList);

            var sharedOrganization = GetSharedOrganizationData(sharedOrganizations, objectArtifact.OrganizationId);
            if (sharedOrganization == null)
            {
                var formFillSummary = _objectArtifactUtilityService.GetFormCompletionSummary(objectArtifact.ItemId, artifactMappingDatas);
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
                    var praxisUserList = _objectArtifactUtilityService.GetPraxisUsersByIdsOrRoles(includingIds, roles, excludingIds, true, praxisUsers, true);
                    return GetAssignedMemberDetailModel(praxisUserList);
                }
            }

            return null;
        }

        private List<SharedOrganizationInfo> GetGroupedActivitySummary(List<RiqsActivitySummaryModel> activitySummary)
        {
            var groupedActivitySummary = activitySummary?
                                    .GroupBy(i => i.OrganizationId)?
                                    .Select(g => new SharedOrganizationInfo
                                    {
                                        OrganizationId = g?.Key,
                                        SharedPersonList = g?.Select(gi => gi.PerformedBy)?.Distinct()?.ToList(),
                                        Tags = new string[] { }
                                    })?.ToList();

            return groupedActivitySummary ?? new List<SharedOrganizationInfo>();
        }

        private AssignedMemberDetail GetFilledFormSummary(string objectArtifactId, List<PraxisUser> praxisUsers = null, List<RiqsObjectArtifactMapping> artifactMappingDatas = null)
        {
            var formFillSummary = _objectArtifactUtilityService.GetFormCompletionSummary(objectArtifactId, artifactMappingDatas);
            if (formFillSummary?.Count() > 0)
            {
                _logger.LogInformation("GetFilledFormSummary -> {FormFillSummary}", string.Join("\n", formFillSummary));
            }
            if (formFillSummary?.Count() == 1)
            {
                var formFillerId = formFillSummary.First().PerformedBy;
                var hideGroupAdmin = !_securityHelperService.IsAAdmin() && !_securityHelperService.IsAGroupAdminUser();
                var praxisUser = praxisUsers?.Find(p => p.ItemId == formFillerId)
                                    ?? _objectArtifactUtilityService.GetPraxisUsersByIdsOrRoles(new string[] { formFillerId }, null, null, false, null, hideGroupAdmin)?.FirstOrDefault();
                return GetAssignedMemberDetailModel(praxisUser != null ? new List<PraxisUser> { praxisUser } : new List<PraxisUser>(), formFillSummary);
            }
            return null;
        }

        private AssignedMemberDetail GetFilledFormPendingSummary(ObjectArtifact objectArtifact, List<PraxisUser> praxisUsers = null, List<RiqsObjectArtifactMapping> artifactMappingDatas = null)
        {
            var formFillSummary = _objectArtifactUtilityService.GetFormCompletionSummary(objectArtifact.ItemId, artifactMappingDatas);
            if (formFillSummary?.Count() > 0)
            {
                _logger.LogInformation("GetFilledFormPendingSummary -> {FormFillSummary}", string.Join("\n", formFillSummary));
            }
            if (formFillSummary == null || formFillSummary.Count == 0)
            {
                var praxisUser = praxisUsers != null ? praxisUsers?.Find(p => p.UserId == objectArtifact.OwnerId) 
                                : _objectArtifactUtilityService.GetPraxisUsersByUserIds(new[] { objectArtifact.OwnerId })?.FirstOrDefault();
                if (praxisUser != null)
                {
                    var praxisUserList = _objectArtifactUtilityService.GetPraxisUsersByIdsOrRoles(new string[] { praxisUser.ItemId }, null, null, false, praxisUsers);
                    return GetAssignedMemberDetailModel(praxisUserList);
                }
            }
            return null;
        }

        #endregion

        #region Assigned to response block

        private List<SharedOrganizationInfo> GetGroupedSharedOrganizations(List<SharedOrganizationInfo> sharedData)
        {
            var sharedOrganizations = sharedData?
                                    .GroupBy(i => i.OrganizationId)?
                                    .Select(g => new SharedOrganizationInfo
                                    {
                                        OrganizationId = g?.Key,
                                        Tags = g?.SelectMany(gi => gi.Tags)?.Distinct()?.ToArray(),
                                        SharedPersonList = g?.SelectMany(gi => gi.SharedPersonList)?.Distinct()?.ToList()
                                    })?.ToList();

            return sharedOrganizations ?? new List<SharedOrganizationInfo>();
        }

        private SharedOrganizationInfo GetSharedOrganizationData(List<SharedOrganizationInfo> sharedOrganizations, string organizationId)
        {
            return sharedOrganizations?.FirstOrDefault(i => i.OrganizationId == organizationId);
        }

        private AssigneeSummary PrepareAssignedOrganizationData(SharedOrganizationInfo sharedOrganization, List<PraxisOrganization> praxisOrganizations = null)
        {
            var assignedOrganization = sharedOrganization != null ?
                new AssigneeSummary()
                {
                    Id = sharedOrganization.OrganizationId,
                    Name = praxisOrganizations?.Find(o => o.ItemId == sharedOrganization.OrganizationId)?.ClientName 
                                    ?? _objectArtifactUtilityService.GetOrganizationById(sharedOrganization.OrganizationId)?.ClientName,
                    SharedRolesWithOrganization = GetSharedRolesWithOrganization(sharedOrganization.Tags?.ToList()),
                } : null;
            return assignedOrganization;
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
            var sharedDepartments = sharedOrganizations?.Where(i => i.OrganizationId != organizationId)?.ToList();
            return sharedDepartments;
        }

        private AssignedMemberDetail PrepareAssignedMembersData(
            List<SharedOrganizationInfo> sharedDepartments, 
            bool isExcludeAdminB,
            List<PraxisUser> praxisUsers,
            List<RiqsActivitySummaryModel> formFillSummary = null
        )
        {
            var roles = sharedDepartments?
                .SelectMany(d => d.Tags?
                    .Select(t => $"{LibraryModuleConstants.StaticRoleDynamicRolePrefixMap[t]}_{d.OrganizationId}")
                ).Distinct().ToArray();
            var praxisUserIds = sharedDepartments?
                .SelectMany(d => d.SharedPersonList?.Count > 0 ? d.SharedPersonList : new List<string>())
                .Distinct().ToArray();

            if (roles?.Count() > 0 || praxisUserIds?.Count() > 0)
            {
                var praxisUserList = _objectArtifactUtilityService.GetPraxisUsersByIdsOrRoles(praxisUserIds, roles, null, isExcludeAdminB, praxisUsers);
                return GetAssignedMemberDetailModel(praxisUserList, formFillSummary);
            }

            return null;
        }

        private List<string> GetSharedRolesWithOrganization(List<string> tags)
        {
            return tags?.Where(t => t == RoleNames.PowerUser)?.ToList() ?? new List<string>();
        }

        private AssignedMemberDetail GetAssignedMemberDetailModel(List<PraxisUser> praxisUsers, List<RiqsActivitySummaryModel> formFillSummary = null)
        {
            var assignedMemberDetail = new AssignedMemberDetail()
            {
                Members = PrepareDepartmentAssigneeList(praxisUsers.Take(2).ToList(), formFillSummary),
                IsMoreDataAvailable = praxisUsers.Count >= LibraryModuleConstants.LibraryAssigneeSummaryLimit
            };

            return assignedMemberDetail;
        }

        private List<AssigneeSummary> PrepareDepartmentAssigneeList(List<PraxisUser> praxisUsers, List<RiqsActivitySummaryModel> formFillSummary = null)
        {
            var assignees =
                praxisUsers?.Select(pu =>
                new AssigneeSummary
                {
                    Id = pu.ItemId,
                    Name = pu.DisplayName,
                    Time = formFillSummary?.Find(f => f.PerformedBy == pu.ItemId)?.PerformedOn,
                    Logo = pu?.Image?.FileId
                })?
                .OrderBy(pu => pu.Name)?
                .ToList();

            return assignees;
        }

        #endregion

        #region Shared data response block

        private GeneralShareSummury PrepareSharedOrganizationData(string organizationId, List<SharedOrganizationInfo> sharedOrganizationList, List<PraxisOrganization> praxisOrganizations = null)
        {
            var sharedOrganization = sharedOrganizationList.FirstOrDefault(i => i.OrganizationId == organizationId);
            var organizationShareSummary = sharedOrganization != null ?
                new GeneralShareSummury()
                {
                    Id = sharedOrganization.OrganizationId,
                    Name = praxisOrganizations?.Find(o => o.ItemId == organizationId)?.ClientName ?? _objectArtifactUtilityService.GetOrganizationById(sharedOrganization.OrganizationId)?.ClientName,
                    IsAGeneralShare = true,
                    SharedRolesWithOrganization = GetSharedRolesWithOrganization(sharedOrganization.Tags?.ToList()),
                    Permission = sharedOrganization.FeatureName
                } : null;
            return organizationShareSummary;
        }

        private List<SharedDepartment> PrepareRestrictedSharedDepartmentData(
            GeneralShareSummury orgLevelSharedData,
            List<SharedOrganizationInfo> sharedOrganizationList,
            List<PraxisUser> praxisUsers,
            List<PraxisClient> praxisClients = null
        )
        {
            var sharedDepartmentList = new List<SharedDepartment>();
            var departmentId = _securityHelperService.ExtractDepartmentIdFromDepartmentLevelUser();
            var sharedDeptList = sharedOrganizationList.Where(s => s.OrganizationId == departmentId).ToList();
            var isSharedToWholeOrg = false;
            if (orgLevelSharedData != null && orgLevelSharedData.IsAGeneralShare)
            {
                if (orgLevelSharedData.SharedRolesWithOrganization?.Count > 0)
                {
                    sharedDeptList = new List<SharedOrganizationInfo> 
                    { 
                        new SharedOrganizationInfo
                        {
                            FeatureName = orgLevelSharedData.Permission,
                            SharedPersonList = new List<string>(),
                            OrganizationId = departmentId,
                            Tags = orgLevelSharedData.SharedRolesWithOrganization.ToArray()
                        }
                    };
                }
                else
                {
                    isSharedToWholeOrg = true;
                }
            }
            var sharedDepartment = isSharedToWholeOrg ?
                new SharedDepartment()
                {
                    Id = departmentId,
                    Name = praxisClients?.Find(c => c.ItemId == departmentId)?.ClientName ?? _objectArtifactUtilityService.GetDepartmentById(departmentId)?.ClientName,
                    IsAGeneralShare = true,
                    Permission = orgLevelSharedData.Permission
                } :
                PrepareSharedDepartmentData(
                    departmentId,
                    sharedDeptList,
                    praxisUsers,
                    praxisClients);
            sharedDepartmentList.Add(sharedDepartment);
            return sharedDepartmentList;
        }

        private List<SharedDepartment> PrepareCompleteSharedDepartmentData(
            string organizationId,
            List<SharedOrganizationInfo> sharedOrganizationList,
            List<PraxisUser> praxisUsers,
            List<PraxisClient> praxisClients = null
        )
        {
            var sharedDepartmentData = new List<SharedDepartment>();
            var groupedSharedDepartments =
                sharedOrganizationList
                .Where(s => s.OrganizationId != organizationId)
                .GroupBy(s => s.OrganizationId).ToList();
            groupedSharedDepartments.ForEach(groupedDepartment =>
            {
                sharedDepartmentData.Add(PrepareSharedDepartmentData(groupedDepartment.Key, groupedDepartment.ToList(), praxisUsers, praxisClients));
            });

            return sharedDepartmentData;
        }

        private SharedDepartment PrepareSharedDepartmentData(
            string departmentId,
            List<SharedOrganizationInfo> sharedDepartmentAssigneeData,
            List<PraxisUser> praxisUsers,
            List<PraxisClient> praxisClients = null
        )
        {
            var isAGeneralShare = IsSharedToWholeDepartment(sharedDepartmentAssigneeData);

            var sharedDepartment = new SharedDepartment()
            {
                Id = departmentId,
                Name = praxisClients?.Find(c => c.ItemId == departmentId)?.ClientName ?? _objectArtifactUtilityService.GetDepartmentById(departmentId)?.ClientName,
                IsAGeneralShare = isAGeneralShare,
                Permission = isAGeneralShare ? sharedDepartmentAssigneeData[0].FeatureName : string.Empty,
                SharedDepartmentList = isAGeneralShare ? new List<AssigneePermissionSummary> { } :
                PrepareSharedDepartmentAssigneeData(sharedDepartmentAssigneeData, praxisUsers)
            };

            return sharedDepartment;
        }

        private bool IsSharedToWholeDepartment(List<SharedOrganizationInfo> sharedDepartmentAssigneeData)
        {
            string[] departmentLevelStaticRoles = new[] { RoleNames.PowerUser, RoleNames.Leitung, RoleNames.MpaGroup1, RoleNames.MpaGroup2 };
            var isSharedToWholeDepartment = sharedDepartmentAssigneeData.Any(p => departmentLevelStaticRoles.All(role => p.Tags.Contains(role)));

            return isSharedToWholeDepartment;
        }

        private List<AssigneePermissionSummary> PrepareSharedDepartmentAssigneeData(
            List<SharedOrganizationInfo> sharedDepartmentAssigneeData, List<PraxisUser> praxisUsers)
        {
            var assigneePermissions = new List<AssigneePermissionSummary>();

            sharedDepartmentAssigneeData.ForEach(groupedDepartment =>
            {
                var assigneeData = new AssigneePermissionSummary()
                {
                    Permission = groupedDepartment.FeatureName,
                    UserGroups = groupedDepartment.Tags,
                    Members = PrepareMemberData(praxisUsers, groupedDepartment.SharedPersonList.ToArray())
                };
                assigneePermissions.Add(assigneeData);
            });
            return assigneePermissions;
        }

        private List<AssigneeSummary> PrepareMemberData(List<PraxisUser> praxisUsers, string[] permittedUserIds)
        {
            var members =
                praxisUsers?
                .Where(pu => permittedUserIds.Contains(pu.ItemId))?
                .Select(pu =>
                new AssigneeSummary
                {
                    Id = pu.ItemId,
                    Name = pu.DisplayName
                })?.ToList() ?? new List<AssigneeSummary>();

            return members;
        }

        #endregion
    }
}
