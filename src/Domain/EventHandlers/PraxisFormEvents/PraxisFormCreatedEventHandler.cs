using System;
using System.Collections.Generic;
using System.Linq;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices;
using Selise.Ecap.SC.PraxisMonitor.Domain.EventHandlers.Contracts;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.Shared;
using SeliseBlocks.Genesis.Framework.Events;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Dms;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.EventHandlers.PraxisFormEvents
{
    public class PraxisFormCreatedEventHandler : IBaseEventHandler<GqlEvent<PraxisForm>>
    {
        private readonly IPraxisFormService praxisFormService;      
        private readonly ILogger<PraxisFormCreatedEventHandler> _logger;
        private readonly IServiceClient _serviceClient;
        private readonly IGenericEventPublishService _genericEventPublishService;
        public PraxisFormCreatedEventHandler(IPraxisFormService praxisFormService,
            ILogger<PraxisFormCreatedEventHandler> logger,
            IServiceClient serviceClient,
            IGenericEventPublishService genericEventPublishService)
        {
            this.praxisFormService = praxisFormService;
            _logger = logger;
            _serviceClient = serviceClient;
            _genericEventPublishService = genericEventPublishService;
        }

        public bool Handle(GqlEvent<PraxisForm> eventPayload)
        {
            try
            {
                _logger.LogInformation("Enter event handler for praxisForm Create for purpose -> {PurposeOfFormKey} & clientId -> {ClientId}",
                    eventPayload.EntityData.PurposeOfFormKey, eventPayload.EntityData.ClientId);

                if (!string.IsNullOrEmpty(eventPayload.EntityData.PurposeOfFormKey) && eventPayload.EntityData.PurposeOfFormKey == "process-guide")
                {
                    var clientInfos = eventPayload.EntityData?.ProcessGuideCheckList?
                        .Where(f => f.ClientInfos != null)?
                        .SelectMany(f => f.ClientInfos)
                        .ToList() ?? new List<FormSpecificClientInfo>();
                    var clientIds = clientInfos.Select(f => f.ClientId).Distinct().ToList();
                    var orgIds = eventPayload.EntityData?.ProcessGuideCheckList?.FirstOrDefault(f => f.OrganizationIds != null && f.OrganizationIds.Count > 0)?.OrganizationIds;
                    praxisFormService.AddRowLevelSecurity(eventPayload.EntityData.ItemId, clientIds, orgIds);
                }
                else
                {
                    var clientInfos = eventPayload.EntityData?.ClientInfos?.ToList() ?? new List<FormSpecificClientInfo>();
                    var clientIds = clientInfos.Select(f => f.ClientId).Distinct().ToList();
                    var orgIds = eventPayload.EntityData?.OrganizationIds;
                    praxisFormService.AddRowLevelSecurity(eventPayload.EntityData.ItemId, clientIds, orgIds);
                }


                _genericEventPublishService.PublishDmsArtifactUsageReferenceEvent(eventPayload.EntityData);

                return true;
            }
            catch (Exception e)
            {
                _logger.LogError("Exception -> {ErrorMessage}", e.Message);
                return false;
            }
        }
    }
}
