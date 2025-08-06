using System;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Dms;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.Entities.PrimaryEntities.DWT;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Entities.LibraryModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Entities.CirsModule;
using Selise.Ecap.Entities.PrimaryEntities.Dms;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.LibraryModule
{
    public class DeleteDmsArtifactUsageReferenceService : IDeleteDmsArtifactUsageReferenceService
    {
        private readonly IRepository _repository;
        private readonly ILogger<DeleteDmsArtifactUsageReferenceService> _logger;

        public DeleteDmsArtifactUsageReferenceService(
            IRepository repository,
            ILogger<DeleteDmsArtifactUsageReferenceService> logger)
        {
            _logger = logger;
            _repository = repository;
        }

        public async Task DeleteDataForClient(string clientId, string orgId = null)
        {
            try
            {
                var artifactUsageReferences = _repository.GetItems<DmsArtifactUsageReference>(x => x.ClientInfos != null && x.ClientInfos.Any(c => c.ClientId.Equals(clientId)));

                foreach(var artifactUsageReference in artifactUsageReferences)
                {
                    var hadClient = artifactUsageReference.ClientInfos?.Any(c => !c.ClientId.Equals(clientId)) == true;
                    if (artifactUsageReference != null && artifactUsageReference.ClientInfos.Count() > 0)
                    {
                        artifactUsageReference.ClientInfos = artifactUsageReference.ClientInfos?
                            .Where(c => !c.ClientId.Equals(clientId))?
                            .ToList();

                        await _repository.UpdateAsync<DmsArtifactUsageReference>(x => x.Equals(artifactUsageReference.ItemId), artifactUsageReference);
                    }

                    if (artifactUsageReference != null && 
                        !(artifactUsageReference.ClientInfos?.Count > 0 || artifactUsageReference.OrganizationIds?.Count > 0 || !string.IsNullOrEmpty(artifactUsageReference.OrganizationId))
                        && !string.IsNullOrEmpty(artifactUsageReference.ObjectArtifactId))
                    {
                        var objectArtifact = await _repository.GetItemAsync<ObjectArtifact>(oa => oa.ItemId.Equals(artifactUsageReference.ObjectArtifactId));

                        if (objectArtifact != null && objectArtifact.MetaData != null
                            && objectArtifact.MetaData.TryGetValue("IsUsedInAnotherEntity", out var usedEntity)
                            && objectArtifact.MetaData.TryGetValue("ArtifactUsageReferenceCounter", out var counter)
                            && int.TryParse(counter.Value, out int currentValue))
                        {
                            if (currentValue > 0)
                            {
                                counter.Value = (currentValue - 1).ToString();

                                if (counter.Value == "0")
                                {
                                    usedEntity.Value = "0";
                                }

                                await _repository.UpdateAsync<ObjectArtifact>(x => x.ItemId.Equals(objectArtifact.ItemId), objectArtifact);
                            }
                        }
                    }
                }
               
            }
            catch (Exception ex)
            {
                _logger.LogError("Exception occured in {Name} in GenerateCode with error -> {ExMessage} trace -> {ExStackTrace}", GetType().Name, ex.Message, ex.StackTrace);
            }
        }
    }
}