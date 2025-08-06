using System.Threading.Tasks;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.LibraryModule;
using MongoDB.Bson;
using Selise.Ecap.Entities.PrimaryEntities.Dms;
using System.Collections.Generic;
using MongoDB.Driver;
using Microsoft.Extensions.Logging;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.CockpitModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Dms;
using SeliseBlocks.Genesis.Framework.PDS.Entity;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.OpenItem;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.ProcessGuide;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.Equipment;
using Newtonsoft.Json;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.LibraryModule
{
    public class LibraryFormUpdateEventHandlerService : ILibraryFormUpdateEventHandlerService
    {
        private readonly ILibraryFormService _libraryFormService;

        public LibraryFormUpdateEventHandlerService(
            ILibraryFormService libraryFormService
            )
        {
            _libraryFormService = libraryFormService;
        }

        public async Task<bool> InitiateLibraryFormUpdateAfterEffects(string artifactId)
        {
            var response = false;
            var objectArtifactData = await _libraryFormService.GetFormObjectArtifactById(artifactId);

            if (objectArtifactData != null)
            {
                await _libraryFormService.GenerateSignatureUrl(objectArtifactData.ItemId);
                await UpdateDependencies(objectArtifactData);
                return true;
            }
            return response;
        }

      
        private async Task UpdateDependencies(ObjectArtifact objectArtifactData) 
        {
            await _libraryFormService.UpdateArtifactWithEvent(objectArtifactData);
        }
    }
}