using System.Collections.Generic;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using Selise.Ecap.Entities.PrimaryEntities.Dms;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.LibraryModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.ResponseModels.LibraryModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Infrastructure;
using System.Linq;
using MongoDB.Bson.Serialization;
using Selise.Ecap.Entities;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants;

namespace Selise.Ecap.SC.PraxisMonitor.Domain
{
    public class LibraryDirectoryGetService : ILibraryDirectoryGetService
    {
        private readonly ISecurityContextProvider _securityContextProvider;
        private readonly ISecurityHelperService _securityHelperService;
        private readonly IBlocksMongoDbDataContextProvider _ecapMongoDbDataContextProvider;
        private readonly IObjectArtifactUtilityService _objectArtifactUtilityService;
        private readonly IObjectArtifactAuthorizationCheckerService _objectArtifactAuthorizationCheckerService;
        private readonly IObjectArtifactFilterUtilityService _objectArtifactFilterUtilityService;
        private readonly IObjectArtifactSearchUtilityService _objectArtifactSearchUtilityService;
        private readonly IObjectArtifactSearchQueryBuilderService _objectArtifactSearchQueryBuilderService;


        public LibraryDirectoryGetService(
            ISecurityContextProvider securityContextProvider,
            ISecurityHelperService securityHelperService,
            IBlocksMongoDbDataContextProvider ecapMongoDbDataContextProvider,
            IObjectArtifactUtilityService objectArtifactUtilityService,
            IObjectArtifactAuthorizationCheckerService objectArtifactAuthorizationCheckerService,
            IObjectArtifactFilterUtilityService objectArtifactFilterUtilityService,
            IObjectArtifactSearchUtilityService objectArtifactSearchUtilityService,
            IObjectArtifactSearchQueryBuilderService objectArtifactSearchQueryBuilderService)
        {
            _securityContextProvider = securityContextProvider;
            _securityHelperService = securityHelperService;
            _ecapMongoDbDataContextProvider = ecapMongoDbDataContextProvider;
            _objectArtifactUtilityService = objectArtifactUtilityService;
            _objectArtifactAuthorizationCheckerService = objectArtifactAuthorizationCheckerService;
            _objectArtifactFilterUtilityService = objectArtifactFilterUtilityService;
            _objectArtifactSearchUtilityService = objectArtifactSearchUtilityService;
            _objectArtifactSearchQueryBuilderService = objectArtifactSearchQueryBuilderService;
        }

        public async Task<List<LibraryDirectoryResponse>> GetLibraryDirectories(LibraryDirectoryGetCommand command)
        {
            UpdateLibraryDirectoryGetCommand(command);

            if (command.IsDirectoryForFilter)
            {
                return await GetLibraryDirectoriesForFilter(command);
            }
            else if (command.IsDirectoryForFileUpload)
            {
                return await GetLibraryDirectoriesForFileUpload(command);
            }
            else
            {
                var objectArtifact = _objectArtifactUtilityService.GetObjectArtifactSecuredById(command.ObjectArtifactId);

                if (objectArtifact == null)
                {
                    return null;
                }

                var result =
                    string.IsNullOrWhiteSpace(command.ParentId) ?
                    await GetRootFolderArtifacts(objectArtifact, command) :
                    await GetAllChildrenArtifacts(objectArtifact, command.ParentId);

                return result;
            }

        }

        private async Task<List<LibraryDirectoryResponse>> GetLibraryDirectoriesForFileUpload(LibraryDirectoryGetCommand command)
        {
            return
                string.IsNullOrWhiteSpace(command.ParentId) ?
                await GetRootFolderArtifacts(null, command) :
                await GetAllChildrenArtifacts(null, command.ParentId);
        }


        private async Task<List<LibraryDirectoryResponse>> GetLibraryDirectoriesForFilter(LibraryDirectoryGetCommand command)
        {
            var pipeline = BuildSearchPipelineOnlyFolder(command);

            var collection = _ecapMongoDbDataContextProvider
                .GetTenantDataContext()
                .GetCollection<BsonDocument>($"{nameof(ObjectArtifact)}s");

            var documents = await collection.Aggregate(pipeline).ToListAsync();
            var result = PrepareRootFolderArtifactResponse(null, documents);

            return result;
          
        }


        private async Task<List<LibraryDirectoryResponse>> GetRootFolderArtifacts(ObjectArtifact targetArtifact, LibraryDirectoryGetCommand command)
        {
            var pipeline = BuildSearchPipeline(command, targetArtifact);

            var collection = _ecapMongoDbDataContextProvider
                .GetTenantDataContext()
                .GetCollection<BsonDocument>($"{nameof(ObjectArtifact)}s");

            var documents = await collection.Aggregate(pipeline).ToListAsync();
            var result = PrepareRootFolderArtifactResponse(targetArtifact, documents);

            return result;
        }

        private async Task<List<LibraryDirectoryResponse>> GetAllChildrenArtifacts(ObjectArtifact targetArtifact, string parentId)
        {
            var artifacts = await _objectArtifactSearchUtilityService.GetAllChildrenObjectArtifacts(parentId);
            var result = PrepareAllChildrenArtifactResponse(targetArtifact, artifacts);

            return result;
        }

        private void UpdateLibraryDirectoryGetCommand(LibraryDirectoryGetCommand command)
        {
            if (string.IsNullOrWhiteSpace(command.ParentId))
            {
                command.ParentId = null;
            }
        }


        private PipelineDefinition<BsonDocument, BsonDocument> BuildSearchPipelineOnlyFolder(LibraryDirectoryGetCommand command)
        {
            var matchFilter = new BsonArray()
               .Add(_objectArtifactFilterUtilityService.PrepareIsMarkedToDeleteFilter());

            var removeOrgFolderFilter = _objectArtifactFilterUtilityService.RemoveOrganizationFolderFromFilter(command?.OrganizationId);
            if (removeOrgFolderFilter != null)
            {
                matchFilter.Add(removeOrgFolderFilter);
            }

            matchFilter
               .Add(_objectArtifactFilterUtilityService.PrepareReadPermissionFilter())
               .Add(_objectArtifactFilterUtilityService.PrepareArtifactTypeWiseFilter(ArtifactTypeEnum.Folder));

            var deptId = _securityHelperService.IsADepartmentLevelUser() ?
                            _securityHelperService.ExtractDepartmentIdFromDepartmentLevelUser() : string.Empty;
            matchFilter.Add(_objectArtifactFilterUtilityService.PrepareExcludeSecretArtifactFilter(deptId));

            var roleWiseFilter = _objectArtifactSearchQueryBuilderService
                                    .PrepareRoleWiseFilter(command.OrganizationId, command.DepartmentId, command.ParentId);

            if (roleWiseFilter?.Count() > 0)
            {
                matchFilter.Add(roleWiseFilter);
            }

            if (!string.IsNullOrWhiteSpace(command.SearchText))
            {
                matchFilter.Add(
                    _objectArtifactSearchUtilityService.PrepareRegExTextSearchFilter(nameof(ObjectArtifact.Name), command.SearchText));
            }

            BsonDocument matchDefinition = new BsonDocument("$match", new BsonDocument("$and", matchFilter));
            BsonDocument sortDefinition = new BsonDocument("$sort", new BsonDocument($"{nameof(ObjectArtifact.Name)}", 1));

            var pipelineDefinition = new BsonDocument[]
            {
                matchDefinition,
                _objectArtifactSearchQueryBuilderService.PrepareAllViewLookupDefinitionDefinition(),
                _objectArtifactSearchQueryBuilderService.PrepareAllViewUnwindDefinition(),
                _objectArtifactSearchQueryBuilderService.PrepareAllViewFinalMatchDefinition(),
                sortDefinition
            };

            return pipelineDefinition;
        }

        private PipelineDefinition<BsonDocument, BsonDocument> BuildSearchPipeline(LibraryDirectoryGetCommand command, ObjectArtifact artifact)
        {
            var matchFilter = new BsonArray()
               .Add(_objectArtifactFilterUtilityService.PrepareIsMarkedToDeleteFilter());
             
            var removeOrgFolderFilter = _objectArtifactFilterUtilityService.RemoveOrganizationFolderFromFilter(command?.OrganizationId ?? artifact?.OrganizationId);
            if (removeOrgFolderFilter != null)
            {
                matchFilter.Add(removeOrgFolderFilter);
            }

            matchFilter
                .Add(_objectArtifactFilterUtilityService.PrepareReadPermissionFilter())
               .Add(_objectArtifactFilterUtilityService.PrepareFolderandApprovedFileExcludingFilledFormFilter());

            var isDeptLevelUser = _securityHelperService.IsADepartmentLevelUser();
            var artifactDeptId = _objectArtifactUtilityService.GetObjectArtifactDepartmentId(artifact?.MetaData);
            var deptId = isDeptLevelUser ? 
                _securityHelperService.ExtractDepartmentIdFromDepartmentLevelUser() : command.DepartmentId;

            var isExcludingSecretArtifact = artifact != null && 
                (string.IsNullOrEmpty(artifactDeptId) || artifactDeptId != deptId);

            var currentDeptId = isExcludingSecretArtifact ? string.Empty :
                                _securityHelperService.IsADepartmentLevelUser() ?
                                    _securityHelperService.ExtractDepartmentIdFromDepartmentLevelUser() : string.Empty;
            
            matchFilter.Add(_objectArtifactFilterUtilityService.PrepareExcludeSecretArtifactFilter(currentDeptId));

            var roleWiseFilter = _objectArtifactSearchQueryBuilderService
                    .PrepareRoleWiseFilter(command.OrganizationId, command.DepartmentId, command.ParentId);

            if (roleWiseFilter?.Count() > 0)
            {
                matchFilter.Add(roleWiseFilter);
            }

            if (!string.IsNullOrWhiteSpace(command.SearchText))
            {
                matchFilter.Add(
                    _objectArtifactSearchUtilityService.PrepareRegExTextSearchFilter(nameof(ObjectArtifact.Name), command.SearchText));
            }

            BsonDocument matchDefinition = new BsonDocument("$match", new BsonDocument("$and", matchFilter));
            BsonDocument sortDefinition = new BsonDocument("$sort", new BsonDocument($"{nameof(ObjectArtifact.Name)}", 1));

            var pipelineDefinition = new BsonDocument[]
            {
                matchDefinition,
                _objectArtifactSearchQueryBuilderService.PrepareAllViewLookupDefinitionDefinition(),
                _objectArtifactSearchQueryBuilderService.PrepareAllViewUnwindDefinition(),
                _objectArtifactSearchQueryBuilderService.PrepareAllViewAddFiledsDefinition(),
                _objectArtifactSearchQueryBuilderService.PrepareAllViewFinalMatchDefinition(),
                sortDefinition 
            };

            return pipelineDefinition;
        }

        private List<LibraryDirectoryResponse> PrepareRootFolderArtifactResponse(ObjectArtifact targetArtifact, List<BsonDocument> documents)
        {
            var artifacts = new List<LibraryDirectoryResponse>();

            documents.ForEach(document =>
            {
                var foundArtifact = BsonSerializer.Deserialize<ObjectArtifact>(document);

                var artifact = new LibraryDirectoryResponse()
                {
                    ItemId = foundArtifact.ItemId,
                    ParentId = foundArtifact.ParentId,
                    Name = foundArtifact.Name,
                    ArtifactType = foundArtifact.ArtifactType,
                    Color = foundArtifact.Color,
                    IsMoveToEnabled = targetArtifact != null ? IsAMoveToEnabledDirectory(foundArtifact, targetArtifact) : false,
                    IsSecretArtifact = _objectArtifactUtilityService.IsASecretArtifact(foundArtifact?.MetaData),
                };

                artifacts.Add(artifact);
            });

            var sortedArtifacts = artifacts.OrderBy(artifact => artifact.ArtifactType).ToList();
            return sortedArtifacts;
        }

        private List<LibraryDirectoryResponse> PrepareAllChildrenArtifactResponse(ObjectArtifact targetArtifact, List<ObjectArtifact> foundArtifacts)
        {
            var artifacts = new List<LibraryDirectoryResponse>();

            foundArtifacts.ForEach(foundArtifact =>
            {
                var artifact = new LibraryDirectoryResponse()
                {
                    ItemId = foundArtifact.ItemId,
                    ParentId = foundArtifact.ParentId,
                    Name = foundArtifact.Name,
                    ArtifactType = foundArtifact.ArtifactType,
                    Color = foundArtifact.Color,
                    IsMoveToEnabled = targetArtifact != null ? IsAMoveToEnabledDirectory(foundArtifact, targetArtifact) : false,
                    IsSecretArtifact = _objectArtifactUtilityService.IsASecretArtifact(foundArtifact?.MetaData)
                };

                artifacts.Add(artifact);
            });

            return artifacts.OrderBy(o => o.Name).ToList();
        }

        private bool IsAMoveToEnabledDirectory(ObjectArtifact artifact, ObjectArtifact targetArtifact)
        {
            return
                CanMoveObjectArtifact(artifact) &&
                artifact.ArtifactType == ArtifactTypeEnum.Folder &&
                artifact.ItemId != targetArtifact.ParentId;
        }

        private bool CanMoveObjectArtifact(ObjectArtifact artifact)
        {
            var securityContext = _securityContextProvider.GetSecurityContext();
            return
                artifact.RolesAllowedToWrite?.Any(r => securityContext.Roles.Contains(r)) == true ||
                artifact.IdsAllowedToWrite?.Contains(securityContext.UserId) == true;
        }
    }
}