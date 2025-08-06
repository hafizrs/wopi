using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using MongoDB.Bson;
using MongoDB.Driver;
using Microsoft.Extensions.Logging;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.Entities.PrimaryEntities.Dms;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.LibraryModule;
using Newtonsoft.Json;
using Selise.Ecap.Entities;
using Selise.Ecap.Entities.PrimaryEntities.Security;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Infrastructure;


namespace Selise.Ecap.SC.PraxisMonitor.Domain
{
    public class ObjectArtifactFilePermissionService : IObjectArtifactFilePermissionService
    {
        private readonly ILogger<ObjectArtifactFilePermissionService> _logger;
        private readonly IRepository _repository;
        private readonly IObjectArtifactPermissionHelperService _objectArtifactPermissionHelperService;
        private readonly IObjectArtifactUtilityService _objectArtifactUtilityService;
        private readonly IChangeLogService _changeLogService;
        private readonly ISecurityHelperService _securityHelperService;

        public ObjectArtifactFilePermissionService(
            ILogger<ObjectArtifactFilePermissionService> logger,
            IRepository repository,
            IObjectArtifactPermissionHelperService objectArtifactPermissionHelperService,
            IObjectArtifactUtilityService objectArtifactUtilityService,
            IChangeLogService changeLogService,
            ISecurityHelperService securityHelperService)
        {
            _logger = logger;
            _repository = repository;
            _objectArtifactPermissionHelperService = objectArtifactPermissionHelperService;
            _objectArtifactUtilityService = objectArtifactUtilityService;
            _changeLogService = changeLogService;
            _securityHelperService = securityHelperService;
        }

        #region Public methods
        public async Task<bool> SetObjectArtifactFilePermissions(ObjectArtifact objectArtifact, ObjectArtifactEvent eventName)
        {
            var response = false;

            if (objectArtifact != null)
            {
                var updates = PrepareObjectArtifactPermissionModel(objectArtifact, eventName);
                if (updates != null)
                {
                    response = await UpdateObjectArtifact(objectArtifact.ItemId, updates);
                }
                _logger.LogInformation("SetObjectArtifactFilePermissions:: Artifact -> {ObjectArtifact}", JsonConvert.SerializeObject(objectArtifact));
                _logger.LogInformation("SetObjectArtifactFilePermissions:: Updates -> {Updates}", JsonConvert.SerializeObject(updates));
            }

            return response;
        }

        public Dictionary<string, object> PrepareObjectArtifactPermissionModel(ObjectArtifact objectArtifact, ObjectArtifactEvent eventName)
        {
            Dictionary<string, object> updates = null;

            var organization = _objectArtifactUtilityService.GetOrganizationById(objectArtifact.OrganizationId);
            var departmentId = _objectArtifactUtilityService.GetObjectArtifactDepartmentId(objectArtifact.MetaData);
            var department = _objectArtifactUtilityService.GetDepartmentById(departmentId);

            if (organization != null)
            {
                updates =
                    eventName == ObjectArtifactEvent.FILE_UPLOADED ?
                    PreparePendingFilePermissionModel(objectArtifact, department) :
                    eventName == ObjectArtifactEvent.FILE_APPROVED ?
                    PrepareApprovedFilePermissionModel(objectArtifact, department) :
                    eventName == ObjectArtifactEvent.DOCUMENT_DRAFTED ?
                    PrepareDraftedArtifactUpdates(objectArtifact, department) :
                    eventName == ObjectArtifactEvent.FORM_RESPONSE_DRAFTED ?
                    PrepareDraftedFormResponsePermissions(objectArtifact) :
                    eventName == ObjectArtifactEvent.FORM_RESPONSE_SAVED ?
                    PrepareCompletedFormResponsePermissionModel(objectArtifact) :
                    eventName == ObjectArtifactEvent.DRAFTED_DOCUMENT_SAVED ?
                    PrepareSavedDraftedDocumentPermissionModel(objectArtifact, department) :
                    null;

                _logger.LogInformation("organization => {OrganizationId}", organization?.ItemId);
                _logger.LogInformation("department => {DepartmentId}", department?.ItemId);
                _logger.LogInformation("PrepareObjectArtifactPermissionModel updates => {Updates}", JsonConvert.SerializeObject(updates));
            }

            return updates;
        }
        #endregion

        private Dictionary<string, object> PrepareApprovedFilePermissionModel(ObjectArtifact objectArtifact, PraxisClient department)
        {
            Dictionary<string, object> updates = null;

            if (_objectArtifactUtilityService.IsASecretArtifact(objectArtifact?.MetaData))
            {
                updates = PrepareSecretFilePermissions(objectArtifact, department);
            }
            else if (_objectArtifactPermissionHelperService.IsAAdminBUpload(objectArtifact.CreatedBy, objectArtifact.OrganizationId) || department == null)
            {
                updates = PrepareAdminBUploadedApprovedFilePermissions(objectArtifact);
            }
            else if ((_objectArtifactPermissionHelperService.IsALibraryAdminUpload(objectArtifact, objectArtifact.CreatedBy) && 
                _objectArtifactUtilityService.IsAOrgLevelArtifact(objectArtifact.MetaData, objectArtifact.ArtifactType)) || _objectArtifactUtilityService.IsAOrgLevelArtifact(objectArtifact.MetaData, objectArtifact.ArtifactType))
            {
                updates = PrepareLibraryAdminUploadedApprovedFilePermissions(objectArtifact);
            }
            else if (department != null && _objectArtifactPermissionHelperService.IsAPowerUserUpload(objectArtifact.CreatedBy, department.ItemId))
            {
                updates = PreparePowerUserUploadedApprovedFilePermissions(objectArtifact, department);
            }
            else if (department != null)
            {
                updates = PreparePowerUserUploadedApprovedFilePermissions(objectArtifact, department);
            }

            return updates;
        }

        private Dictionary<string, object> PrepareDraftedFormResponsePermissions(ObjectArtifact objectArtifact)
        {
            var idPermission = new string[] { objectArtifact.OwnerId };
            var emptyPermission = new string[] { };

            var updates = new Dictionary<string, object>
            {
                {
                    "RolesAllowedToRead", emptyPermission
                },
                {
                    "IdsAllowedToRead", idPermission
                },
                {
                    "RolesAllowedToUpdate", emptyPermission
                },
                {
                    "IdsAllowedToUpdate", idPermission
                },
                {
                    "RolesAllowedToWrite", emptyPermission
                },
                {
                    "IdsAllowedToWrite", idPermission
                },
                {
                    "RolesAllowedToDelete", emptyPermission
                },
                {
                    "IdsAllowedToDelete", idPermission
                }
            };

            return updates;
        }

        private Dictionary<string, object> PrepareDraftedArtifactUpdates(ObjectArtifact artifact, PraxisClient department)
        {
            Dictionary<string, object> updates = null;
            var approvalPermissions = new Dictionary<string, object>();
            ObjectArtifact parentArtifact = null;
            var originalArtifactIdKey = LibraryModuleConstants.ObjectArtifactMetaDataKeys[ObjectArtifactMetaDataKeyEnum.ORIGINAL_ARTIFACT_ID.ToString()];
            if (artifact != null && artifact.MetaData != null && artifact.MetaData.TryGetValue(originalArtifactIdKey, out MetaValuePair originalArtifactId))
            {
                if (!string.IsNullOrEmpty(originalArtifactId.Value))
                {
                    parentArtifact = _objectArtifactUtilityService.GetObjectArtifactById(originalArtifactId.Value);
                }
            }
            if (parentArtifact != null)
            {
                approvalPermissions = PreparePendingFilePermissionModel(artifact, department);
                updates = new Dictionary<string, object>
                {
                    // {
                    //     nameof(ObjectArtifact.SharedOrganizationList), parentArtifact.SharedOrganizationList
                    // },
                    // {
                    //     nameof(ObjectArtifact.SharedPersonIdList), parentArtifact.SharedPersonIdList
                    // },
                    // {
                    //     nameof(ObjectArtifact.SharedRoleList), parentArtifact.SharedRoleList
                    // },
                    // {
                    //     nameof(ObjectArtifact.SharedUserIdList), parentArtifact.SharedUserIdList
                    // },
                    // {
                    //     nameof(ObjectArtifact.RolesAllowedToRead), parentArtifact.RolesAllowedToRead
                    // },
                    // {
                    //     nameof(ObjectArtifact.IdsAllowedToRead), parentArtifact.IdsAllowedToRead
                    // },
                    // {
                    //     nameof(ObjectArtifact.RolesAllowedToUpdate), parentArtifact.RolesAllowedToUpdate
                    // },
                    // {
                    //     nameof(ObjectArtifact.IdsAllowedToUpdate), parentArtifact.IdsAllowedToUpdate
                    // },
                    // {
                    //     nameof(ObjectArtifact.RolesAllowedToWrite), parentArtifact.RolesAllowedToWrite
                    // },
                    // {
                    //     nameof(ObjectArtifact.IdsAllowedToWrite), parentArtifact.IdsAllowedToWrite
                    // },
                    // {
                    //     nameof(ObjectArtifact.IdsAllowedToDelete), parentArtifact.IdsAllowedToDelete
                    // },
                    // {
                    //     nameof(ObjectArtifact.RolesAllowedToDelete), parentArtifact.RolesAllowedToDelete
                    // }
                };
            }
            return MergePermissionModels(updates, approvalPermissions);
        }

        private Dictionary<string, object> PreparePendingFilePermissionModel(ObjectArtifact objectArtifact, PraxisClient department)
        {
            Dictionary<string, object> updates = null;
            if (_objectArtifactUtilityService.IsASecretArtifact(objectArtifact?.MetaData))
            {
                updates = PrepareSecretFilePermissions(objectArtifact, department);
            }
            else if (_objectArtifactPermissionHelperService.IsAAdminBUpload(objectArtifact.CreatedBy, objectArtifact.OrganizationId) || department == null)
            {
                updates = PrepareAdminBUploadedPendingFilePermissions(objectArtifact);
            }
            else if ((_objectArtifactPermissionHelperService.IsALibraryAdminUpload(objectArtifact, objectArtifact.CreatedBy) 
                && _objectArtifactUtilityService.IsAOrgLevelArtifact(objectArtifact.MetaData, objectArtifact.ArtifactType)) || _objectArtifactUtilityService.IsAOrgLevelArtifact(objectArtifact.MetaData, objectArtifact.ArtifactType))
            {
                updates = PrepareLibraryAdminUploadedPendingFilePermissions(objectArtifact);
            }
            else if (department != null && _objectArtifactPermissionHelperService.IsAPowerUserUpload(objectArtifact.CreatedBy, department.ItemId))
            {
                updates = PreparePowerUserUploadedPendingFilePermissions(objectArtifact, department);
            }
            else if (department != null)
            {
                updates = PreparePowerUserUploadedPendingFilePermissions(objectArtifact, department);
            }

            return updates;
        }

        private Dictionary<string, object> PrepareCompletedFormResponsePermissionModel(ObjectArtifact objectArtifact)
        {
            var originalFormId = _objectArtifactUtilityService.GetOriginalArtifactId(objectArtifact.MetaData);
            var originalFormArtifact = _objectArtifactUtilityService.GetObjectArtifactById(originalFormId);

            var organization = _objectArtifactUtilityService.GetOrganizationById(originalFormArtifact.OrganizationId);
            var departmentId = _objectArtifactUtilityService.GetObjectArtifactDepartmentId(originalFormArtifact.MetaData);
            var department = _objectArtifactUtilityService.GetDepartmentById(departmentId);

            Dictionary<string, object> updates = null;

            if (organization != null)
            {
                updates = PrepareFormsCompletedResponsePermissionModel(originalFormArtifact, department, objectArtifact.OwnerId);
            }

            return updates;
        }

        private Dictionary<string, object> PrepareFormsCompletedResponsePermissionModel(
            ObjectArtifact originalFormArtifact, PraxisClient department, string responseOwnerId)
        {
            Dictionary<string, object> updates = PrepareApprovedFilePermissionModel(originalFormArtifact, department);
            InjectIdsInPermission(updates, new string[] { responseOwnerId });

            return updates;
        }

        private void InjectIdsInPermission(Dictionary<string, object> permissionModel, string[] ids)
        {
            if (permissionModel.TryGetValue(nameof(ObjectArtifact.IdsAllowedToRead), out object idsToRead))
            {
                permissionModel[nameof(ObjectArtifact.IdsAllowedToRead)] = ((string[])idsToRead).Union(ids).Distinct().ToArray();
            }
            if (permissionModel.TryGetValue(nameof(ObjectArtifact.IdsAllowedToUpdate), out object idsToUpdate))
            {
                permissionModel[nameof(ObjectArtifact.IdsAllowedToUpdate)] = ((string[])idsToUpdate).Union(ids).Distinct().ToArray();
            }
            if (permissionModel.TryGetValue(nameof(ObjectArtifact.IdsAllowedToDelete), out object idsToDelete))
            {
                permissionModel[nameof(ObjectArtifact.IdsAllowedToDelete)] = ((string[])idsToDelete).Union(ids).Distinct().ToArray();
            }
        }
        private Dictionary<string, object> PrepareSavedDraftedDocumentPermissionModel(ObjectArtifact objectArtifact, PraxisClient department)
        {
            var originalArtifactIdKey = LibraryModuleConstants.ObjectArtifactMetaDataKeys[ObjectArtifactMetaDataKeyEnum.ORIGINAL_ARTIFACT_ID.ToString()];
            var originalArtifactId = objectArtifact.MetaData[originalArtifactIdKey];
            var parentArtifact = _objectArtifactUtilityService.GetObjectArtifactById(originalArtifactId.Value);
            var approvalPermissions = PreparePendingFilePermissionModel(objectArtifact, department);
            var sharedPermissions = new Dictionary<string, object>();
            if (_securityHelperService.IsAAdminBUser())
            {
                // sharedPermissions = PrepareSavedDraftedDocumentPermissionModelForOrganizationalUser(objectArtifact, parentArtifact);
            }
            else if (_securityHelperService.IsADepartmentLevelUser())
            {
                // sharedPermissions = PrepareSavedDraftedDocumentPermissionModelForDepartmentalUser(objectArtifact, parentArtifact);
            }

            return MergePermissionModels(sharedPermissions, approvalPermissions);
        }
        private Dictionary<string, object> MergePermissionModels(Dictionary<string, object> sharedPermissions, 
            Dictionary<string, object> approvalPermissions)
        {
            var keys = new List<string>
            {
                nameof(ObjectArtifact.IdsAllowedToRead),
                nameof(ObjectArtifact.IdsAllowedToUpdate),
                nameof(ObjectArtifact.IdsAllowedToDelete),
                nameof(ObjectArtifact.IdsAllowedToWrite),
                nameof(ObjectArtifact.RolesAllowedToWrite),
                nameof(ObjectArtifact.RolesAllowedToRead),
                nameof(ObjectArtifact.RolesAllowedToUpdate),
                nameof(ObjectArtifact.RolesAllowedToDelete)
            };

            foreach (var key in keys)
            {
                if (!sharedPermissions.ContainsKey(key) && !approvalPermissions.ContainsKey(key))
                    continue;

                var sharedList = sharedPermissions.TryGetValue(key, out var shared) && shared != null
                    ? (string[])shared
                    : Array.Empty<string>();

                var approvalList = approvalPermissions.TryGetValue(key, out var approval) && approval != null
                    ? (string[])approval
                    : Array.Empty<string>();

                var mergedList = (sharedList ?? new string[] {}).Union((approvalList ?? new string[] { }))?.Where(r => !string.IsNullOrEmpty(r)).Distinct().ToArray();
                sharedPermissions[key] = mergedList;
            }

            return sharedPermissions;
        }


        #region Role wise pending file permission preperation block
        private Dictionary<string, object> PrepareAdminBUploadedPendingFilePermissions(ObjectArtifact artifact)
        {
            var organizationId = artifact.OrganizationId;
            var authorizedRoles = _objectArtifactPermissionHelperService.GetOrganizationLevelObjectArtifactRoles(organizationId);
            var authorizedIds = _objectArtifactPermissionHelperService.GetObjectArtifactAuthorizedIds(artifact);

            var updates = new Dictionary<string, object>
            {
                {
                    "RolesAllowedToRead", authorizedRoles
                },
                {
                    "IdsAllowedToRead", authorizedIds
                },
                {
                    "RolesAllowedToUpdate", authorizedRoles
                },
                {
                    "IdsAllowedToUpdate", authorizedIds
                },
                {
                    "RolesAllowedToWrite", authorizedRoles
                },
                {
                    "IdsAllowedToWrite", authorizedIds
                },
                {
                    "RolesAllowedToDelete", authorizedRoles
                },
                {
                    "IdsAllowedToDelete", authorizedIds
                }
            };

            return updates;
        }

        private Dictionary<string, object> PrepareLibraryAdminUploadedPendingFilePermissions(ObjectArtifact artifact)
        {
            var organizationId = artifact.OrganizationId;
            var authorizedIds = _objectArtifactPermissionHelperService.GetObjectArtifactAuthorizedIds(artifact);

            var updates = new Dictionary<string, object>
            {
                {
                    "RolesAllowedToRead",
                    _objectArtifactPermissionHelperService.GetOrganizationLevelObjectArtifactRoles(organizationId)
                },
                {
                    "IdsAllowedToRead", authorizedIds
                },
                {
                    "RolesAllowedToUpdate",
                    _objectArtifactPermissionHelperService.GetOrganizationLevelObjectArtifactFileApproverRoles(organizationId)
                },
                {
                    "IdsAllowedToUpdate", authorizedIds
                },
                {
                    "RolesAllowedToWrite",
                    _objectArtifactPermissionHelperService.GetOrganizationLevelObjectArtifactFileApproverRoles(organizationId)
                },
                {
                    "IdsAllowedToWrite", authorizedIds
                },
                {
                    "RolesAllowedToDelete",
                    _objectArtifactPermissionHelperService.GetOrganizationLevelObjectArtifactRoles(organizationId)
                },
                {
                    "IdsAllowedToDelete", authorizedIds
                }
            };

            return updates;
        }

        private Dictionary<string, object> PreparePowerUserUploadedPendingFilePermissions(ObjectArtifact artifact, PraxisClient department)
        {
            var organizationId = artifact.OrganizationId;
            var authorizedIds = _objectArtifactPermissionHelperService.GetObjectArtifactAuthorizedIds(artifact);

            var updates = new Dictionary<string, object>
            {
                {
                    "RolesAllowedToRead",
                    _objectArtifactPermissionHelperService.GetDepartmentLevelObjectArtifactRoles(organizationId, department.ItemId)
                },
                {
                    "IdsAllowedToRead", authorizedIds
                },
                {
                    "RolesAllowedToUpdate",
                    _objectArtifactPermissionHelperService.GetDepartmentLevelObjectArtifactFileApproverRoles(organizationId, department.ItemId)
                },
                {
                    "IdsAllowedToUpdate", authorizedIds
                },
                {
                    "RolesAllowedToWrite",
                    _objectArtifactPermissionHelperService.GetDepartmentLevelObjectArtifactFileApproverRoles(organizationId, department.ItemId)
                },
                {
                    "IdsAllowedToWrite", authorizedIds
                },
                {
                    "RolesAllowedToDelete",
                    _objectArtifactPermissionHelperService.GetDepartmentLevelObjectArtifactRemoverRoles(organizationId, department)
                },
                {
                    "IdsAllowedToDelete",
                    authorizedIds
                }
            };

            return updates;
        }

        private Dictionary<string, object> PrepareSecretFilePermissions(ObjectArtifact artifact, PraxisClient department)
        {
            var authorizedIds = _objectArtifactPermissionHelperService.GetObjectArtifactAuthorizedIds(artifact, onlyDeptLevel: true);
            var emptyArray = new string[] { };
            var updates = new Dictionary<string, object>
            {
                {
                    "RolesAllowedToRead", new string[] {RoleNames.Admin}
                },
                {
                    "IdsAllowedToRead", authorizedIds
                },
                {
                    "RolesAllowedToUpdate", emptyArray
                },
                {
                    "IdsAllowedToUpdate", authorizedIds
                },
                {
                    "RolesAllowedToWrite", emptyArray
                },
                {
                    "IdsAllowedToWrite", authorizedIds
                },
                {
                    "RolesAllowedToDelete", new string[] {RoleNames.Admin}
                },
                {
                    "IdsAllowedToDelete", authorizedIds
                }
            };

            return updates;
        }
        
        private Dictionary<string, object> PrepareSavedDraftedDocumentPermissionModelForOrganizationalUser(ObjectArtifact artifact, ObjectArtifact  parentArtifact)
        { 
            return new Dictionary<string, object> 
            {
                {
                    nameof(ObjectArtifact.SharedOrganizationList), parentArtifact.SharedOrganizationList
                },
                {
                    nameof(ObjectArtifact.SharedPersonIdList), parentArtifact.SharedPersonIdList
                },
                {
                    nameof(ObjectArtifact.SharedRoleList), parentArtifact.SharedRoleList
                },
                {
                    nameof(ObjectArtifact.SharedUserIdList), parentArtifact.SharedUserIdList
                },
                {
                    nameof(ObjectArtifact.RolesAllowedToRead), parentArtifact.RolesAllowedToRead
                },
                {
                    nameof(ObjectArtifact.IdsAllowedToRead), parentArtifact.IdsAllowedToRead
                },
                {
                    nameof(ObjectArtifact.RolesAllowedToUpdate), parentArtifact.RolesAllowedToUpdate
                },
                {
                    nameof(ObjectArtifact.IdsAllowedToUpdate), parentArtifact.IdsAllowedToUpdate
                },
                {
                    nameof(ObjectArtifact.RolesAllowedToWrite), parentArtifact.RolesAllowedToWrite
                },
                {
                    nameof(ObjectArtifact.IdsAllowedToWrite), parentArtifact.IdsAllowedToWrite
                },
                {
                    nameof(ObjectArtifact.IdsAllowedToDelete), parentArtifact.IdsAllowedToDelete
                },
                {
                    nameof(ObjectArtifact.RolesAllowedToDelete), parentArtifact.RolesAllowedToDelete
                }
            };
        }

        private Dictionary<string, object> PrepareSavedDraftedDocumentPermissionModelForDepartmentalUser(
            ObjectArtifact artifact, ObjectArtifact parentArtifact)
        {
            var departmentId = _securityHelperService.ExtractDepartmentIdFromDepartmentLevelUser();
            var sharedOrganizationListData = PrepareSharedOrganizationListByDepartment(parentArtifact, departmentId);
            var sharedPermissions = PrepareObjectArtifactPermissions(sharedOrganizationListData);
            var otherSharedData = PrepareSharedObjectArtifactSummary(sharedOrganizationListData, sharedPermissions);
            artifact.IdsAllowedToWrite = (artifact.IdsAllowedToWrite ?? new string[] { })
                                            .Union(parentArtifact?.IdsAllowedToWrite ?? new string[] { })
                                            .Distinct()
                                            .ToArray();
            artifact.RolesAllowedToWrite = (artifact.RolesAllowedToWrite ?? new string[] { })
                                            .Union(parentArtifact?.RolesAllowedToWrite ?? new string[] { })
                                            .Distinct()
                                            .ToArray();
            var updates = new Dictionary<string, object>
            {
                { nameof(ObjectArtifact.SharedOrganizationList), sharedOrganizationListData },
                { nameof(ObjectArtifact.SharedUserIdList), otherSharedData.SharedUserIdList },
                { nameof(ObjectArtifact.SharedPersonIdList), otherSharedData.SharedPersonIdList },
                { nameof(ObjectArtifact.SharedRoleList), otherSharedData.SharedRoleList },
                { nameof(ObjectArtifact.RolesAllowedToRead), sharedPermissions.RolesAllowedToRead },
                { nameof(ObjectArtifact.IdsAllowedToRead), sharedPermissions.IdsAllowedToRead },
                { nameof(ObjectArtifact.RolesAllowedToUpdate), sharedPermissions.RolesAllowedToUpdate },
                { nameof(ObjectArtifact.IdsAllowedToUpdate), sharedPermissions.IdsAllowedToUpdate },
                { nameof(ObjectArtifact.RolesAllowedToWrite), artifact.RolesAllowedToWrite },
                { nameof(ObjectArtifact.IdsAllowedToWrite), artifact.IdsAllowedToWrite },
                { nameof(ObjectArtifact.IdsAllowedToDelete), sharedPermissions.IdsAllowedToDelete },
                { nameof(ObjectArtifact.RolesAllowedToDelete), sharedPermissions.RolesAllowedToDelete }
            };

            return updates;
        }

        private List<SharedOrganizationInfo> PrepareSharedOrganizationListByDepartment(ObjectArtifact parentArtifact, string departmentId)
        {
            var sharedOrganizationList = parentArtifact.SharedOrganizationList;

            if (!(sharedOrganizationList?.Exists(sol => sol.OrganizationId == parentArtifact.OrganizationId) ?? false))
            {
                return sharedOrganizationList?.Where(sol => sol.OrganizationId == departmentId).ToList()
                       ?? new List<SharedOrganizationInfo>();
            }

            var tags = _objectArtifactUtilityService.IsAForm(parentArtifact.MetaData)
                ? new[] { RoleNames.PowerUser, RoleNames.Leitung, RoleNames.MpaGroup1, RoleNames.MpaGroup2 }
                : new[] { RoleNames.PowerUser, RoleNames.Leitung };

            var sharedList = new List<SharedOrganizationInfo>
            {
                new()
                {
                    FeatureName = "update",
                    OrganizationId = departmentId,
                    SharedPersonList = new List<string>(),
                    Tags = tags
                }
            };

            if (!_objectArtifactUtilityService.IsAForm(parentArtifact.MetaData))
            {
                sharedList.Add(new SharedOrganizationInfo
                {
                    FeatureName = "read",
                    OrganizationId = departmentId,
                    SharedPersonList = new List<string>(),
                    Tags = new[] { RoleNames.MpaGroup1, RoleNames.MpaGroup2 }
                });
            }

            return sharedList;
        }

        private ObjectArtifact PrepareObjectArtifactPermissions(List<SharedOrganizationInfo> sharedOrganizationList)
        {
            var objectArtifactPermissions = new ObjectArtifact()
            {
                IdsAllowedToRead = GetSharedIdsAllowedToRead(sharedOrganizationList),
                RolesAllowedToRead = GetSharedRolesAllowedToRead(sharedOrganizationList),
                IdsAllowedToUpdate = GetSharedIdsAllowedToUpdate(sharedOrganizationList),
                RolesAllowedToUpdate = GetSharedRolesAllowedToUpdate(sharedOrganizationList),
                IdsAllowedToDelete = Array.Empty<string>(),
                RolesAllowedToDelete = Array.Empty<string>()
            };

            return objectArtifactPermissions;
        }
        private string[] GetSharedIdsAllowedToRead(List<SharedOrganizationInfo> sharedOrganizationList)
        {
            var sharedPersonIdList = sharedOrganizationList.SelectMany(s => s.SharedPersonList).Distinct().ToArray();
            var userIds = _objectArtifactUtilityService.GetPraxisUsersByIds(sharedPersonIdList).Select(pu => pu.UserId).ToArray();
            return userIds;
        }

        private string[] GetSharedRolesAllowedToRead(List<SharedOrganizationInfo> sharedOrganizationList)
        {
            var groupedSharedRoles = sharedOrganizationList
                .GroupBy(i => i.OrganizationId)
                .Select(g => new { OrganizationId = g.Key, Roles = g.ToList().SelectMany(gi => gi.Tags).ToArray() })
                .ToList();

            var roles = new List<string>();

            foreach (var group in groupedSharedRoles)
            {
                foreach (var role in group.Roles)
                {
                    var dynamicRole = $"{LibraryModuleConstants.StaticRoleDynamicRolePrefixMap[role]}_{group.OrganizationId}";
                    roles.Add(dynamicRole);
                }
            }

            return roles.Distinct().ToArray();
        }

        private string[] GetSharedIdsAllowedToUpdate(List<SharedOrganizationInfo> sharedOrganizationList)
        {
            var sharedPersonIdList = sharedOrganizationList.Where(s => s.FeatureName == "update").SelectMany(s => s.SharedPersonList).Distinct().ToArray();
            var userIds = _objectArtifactUtilityService.GetPraxisUsersByIds(sharedPersonIdList).Select(pu => pu.UserId).ToArray();
            return userIds;
        }

        private string[] GetSharedRolesAllowedToUpdate(List<SharedOrganizationInfo> sharedOrganizationList)
        {
            var groupedSharedRoles = sharedOrganizationList
                .Where(i => i.FeatureName == "update")
                .GroupBy(i => i.OrganizationId)
                .Select(g => new { OrganizationId = g.Key, Roles = g.ToList().SelectMany(gi => gi.Tags).ToArray() })
                .ToList();

            var roles = new List<string>();

            foreach (var group in groupedSharedRoles)
            {
                foreach (var staticRole in group.Roles)
                {
                    var dynamicRole = $"{LibraryModuleConstants.StaticRoleDynamicRolePrefixMap[staticRole]}_{group.OrganizationId}";
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

        #region Role wise approved file permission preperation block

        private Dictionary<string, object> PrepareAdminBUploadedApprovedFilePermissions(ObjectArtifact artifact)
        {
            var organizationId = artifact.OrganizationId;
            var authorizedRoles = _objectArtifactPermissionHelperService.GetOrganizationLevelObjectArtifactRoles(organizationId);
            var authorizedIds = _objectArtifactPermissionHelperService.GetObjectArtifactAuthorizedIds(artifact);

            var updates = new Dictionary<string, object>
            {
                {
                    "RolesAllowedToRead", authorizedRoles
                },
                {
                    "IdsAllowedToRead", authorizedIds
                },
                {
                    "RolesAllowedToUpdate", authorizedRoles
                },
                {
                    "IdsAllowedToUpdate", authorizedIds
                },
                {
                    "RolesAllowedToWrite", authorizedRoles
                },
                {
                    "IdsAllowedToWrite", authorizedIds
                },
                {
                    "RolesAllowedToDelete", authorizedRoles
                },
                {
                    "IdsAllowedToDelete", authorizedIds
                }
            };

            return updates;
        }

        private Dictionary<string, object> PrepareLibraryAdminUploadedApprovedFilePermissions(ObjectArtifact artifact)
        {
            var organizationId = artifact.OrganizationId;
            var authorizedRoles = _objectArtifactPermissionHelperService.GetOrganizationLevelObjectArtifactRoles(organizationId);
            var authorizedIds = _objectArtifactPermissionHelperService.GetObjectArtifactAuthorizedIds(artifact);

            var updates = new Dictionary<string, object>
            {
                {
                    "RolesAllowedToRead", authorizedRoles
                },
                {
                    "IdsAllowedToRead", authorizedIds
                },
                {
                    "RolesAllowedToUpdate", authorizedRoles
                },
                {
                    "IdsAllowedToUpdate", authorizedIds
                },
                {
                    "RolesAllowedToWrite", authorizedRoles
                },
                {
                    "IdsAllowedToWrite", authorizedIds
                },
                {
                    "RolesAllowedToDelete", authorizedRoles
                },
                {
                    "IdsAllowedToDelete", authorizedIds
                }
            };

            return updates;
        }

        private Dictionary<string, object> PreparePowerUserUploadedApprovedFilePermissions(ObjectArtifact artifact, PraxisClient department)
        {
            var organizationId = artifact.OrganizationId;
            var authorizedRoles = _objectArtifactPermissionHelperService.GetDepartmentLevelObjectArtifactRoles(organizationId, department.ItemId);
            var authorizedIds = _objectArtifactPermissionHelperService.GetObjectArtifactAuthorizedIds(artifact);

            var updates = new Dictionary<string, object>
            {
                {
                    "RolesAllowedToRead", authorizedRoles
                },
                {
                    "IdsAllowedToRead", authorizedIds
                },
                {
                    "RolesAllowedToUpdate", authorizedRoles
                },
                {
                    "IdsAllowedToUpdate", authorizedIds
                },
                {
                    "RolesAllowedToWrite", authorizedRoles
                },
                {
                    "IdsAllowedToWrite", authorizedIds
                },
                {
                    "RolesAllowedToDelete",
                    _objectArtifactPermissionHelperService.GetDepartmentLevelObjectArtifactRemoverRoles(organizationId, department)
                },
                {
                    "IdsAllowedToDelete", authorizedIds
                }
            };

            return updates;
        }

        #endregion

        private async Task<bool> UpdateObjectArtifact(string objectArtifactId, Dictionary<string, object> updates)
        {
            var builder = Builders<BsonDocument>.Filter;
            var filter = builder.Eq("_id", objectArtifactId);

            return await _changeLogService.UpdateChange(nameof(ObjectArtifact), filter, updates);
        }
    }
}