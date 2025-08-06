using System;
using Microsoft.Extensions.Logging;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices;
using Selise.Ecap.SC.PraxisMonitor.Domain.EventHandlers.Contracts;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using System.Collections.Generic;
using System.Linq;
using Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.Services;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.EventHandlers.PraxisFormEvents
{
    public class PraxisFormUpdatedEventHandler : IBaseEventHandler<GqlEvent<PraxisForm>>
    {
        private readonly IPraxisFormService _praxisFormService;
        private readonly ILogger<PraxisFormUpdatedEventHandler> _logger;
        private readonly IGenericEventPublishService _genericEventPublishService;
        public PraxisFormUpdatedEventHandler(IPraxisFormService praxisFormService,
            ILogger<PraxisFormUpdatedEventHandler> logger,
            IGenericEventPublishService genericEventPublishService)
        {
            _praxisFormService = praxisFormService;
            _logger = logger;
            _genericEventPublishService = genericEventPublishService;
        }
        public bool Handle(GqlEvent<PraxisForm> eventPayload)
        {
            try
            {
                _logger.LogInformation("Enter into the praxis update handler for itemId -> {ItemId}",eventPayload.Filter);
                _praxisFormService.UpdatePraxisFormRelatedData(eventPayload.EntityData, eventPayload.Filter);
                if (!string.IsNullOrEmpty(eventPayload.EntityData.PurposeOfFormKey) && eventPayload.EntityData.PurposeOfFormKey == "process-guide")
                {
                    var clientInfos = eventPayload.EntityData?.ProcessGuideCheckList?
                        .Where(f => f.ClientInfos != null)?
                        .SelectMany(f => f.ClientInfos)
                        .ToList() ?? new List<FormSpecificClientInfo>();
                    var clientIds = clientInfos.Select(f => f.ClientId).Distinct().ToList();
                    var orgIds = eventPayload.EntityData?.ProcessGuideCheckList?.FirstOrDefault(f => f.OrganizationIds != null && f.OrganizationIds.Count > 0)?.OrganizationIds ?? new List<string>();
                    _praxisFormService.AddRowLevelSecurity(eventPayload.Filter, clientIds, orgIds);
                }
                else
                {
                    var clientInfos = eventPayload.EntityData?.ClientInfos?.ToList() ?? new List<FormSpecificClientInfo>();
                    var clientIds = clientInfos.Select(f => f.ClientId).Distinct().ToList();
                    var orgIds = eventPayload.EntityData?.OrganizationIds ?? new List<string>();
                    _praxisFormService.AddRowLevelSecurity(eventPayload.Filter, clientIds, orgIds);
                }

                if (!string.IsNullOrEmpty(eventPayload.Filter))
                {
                    var form = _praxisFormService.GetPraxisFromById(eventPayload.Filter);
                    _genericEventPublishService.PublishDmsArtifactUsageReferenceEvent(form);
                }
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
