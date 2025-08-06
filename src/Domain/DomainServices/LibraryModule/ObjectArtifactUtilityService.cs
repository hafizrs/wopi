using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.LibraryModule;
using System.Collections.Generic;
using Selise.Ecap.Entities.PrimaryEntities.Dms;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using System.Linq;
using System;
using Selise.Ecap.Entities.PrimaryEntities.Security;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Entities.LibraryModule;
using System.Linq.Expressions;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Infrastructure;
using Selise.Ecap.Entities;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using Enum = System.Enum;

namespace Selise.Ecap.SC.PraxisMonitor.Domain
{
    public class ObjectArtifactUtilityService : IObjectArtifactUtilityService
    {
        private readonly IRepository _repository;
        private readonly ISecurityContextProvider _securityContextProvider;
        private readonly ISecurityHelperService _securityHelperService;
        private readonly IChangeLogService _changeLogService;
        private readonly IBlocksMongoDbDataContextProvider _ecapMongoDbDataContextProvider;

        public ObjectArtifactUtilityService(
            IRepository repository,
            ISecurityContextProvider securityContextProvider,
            ISecurityHelperService securityHelperService,
            IChangeLogService changeLogService,
            IBlocksMongoDbDataContextProvider ecapMongoDbDataContextProvider)
        {
            _repository = repository;
            _securityContextProvider = securityContextProvider;
            _securityHelperService = securityHelperService;
            _changeLogService = changeLogService;
            _ecapMongoDbDataContextProvider = ecapMongoDbDataContextProvider;
        }

        public string GetLibraryViewModeKey(string type)
        {
            return LibraryModuleConstants.LibraryViewModeList.Find(v => v.Value == type).Key;
        }

        public string GetLibraryViewModeName(string type)
        {
            return LibraryModuleConstants.LibraryViewModeList.Find(v => v.Key == type).Value;
        }

        public ObjectArtifact GetObjectArtifactById(string id)
        {
            ObjectArtifact artifact = null;
            if (!string.IsNullOrWhiteSpace(id))
            {
                artifact = _repository.GetItem<ObjectArtifact>(o => o.ItemId == id && !o.IsMarkedToDelete);
            }
            return artifact;
        }

        public bool IsADeletedObjectArtifact(string objectArtifactId)
        {
            return _repository.GetItem<ObjectArtifact>(o => o.ItemId == objectArtifactId) == null;
        }

        public List<ObjectArtifact> GetObjectArtifacts(string[] objectArtifactIds)
        {
            return _repository.GetItems<ObjectArtifact>(o => objectArtifactIds.Contains(o.ItemId))?.ToList() ?? new List<ObjectArtifact>();
        }

        public List<ObjectArtifact> GetFileObjectArtifacts(string[] objectArtifactIds)
        {
            return _repository.GetItems<ObjectArtifact>(o => objectArtifactIds.Contains(o.ItemId) && o.ArtifactType == ArtifactTypeEnum.File)?.ToList() ?? new List<ObjectArtifact>();
        }

        public List<ObjectArtifact> GetFolderObjectArtifacts(string[] objectArtifactIds)
        {
            return _repository.GetItems<ObjectArtifact>(o => objectArtifactIds.Contains(o.ItemId) && o.ArtifactType == ArtifactTypeEnum.Folder)?.ToList() ?? new List<ObjectArtifact>();
        }

        public ObjectArtifact GetObjectArtifactSecuredById(string id)
        {
            ObjectArtifact artifact = null;
            if (!string.IsNullOrWhiteSpace(id))
            {
                var securityContext = _securityContextProvider.GetSecurityContext();
                artifact =
                    _repository.GetItem<ObjectArtifact>(o =>
                    o.ItemId == id &&
                    !o.IsMarkedToDelete &&
                    (o.RolesAllowedToRead.Any(r => securityContext.Roles.Contains(r)) || o.IdsAllowedToRead.Contains(securityContext.UserId)));
            }
            return artifact;
        }

        public ObjectArtifact GetEditableObjectArtifactById(string id)
        {
            ObjectArtifact artifact = null;
            if (!string.IsNullOrWhiteSpace(id))
            {
                var securityContext = _securityContextProvider.GetSecurityContext();
                artifact =
                    _repository.GetItem<ObjectArtifact>(o =>
                    o.ItemId == id &&
                    !o.IsMarkedToDelete &&
                    (
                        o.RolesAllowedToUpdate.Any(r => securityContext.Roles.Contains(r)) || o.IdsAllowedToUpdate.Contains(securityContext.UserId)
                        || (o.RolesAllowedToWrite != null && o.RolesAllowedToWrite.Any(r => securityContext.Roles.Contains(r)))
                        || (o.IdsAllowedToWrite != null && o.IdsAllowedToWrite.Contains(securityContext.UserId))
                    ));
            }
            return artifact;
        }

        public ObjectArtifact GetWritableObjectArtifactById(string id)
        {
            ObjectArtifact artifact = null;
            if (!string.IsNullOrWhiteSpace(id))
            {
                var securityContext = _securityContextProvider.GetSecurityContext();
                artifact =
                    _repository.GetItem<ObjectArtifact>(o =>
                    o.ItemId == id &&
                    !o.IsMarkedToDelete &&
                    (
                        (o.RolesAllowedToWrite != null && o.RolesAllowedToWrite.Any(r => securityContext.Roles.Contains(r)))
                        || (o.IdsAllowedToWrite != null && o.IdsAllowedToWrite.Contains(securityContext.UserId))
                    ));
            }
            return artifact;
        }

        public List<ObjectArtifact> GetObjectArtifactsByParentId(string parentId)
        {
            var artifacts = new List<ObjectArtifact>();
            if (!string.IsNullOrWhiteSpace(parentId))
            {
                artifacts = _repository.GetItems<ObjectArtifact>
                                    (o => !o.IsMarkedToDelete && o.ParentId == parentId)?
                                    .ToList() ?? new List<ObjectArtifact>();
            }
            return artifacts;
        }

        public List<ObjectArtifact> GetObjectArtifactNames(string[] ids)
        {
            return
                _repository.GetItems<ObjectArtifact>(ar => ids.Contains(ar.ItemId))?.
                Select(ar => new ObjectArtifact()
                {
                    ItemId = ar.ItemId,
                    Name = ar.Name
                })?.ToList() ?? new List<ObjectArtifact>();
        }

        public List<ObjectArtifact> GetOrganizationObjectArtifacts(string organizationId)
        {
            return _repository.GetItems<ObjectArtifact>(o => o.OrganizationId == organizationId && !o.IsMarkedToDelete)?.ToList() ?? new List<ObjectArtifact>();
        }

        public RiqsLibraryControlMechanism GetOrganizationLibraryControlMechanism(string organizationId)
        {
            return _repository.GetItem<RiqsLibraryControlMechanism>(m => m.OrganizationId == organizationId) ?? new RiqsLibraryControlMechanism() { };
        }

        public RiqsLibraryControlMechanism GetOrganizationLibraryControlMechanismForDept(string departmentId)
        {
            return _repository.GetItem<RiqsLibraryControlMechanism>(m => m.DepartmentId == departmentId) ?? new RiqsLibraryControlMechanism() { };
        }

        public async Task SetObjectArtifactExtension(ObjectArtifact artifact)
        {
            var extension = LibraryModuleFileFormats.GetFileExtension(artifact.Name);
            if (artifact.MetaData == null || string.IsNullOrWhiteSpace(extension))
            {
                return;
            }

            artifact.Extension = extension;
            var fileFormats = LibraryModuleFileFormats.GetFileFormat(artifact.Extension);
            var fileFormatMetaData = new MetaValuePair
            { Type = "string", Value = ((int)fileFormats).ToString() };

            var fileFormatKey = LibraryModuleConstants.ObjectArtifactMetaDataKeys[
                    ObjectArtifactMetaDataKeyEnum.FILE_TYPE.ToString()];

            if (!artifact.MetaData.ContainsKey(fileFormatKey))
            {
                artifact.MetaData.Add(fileFormatKey, fileFormatMetaData);
            }

            var builder = Builders<BsonDocument>.Filter;
            var updateFilters = builder.Eq("_id", artifact.ItemId);

            var updates = new Dictionary<string, object>
            {
                {
                    "MetaData", artifact.MetaData
                },
                {
                    "Extension", extension
                }
            };

            await _changeLogService.UpdateChange(nameof(ObjectArtifact), updateFilters, updates);
        }

        public async Task SetMetaDataProperties(string artifactId)
        {
            var artifact = GetObjectArtifactById(artifactId);
            if (artifact?.MetaData == null)
            {
                return;
            }

            bool isChanged = false;

            if (!string.IsNullOrEmpty(artifact.ParentId) && !IsASecretArtifact(artifact.MetaData)) 
            {
                var parentArtifact = GetObjectArtifactById(artifact.ParentId);
                if (IsASecretArtifact(parentArtifact?.MetaData))
                {
                    isChanged = true;
                    var isSecretKey = LibraryModuleConstants.ObjectArtifactMetaDataKeys[ObjectArtifactMetaDataKeyEnum.IS_SECRET_ARTIFACT.ToString()];
                    artifact.MetaData.Add
                    (
                        isSecretKey,
                        new MetaValuePair { Type = "string", Value = ((int)LibraryBooleanEnum.TRUE).ToString() }
                    );
                }
            }

            if (isChanged)
            {
                var builder = Builders<BsonDocument>.Filter;
                var updateFilters = builder.Eq("_id", artifact.ItemId);

                var updates = new Dictionary<string, object>
                {
                    { "MetaData", artifact.MetaData }
                };

                await _changeLogService.UpdateChange(nameof(ObjectArtifact), updateFilters, updates);
            }
        }

        public PraxisOrganization GetOrganizationById(string id)
        {
            PraxisOrganization organization = null;
            if (!string.IsNullOrWhiteSpace(id))
            {
                organization = _repository.GetItem<PraxisOrganization>(x => x.ItemId == id);
            }
            return organization;
        }

        public PraxisClient GetDepartmentById(string id)
        {
            PraxisClient department = null;
            if (!string.IsNullOrWhiteSpace(id))
            {
                department = _repository.GetItem<PraxisClient>(x => x.ItemId == id);
            }
            return department;
        }

        public List<string> GetDepartmentIds(string organizationId)
        {
            var ids = new List<string>();
            if (!string.IsNullOrWhiteSpace(organizationId))
            {
                ids =
                    _repository.GetItems<PraxisClient>(pc => pc.ParentOrganizationId == organizationId && !pc.IsMarkedToDelete)?
                    .Select(pc => pc.ItemId)?
                    .ToList() ?? new List<string>();
            }
            return ids;
        }

        public List<PraxisUser> GetOrganizationAdminBPraxisUsers(string organizationId)
        {
            var adminBDynamicRole = _securityHelperService.GenerateOrganizationAdminBRole(organizationId);
            return
                _repository.GetItems<PraxisUser>(pu => !pu.IsMarkedToDelete && pu.Active && pu.Roles.Contains(adminBDynamicRole) && !pu.Roles.Contains(RoleNames.GroupAdmin))?.
                Select(pu => new PraxisUser()
                {
                    ItemId = pu.ItemId,
                    UserId = pu.UserId,
                    DisplayName = pu.DisplayName,
                    Roles = pu.Roles ?? new List<string>()
                })?.ToList() ?? new List<PraxisUser>();
        }

        public List<PraxisUser> GetPraxisUsersByIds(string[] ids)
        {
            return
                _repository.GetItems<PraxisUser>(pu => ids.Contains(pu.ItemId))?.
                Select(pu => new PraxisUser()
                {
                    ItemId = pu.ItemId,
                    UserId = pu.UserId,
                    DisplayName = pu.DisplayName,
                    Roles = pu.Roles ?? new List<string>()
                })?.ToList() ?? new List<PraxisUser>();
        }

        public PraxisUser GetPraxisUserById(string id)
        {
            return _repository.GetItem<PraxisUser>(pu => pu.ItemId == id) ?? new PraxisUser();
        }

        public List<PraxisUser> GetPraxisUsersByUserIds(string[] userIds)
        {
            return
                _repository.GetItems<PraxisUser>(pu => userIds.Contains(pu.UserId)).
                Select(pu => new PraxisUser()
                {
                    ItemId = pu.ItemId,
                    UserId = pu.UserId,
                    DisplayName = pu.DisplayName,
                    Roles = pu.Roles ?? new List<string>()
                }).ToList() ?? new List<PraxisUser>();
        }

        public PraxisUser GetPraxisUserByUserId(string userId)
        {
            return _repository.GetItem<PraxisUser>(pu => pu.UserId == userId) ?? new PraxisUser();
        }

        public List<User> GetUsersByIds(string[] ids)
        {
            return
                _repository.GetItems<User>(u => ids.Contains(u.ItemId)).
                Select(u => new User()
                {
                    ItemId = u.ItemId,
                    Roles = u.Roles
                }).ToList();
        }

        public List<PraxisUser> GetPraxisUsersByOrganizationId(string id)
        {
            return
                _repository.GetItems<PraxisUser>(pu => pu.ClientList.Any(c => c.ParentOrganizationId == id && !c.Roles.Contains($"{RoleNames.AdminB}")) && !pu.Roles.Contains(RoleNames.GroupAdmin))?
                .Select(pu => new PraxisUser()
                {
                    ItemId = pu.ItemId,
                    DisplayName = pu.DisplayName,
                    ClientList = pu.ClientList,
                    Roles = pu.Roles ?? new List<string>()
                })?.ToList();
        }

        public List<PraxisUser> GetPraxisUsersByIdsOrRoles(string[] includingIds, string[] roles = null, string[] excludingIds = null, bool isExcludeAdminB = true, List<PraxisUser> praxisUsers = null, bool hideGroupAdmin = true)
        {
            roles ??= new string[] { };
            excludingIds ??= new string[] { };

            var hideRoles = new List<string>() { };
            if (isExcludeAdminB) hideRoles.Add(RoleNames.AdminB);
            if (hideGroupAdmin) hideRoles.Add(RoleNames.GroupAdmin);


            Expression<Func<PraxisUser, bool>> filter =
                pu => !excludingIds.Contains(pu.ItemId) && pu.Roles != null &&
                !pu.Roles.Any(r => hideRoles.Contains(r)) && !pu.IsMarkedToDelete &&
                (includingIds.Contains(pu.ItemId) || pu.Roles.Any(r => roles.Contains(r)));

            if (praxisUsers != null)
            {
                var predicate = filter.Compile();
                return praxisUsers.Where(predicate)?
                        .Select(pu => new PraxisUser()
                        {
                            ItemId = pu.ItemId,
                            DisplayName = pu.DisplayName,
                            ClientList = pu.ClientList,
                            Roles = pu.Roles ?? new List<string>(),
                            Image = pu.Image
                        })?
                        .ToList();
            }

            return _repository.GetItems(filter)?
                    .Select(pu => new PraxisUser()
                    {
                        ItemId = pu.ItemId,
                        DisplayName = pu.DisplayName,
                        ClientList = pu.ClientList,
                        Roles = pu.Roles ?? new List<string>(),
                        Image = pu.Image
                    })?
                    .ToList();
        }

        public List<PraxisUser> GetDepartmentWiseAssignees(string departmentId, string[] roles, string[] ids)
        {
            return
                _repository.GetItems<PraxisUser>(
                    pu => ids.Contains(pu.ItemId) ||
                    (pu.ClientList.Any(c => c.ClientId == departmentId && !c.Roles.Contains($"{RoleNames.AdminB}") && !c.Roles.Contains(RoleNames.GroupAdmin) && c.Roles.Any(r => roles.Contains(r)))))?
                .Select(pu => new PraxisUser()
                {
                    ItemId = pu.ItemId,
                    DisplayName = pu.DisplayName,
                    Roles = pu.Roles ?? new List<string>()
                })?.ToList();
        }

        public bool IsAAuditSaveDepartMent(PraxisClient department)
        {
            return department.IsOpenOrganization.Value;
        }

        public List<RiqsActivitySummaryModel> GetFormCompletionSummary(string objectArtifactId, List<RiqsObjectArtifactMapping> artifactMappingDatas = null)
        {
            return RiqsObjectArtifactMappingConstant.GetRiqsObjectArtifactMappingByArtifactId(objectArtifactId, artifactMappingDatas)?.FormCompletionSummary;
        }

        public string GetObjectArtifactDepartmentId(IDictionary<string, MetaValuePair> metaData)
        {
            var departmentIdKey = LibraryModuleConstants.ObjectArtifactMetaDataKeys[$"{ObjectArtifactMetaDataKeyEnum.DEPARTMENT_ID}"];
            var departmentId = string.Empty;
            if (metaData != null && metaData.TryGetValue(departmentIdKey, out MetaValuePair departmentIdValue))
            {
                departmentId = departmentIdValue.Value;
            }

            return departmentId;
        }

        public string[] GetDepartmentDynamicRolesFromStaticRoles(string[] staticRoles, string departmentId)
        {
            var dynamicRoles = new List<string> { };

            foreach (var role in staticRoles)
            {
                if (role == RoleNames.PowerUser)
                {
                    dynamicRoles.Add($"{RoleNames.PowerUser_Dynamic}_{departmentId}");
                }
                else if (role == RoleNames.Leitung)
                {
                    dynamicRoles.Add($"{RoleNames.Leitung_Dynamic}_{departmentId}");
                }
                else
                {
                    dynamicRoles.Add($"{RoleNames.MpaGroup_Dynamic}_{departmentId}");
                }
            }

            return dynamicRoles.Distinct().ToArray();
        }

        public string[] GetDepartmentStaticRolesFromDynamicRoles(string[] dynamicRoles, string departmentId)
        {
            var staticRoles = new List<string> { };

            foreach (var role in dynamicRoles)
            {
                if (role == $"{RoleNames.PowerUser_Dynamic}_{departmentId}")
                {
                    staticRoles.Add(RoleNames.PowerUser);
                }
                else if (role == $"{RoleNames.Leitung_Dynamic}_{departmentId}")
                {
                    staticRoles.Add(RoleNames.Leitung);
                }
                else
                {
                    staticRoles.Add(RoleNames.MpaGroup1);
                    staticRoles.Add(RoleNames.MpaGroup2);
                }
            }

            return staticRoles.ToArray();
        }

        public string GetMetaDataValueByKey(IDictionary<string, MetaValuePair> metaData, string key)
        {
            string value = null;

            metaData ??= new Dictionary<string, MetaValuePair>() { };
            if (metaData.TryGetValue(key, out MetaValuePair mataValue))
            {
                value = mataValue.Value;
            }

            return value;
        }

        public bool IsADraftedFormResponse(IDictionary<string, MetaValuePair> metaData)
        {
            return
                IsAForm(metaData) &&
                !IsAOriginalArtifact(metaData) &&
                GetFormFillStatus(metaData) == $"{(int)FormFillStatus.DRAFT}";
        }

        public bool IsACompletedFormResponse(IDictionary<string, MetaValuePair> metaData)
        {
            return
                IsAForm(metaData) &&
                !IsAOriginalArtifact(metaData) &&
                GetFormFillStatus(metaData) == $"{(int)FormFillStatus.COMPLETE}";
        }

        public bool IsAPendingSignatureFormResponse(IDictionary<string, MetaValuePair> metaData)
        {
            return
                IsAForm(metaData) &&
                !IsAOriginalArtifact(metaData) &&
                GetFormFillStatus(metaData) == $"{(int)FormFillStatus.PENDING_SIGNATURE}";
        }

        public bool IsAGeneralForm(IDictionary<string, MetaValuePair> metaData)
        {
            return
                IsAForm(metaData) &&
                IsAOriginalArtifact(metaData) &&
                GetFormType(metaData) == $"{(int)LibraryFormTypeEnum.GENERAL}";
        }

        public bool IsACompletedGeneralFormResponse(IDictionary<string, MetaValuePair> metaData)
        {
            return
                IsAForm(metaData) &&
                !IsAOriginalArtifact(metaData) &&
                GetFormType(metaData) == $"{(int)LibraryFormTypeEnum.GENERAL}" &&
                GetFormFillStatus(metaData) == $"{(int)FormFillStatus.COMPLETE}";
        }

        public bool IsAForm(IDictionary<string, MetaValuePair> metaData)
        {
            var keyName = LibraryModuleConstants.ObjectArtifactMetaDataKeys[$"{ObjectArtifactMetaDataKeyEnum.FILE_TYPE}"];
            string value = GetMetaDataValueByKey(metaData, keyName);
            return value == $"{(int)LibraryFileTypeEnum.FORM}";
        }

        public bool IsAOriginalForm(IDictionary<string, MetaValuePair> metaData)
        {
            return IsAForm(metaData) && IsAOriginalArtifact(metaData);
        }

        public bool IsAOriginalArtifact(IDictionary<string, MetaValuePair> metaData)
        {
            var keyName = LibraryModuleConstants.ObjectArtifactMetaDataKeys[$"{ObjectArtifactMetaDataKeyEnum.IS_A_ORIGINAL_ARTIFACT}"];
            string value = GetMetaDataValueByKey(metaData, keyName);
            return value == $"{(int)LibraryBooleanEnum.TRUE}";
        }

        public string GetOriginalArtifactId(IDictionary<string, MetaValuePair> metaData, bool forOriginalArtiafact = false)
        {
            if (forOriginalArtiafact || !IsAOriginalArtifact(metaData))
            {
                var keyName = LibraryModuleConstants.ObjectArtifactMetaDataKeys[$"{ObjectArtifactMetaDataKeyEnum.ORIGINAL_ARTIFACT_ID}"];
                return GetMetaDataValueByKey(metaData, keyName);
            }

            return null;
        }

        public string GetFormType(IDictionary<string, MetaValuePair> metaData)
        {
            var formTypeKey = LibraryModuleConstants.ObjectArtifactMetaDataKeys[$"{ObjectArtifactMetaDataKeyEnum.FORM_TYPE}"];
            string formtype = GetMetaDataValueByKey(metaData, formTypeKey);
            return formtype;
        }

        public List<string> GetPreviousApproverIdsByInterval(ObjectArtifact artifact, RiqsObjectArtifactMapping mappingData = null)
        {
            if (artifact == null) return new List<string>();

            var nextIntervalDate = DateTime.MinValue;
            var reapproveProcessStartDateKey = LibraryModuleConstants.ObjectArtifactMetaDataKeys[ObjectArtifactMetaDataKeyEnum.REAPPROVE_PROCESS_START_DATE.ToString()];

            var reapproveProcessStartDate = GetMetaDataValueByKey(artifact.MetaData, reapproveProcessStartDateKey);
            if (DateTime.TryParse(reapproveProcessStartDate, out DateTime dateTime))
            {
                nextIntervalDate = dateTime.ToUniversalTime();
            }

            if (string.IsNullOrEmpty(mappingData?.ItemId)) mappingData = RiqsObjectArtifactMappingConstant.GetRiqsObjectArtifactMappingByArtifactId(artifact.ItemId);

            var previousApproverIds = mappingData?.ApproverInfos?
                    .Where(a => a.ApprovedDate > nextIntervalDate)?.Select(a => a.ApproverId)?.ToList() ?? new List<string>();

            return previousApproverIds;
        }

        public bool IsADocument(IDictionary<string, MetaValuePair> metaData, bool isDraft)
        {
            if (metaData == null) return false;
            var fileTypeKey = LibraryModuleConstants.ObjectArtifactMetaDataKeys[ObjectArtifactMetaDataKeyEnum.FILE_TYPE.ToString()];
            var isDraftKey = LibraryModuleConstants.ObjectArtifactMetaDataKeys[ObjectArtifactMetaDataKeyEnum.IS_DRAFT.ToString()];
            var isDocumentType = metaData.TryGetValue(fileTypeKey, out MetaValuePair fileType) &&
                                    fileType?.Value == ((int)LibraryFileTypeEnum.DOCUMENT).ToString();

            if (!isDraft) return isDocumentType;

            var _isDraft = metaData.TryGetValue(isDraftKey, out MetaValuePair _) && isDocumentType;
            return _isDraft;
        }

        public bool IsADocument(IDictionary<string, MetaValuePair> metaData)
        {
            var keyName = LibraryModuleConstants.ObjectArtifactMetaDataKeys[$"{ObjectArtifactMetaDataKeyEnum.FILE_TYPE}"];
            string value = GetMetaDataValueByKey(metaData, keyName);
            return value == $"{(int)LibraryFileTypeEnum.DOCUMENT}";
        }

        public bool IsADraftedArtifact(IDictionary<string, MetaValuePair> metaData)
        {
            var keyName = LibraryModuleConstants.ObjectArtifactMetaDataKeys[$"{ObjectArtifactMetaDataKeyEnum.IS_DRAFT}"];
            string value = GetMetaDataValueByKey(metaData, keyName);
            return value == $"{(int)LibraryBooleanEnum.TRUE}";
        }

        public bool IsADrraftedDocument(IDictionary<string, MetaValuePair> metaData)
        {
            return IsADocument(metaData) && IsADraftedArtifact(metaData);
        }

        public bool IsADeletedArtifact(ObjectArtifact artifact)
        {
            return artifact == null || artifact.IsMarkedToDelete;
        }

        public bool IsASecretArtifact(IDictionary<string, MetaValuePair> metaData)
        {
            if (metaData == null) return false;
            var isSecretKey = LibraryModuleConstants.ObjectArtifactMetaDataKeys[ObjectArtifactMetaDataKeyEnum.IS_SECRET_ARTIFACT.ToString()];

            var isSecret= metaData.TryGetValue(isSecretKey, out MetaValuePair secretValue) && secretValue?.Value == ((int)LibraryBooleanEnum.TRUE).ToString();
            return isSecret;
        }

        public bool CanReadObjectArtifact(ObjectArtifact artifact)
        {
            var securityContext = _securityContextProvider.GetSecurityContext();
            return
                (artifact?.RolesAllowedToRead?.Count() > 0 && artifact.RolesAllowedToRead.Any(r => securityContext.Roles.Contains(r))) ||
                (artifact?.IdsAllowedToRead?.Count() > 0 && artifact.IdsAllowedToRead.Contains(securityContext.UserId));
        }

        public string GetFileTypeName(IDictionary<string, MetaValuePair> metaData)
        {
            var fileTypeName = string.Empty;
            var keyName = LibraryModuleConstants.ObjectArtifactMetaDataKeys[$"{ObjectArtifactMetaDataKeyEnum.FILE_TYPE}"];
            var value = GetMetaDataValueByKey(metaData, keyName);
            if (value != null)
            {
                var fileTypeEnum = (LibraryFileTypeEnum)Enum.Parse(typeof(LibraryFileTypeEnum), value);
                fileTypeName = fileTypeEnum.ToString();
            }
            return fileTypeName;
        }

        public List<ObjectArifactApproverInfo> GetObjectArtifactApproverInfos(string objectArtifactId)
        {
            return RiqsObjectArtifactMappingConstant.GetRiqsObjectArtifactMappingByArtifactId(objectArtifactId)?.ApproverInfos ?? new List<ObjectArifactApproverInfo>();
        }

        public RiqsObjectArtifactMapping GetRiqsObjectArtifactMappingByArtifactId(string objectArtifactId)
        {
            return RiqsObjectArtifactMappingConstant.GetRiqsObjectArtifactMappingByArtifactId(objectArtifactId);
        }

        public bool IsAApprovedObjectArtifact(IDictionary<string, MetaValuePair> metaData)
        {
            var keyName = LibraryModuleConstants.ObjectArtifactMetaDataKeys[$"{ObjectArtifactMetaDataKeyEnum.APPROVAL_STATUS}"];
            string value = GetMetaDataValueByKey(metaData, keyName);
            return value == $"{(int)LibraryFileApprovalStatusEnum.APPROVED}";
        }

        public bool IsNotifiedToCockpit(IDictionary<string, MetaValuePair> metaData)
        {
            var keyName = LibraryModuleConstants.ObjectArtifactMetaDataKeys[$"{ObjectArtifactMetaDataKeyEnum.IS_NOTIFIED_TO_COCKPIT}"];
            string value = GetMetaDataValueByKey(metaData, keyName);
            return value == $"{(int)LibraryBooleanEnum.TRUE}";
        }

        public List<ObjectArtifact> GetFilteredFileObjectArtifacts(List<ObjectArtifact> objectArtifacts)
        {
            return objectArtifacts.Where(o => o.ArtifactType == ArtifactTypeEnum.File).ToList();
        }

        public List<ObjectArtifact> GetFilteredFolderObjectArtifacts(List<ObjectArtifact> objectArtifacts)
        {
            return objectArtifacts.Where(o => o.ArtifactType == ArtifactTypeEnum.Folder).ToList();
        }

        public List<ObjectArtifact> GetFilteredFormObjectArtifacts(List<ObjectArtifact> fileObjectArtifacts)
        {
            return fileObjectArtifacts.Where(o => IsAOriginalForm(o.MetaData)).ToList();
        }

        public List<ObjectArtifact> GetFilteredDocumentObjectArtifacts(List<ObjectArtifact> fileObjectArtifacts)
        {
            return fileObjectArtifacts.Where(o => IsADocument(o.MetaData)).ToList();
        }

        public List<ObjectArtifact> GetFilteredDraftAvailableDocumentObjectArtifacts(List<ObjectArtifact> documentObjectArtifacts)
        {
            return documentObjectArtifacts.Where(d => IsExistDocumentDraft(d.ItemId)).ToList();
        }

        public bool IsExistDocumentDraft(string documentObjectArtifactId)
        {
            var documentDraftMappingData = GetDocumentDraftMappingData(documentObjectArtifactId);
            return !string.IsNullOrWhiteSpace(documentDraftMappingData?.ItemId);
        }

        public DocumentEditMappingRecord GetDocumentDraftMappingData(string documentObjectArtifactId)
        {
            return _repository.GetItem<DocumentEditMappingRecord>(d => d.IsDraft && d.ParentObjectArtifactId == documentObjectArtifactId);
        }

        public bool IsDocumentDraftedByOtherUser(DocumentEditMappingRecord draftedDocumentMappingData, string userId)
        {
            if (string.IsNullOrEmpty(userId) || draftedDocumentMappingData == null || draftedDocumentMappingData?.EditHistory == null) return false;
            return (bool)(draftedDocumentMappingData?.EditHistory?.Exists(history => history.EditorUserId != userId));
        }

        private string GetFormFillStatus(IDictionary<string, MetaValuePair> metaData)
        {
            var formFillStatusKey = LibraryModuleConstants.ObjectArtifactMetaDataKeys[$"{ObjectArtifactMetaDataKeyEnum.FORM_FILL_STATUS}"];
            string status = GetMetaDataValueByKey(metaData, formFillStatusKey);
            return status;
        }
        public DocumentEditMappingRecord GetDocumentNotDraftedEditMappingData(string documentObjectArtifactId)
        {
            return _repository.GetItem<DocumentEditMappingRecord>(d => !d.IsDraft && d.ParentObjectArtifactId == documentObjectArtifactId);
        }
        public bool IsDocumentEditedByOtherUser(DocumentEditMappingRecord draftedDocumentMappingData, string userId)
        {
            return draftedDocumentMappingData?.EditHistory?.Exists(history => history.EditorUserId != userId) ?? false;
        }
        public async Task CreateDocumentEditMappingRecordForExternalFiles(string objectArtifactId)
        {
            var artifact = GetObjectArtifactById(objectArtifactId);
            if (artifact == null) return ;
            var originalArtifactKey = LibraryModuleConstants.ObjectArtifactMetaDataKeys[nameof(ObjectArtifactMetaDataKeyEnum.ORIGINAL_ARTIFACT_ID)];
            var versionKey = LibraryModuleConstants.ObjectArtifactMetaDataKeys[nameof(ObjectArtifactMetaDataKeyEnum.VERSION)];
            var fileTypeKey = LibraryModuleConstants.ObjectArtifactMetaDataKeys[nameof(ObjectArtifactMetaDataKeyEnum.FILE_TYPE)];
            var documentEditMappingRecord = new DocumentEditMappingRecord
            {
                ItemId = Guid.NewGuid().ToString(),
                ParentObjectArtifactId = GetMetaDataValueByKey(artifact.MetaData, originalArtifactKey),
                ObjectArtifactId = artifact.ItemId,
                IsDraft = false,
                EditHistory = new List<DocumentEditRecordHistory>
                {
                    new()
                    {
                        EditorUserId = _securityContextProvider.GetSecurityContext().UserId,
                        EditDate = DateTime.UtcNow
                    }
                },
                ArtifactVersionCreateDate = DateTime.UtcNow,
                CreateDate = DateTime.UtcNow,
                CreatedBy = _securityContextProvider.GetSecurityContext().UserId,
                OriginalHtmlFileId = null,
                CurrentHtmlFileId = null,
                CurrentDocFileId = artifact.FileStorageId,
                Version = GetMetaDataValueByKey(artifact.MetaData, versionKey),
                IsUploadedFromWeb = true,
                SavedDocUserId = _securityContextProvider.GetSecurityContext().UserId,
                SavedDocUserDisplayName = _securityContextProvider.GetSecurityContext().DisplayName,
                DepartmentId = GetObjectArtifactDepartmentId(artifact.MetaData),
                OrganizationId = artifact.OrganizationId,
                FileType = GetMetaDataValueByKey(artifact.MetaData, fileTypeKey)
            };
            await _repository.SaveAsync(documentEditMappingRecord);
        }

        public async Task CreateDocumentMarkedAsReadEntry(ObjectArtifact artifact)
        {
            var userId = _securityContextProvider.GetSecurityContext().UserId;
            var praxisUser = GetPraxisUserByUserId(userId);
            var documentMarkedAsRead = new DocumentsMarkedAsRead
            {
                ItemId = Guid.NewGuid().ToString(),
                ObjectArtifactId = artifact.ItemId,
                ReadByUserId = praxisUser.ItemId,
                ReadByUserName = praxisUser.DisplayName,
                CreateDate = DateTime.UtcNow,
                CreatedBy = userId,
                ReadOn = DateTime.UtcNow,
                DepartmentId = GetMetaDataValueByKey(artifact.MetaData, "DepartmentId"),
                OrganizationId = artifact.OrganizationId,
            };
            await _repository.SaveAsync(documentMarkedAsRead);
        }

        public bool IsASavedDraftedChildDocument(IDictionary<string, MetaValuePair> metadata)
        {
            if (metadata == null) return false;
            return IsADocument(metadata) && GetOriginalArtifactId(metadata) != null;
        }

        private bool IsUploadedFromWeb(IDictionary<string, MetaValuePair> metadata)
        {
            if (metadata == null) return false;
            var isUploadedFromWebKey = LibraryModuleConstants.ObjectArtifactMetaDataKeys[ObjectArtifactMetaDataKeyEnum.IS_UPLOADED_FROM_WEB.ToString()];
            return metadata.TryGetValue(isUploadedFromWebKey, out MetaValuePair isUploadedFromWeb) && isUploadedFromWeb.Value == LibraryBooleanEnum.TRUE.ToString();
        }

        public async Task<List<DocumentsMarkedAsRead>> GetDocumentsMarkAsReadByArtifactId(string artifactId)
        {
            var builder = Builders<DocumentsMarkedAsRead>.Filter;
            var filter = builder.Eq(r => r.ObjectArtifactId, artifactId) &
                         builder.Eq(r => r.IsMarkedToDelete, false);
            var projection = Builders<DocumentsMarkedAsRead>.Projection
                .Include(r => r.ReadByUserId)
                .Include(r => r.ReadByUserName);
            var collection = _ecapMongoDbDataContextProvider
                .GetTenantDataContext()
                .GetCollection<DocumentsMarkedAsRead>($"{nameof(DocumentsMarkedAsRead)}s");
            var documents = await collection
                .Find(filter)
                .Project(projection)
                .ToListAsync();
            var responses = documents != null && documents.Any()
                ? documents.Select(i => BsonSerializer.Deserialize<DocumentsMarkedAsRead>(i)).ToList()
                : null;
            return responses;
        }

        public string GetObjectArtifactDepartmentIdForSubscription(IDictionary<string, MetaValuePair> metaData)
        {
            var departmentIdForSubscriptionKey = LibraryModuleConstants.ObjectArtifactMetaDataKeys[$"{ObjectArtifactMetaDataKeyEnum.DEPARTMENT_ID_FOR_SUBSCRIPTION}"];
            var departmentId = string.Empty;
            if (metaData != null && metaData.TryGetValue(departmentIdForSubscriptionKey, out MetaValuePair departmentIdValue))
            {
                departmentId = departmentIdValue.Value;
            }

            return departmentId;
        }

        public bool IsAInterfaceMigrationArtifact(IDictionary<string, MetaValuePair> metaData)
        {
            if (metaData == null) return false;
            var interfaceMigrationSummaryId = LibraryModuleConstants.ObjectArtifactMetaDataKeys[ObjectArtifactMetaDataKeyEnum.INTERFACE_MIGRATION_SUMMARY_ID.ToString()];

            var isAInterfaceMigrationArtifact = metaData.TryGetValue(interfaceMigrationSummaryId, out MetaValuePair interfaceMigrationArtifact);
            return isAInterfaceMigrationArtifact && !string.IsNullOrEmpty(interfaceMigrationArtifact?.Value?.ToString());
        }

        public bool IsAOrgLevelArtifact(IDictionary<string, MetaValuePair> metaData, ArtifactTypeEnum artifactType)
        {
            if (metaData == null) return false;

            if (artifactType == ArtifactTypeEnum.File)
            {
                var version = GetMetaDataValueByKey(metaData, LibraryModuleConstants.ObjectArtifactMetaDataKeys[$"{ObjectArtifactMetaDataKeyEnum.VERSION}"]);
                double versionDouble;
                var isPared = double.TryParse(version, out versionDouble);

                if (isPared && versionDouble % 1 == 0) return true;
            }

            var isOrgLevel = GetMetaDataValueByKey(metaData, LibraryModuleConstants.ObjectArtifactMetaDataKeys[$"{ObjectArtifactMetaDataKeyEnum.IS_ORG_LEVEL}"]);
            return isOrgLevel == "1";
        }
    }
}
