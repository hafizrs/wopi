using System;
using System.Globalization;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.LibraryModule;
using Selise.Ecap.Entities.PrimaryEntities.Dms;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants;
using SharedOrganizationInfo = Selise.Ecap.Entities.PrimaryEntities.Dms.SharedOrganizationInfo;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Infrastructure;
using Microsoft.Extensions.Logging;
using Selise.Ecap.Entities;

namespace Selise.Ecap.SC.PraxisMonitor.Domain
{
    public class ObjectArtifactShareService : IObjectArtifactShareService
    {
        private readonly ILogger<ObjectArtifactShareService> _logger;
        private readonly ISecurityContextProvider _securityContextProvider;
        private readonly ISecurityHelperService _securityHelperService;
        private readonly IObjectArtifactUtilityService _objectArtifactUtilityService;
        private readonly IObjectArtifactPermissionHelperService _objectArtifactPermissionHelperService;
        private readonly IObjectArtifactFilePermissionService _objectArtifactFilePermissionService;
        private readonly IObjectArtifactFolderPermissionService _objectArtifactFolderPermissionService;
        private readonly IObjectArtifactSearchService _objectArtifactSearchService;
        private readonly IObjectArtifactSearchResponseGeneratorService _objectArtifactSearchResponseGeneratorService;
        private readonly IChangeLogService _changeLogService;
        private readonly IRiqsPediaViewControlService _riqsPediaViewControlService;

        public ObjectArtifactShareService(
            ILogger<ObjectArtifactShareService> logger,
            ISecurityContextProvider securityContextProvider,
            ISecurityHelperService securityHelperService,
            IObjectArtifactUtilityService objectArtifactUtilityService,
            IObjectArtifactPermissionHelperService objectArtifactPermissionHelperService,
            IObjectArtifactFilePermissionService objectArtifactFilePermissionService,
            IObjectArtifactFolderPermissionService objectArtifactFolderPermissionService,
            IObjectArtifactSearchService objectArtifactSearchService,
            IObjectArtifactSearchResponseGeneratorService objectArtifactSearchResponseGeneratorService,
            IChangeLogService changeLogService,
            IRiqsPediaViewControlService riqsPediaViewControlService
        )
        {
            _logger = logger;
            _securityContextProvider = securityContextProvider;
            _securityHelperService = securityHelperService;
            _objectArtifactUtilityService = objectArtifactUtilityService;
            _objectArtifactPermissionHelperService = objectArtifactPermissionHelperService;
            _objectArtifactFilePermissionService = objectArtifactFilePermissionService;
            _objectArtifactFolderPermissionService = objectArtifactFolderPermissionService;
            _objectArtifactSearchService = objectArtifactSearchService;
            _objectArtifactSearchResponseGeneratorService = objectArtifactSearchResponseGeneratorService;
            _changeLogService = changeLogService;
            _riqsPediaViewControlService = riqsPediaViewControlService;
        }

        #region public methods

        public async Task<bool> ShareObjectArtifact(ObjectArtifact objectArtifact, ObjectArtifactFileShareCommand accessControlCommand)
        {
            if (accessControlCommand?.IsAdminViewEnabled == null)
            {
                accessControlCommand.IsAdminViewEnabled = (await _riqsPediaViewControlService.GetRiqsPediaViewControl())?.IsAdminViewEnabled ?? false;
            }
            var currentAccessControl = GetCurrentAccessControl(objectArtifact);
            var updatedAccessControl = GetUpdatedAccessControl(currentAccessControl, accessControlCommand, objectArtifact);
            var updatedObjectArtifact = PrepareSharedObjectArtifactData(updatedAccessControl, objectArtifact);
            var updates = PrepareObjectArtifactUpdates(objectArtifact, updatedObjectArtifact, accessControlCommand.IsStandardFile, accessControlCommand.NotifyToCockpit);
            var response = await UpdateObjectArtifact(objectArtifact.ItemId, updates);
            return response;
        }

        public bool IsObjectArtifactInASharedDirectory(ObjectArtifact objectArtifact)
        {
            bool isFolderInASharedDirectory = false;
            if (!string.IsNullOrWhiteSpace(objectArtifact.ParentId))
            {
                var parentObjectArtifact = _objectArtifactUtilityService.GetObjectArtifactById(objectArtifact.ParentId);
                isFolderInASharedDirectory = parentObjectArtifact?.SharedOrganizationList?.Count > 0;
            }
            return isFolderInASharedDirectory;
        }

        public async Task<bool> InitiateShareWithParentSharedUsers(ObjectArtifact objectArtifact)
        {
             _logger.LogInformation("Share with parent folder for artifact: {id}", objectArtifact.ItemId);
            var parentObjectArtifact = _objectArtifactUtilityService.GetObjectArtifactById(objectArtifact.ParentId);

            return
                parentObjectArtifact?.SharedOrganizationList?.Count > 0 &&
                await ShareObjectArtifact(objectArtifact, GetCurrentAccessControl(parentObjectArtifact));
        }

        public async Task<bool> ShareGeneralForm(ObjectArtifact objectArtifact)
        {
            var organizationGeneralShareCommand =
                _objectArtifactPermissionHelperService.IsAAdminBUpload(objectArtifact.CreatedBy, objectArtifact.OrganizationId) ?
                new ObjectArtifactFileShareCommand()
                {
                    IsSharedToWholeOrganization = true,
                    ObjectArtifactId = objectArtifact.ItemId,
                    OrganizationId = objectArtifact.OrganizationId,
                    Permission = "form_fill"
                } :
                new ObjectArtifactFileShareCommand()
                {
                    IsSharedToWholeOrganization = false,
                    ObjectArtifactId = objectArtifact.ItemId,
                    OrganizationId = objectArtifact.OrganizationId,
                    SharedDepartmentList = new List<DepartmentWiseObjectArtifactSharedDetail>
                    {
                        new DepartmentWiseObjectArtifactSharedDetail()
                        {
                            DepartmentId = _objectArtifactUtilityService.GetObjectArtifactDepartmentId(objectArtifact.MetaData),
                            IsSharedToWholeDepartment = true,
                            Permission = "form_fill"
                        }
                    }
                };

            return await ShareObjectArtifact(objectArtifact, organizationGeneralShareCommand);
        }

        #endregion

        #region  Get Current Access Control

        public ObjectArtifactFileShareCommand GetCurrentAccessControl(ObjectArtifact objectArtifact)
        {
            ObjectArtifactFileShareCommand accessControl = new ObjectArtifactFileShareCommand()
            {
                ObjectArtifactId = objectArtifact.ItemId,
                OrganizationId = objectArtifact.OrganizationId,
            };

            if (objectArtifact.SharedOrganizationList?.Count > 0)
            {
                var groupedSharedData = objectArtifact.SharedOrganizationList.GroupBy(o => o.OrganizationId).ToList();
                var sharedOrganizationData = GetSharedOrganizationData(groupedSharedData, objectArtifact.OrganizationId);
                if (sharedOrganizationData != null)
                {
                    PrepareCurrentSharedOrganizationData(sharedOrganizationData, accessControl, objectArtifact);
                }
                else
                {
                    accessControl.SharedDepartmentList = PrepareCurrentSharedDepartmentData(groupedSharedData, objectArtifact);
                }
            }

            return accessControl;
        }

        private SharedOrganizationInfo GetSharedOrganizationData(
            List<IGrouping<string, SharedOrganizationInfo>> groupedSharedData,
            string organizationId)
        {
            var sharedOrganizationData = groupedSharedData.Find(g => g.Key == organizationId)?.ToList();

            if (sharedOrganizationData?.Count == 1)
            {
                return sharedOrganizationData[0];
            }

            return null;
        }

        private void PrepareCurrentSharedOrganizationData(
            SharedOrganizationInfo sharedOrganizationData,
            ObjectArtifactFileShareCommand accessControl,
            ObjectArtifact artifact)
        {
            accessControl.SharedRolesWithOrganization = GetSharedRolesWithOrganization(sharedOrganizationData?.Tags?.ToList());
            accessControl.IsSharedToWholeOrganization = IsSharedToWholeOrganization(sharedOrganizationData);
            accessControl.Permission = _objectArtifactUtilityService.IsAForm(artifact.MetaData) && sharedOrganizationData.FeatureName == "update" ? "form_fill" : sharedOrganizationData.FeatureName;
        }

        private List<string> GetSharedRolesWithOrganization(List<string> tags)
        {
            return tags?.Where(t => t == RoleNames.PowerUser)?.ToList() ?? new List<string>();
        }

        private bool IsSharedToWholeOrganization(SharedOrganizationInfo orgLevelPermission)
        {
            var organizationLevelStaticRoles = new List<string> { RoleNames.Organization_Read_Dynamic, RoleNames.PowerUser };
            var isSharedToWholeOrganization = orgLevelPermission.Tags.All(t => organizationLevelStaticRoles.Contains(t));

            return isSharedToWholeOrganization;
        }

        private List<DepartmentWiseObjectArtifactSharedDetail> PrepareCurrentSharedDepartmentData(
            List<IGrouping<string, SharedOrganizationInfo>> groupedSharedDepartments, ObjectArtifact artifact)
        {
            var sharedDepartmentData = new List<DepartmentWiseObjectArtifactSharedDetail>();
            groupedSharedDepartments.ForEach(groupedDepartment =>
            {
                var departmentWisePermissions = groupedDepartment.ToList();
                departmentWisePermissions.ForEach(permission =>
                {
                    permission.FeatureName = _objectArtifactUtilityService.IsAForm(artifact.MetaData) && permission.FeatureName == "update" ? "form_fill" : permission.FeatureName;
                });
                var sharedToWholeDepartment = IsSharedToWholeDepartment(departmentWisePermissions);

                var sharedDepartment = new DepartmentWiseObjectArtifactSharedDetail()
                {
                    DepartmentId = groupedDepartment.Key,
                    IsSharedToWholeDepartment = sharedToWholeDepartment != null,
                    Permission = sharedToWholeDepartment != null ?
                    sharedToWholeDepartment.FeatureName :
                        string.Empty,
                    AssigneePermissions = sharedToWholeDepartment != null ?
                        new List<AssigneePermissionModel> { } :
                    GetDepartmentWiseSharedPermissionModels(departmentWisePermissions)
                };

                sharedDepartmentData.Add(sharedDepartment);
            });

            return sharedDepartmentData;
        }

        private SharedOrganizationInfo IsSharedToWholeDepartment(List<SharedOrganizationInfo> departmentWisePermissions)
        {
            string[] departmentLevelStaticRoles = new[] { RoleNames.PowerUser, RoleNames.Leitung, RoleNames.MpaGroup1, RoleNames.MpaGroup2 };
            var sharedToWholeDepartment = departmentWisePermissions.Find(p => departmentLevelStaticRoles.All(role => p.Tags.Contains(role)));

            return sharedToWholeDepartment;
        }

        private List<AssigneePermissionModel> GetDepartmentWiseSharedPermissionModels(List<SharedOrganizationInfo> currentSharedDepartmentList)
        {
            var permissionModels = new List<AssigneePermissionModel>();
            currentSharedDepartmentList.ForEach(currentSharedDepartment =>
            {
                permissionModels.Add(GetSharedPermissionModel(currentSharedDepartment));
            });
            return permissionModels;
        }

        private AssigneePermissionModel GetSharedPermissionModel(SharedOrganizationInfo currentSharedDepartment)
        {
            var permissionModel = new AssigneePermissionModel()
            {
                Permission = currentSharedDepartment.FeatureName,
                UserGroups = new AssigneeChanges()
                {
                    Added = currentSharedDepartment.Tags
                },
                Members = new AssigneeChanges() { Added = currentSharedDepartment.SharedPersonList.ToArray() }
            };

            return permissionModel;
        }

        #endregion

        #region Prepare Updated Access Control

        private ObjectArtifactFileShareCommand GetUpdatedAccessControl(
            ObjectArtifactFileShareCommand currentAccessControl, ObjectArtifactFileShareCommand accessControlCommand, ObjectArtifact objectArtifact)
        {
            var accessControl = new ObjectArtifactFileShareCommand()
            {
                ObjectArtifactId = currentAccessControl.ObjectArtifactId,
                ViewMode = accessControlCommand.ViewMode,
                OrganizationId = currentAccessControl.OrganizationId,
                SharedDepartmentList = !accessControlCommand.IsSharedToWholeOrganization ? PrepareUpdatedSharedDepartmentData(
                    currentAccessControl, accessControlCommand.SharedDepartmentList, objectArtifact) :
                    new List<DepartmentWiseObjectArtifactSharedDetail>(),
                Permission = _objectArtifactUtilityService.IsAForm(objectArtifact.MetaData) && accessControlCommand.Permission == "update" ? "form_fill" : accessControlCommand.Permission,
                SharedRolesWithOrganization = accessControlCommand.SharedRolesWithOrganization,
                IsAdminViewEnabled = accessControlCommand.IsAdminViewEnabled,
            };


            if (!accessControlCommand.IsSharedToWholeOrganization && currentAccessControl.IsSharedToWholeOrganization &&
                !(_securityHelperService.IsAOrganizationAdminB(accessControl.OrganizationId) || accessControl?.IsAdminViewEnabled == true))
            {
                var restrictedSharedDeptList = PrepareDepartmentDataOnGeneralToRestrictedChange(
                        accessControl.OrganizationId, currentAccessControl.Permission, accessControl.SharedDepartmentList, currentAccessControl.SharedRolesWithOrganization);

                accessControl.SharedDepartmentList.AddRange(restrictedSharedDeptList);
            }

            PrepareUpdatedSharedOrganizationData(accessControl);

            return accessControl;
        }

        private List<DepartmentWiseObjectArtifactSharedDetail> PrepareUpdatedSharedDepartmentData(
            ObjectArtifactFileShareCommand currentAccessControl,
            List<DepartmentWiseObjectArtifactSharedDetail> sharedDepartmentsCommand,
            ObjectArtifact artifact)
        {
            var organizationId = currentAccessControl.OrganizationId;
            var currentSharedDepartments = currentAccessControl.SharedDepartmentList;
            var sharedDepartmentData = new List<DepartmentWiseObjectArtifactSharedDetail>();
            currentSharedDepartments?.RemoveAll(s => s.DepartmentId == organizationId);

            if (currentSharedDepartments != null && currentSharedDepartments.Count > 0)
            {
                sharedDepartmentData.AddRange(currentSharedDepartments);
            }

            if (sharedDepartmentsCommand != null && sharedDepartmentsCommand.Count > 0)
            {
                foreach (var item in sharedDepartmentsCommand)
                {
                    var currentSharedDepartment = currentSharedDepartments?.FirstOrDefault(s => s.DepartmentId == item.DepartmentId);
                    if (currentSharedDepartment != null)
                    {
                        sharedDepartmentData.Remove(currentSharedDepartment);
                    }

                    item.Permission = _objectArtifactUtilityService.IsAForm(artifact.MetaData) && item.Permission == "update" ? "form_fill" : item.Permission;
                    var sharedDepartment = new DepartmentWiseObjectArtifactSharedDetail()
                    {
                        DepartmentId = item.DepartmentId,
                        IsSharedToWholeDepartment = item.IsSharedToWholeDepartment,
                        Permission = item.Permission,
                        AssigneePermissions = !item.IsSharedToWholeDepartment ?
                        PrepareUpdatedAssigneePermissions(
                            currentSharedDepartment?.AssigneePermissions, item.AssigneePermissions, 
                            item.Permission == currentAccessControl.Permission ? currentAccessControl.SharedRolesWithOrganization : new List<string>()
                        ) : null
                    };

                    sharedDepartmentData.Add(sharedDepartment);
                }
            }

            return sharedDepartmentData;
        }

        private void PrepareUpdatedSharedOrganizationData(ObjectArtifactFileShareCommand accessControl)
        {
            if (accessControl.SharedDepartmentList != null && accessControl.SharedDepartmentList.Count > 0)
            {
                var allDepartmentIds = _objectArtifactUtilityService.GetDepartmentIds(accessControl.OrganizationId);
                var sharedDepartmentIds = accessControl?.SharedDepartmentList?.Select(d => d.DepartmentId)?.ToList();

                var isAllDepartmentExist = allDepartmentIds.All(id => sharedDepartmentIds.Contains(id));

                if (isAllDepartmentExist)
                {
                    var isAReadPermission =
                        accessControl?.SharedDepartmentList?.All(d => d.IsSharedToWholeDepartment && d.Permission == "read") ?? false;

                    var isAFormFillPermission =
                        accessControl?.SharedDepartmentList?.All(d => d.IsSharedToWholeDepartment && d.Permission == "form_fill") ?? false;

                    var isAEditPermission =
                        accessControl?.SharedDepartmentList?.All(d => d.IsSharedToWholeDepartment && d.Permission == "update") ?? false;

                    accessControl.Permission =
                        isAEditPermission ? "update" :
                        isAFormFillPermission ? "form_fill" :
                        isAReadPermission ? "read" :
                        null;
                }
            }

            accessControl.IsSharedToWholeOrganization = !string.IsNullOrWhiteSpace(accessControl.Permission);
        }

        private List<DepartmentWiseObjectArtifactSharedDetail> PrepareDepartmentDataOnGeneralToRestrictedChange(
            string organizationId, string permission,
            List<DepartmentWiseObjectArtifactSharedDetail> currentSharedDepartments,
            List<string> sharedRolesWithOrg)
        {
            var sharedDepartmentData = new List<DepartmentWiseObjectArtifactSharedDetail>();

            if (currentSharedDepartments != null && currentSharedDepartments.Count > 0)
            {
                var allDepartmentIds = _objectArtifactUtilityService.GetDepartmentIds(organizationId);
                var sharedDepartmentIds = currentSharedDepartments?.Select(d => d.DepartmentId)?.ToList();
                var remainingDepartmentIds = allDepartmentIds.Except(sharedDepartmentIds).ToList();

                foreach (var id in remainingDepartmentIds)
                {
                    var sharedDepartment = new DepartmentWiseObjectArtifactSharedDetail()
                    {
                        DepartmentId = id,
                        IsSharedToWholeDepartment = true,
                        Permission = permission
                    };

                    if (sharedRolesWithOrg?.Count > 0)
                    {
                        sharedDepartment.IsSharedToWholeDepartment = false;
                        sharedDepartment.AssigneePermissions = new List<AssigneePermissionModel>
                        {
                            new AssigneePermissionModel
                            {
                                Permission = permission,
                                UserGroups = new AssigneeChanges()
                                {
                                    Added = sharedRolesWithOrg.ToArray(),
                                    Removed = Array.Empty<string>()
                                },
                                Members = new AssigneeChanges()
                                {
                                    Added = Array.Empty<string>(),
                                    Removed = Array.Empty<string>()
                                }
                            }
                        };
                        sharedDepartment.Permission = null;
                    }

                    sharedDepartmentData.Add(sharedDepartment);
                }
            }

            return sharedDepartmentData;
        }

        private List<AssigneePermissionModel> PrepareUpdatedAssigneePermissions(
            List<AssigneePermissionModel> currentAssigneePermissions,
            List<AssigneePermissionModel> assigneePermissionsCommand,
            List<string> prevOrgRoles)
        {
            var assigneePermissions = new List<AssigneePermissionModel> { };

            if (assigneePermissionsCommand != null)
            {
                foreach (var item in assigneePermissionsCommand)
                {
                    var currentAssigneePermission = currentAssigneePermissions?.FirstOrDefault(p => p.Permission == item.Permission);
                    var assigneePermission = GetAssigneePermissionModel(currentAssigneePermission, item, prevOrgRoles);
                    assigneePermissions.Add(assigneePermission);
                }
            }

            return assigneePermissions;
        }

        private AssigneePermissionModel GetAssigneePermissionModel(AssigneePermissionModel currentAssigneePermission, AssigneePermissionModel assigneePermissionsCommand, List<string> prevOrgRoles)
        {
            var currentGroups = (prevOrgRoles ?? new List<string>()).Union(currentAssigneePermission?.UserGroups?.Added ?? new string[] { }).Distinct().ToArray();
            var assigneePermission = new AssigneePermissionModel()
            {
                Permission = assigneePermissionsCommand.Permission,
                UserGroups = new AssigneeChanges()
                {
                    Added = AdoptAssigneeChanges(
                               currentGroups, assigneePermissionsCommand?.UserGroups?.Added, assigneePermissionsCommand?.UserGroups?.Removed)
                },
                Members = new AssigneeChanges()
                {
                    Added = AdoptAssigneeChanges(
                                currentAssigneePermission?.Members?.Added, assigneePermissionsCommand?.Members?.Added, assigneePermissionsCommand?.Members?.Removed)
                }
            };

            return assigneePermission;
        }

        private string[] AdoptAssigneeChanges(string[] currentArray, string[] addedArray, string[] removedArray)
        {
            var currentList = currentArray != null ? currentArray.ToList() : new List<string>();
            var addedList = addedArray != null ? addedArray.ToList() : new List<string>();
            var removedList = removedArray != null ? removedArray.ToList() : new List<string>();

            currentList.AddRange(addedList);
            currentList.RemoveAll(ug => removedList.Contains(ug));

            return currentList.Distinct().ToArray();
        }

        #endregion

        #region Prepare Updated SharedObjectArtifactData

        private ObjectArtifact PrepareSharedObjectArtifactData(ObjectArtifactFileShareCommand accessControl, ObjectArtifact artifact)
        {
            var sharedOrganizationData = PrepareSharedOrganizationList(accessControl);
            var sharedPermissions = PrepareObjectArtifactPermissions(sharedOrganizationData, accessControl.OrganizationId);
            var otherSharedData = PrepareSharedObjectArtifactSummary(sharedOrganizationData, sharedPermissions);
            var emptyArray = new string[] { };
            var authorizedIds = _objectArtifactPermissionHelperService.GetObjectArtifactAuthorizedIds(artifact, onlyDeptLevel: _objectArtifactUtilityService.IsASecretArtifact(artifact.MetaData));

            var objectArtifact = new ObjectArtifact()
            {
                ItemId = accessControl.ObjectArtifactId,
                SharedOrganizationList = sharedOrganizationData,
                SharedPersonIdList = otherSharedData.SharedPersonIdList,
                SharedUserIdList = otherSharedData.SharedUserIdList,
                SharedRoleList = otherSharedData.SharedRoleList,
                RolesAllowedToRead = sharedPermissions.RolesAllowedToRead,
                IdsAllowedToRead = (sharedPermissions.IdsAllowedToRead ?? emptyArray).Union(authorizedIds).Distinct().ToArray(),
                RolesAllowedToUpdate = sharedPermissions.RolesAllowedToUpdate,
                IdsAllowedToUpdate = (sharedPermissions.IdsAllowedToUpdate ?? emptyArray).Union(authorizedIds).Distinct().ToArray()
            };

            return objectArtifact;
        }

        private List<SharedOrganizationInfo> PrepareSharedOrganizationList(ObjectArtifactFileShareCommand accessControl)
        {
            var sharedOrganizationList = new List<SharedOrganizationInfo>() { };

            if (accessControl.IsSharedToWholeOrganization)
            {
                sharedOrganizationList.Add(PrepareSharedOrganization(accessControl));
            }
            else
            {
                sharedOrganizationList.AddRange(PrepareSharedDepartments(accessControl.SharedDepartmentList));
            }

            return sharedOrganizationList;
        }

        private SharedOrganizationInfo PrepareSharedOrganization(ObjectArtifactFileShareCommand accessControl)
        {
            var sharedRoles = GetSharedRolesWithOrganization(accessControl.SharedRolesWithOrganization);
            var sharedOrganization = new SharedOrganizationInfo()
            {
                OrganizationId = accessControl.OrganizationId,
                FeatureName = accessControl.Permission,
                Tags = sharedRoles.Count > 0 ? sharedRoles.ToArray() : new string[] { RoleNames.Organization_Read_Dynamic },
                SharedPersonList = new List<string> { }
            };

            return sharedOrganization;
        }

        private List<SharedOrganizationInfo> PrepareSharedDepartments(List<DepartmentWiseObjectArtifactSharedDetail> accessControl)
        {
            var sharedDepartments = new List<SharedOrganizationInfo>() { };

            foreach (var sharedDepartment in accessControl)
            {
                if (sharedDepartment.IsSharedToWholeDepartment)
                {
                    sharedDepartments.Add(PrepareWholeSharedDepartment(sharedDepartment));
                }
                else
                {
                    sharedDepartments.AddRange(PrepareSharedDepartment(sharedDepartment));
                }
            }

            return sharedDepartments;
        }

        private SharedOrganizationInfo PrepareWholeSharedDepartment(DepartmentWiseObjectArtifactSharedDetail accessControl)
        {
            string[] departmentLevelStaticRoles = new[] { RoleNames.PowerUser, RoleNames.Leitung, RoleNames.MpaGroup1, RoleNames.MpaGroup2 };
            var sharedDepartment = new SharedOrganizationInfo()
            {
                OrganizationId = accessControl.DepartmentId,
                FeatureName = accessControl.Permission,
                Tags = departmentLevelStaticRoles,
                SharedPersonList = new List<string> { }
            };

            return sharedDepartment;
        }

        private List<SharedOrganizationInfo> PrepareSharedDepartment(DepartmentWiseObjectArtifactSharedDetail accessControl)
        {
            var sharedDepartmentPermissions = new List<SharedOrganizationInfo>() { };

            foreach (var assigneePermission in accessControl.AssigneePermissions)
            {
                var sharedDepartment = new SharedOrganizationInfo()
                {
                    OrganizationId = accessControl.DepartmentId,
                    FeatureName = assigneePermission.Permission,
                    Tags = assigneePermission.UserGroups.Added,
                    SharedPersonList = assigneePermission.Members?.Added?.ToList() ?? new List<string>()
                };

                sharedDepartmentPermissions.Add(sharedDepartment);
            }

            return sharedDepartmentPermissions;
        }

        private ObjectArtifact PrepareObjectArtifactPermissions(List<SharedOrganizationInfo> sharedOrganizationList, string orgId)
        {
            var objectArtifactPermissions = new ObjectArtifact()
            {
                IdsAllowedToRead = GetSharedIdsAllowedToRead(sharedOrganizationList),
                RolesAllowedToRead = GetSharedRolesAllowedToRead(sharedOrganizationList, orgId),
                IdsAllowedToUpdate = GetSharedIdsAllowedToUpdate(sharedOrganizationList),
                RolesAllowedToUpdate = GetSharedRolesAllowedToUpdate(sharedOrganizationList, orgId)
            };

            return objectArtifactPermissions;
        }

        public string[] GetSharedIdsAllowedToRead(List<SharedOrganizationInfo> sharedOrganizationList)
        {
            var sharedPersonIdList = sharedOrganizationList?.SelectMany(s => s.SharedPersonList)?.Distinct()?.ToArray() ?? new string[] {};
            var userIds = sharedPersonIdList.Length > 0 ? _objectArtifactUtilityService.GetPraxisUsersByIds(sharedPersonIdList).Select(pu => pu.UserId).ToArray() : new string[] {};
            return userIds?.Where(c => !string.IsNullOrEmpty(c))?.ToArray() ?? new string[] {};
        }

        private string[] GetSharedRolesAllowedToRead(List<SharedOrganizationInfo> sharedOrganizationList, string orgId)
        {
            var groupedSharedRoles = sharedOrganizationList
                .GroupBy(i => i.OrganizationId)
                .Select(g => new { OrganizationId = g.Key, Roles = g.ToList().SelectMany(gi => gi.Tags).ToArray() })
                .ToList();

            var roles = new List<string>();

            foreach (var group in groupedSharedRoles)
            {
                var isSharedWithPowerUser = false;
                if (group.OrganizationId == orgId)
                {
                    var sharedRolesWithOrg = GetSharedRolesWithOrganization(group.Roles?.ToList());
                    if (sharedRolesWithOrg?.Count > 0)
                    {
                        isSharedWithPowerUser = true;
                    }
                }
                foreach (var role in group.Roles)
                {
                    var dynamicRole = isSharedWithPowerUser ? role : 
                                        $"{LibraryModuleConstants.StaticRoleDynamicRolePrefixMap[role]}_{group.OrganizationId}";
                    
                    roles.Add(dynamicRole);
                }
            }

            return roles.Distinct().ToArray();
        }

        public string[] GetSharedIdsAllowedToUpdate(List<SharedOrganizationInfo> sharedOrganizationList)
        {
            var sharedPersonIdList = sharedOrganizationList?.Where(s => s.FeatureName == "update" || s.FeatureName == "form_fill")?.SelectMany(s => s.SharedPersonList)?.Distinct()?.ToArray() ?? new string[] {};
            var userIds = sharedPersonIdList.Length > 0 ? _objectArtifactUtilityService.GetPraxisUsersByIds(sharedPersonIdList).Select(pu => pu.UserId).ToArray() : new string[] { };
            return userIds?.Where(c => !string.IsNullOrEmpty(c))?.ToArray() ?? new string[] { };
        }

        private string[] GetSharedRolesAllowedToUpdate(List<SharedOrganizationInfo> sharedOrganizationList, string orgId)
        {
            var groupedSharedRoles = sharedOrganizationList
                .Where(i => i.FeatureName == "update" || i.FeatureName == "form_fill")
                .GroupBy(i => i.OrganizationId)
                .Select(g => new { OrganizationId = g.Key, Roles = g.ToList().SelectMany(gi => gi.Tags).ToArray() })
                .ToList();

            var roles = new List<string>();

            foreach (var group in groupedSharedRoles)
            {
                var isSharedWithPowerUser = false;
                if (group.OrganizationId == orgId)
                {
                    var sharedRolesWithOrg = GetSharedRolesWithOrganization(group.Roles?.ToList());
                    if (sharedRolesWithOrg?.Count > 0)
                    {
                        isSharedWithPowerUser = true;
                    }
                }
                foreach (var role in group.Roles)
                {
                    var dynamicRole = isSharedWithPowerUser ? role :
                                        $"{LibraryModuleConstants.StaticRoleDynamicRolePrefixMap[role]}_{group.OrganizationId}";

                    roles.Add(dynamicRole);
                }
            }

            return roles.Distinct().ToArray();
        }

        private ObjectArtifact PrepareSharedObjectArtifactSummary(
            List<SharedOrganizationInfo> sharedOrganizationList,
            ObjectArtifact sharedPermissions)
        {
            var sharedPersonIdList = sharedOrganizationList.SelectMany(s => s.SharedPersonList).Distinct();
            var userIds = _objectArtifactUtilityService.GetPraxisUsersByIds(sharedPersonIdList.ToArray()).Select(pu => pu.UserId);

            return new ObjectArtifact
            {
                SharedPersonIdList = sharedPersonIdList.ToList(),
                SharedUserIdList = userIds.ToList(),
                SharedRoleList = sharedPermissions.RolesAllowedToRead.Union(sharedPermissions.RolesAllowedToUpdate).Distinct().ToList()
            };
        }

        #endregion

        #region Prepare Object Artifact Updates

        private Dictionary<string, object> PrepareObjectArtifactUpdates(
            ObjectArtifact currentObjectArtifact,
            ObjectArtifact updatedObjectArtifact, 
            bool? isStandardFile = null,
            bool isNotifiedToCockpit = false)
        {
            var securityContext = _securityContextProvider.GetSecurityContext();
            var currentTime = DateTime.UtcNow;
            var permissionUpdates =
                currentObjectArtifact.ArtifactType == ArtifactTypeEnum.File ?
                _objectArtifactFilePermissionService.PrepareObjectArtifactPermissionModel(currentObjectArtifact, ObjectArtifactEvent.FILE_APPROVED) :
                _objectArtifactFolderPermissionService.PrepareObjectArtifactFolderPermissionModel(currentObjectArtifact);

            var updates = new Dictionary<string, object>
            {
                {
                    nameof(ObjectArtifact.LastUpdateDate), currentTime
                },
                {
                    nameof(ObjectArtifact.LastUpdatedBy), securityContext.UserId
                },
                {
                    nameof(ObjectArtifact.MetaData), PrepareObjectArtifactMetaDataUpdate(currentObjectArtifact.MetaData, currentTime, isStandardFile, isNotifiedToCockpit)
                },
                {
                    nameof(ObjectArtifact.SharedOrganizationList), updatedObjectArtifact.SharedOrganizationList
                },
                {
                    nameof(ObjectArtifact.SharedPersonIdList), updatedObjectArtifact.SharedPersonIdList
                },
                {
                    nameof(ObjectArtifact.SharedRoleList), updatedObjectArtifact.SharedRoleList
                },
                {
                    nameof(ObjectArtifact.SharedUserIdList), updatedObjectArtifact.SharedUserIdList
                },
                {
                    nameof(ObjectArtifact.RolesAllowedToRead),
                    GetPermissions(permissionUpdates, nameof(ObjectArtifact.RolesAllowedToRead), updatedObjectArtifact.RolesAllowedToRead)
                },
                {
                    nameof(ObjectArtifact.IdsAllowedToRead),
                    GetPermissions(permissionUpdates, nameof(ObjectArtifact.IdsAllowedToRead), updatedObjectArtifact.IdsAllowedToRead)
                },
                {
                    nameof(ObjectArtifact.RolesAllowedToUpdate),
                    GetPermissions(permissionUpdates, nameof(ObjectArtifact.RolesAllowedToUpdate), updatedObjectArtifact.RolesAllowedToUpdate)
                },
                {
                    nameof(ObjectArtifact.IdsAllowedToUpdate),
                    GetPermissions(permissionUpdates, nameof(ObjectArtifact.IdsAllowedToUpdate), updatedObjectArtifact.IdsAllowedToUpdate)
                }
            };

            return updates;
        }

        public IDictionary<string, MetaValuePair> PrepareObjectArtifactMetaDataUpdate(IDictionary<string, MetaValuePair> metaData, 
            DateTime currentTime,
            bool? isStandardFile = null,
            bool isNotifiedToCockpit = false)
        {
            metaData ??= new Dictionary<string, MetaValuePair>();

            var dateType = LibraryModuleConstants.ObjectArtifactMetaDataKeyTypes[$"{nameof(ObjectArtifactMetaDataKeyTypeEnum.DATETIME)}"];
            var stringType = LibraryModuleConstants.ObjectArtifactMetaDataKeyTypes[$"{nameof(ObjectArtifactMetaDataKeyTypeEnum.STRING)}"];

            var assignedOnKey = LibraryModuleConstants.ObjectArtifactMetaDataKeys[nameof(ObjectArtifactMetaDataKeyEnum.ASSIGNED_ON)];
            var isNotifiedToCockpitKey = LibraryModuleConstants.ObjectArtifactMetaDataKeys[nameof(ObjectArtifactMetaDataKeyEnum.IS_NOTIFIED_TO_COCKPIT)];
            var isStandardFileKey = LibraryModuleConstants.ObjectArtifactMetaDataKeys[nameof(ObjectArtifactMetaDataKeyEnum.IS_STANDARD_FILE)];

            void Set(string key, string type, string value) =>
                metaData[key] = new MetaValuePair
                {
                    Type = type,
                    Value = value
                };

            Set(assignedOnKey, dateType, currentTime.ToString("o", CultureInfo.InvariantCulture));

            Set(isNotifiedToCockpitKey, stringType, ((int)(isNotifiedToCockpit ? LibraryBooleanEnum.TRUE : LibraryBooleanEnum.FALSE)).ToString());

            if (isStandardFile.HasValue)
            {
                Set(isStandardFileKey, stringType, ((int)(isStandardFile.Value ? LibraryBooleanEnum.TRUE : LibraryBooleanEnum.FALSE)).ToString());
            }

            return metaData;
        }


        private string[] GetPermissions(Dictionary<string, object> permissions, string propertyName, string[] sharedPermission)
        {
            var permission = new string[] { };
            if (permissions?.TryGetValue(propertyName, out object value) == true)
            {
                var currentPermission = value as string[];
                permission = currentPermission.Union(sharedPermission).Distinct().ToArray();
            }
            return permission;
        }

        #endregion

        #region Update Object Artifact

        private async Task<bool> UpdateObjectArtifact(string objectArtifactId, Dictionary<string, object> updates)
        {
            var builder = Builders<BsonDocument>.Filter;
            var filter = builder.Eq("_id", objectArtifactId);

            return await _changeLogService.UpdateChange(nameof(ObjectArtifact), filter, updates);
        }

        #endregion
    }
}