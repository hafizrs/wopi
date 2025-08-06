using Selise.Ecap.Entities.PrimaryEntities.Dms;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.LibraryModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.EntityResponse;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.LibraryModule
{
    public class DocumentEditHistoryService: IDocumentEditHistoryService
    {
        private readonly IRepository _repository;
        private readonly IObjectArtifactUtilityService _objectArtifactUtilityService;

        public DocumentEditHistoryService
        (
            IRepository repository,
            IObjectArtifactUtilityService objectArtifactUtilityService
        )
        {
            _repository = repository;
            _objectArtifactUtilityService = objectArtifactUtilityService;
        }
        public IEnumerable<DocumentEditHistoryResponse> GetDocumentEditHistory(string objectArtifactId)
        {
            var histories = GeneratePreviousHistoryByArtifactId(objectArtifactId).GetAwaiter().GetResult();
            var documentEditHistoryResponse = GetDocumentEditHistroyResponse(histories);
            return documentEditHistoryResponse;
        }

        public async Task<List<DocumentEditMappingRecord>> GeneratePreviousHistoryByArtifactId(string artifactId)
        {
            var allHistories = new List<DocumentEditMappingRecord>();
            var history = new DocumentEditMappingRecord();
            while (history != null)
            {
                history = await _repository.GetItemAsync<DocumentEditMappingRecord>(d => d.ObjectArtifactId == artifactId);
                if (history != null)
                {
                    allHistories.Add(history);
                    artifactId = history.ParentObjectArtifactId;
                }
            }
            var artifact = _repository.GetItem<ObjectArtifact>(o => o.ItemId == artifactId);
            if (artifact != null && !allHistories.Exists(h => h.ObjectArtifactId == artifact.ItemId))
            {
                var versionKey = LibraryModuleConstants.ObjectArtifactMetaDataKeys[ObjectArtifactMetaDataKeyEnum.VERSION.ToString()];
                var version = _objectArtifactUtilityService.GetMetaDataValueByKey(artifact?.MetaData, versionKey);
                allHistories.Add
                (
                    new DocumentEditMappingRecord()
                    {
                        Version = version ?? "1.00",
                        SavedDocUserId = artifact.CreatedBy,
                        ArtifactVersionCreateDate = artifact.CreateDate,
                        ObjectArtifactId = artifact.ItemId
                    }
                );
            }
            return allHistories;
        }

        public List<DocumentEditMappingRecord> GenerateAllLinkedArtifactsByArtifactIds(List<string> artifactIds)
        {
            var allHistories = new List<DocumentEditMappingRecord>();
            while (artifactIds.Count > 0)
            {
                var histories = _repository.GetItems<DocumentEditMappingRecord>(d => artifactIds.Contains(d.ParentObjectArtifactId)).ToList();
                if (histories?.Count > 0)
                {
                    allHistories.AddRange(histories);
                    artifactIds = histories
                            .Select(h => h.ObjectArtifactId)
                            .Where(artifactId => !string.IsNullOrEmpty(artifactId) && !artifactIds.Contains(artifactId))
                            .ToList();
                } 
                else
                {
                    artifactIds = new List<string>();
                }
            }
            return allHistories;
        }

        private IEnumerable<DocumentEditHistoryResponse> GetDocumentEditHistroyResponse(List<DocumentEditMappingRecord> histories)
        {
            var documentEditHistoryResponse = new List<DocumentEditHistoryResponse>();
            foreach (var item in histories)
            {
                var praxisUser = _repository.GetItem<PraxisUser>(u => u.UserId == item.SavedDocUserId);
                var responseModel = new DocumentEditHistoryResponse
                {
                    Version = item.Version,
                    DocSaverDisplayName = praxisUser?.DisplayName ?? item.SavedDocUserDisplayName,
                    ArtifactVersionCreateDate = item.ArtifactVersionCreateDate,
                    VersionComparisonObjectArtifactId = item.VersionComparisonObjectArtifactId,
                    VersionComparisonFileStorageId = item.VersionComparisonFileStorageId,
                    ObjectArtifactId= item.ObjectArtifactId,
                    ParentVersion=item.ParentVersion,
                    NewVersionComparisonFileStorageId=item.NewVersionComparisonFileStorageId,
                    NewVersionComparisonObjectArtifactId=item.NewVersionComparisonObjectArtifactId
                };
                documentEditHistoryResponse.Add(responseModel);
            }
            return documentEditHistoryResponse;
        }
    }
}
