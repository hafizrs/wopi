using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using Selise.Ecap.Entities.PrimaryEntities.Dms;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.LibraryModule;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.GenericActionQueueService
{
    public class GenericActionQueueService : IGenericActionQueueService
    {
        private readonly ILogger<GenericActionQueueService> _logger;
        private readonly IRepository _repository;
        private readonly IObjectArtifactFileConversionService _objectArtifactFileConversionService;
        private readonly IObjectArtifactUtilityService _objectArtifactUtilityService;
        public GenericActionQueueService(
            ILogger<GenericActionQueueService> logger,
            IObjectArtifactUtilityService objectArtifactUtilityService,
            IObjectArtifactFileConversionService objectArtifactFileConversionService,
            IRepository repository)
        {
            _objectArtifactUtilityService = objectArtifactUtilityService;
            _logger = logger ;
            _objectArtifactFileConversionService = objectArtifactFileConversionService ;
            _repository = repository;
        }

        public async Task CreateHtmlFileIdFromArtifact(string objectArtifactId)
        {
            if (string.IsNullOrWhiteSpace(objectArtifactId))
            {
                _logger.LogWarning("Invalid or missing artifact ID.");
                return;
            }

            try
            {
                _logger.LogInformation("Starting HTML file generation for artifact ID: {ArtifactId}", objectArtifactId);

                var objectArtifactData = await _repository.GetItemAsync<ObjectArtifact>(o => o.ItemId == objectArtifactId); ;
                if (objectArtifactData == null)
                {
                    _logger.LogWarning("Artifact with ID {ArtifactId} not found.", objectArtifactId);
                    return;
                }

               

                var key = LibraryModuleConstants.ObjectArtifactMetaDataKeys[$"{ObjectArtifactMetaDataKeyEnum.PROCESSED_ORIGINAL_HTML_FILE_ID}"];
               
                if (!string.IsNullOrEmpty(_objectArtifactUtilityService.GetMetaDataValueByKey(objectArtifactData.MetaData,
                        key)))
                {
                    return;
                }
                var htmlFileId = await _objectArtifactFileConversionService.ConvertToHtmlAndUpload(objectArtifactData.FileStorageId);
                if (string.IsNullOrEmpty(htmlFileId))
                {
                    _logger.LogWarning("Failed to generate HTML file for Artifact ID: {ArtifactId}", objectArtifactId);
                    return;
                }

                objectArtifactData.MetaData[key].Value = htmlFileId;

                await _repository.UpdateAsync(
                    o => o.ItemId.Equals(objectArtifactData.ItemId),
                    PraxisConstants.PraxisTenant,
                    objectArtifactData);

                _logger.LogInformation("HTML file ID {HtmlFileId} saved successfully for artifact ID {ArtifactId}.",
                    htmlFileId, objectArtifactId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while creating the HTML file ID for artifact ID: {ArtifactId}", objectArtifactId);
                
            }
        }

        
    }


}
