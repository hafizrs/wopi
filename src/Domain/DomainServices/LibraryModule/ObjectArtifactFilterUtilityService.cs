using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.LibraryModule;
using MongoDB.Bson;
using Selise.Ecap.Entities.PrimaryEntities.Dms;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants;
using Selise.Ecap.Entities;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using System.Linq;
using System.Collections.Generic;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Infrastructure;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor;
using System;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Entities.LibraryModule;

namespace Selise.Ecap.SC.PraxisMonitor.Domain
{
    public class ObjectArtifactFilterUtilityService : IObjectArtifactFilterUtilityService
    {
        private readonly ISecurityContextProvider _securityContextProvider;
        private readonly IRepository _repository;
        private readonly ISecurityHelperService _securityHelperService;

        public ObjectArtifactFilterUtilityService(
            ISecurityContextProvider securityContextProvider,
            IRepository repository,
            ISecurityHelperService securityHelperService
        )
        {
            _securityContextProvider = securityContextProvider;
            _repository = repository;
            _securityHelperService = securityHelperService;
        }

        #region public methods

        public BsonDocument PrepareMatchDefinitionFilter(BsonArray filter)
        {
            return new BsonDocument("$match", new BsonDocument("$and", filter));
        }

        public BsonDocument PrepareSortDefinitionFilter(string propertyName, SortDirectionEnum sortDirection)
        {
            return new BsonDocument("$sort", new BsonDocument(propertyName, sortDirection));
        }

        public BsonDocument PrepareSkipDefinitionFilter(int pageNumber, int pageSize)
        {
            return new BsonDocument("$skip", pageNumber * pageSize);
        }

        public BsonDocument PrepareLimitDefinitionFilter(int pageSize)
        {
            return new BsonDocument("$limit", pageSize);
        }

        public BsonDocument PrepareObjectArtifactIdFilter(string objectArtifactId)
        {
            var objectArtifactIdFilter = new BsonDocument("_id", BsonValue.Create(objectArtifactId));
            return objectArtifactIdFilter;
        }

        public BsonDocument PrepareFindByObjectArtifactIdsFilter(string[] objectArtifactIds)
        {
            return PrepareStringFieldIncludingFilter("_id", objectArtifactIds);
        }

        public BsonDocument PrepareReadPermissionFilter()
        {
            var securityContextProvider = _securityContextProvider.GetSecurityContext();

            var readPermissionFilter = new BsonDocument()
                            .Add("$or", new BsonArray()
                                    .Add(new BsonDocument()
                                            .Add(nameof(ObjectArtifact.IdsAllowedToRead), new BsonDocument()
                                                    .Add("$eq", $"{securityContextProvider.UserId}")
                                            )
                                    )
                                    .Add(new BsonDocument()
                                            .Add(nameof(ObjectArtifact.RolesAllowedToRead), new BsonDocument()
                                                    .Add("$in", new BsonArray(securityContextProvider.Roles?.ToArray() ?? Array.Empty<string>()))
                                            )
                                    )
                            );

            return readPermissionFilter;
        }

        public BsonDocument PrepareIsMarkedToDeleteFilter()
        {
            var filter = new BsonDocument(nameof(ObjectArtifact.IsMarkedToDelete), BsonValue.Create(false));
            return filter;
        }

        public BsonDocument PrepareParentIdFilter(string parentId)
        {
            var parentIdFilter = PrepareStringFieldFilter(nameof(ObjectArtifact.ParentId), parentId);
            return parentIdFilter;
        }

        public BsonDocument PrepareOrganizationIdFilter(string organizationId)
        {
            var organizationIdFilter = PrepareStringFieldFilter(nameof(ObjectArtifact.OrganizationId), organizationId);
            return organizationIdFilter;
        }

        public BsonDocument PrepareDepartmentIdFilter(string departmentId, string orgId = null, bool isShared = false)
        {
            var departmentIdFilter = PrepareStringFieldFilter("MetaData.DepartmentId.Value", departmentId);
            if (isShared)
            {
                var filters =
                    new BsonArray()
                    .Add(departmentIdFilter)
                    .Add(PreparedSharedDepartmentIdFilter(new List<string> { departmentId }, !string.IsNullOrEmpty(orgId) ? new List<string> { orgId} : null));

                departmentIdFilter = new BsonDocument("$or", filters);
            }
            return departmentIdFilter;
        }

        public BsonDocument PrepareMultiDeptAndMultiOrgFilter(List<string> departmentIds, List<string> organizationIds, List<string> sharedOrgIds, bool isShared = false)
        {
            var departmentIdFilter = PrepareStringFieldIncludingFilter("MetaData.DepartmentId.Value", departmentIds?.ToArray() ?? Array.Empty<string>());
            var organizationIdFilter = PrepareStringFieldIncludingFilter(nameof(ObjectArtifact.OrganizationId), organizationIds?.ToArray() ?? Array.Empty<string>());
            var filters = new BsonArray()
            {
                departmentIdFilter,
                organizationIdFilter
            };
            if (isShared)
            {
                filters.Add(PreparedSharedDepartmentIdFilter(departmentIds, organizationIds));
            }
            if (sharedOrgIds?.Count > 0)
            {
                var sharedOrgIdFilter = PrepareStringFieldIncludingFilter(nameof(ObjectArtifact.OrganizationId), sharedOrgIds?.ToArray() ?? Array.Empty<string>());
                filters.Add(sharedOrgIdFilter);
            }
            return new BsonDocument("$or", filters);
        }

        public BsonDocument PrepareExcludeSecretArtifactFilter(string deptId = "")
        {
            var metaDataKeyName = LibraryModuleConstants.ObjectArtifactMetaDataKeys[$"{ObjectArtifactMetaDataKeyEnum.IS_SECRET_ARTIFACT}"];
            var value = $"{(int)LibraryBooleanEnum.TRUE}";
            var propertyName = $"{nameof(ObjectArtifact.MetaData)}.{metaDataKeyName}.{nameof(MetaValuePair.Value)}";

            var filter =
                new BsonDocument(propertyName,
                    new BsonDocument("$ne",
                        !string.IsNullOrEmpty(value) ? BsonValue.Create(value) : BsonNull.Value));

            if (!string.IsNullOrEmpty(deptId))
            {
                var securityContext = _securityContextProvider.GetSecurityContext();
                var isView = _repository.GetItem<RiqsPediaViewControl>(x => x.UserId == securityContext.UserId)?.ViewState ?? false;
                var isOrgOfficer = _repository.ExistsAsync<RiqsLibraryControlMechanism>(x => !string.IsNullOrEmpty(x.OrganizationId) &&
                        ((x.ApprovalAdmins != null && x.ApprovalAdmins.Any(y => y.UserId == securityContext.UserId)) ||
                        (x.UploadAdmins != null && x.UploadAdmins.Any(y => y.UserId == securityContext.UserId)))
                    ).GetAwaiter().GetResult();

                if (isOrgOfficer && isView)
                {
                    return filter;
                }

                // Exclude other department secret folder
                var filters = new BsonArray()
                                .Add(filter)
                                .Add(PrepareDepartmentIdFilter(deptId));

                filter = new BsonDocument("$or", filters);
            }

            return filter;
        }

        public BsonDocument PrepareTextSearchByArtifactNameFilter(string text)
        {
            var filter = new BsonDocument(nameof(ObjectArtifact.Name), new BsonRegularExpression($"{text}", "i"));
            return filter;
        }

        public BsonDocument PrepareObjectArtifactTextSearchFilter(string text)
        {
            var nameTextSearchFilter = new BsonDocument(
                nameof(ObjectArtifact.Name),
                new BsonRegularExpression($"{text}", "i"));

            var keywordsTextSearchFilter = new BsonDocument(
                "MetaData.Keywords.Value",
                new BsonRegularExpression($"{text}", "i"));

            var filterValue =
                new BsonArray()
                .Add(nameTextSearchFilter)
                .Add(keywordsTextSearchFilter);

            var textSearchFilter = new BsonDocument("$or", filterValue);
            return textSearchFilter;
        }

        public BsonDocument PrepareRegxTextSearchFilter(string propertyName, string text)
        {
            var textSearchFilter = new BsonDocument(
                propertyName,
                new BsonRegularExpression($"{text}", "i"));
            return textSearchFilter;
        }

        public BsonDocument PrepareClildFindingFilter()
        {
            var filters = new BsonArray()
                .Add(PrepareIsMarkedToDeleteFilter())
                .Add(PrepareReadPermissionFilter())
                .Add(PrepareFolderandApprovedFileExcludingFilledFormFilter());

            var deptId = _securityHelperService.IsADepartmentLevelUser() ?
                            _securityHelperService.ExtractDepartmentIdFromDepartmentLevelUser() : string.Empty;
            filters.Add(PrepareExcludeSecretArtifactFilter(deptId));

            return new BsonDocument("$and", filters);
        }

        public BsonDocument PrepareFolderAndApprovedFileFilter()
        {
            var filters = new BsonArray()
                .Add(PrepareArtifactTypeWiseFilter(ArtifactTypeEnum.Folder))
                .Add(PrepareApprovedFileFilter());

            return new BsonDocument("$or", filters);
        }

        public BsonDocument PrepareFolderandApprovedFileExcludingFilledFormFilter()
        {
            var filters = new BsonArray()
                .Add(PrepareArtifactTypeWiseFilter(ArtifactTypeEnum.Folder))
                .Add(PrepareApprovedFileExcludingFilledFormFilter());

            return new BsonDocument("$or", filters);
        }

        public BsonDocument PrepareArtifactTypeWiseFilter(ArtifactTypeEnum artifactType)
        {
            var filter = new BsonDocument(nameof(ObjectArtifact.ArtifactType), artifactType);
            return filter;
        }

        public BsonDocument PrepareApprovedFileFilter()
        {
            string filterName = $"{LibraryViewModeEnum.APPROVED}";
            return GetViewModeFilter(filterName);
        }

        public BsonDocument PrepareApprovedFileExcludingFilledFormFilter()
        {
            var filterValue =
                new BsonArray()
                .Add(PrepareApprovedFileFilter())
                .Add(PrepareExcludeFilledFormFilter());
            var filter = new BsonDocument("$and", filterValue);
            return filter;
        }

        public BsonDocument PrepareWordFileFilter()
        {
            string filterName = $"{LibraryViewModeEnum.DOCUMENT}";
            return GetViewModeFilter(filterName);
        }

        public BsonDocument PrepareFormFilter()
        {
            string filterName = $"{LibraryViewModeEnum.FORM}";
            return GetViewModeFilter(filterName);
        }

        public BsonDocument PrepareOriginalArtifactFilter()
        {
            var metaDataKeyName = LibraryModuleConstants.ObjectArtifactMetaDataKeys[$"{ObjectArtifactMetaDataKeyEnum.IS_A_ORIGINAL_ARTIFACT}"];
            var value = $"{(int)LibraryBooleanEnum.TRUE}";
            return GetMetaDataKeyFilter(metaDataKeyName, value);
        }

        public BsonDocument PrepareChildArtifactFilter(string originalArtifactId)
        {
            var originalArtifactIdKey = LibraryModuleConstants.ObjectArtifactMetaDataKeys[nameof(ObjectArtifactMetaDataKeyEnum.ORIGINAL_ARTIFACT_ID)];
            var bsonArray = new BsonArray
            {
                GetMetaDataKeyFilter(originalArtifactIdKey, originalArtifactId),
                PrepareExcludeOriginalFormFilter()
            };
            return new BsonDocument("$and", bsonArray);
        }

        public BsonDocument PrepareFileFormatFilter(LibraryFileTypeEnum fileFormats)
        {
            var metaDataKeyName =
                LibraryModuleConstants.ObjectArtifactMetaDataKeys[ObjectArtifactMetaDataKeyEnum.FILE_TYPE.ToString()];
            var value = ((int)fileFormats).ToString();
            return GetMetaDataKeyFilter(metaDataKeyName, value);
        }

        public BsonDocument PrepareStatusFilter(bool isActive)
        {
            var metaDataKeyName = LibraryModuleConstants.ObjectArtifactMetaDataKeys[$"{ObjectArtifactMetaDataKeyEnum.STATUS}"];
            var value = !isActive ? $"{(int)LibraryFileStatusEnum.INACTIVE}" : $"{(int)LibraryFileStatusEnum.ACTIVE}";
            return GetMetaDataKeyFilter(metaDataKeyName, value);
        }

        public BsonDocument PrepareExcludeFilledFormFilter()
        {
            var metaDataKeyName = LibraryModuleConstants.ObjectArtifactMetaDataKeys[nameof(ObjectArtifactMetaDataKeyEnum.IS_A_ORIGINAL_ARTIFACT)];
            var value = $"{(int)LibraryBooleanEnum.FALSE}";
            var propertyName = $"{nameof(ObjectArtifact.MetaData)}.{metaDataKeyName}.{nameof(MetaValuePair.Value)}";
            var filter =
                new BsonDocument(propertyName,
                    new BsonDocument("$ne",
                        !string.IsNullOrEmpty(value) ? BsonValue.Create(value) : BsonNull.Value));

            return filter;
        }

        private BsonDocument PrepareExcludeOriginalFormFilter()
        {
            var metaDataKeyName = LibraryModuleConstants.ObjectArtifactMetaDataKeys[nameof(ObjectArtifactMetaDataKeyEnum.IS_A_ORIGINAL_ARTIFACT)];
            var value = $"{(int)LibraryBooleanEnum.TRUE}";
            var propertyName = $"{nameof(ObjectArtifact.MetaData)}.{metaDataKeyName}.{nameof(MetaValuePair.Value)}";
            var filter =
                new BsonDocument(propertyName,
                    new BsonDocument("$ne",
                        !string.IsNullOrEmpty(value) ? BsonValue.Create(value) : BsonNull.Value));

            return filter;
        }

        public BsonDocument PrepareExcludeGeneralFormFilter()
        {
            var metaDataKeyName = LibraryModuleConstants.ObjectArtifactMetaDataKeys[$"{ObjectArtifactMetaDataKeyEnum.FORM_TYPE}"];
            var value = $"{(int)LibraryFormTypeEnum.GENERAL}";
            var propertyName = $"{nameof(ObjectArtifact.MetaData)}.{metaDataKeyName}.{nameof(MetaValuePair.Value)}";
            var filter =
                new BsonDocument(propertyName,
                    new BsonDocument("$ne",
                        !string.IsNullOrEmpty(value) ? BsonValue.Create(value) : BsonNull.Value));

            return filter;
        }

        public BsonDocument RemoveOrganizationFolderFromFilter(string orgId = null)
        {
            BsonDocument orgfilter = null;

            var orgIds = new List<string>();
            if (_securityHelperService.IsAAdminOrTaskConrtroller())
            {
                orgIds = _repository.GetItems<PraxisOrganization>(o => !o.IsMarkedToDelete)?.Select(o => o.ItemId)?.ToList();
            }
            else
            {
                var organizationIds = _securityHelperService.ExtractOrganizationIdsFromOrgLevelUser();

                if (organizationIds?.Count > 0)
                {
                    orgIds.AddRange(organizationIds);
                }
            }

            if (!string.IsNullOrEmpty(orgId))
            {
                orgIds.Add(orgId);
            }

            if (orgIds?.Count > 0)
            {
                orgfilter = new BsonDocument("_id", new BsonDocument().Add("$nin", new BsonArray(orgIds)));
            }

            var filter = new BsonDocument
            {
                {
                    "$expr",
                    new BsonDocument
                    {
                        { "$ne", new BsonArray { "$_id", "$OrganizationId" } }
                    }
                }
            };

            if (orgfilter != null)
            {
                filter = new BsonDocument
                {
                    {
                        "$or", new BsonArray
                        {
                            orgfilter,
                            filter
                        }
                    }
                };
            }
            

            return filter;
        }

        public BsonDocument PrepareMetaDataPropertyFilter(string metaDataKeyName, string[] value, bool isExclude = false)
        {
            BsonDocument filter = null;
            var propertyName = GetMetaDataFilterPropertyName(metaDataKeyName);
            if (propertyName != null)
            {
                filter = !isExclude ?
                PrepareStringFieldIncludingFilter(propertyName, value) :
                PrepareStringFieldExcludingFilter(propertyName, value);
            }
            return filter;
        }

        #endregion

        private string GetMetaDataFilterPropertyName(string metaDataKeyName)
        {
            LibraryModuleConstants.LibraryMetaDataFilterPropertyMap.TryGetValue(metaDataKeyName, out string filterPropertyName);
            return filterPropertyName;
        }

        private BsonDocument GetMetaDataKeyFilter(string metaDataKeyName, string value)
        {
            var propertyName = $"{nameof(ObjectArtifact.MetaData)}.{metaDataKeyName}.{nameof(MetaValuePair.Value)}";
            return PrepareStringFieldFilter(propertyName, value);
        }

        private BsonDocument GetViewModeFilter(string viewMode)
        {
            var propertyName = GetViewModeFilterPropertyName(viewMode);
            var value = GetViewModeFilterValue(viewMode);
            var filter = PrepareStringFieldFilter(propertyName, value);
            return filter;
        }

        private string GetViewModeFilterPropertyName(string type)
        {
            return LibraryModuleConstants.LibraryViewModeFilterPropertyMap[type];
        }

        private string GetViewModeFilterValue(string type)
        {
            return LibraryModuleConstants.LibraryViewModeFilterValueMap[type];
        }

        private BsonDocument PrepareStringFieldFilter(string propertyName, string value)
        {
            var filter = new BsonDocument(
                propertyName,
                !string.IsNullOrEmpty(value) ? BsonValue.Create(value) : BsonNull.Value);

            return filter;
        }

        private BsonDocument PrepareStringFieldIncludingFilter(string propertyName, string[] values)
        {
            var filter = new BsonDocument(propertyName, new BsonDocument("$in", new BsonArray(values ?? Array.Empty<string>())));

            return filter;
        }

        private BsonDocument PrepareStringFieldExcludingFilter(string propertyName, string[] values)
        {
            var filter = new BsonDocument(propertyName, new BsonDocument("$nin", new BsonArray(values ?? Array.Empty<string>())));

            return filter;
        }

        private BsonDocument PreparedSharedDepartmentIdFilter(List<string> departmentIds, List<string> orgIds)
        {
            var organizationIdFilterValues = new List<string>();
            if (departmentIds?.Count > 0) organizationIdFilterValues.AddRange(departmentIds);
            if (orgIds?.Count > 0) organizationIdFilterValues.AddRange(orgIds);

            var isSharedFilter =
                new BsonDocument(nameof(ObjectArtifact.SharedOrganizationList),
                    new BsonDocument("$elemMatch",
                         new BsonDocument().Add("OrganizationId",
                             new BsonDocument().Add("$in",
                                 new BsonArray(organizationIdFilterValues.ToArray())))));

            return isSharedFilter;
        }
    }
}
