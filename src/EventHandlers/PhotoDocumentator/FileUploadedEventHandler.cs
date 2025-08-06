using EventHandlers.Models;
using EventHandlers.Services;
using Microsoft.Extensions.Logging;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using System.Linq;
using System.Threading.Tasks;
using Selise.Ecap.SC.PraxisMonitor.Events;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.LibraryModule;
using System.Collections.Generic;
using Selise.Ecap.Entities.PrimaryEntities.GermanRailway;

namespace EventHandlers.PhotoDocumentator
{
    public class FileUploadedEventHandler : IEventHandler<FileUploadedEvent, bool>
    {
        private readonly IFileConversionService fileConversionService;
        private readonly ISecurityContextProvider _securityContextProvider;
        private readonly ILogger<FileUploadedEventHandler> _logger;
        private readonly ILibraryFileVersionComparisonService _fileVersionComparisonService;
        public FileUploadedEventHandler(
            IFileConversionService fileConversionSvc,
            ISecurityContextProvider securityContextProvider,
            ILogger<FileUploadedEventHandler> logger,
            ILibraryFileVersionComparisonService fileVersionComparisonService
            )
        {
            fileConversionService = fileConversionSvc;
            _securityContextProvider = securityContextProvider;
            _logger = logger;
            _fileVersionComparisonService = fileVersionComparisonService;
        }

        public bool Handle(FileUploadedEvent @event)
        {
            throw new System.NotImplementedException();
        }

        public async Task<bool> HandleAsync(FileUploadedEvent @event)
        {
            var securityContext = _securityContextProvider.GetSecurityContext();
            if (@event.TenantId.ToString().ToLower() == securityContext.TenantId.ToLower())
            {
                _logger.LogInformation("Received uploaded file with id:" + @event.FileId);
                fileConversionService.Convert(@event.FileId, @event.Tags.ToList(), ImageDimension.Dimensions.Keys.ToList());
                await _fileVersionComparisonService.HandleLibraryFileVersionComparisonByFileStorageId(@event.FileId);

            }

            return true;
        }




    }
}
