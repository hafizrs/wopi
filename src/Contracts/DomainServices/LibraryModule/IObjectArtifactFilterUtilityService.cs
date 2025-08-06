using MongoDB.Bson;
using Selise.Ecap.Entities;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants;
using System.Collections.Generic;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.LibraryModule
{
    public interface IObjectArtifactFilterUtilityService
    {
        BsonDocument PrepareMatchDefinitionFilter(BsonArray filter);
        BsonDocument PrepareSortDefinitionFilter(string propertyName, SortDirectionEnum sortDirection);
        BsonDocument PrepareSkipDefinitionFilter(int pageNumber, int pageSize);
        BsonDocument PrepareLimitDefinitionFilter(int pageSize);
        BsonDocument PrepareObjectArtifactIdFilter(string objectArtifactId);
        BsonDocument PrepareFindByObjectArtifactIdsFilter(string[] objectArtifactIds);
        BsonDocument PrepareReadPermissionFilter();
        BsonDocument PrepareArtifactTypeWiseFilter(ArtifactTypeEnum artifactType);
        BsonDocument PrepareIsMarkedToDeleteFilter();
        BsonDocument PrepareParentIdFilter(string parentId);
        BsonDocument PrepareOrganizationIdFilter(string organizationId);
        BsonDocument PrepareDepartmentIdFilter(string departmentId, string orgId = null, bool isShared = false);
        BsonDocument PrepareTextSearchByArtifactNameFilter(string text);
        BsonDocument PrepareFolderAndApprovedFileFilter();
        BsonDocument PrepareApprovedFileFilter();
        BsonDocument PrepareClildFindingFilter();
        BsonDocument PrepareWordFileFilter();
        BsonDocument PrepareFormFilter();
        BsonDocument PrepareOriginalArtifactFilter();
        BsonDocument PrepareStatusFilter(bool isActive);
        BsonDocument PrepareExcludeFilledFormFilter();
        BsonDocument RemoveOrganizationFolderFromFilter(string orgId = null);
        BsonDocument PrepareExcludeGeneralFormFilter();
        BsonDocument PrepareFolderandApprovedFileExcludingFilledFormFilter();
        BsonDocument PrepareApprovedFileExcludingFilledFormFilter();
        BsonDocument PrepareObjectArtifactTextSearchFilter(string text);
        BsonDocument PrepareChildArtifactFilter(string originalArtifactId);
        BsonDocument PrepareMetaDataPropertyFilter(string metaDataKeyName, string[] value, bool isExclude = false);
        BsonDocument PrepareRegxTextSearchFilter(string propertyName, string text);
        BsonDocument PrepareFileFormatFilter(LibraryFileTypeEnum fileFormats);
        BsonDocument PrepareExcludeSecretArtifactFilter(string deptId = "");
        BsonDocument PrepareMultiDeptAndMultiOrgFilter(List<string> departmentIds, List<string> organizationIds, List<string> sharedOrgIds, bool isShared = false);
    }
}
