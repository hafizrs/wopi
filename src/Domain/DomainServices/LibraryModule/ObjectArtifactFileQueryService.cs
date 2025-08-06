using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MongoDB.Bson.Serialization;
using MongoDB.Bson;
using MongoDB.Driver;
using Selise.Ecap.Entities.PrimaryEntities.Dms;
using Selise.Ecap.Entities;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.LibraryModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Queries.LibraryModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.ResponseModels.LibraryModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants;
using Selise.Ecap.Entities.PrimaryEntities.SLPC;

namespace Selise.Ecap.SC.PraxisMonitor.Domain
{
    public class ObjectArtifactFileQueryService : IObjectArtifactFileQueryService
    {
        private readonly ILogger<ObjectArtifactFileQueryService> _logger;
        private readonly ISecurityContextProvider _securityContextProvider;
        private readonly ISecurityHelperService _securityHelperService;
        private readonly IBlocksMongoDbDataContextProvider _ecapMongoDbDataContextProvider;
        private readonly IObjectArtifactFilterUtilityService _objectArtifactFilterUtilityService;
        private readonly IObjectArtifactUtilityService _objectArtifactUtilityService;

        public ObjectArtifactFileQueryService(
            ILogger<ObjectArtifactFileQueryService> logger,
            ISecurityContextProvider securityContextProvider,
            ISecurityHelperService securityHelperService,
            IBlocksMongoDbDataContextProvider ecapMongoDbDataContextProvider,
            IObjectArtifactUtilityService objectArtifactUtilityService,
            IObjectArtifactFilterUtilityService objectArtifactFilterUtilityService)
        {
            _logger = logger;
            _securityContextProvider = securityContextProvider;
            _securityHelperService = securityHelperService;
            _ecapMongoDbDataContextProvider = ecapMongoDbDataContextProvider;
            _objectArtifactUtilityService = objectArtifactUtilityService;
            _objectArtifactFilterUtilityService = objectArtifactFilterUtilityService;
        }

        #region public block
        public async Task<List<ObjectArtifactFileResponse>> InitiateGetFileArtifacts(ObjectArtifactFileQuery query)
        {
            var collection =
                _ecapMongoDbDataContextProvider
                .GetTenantDataContext()
                .GetCollection<BsonDocument>($"{nameof(ObjectArtifact)}s");

            return await GetFileArtifacts(query, collection);
        }
        #endregion

        #region query block

        public async Task<List<ObjectArtifactFileResponse>> GetFileArtifacts(ObjectArtifactFileQuery query, IMongoCollection<BsonDocument> collection)
        {
            return
                query.ObjectArtifactIds.Count() > 0 ?
                await GetFileArtifactsByIds(query, collection) :
                await GetFileArtifactsByFilter(query, collection);
        }

        private async Task<List<ObjectArtifactFileResponse>> GetFileArtifactsByIds(ObjectArtifactFileQuery query, IMongoCollection<BsonDocument> collection)
        {
            var pipeline = BuildSearchByIdsPipeline(query);
            var documents = await collection.Aggregate(pipeline).ToListAsync();
            return PrepareIdFilteredFileArtifactResponse(query.ObjectArtifactIds.ToList(), GetParsedObjestArtifacts(documents));
        }

        private async Task<List<ObjectArtifactFileResponse>> GetFileArtifactsByFilter(ObjectArtifactFileQuery query, IMongoCollection<BsonDocument> collection)
        {
            var pipeline = BuildSearchByFilterPipeline(query);
            var documents = await collection.Aggregate(pipeline).ToListAsync();
            return PrepareFilteredFileArtifactResponse(documents);
        }

        private PipelineDefinition<BsonDocument, BsonDocument> BuildSearchByIdsPipeline(ObjectArtifactFileQuery query)
        {
            var pipelineDefinition = new BsonDocument[]
            {
                _objectArtifactFilterUtilityService.PrepareMatchDefinitionFilter(PrepareFindByObjectArtifactIdsFilter(query.ObjectArtifactIds)),
                _objectArtifactFilterUtilityService.PrepareSortDefinitionFilter(query.Sort.PropertyName, query.Sort.Direction)
            };
            return pipelineDefinition;
        }

        private PipelineDefinition<BsonDocument, BsonDocument> BuildSearchByFilterPipeline(ObjectArtifactFileQuery query)
        {
            var pipelineDefinition = new BsonDocument[]
            {
                _objectArtifactFilterUtilityService.PrepareMatchDefinitionFilter(PrepareFilter(query)),
                _objectArtifactFilterUtilityService.PrepareSortDefinitionFilter(query.Sort.PropertyName, query.Sort.Direction),
                _objectArtifactFilterUtilityService.PrepareSkipDefinitionFilter(query.PageNumber - 1, query.PageSize),
                _objectArtifactFilterUtilityService.PrepareLimitDefinitionFilter(query.PageSize)
            };
            return pipelineDefinition;
        }

        private BsonArray PrepareFindByObjectArtifactIdsFilter(string[] objectArtifactIds)
        {
            var matchFilter = new BsonArray()
                .Add(_objectArtifactFilterUtilityService.PrepareFindByObjectArtifactIdsFilter(objectArtifactIds));

            var deptId = _securityHelperService.IsADepartmentLevelUser() ?
                            _securityHelperService.ExtractDepartmentIdFromDepartmentLevelUser() : string.Empty;
            matchFilter.Add(_objectArtifactFilterUtilityService.PrepareExcludeSecretArtifactFilter(deptId));
            return matchFilter;
        }

        private BsonArray PrepareFilter(ObjectArtifactFileQuery query)
        {
            var matchFilter = new BsonArray()
                .Add(_objectArtifactFilterUtilityService.PrepareIsMarkedToDeleteFilter())
                .Add(_objectArtifactFilterUtilityService.PrepareReadPermissionFilter())
                .Add(_objectArtifactFilterUtilityService.PrepareApprovedFileExcludingFilledFormFilter()) 
                .Add(_objectArtifactFilterUtilityService.PrepareStatusFilter(query.Active))
                .Add(_objectArtifactFilterUtilityService.PrepareArtifactTypeWiseFilter(ArtifactTypeEnum.File));

            var deptId = query.HideSecretArtifact ? string.Empty :
                      (
                        _securityHelperService.IsADepartmentLevelUser() ?
                        _securityHelperService.ExtractDepartmentIdFromDepartmentLevelUser() : string.Empty
                      );
            matchFilter.Add(_objectArtifactFilterUtilityService.PrepareExcludeSecretArtifactFilter(deptId));

            query.DepartmentIds ??= new List<string>();
            query.OrganizationIds ??= new List<string>();

            if (!string.IsNullOrEmpty(query.DepartmentId) && !query.DepartmentIds.Contains(query.DepartmentId))
            {
                query.DepartmentIds.Add(query.DepartmentId);
            }

            if (!string.IsNullOrEmpty(query.OrganizationId) && !query.OrganizationIds.Contains(query.OrganizationId))
            {
                query.OrganizationIds.Add(query.OrganizationId);
            }

            if (query.OrganizationIds.Count > 0 || query.DepartmentIds.Count > 0)
            {
                matchFilter.Add(PrepareDeptsAndOrgsFilter(query.OrganizationIds, query.DepartmentIds, query.SharedOrganizationIds));
            }

            if (query.IncludingFileTypes.Length > 0)
            {
                matchFilter.Add(PrepareFileTypeFilter(query.IncludingFileTypes));
            }
            if (query.ExcludingFileTypes.Length > 0)
            {
                matchFilter.Add(PrepareFileTypeFilter(query.ExcludingFileTypes, true));
            }

            if (!string.IsNullOrWhiteSpace(query.SearchText))
            {
                matchFilter
                    .Add(_objectArtifactFilterUtilityService.PrepareRegxTextSearchFilter(nameof(ObjectArtifact.Name), query.SearchText));
            }

            return matchFilter;
        }

        private BsonDocument PrepareFileTypeFilter(LibraryFileTypeEnum[] fileTypes, bool isExclude = false)
        {
            var fileTypeStringValues = fileTypes.Select(t => $"{(int)t}").ToArray();
            return
                _objectArtifactFilterUtilityService.PrepareMetaDataPropertyFilter(
                    $"{ObjectArtifactMetaDataKeyEnum.FILE_TYPE}", fileTypeStringValues, isExclude);
        }

        private BsonDocument PrepareOrganizationOrDepartmentFilter(string organizationId, string departmentId)
        {
            var matchFilter = !string.IsNullOrWhiteSpace(departmentId) ?
                _objectArtifactFilterUtilityService.PrepareDepartmentIdFilter(departmentId, organizationId, true) :
                !string.IsNullOrWhiteSpace(organizationId) ?
                _objectArtifactFilterUtilityService.PrepareOrganizationIdFilter(organizationId) : null;

            return matchFilter;
        }

        private BsonDocument PrepareDeptsAndOrgsFilter(List<string> organizationIds, List<string> departmentIds, List<string> sharedOrgIds)
        {
            return _objectArtifactFilterUtilityService.PrepareMultiDeptAndMultiOrgFilter(departmentIds, organizationIds, sharedOrgIds, true);
        }
        #endregion

        #region response generation block
        private List<ObjectArtifactFileResponse> PrepareFilteredFileArtifactResponse(List<BsonDocument> documents)
        {
            var artifacts = new List<ObjectArtifactFileResponse>();

            documents.ForEach(document =>
            {
                var foundArtifact = BsonSerializer.Deserialize<ObjectArtifact>(document);

                var artifact = new ObjectArtifactFileResponse()
                {
                    ItemId = foundArtifact.ItemId,
                    Name = foundArtifact.Name,
                    Extension = foundArtifact.Extension,
                    FileType = _objectArtifactUtilityService.GetFileTypeName(foundArtifact.MetaData),
                    FileStorageId = foundArtifact.FileStorageId,
                    FileSizeInByte = foundArtifact.FileSizeInByte,
                    Version = _objectArtifactUtilityService.GetMetaDataValueByKey(foundArtifact.MetaData, LibraryModuleConstants.ObjectArtifactMetaDataKeys[$"{ObjectArtifactMetaDataKeyEnum.VERSION}"]) ?? "1.00",
                    MetaData = foundArtifact?.MetaData
                };

                artifacts.Add(artifact);
            });

            return artifacts;
        }

        private List<ObjectArtifactFileResponse> PrepareIdFilteredFileArtifactResponse(
            List<string> objectArtifactIds, List<ObjectArtifact> foundArtifacts)
        {
            var artifacts = new List<ObjectArtifactFileResponse>();
            var artifactsByIds = _objectArtifactUtilityService.GetObjectArtifacts(objectArtifactIds.ToArray());
            objectArtifactIds.ForEach(id =>
            {
                var foundArtifact = foundArtifacts.FirstOrDefault(a => a.ItemId == id);
                var (isDeleted, isRestricted) = CheckIfDeletedOrRestricted(foundArtifact, artifactsByIds.Find(a => a.ItemId == id));
                var artifact = new ObjectArtifactFileResponse()
                {
                    ItemId = id,
                    Name = isDeleted || isRestricted ? string.Empty : foundArtifact.Name,
                    Extension = isDeleted || isRestricted ? string.Empty : foundArtifact.Extension,
                    FileType = isDeleted || isRestricted ? string.Empty : _objectArtifactUtilityService.GetFileTypeName(foundArtifact.MetaData),
                    FileStorageId = isDeleted || isRestricted ? string.Empty : foundArtifact.FileStorageId,
                    FileSizeInByte = isDeleted || isRestricted ? 0 : foundArtifact.FileSizeInByte,
                    Version = isDeleted || isRestricted ? string.Empty : (_objectArtifactUtilityService.GetMetaDataValueByKey(foundArtifact.MetaData, LibraryModuleConstants.ObjectArtifactMetaDataKeys[$"{ObjectArtifactMetaDataKeyEnum.VERSION}"]) ?? "1.00"),
                    IsDeleted = isDeleted,
                    IsRestricted = isRestricted,
                    MetaData = foundArtifact?.MetaData
                };

                artifacts.Add(artifact);
            });
            return artifacts.OrderBy(a => a.Name).ToList();
        }

        private List<ObjectArtifact> GetParsedObjestArtifacts(List<BsonDocument> documents)
        {
            return documents.Select(document => BsonSerializer.Deserialize<ObjectArtifact>(document)).ToList();
        }

        private (bool, bool) CheckIfDeletedOrRestricted(ObjectArtifact artifact, ObjectArtifact mainArtifact)
        {
            var isDeleted = _objectArtifactUtilityService.IsADeletedArtifact(mainArtifact);
            var isRestricted = !isDeleted && (artifact == null || !_objectArtifactUtilityService.CanReadObjectArtifact(artifact));
            return (isDeleted, isRestricted);
        }
        #endregion
    }
}