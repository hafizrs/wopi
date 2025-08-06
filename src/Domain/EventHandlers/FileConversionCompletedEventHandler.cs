using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Commands;
using Selise.Ecap.SC.PraxisMonitor.Domain.Events;
using Selise.Ecap.SC.PraxisMonitor.Contracts;
using System;
using System.Threading.Tasks;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.EventHandlers
{
    public class FileConversionCompletedEventHandler : IEventHandler<FileConversionCompletedEvent, bool>
    {
        private readonly CommandHandler commandService;
        private readonly ILogger<FileConversionCompletedEventHandler> _logger;
        private readonly IPraxisFileConversionService fileConversionService;

        public FileConversionCompletedEventHandler(CommandHandler commandService,
            ILogger<FileConversionCompletedEventHandler> logger,
            IPraxisFileConversionService fileConversionSvc)
        {
            _logger = logger;
            this.commandService = commandService;
            this.fileConversionService = fileConversionSvc;
        }

        public bool Handle(FileConversionCompletedEvent @event)
        {
            throw new NotImplementedException();
        }

        public async Task<bool> HandleAsync(FileConversionCompletedEvent @event)
        {
            try
            {
                if (@event.Request != null &&
                    @event.Request.RequestProperties.ContainsKey("ImageResizeSetting") &&
                    @event.TenantId.ToString().ToUpper().Equals(PraxisConstants.PraxisTenant)
                )
                {
                    var payload =
                        JsonConvert.DeserializeObject<ImageResizeSetting>(@event.Request
                            .RequestProperties["ImageResizeSetting"].ToString());

                    if (payload.ParentEntities.Length > 0)
                    {
                        var parentEntity = payload.ParentEntities[0];

                        if (parentEntity.TagPrefix != PraxisTag.ResizeImage)
                        {
                            PraxisImageUpdateCommand command = new PraxisImageUpdateCommand
                            {
                                Dimensions = payload.Dimensions,
                                EntityItemId = parentEntity.Id,
                                EntityName = parentEntity.Name,
                                FileId = payload.SourceFileId,
                                FileTag = parentEntity.TagPrefix
                            };

                            _logger.LogInformation("Got FileConversionCompletedEvent -> {Command}.", command);

                            await commandService.SubmitAsync<PraxisImageUpdateCommand, CommandResponse>(command);
                        }

                    }
                }

                if (@event.Request != null &&
                    @event.Request.RequestProperties.ContainsKey("ImageResizeSetting") &&
                    @event.TenantId.ToString().ToUpper().Equals(PraxisConstants.PraxisTenant)
                )
                {
                    var imageResizeSetting = JsonConvert.DeserializeObject<PraxisImageConversionSetting>(@event.Request.RequestProperties["ImageResizeSetting"].ToString());
                    var parentEntities = imageResizeSetting.ParentEntities;

                    _logger.LogInformation("Received converted file event with source file id: {SourceFileId}.", imageResizeSetting.SourceFileId);

                    foreach (var parentEntity in parentEntities)
                    {
                        if(parentEntity.TagPrefix == PraxisTag.ResizeImage)
                        {
                            fileConversionService.AddConvertedFileMaps(imageResizeSetting.SourceFileId);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred in FileConversionCompletedEventHandler.");
            }

            return false;
        }
    }
}