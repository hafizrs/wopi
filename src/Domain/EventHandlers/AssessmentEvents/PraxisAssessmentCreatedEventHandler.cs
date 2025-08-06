using Microsoft.Extensions.Logging;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.Risk;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices;
using Selise.Ecap.SC.PraxisMonitor.Domain.EventHandlers.Contracts;
using System;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.EventHandlers.AssessmentEvents
{
    public class PraxisAssessmentCreatedEventHandler : IBaseEventHandler<GqlEvent<PraxisAssessment>>
    {
        private readonly IPraxisAssessmentService praxisAssessmentService;
        private readonly ILogger<PraxisAssessmentCreatedEventHandler> _logger;

        public PraxisAssessmentCreatedEventHandler(IPraxisAssessmentService praxisAssessmentService,
            ILogger<PraxisAssessmentCreatedEventHandler> logger)
        {
            this.praxisAssessmentService = praxisAssessmentService;
            _logger = logger;
        }
        public bool Handle(GqlEvent<PraxisAssessment> eventPayload)
        {
            try
            {
                AddRowLevelSecurity(eventPayload);
                praxisAssessmentService.UpdateRecentAssessment(eventPayload.EntityData.RiskId);

                return true;
            }
            catch (Exception e)
            {
                _logger.LogError("PraxisAssessmentCreatedEventHandler -> {ErrorMessage}", e.Message);
            }

            return false;
        }

        private void AddRowLevelSecurity(GqlEvent<PraxisAssessment> eventPayload)
        {
            string clientId = eventPayload.EntityData.ClientId;
            string assessmentId = eventPayload.EntityData.ItemId;

            praxisAssessmentService.AddRowLevelSecurity(assessmentId, clientId);
        }
    }
}
