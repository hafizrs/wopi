using System;
using Microsoft.Extensions.Logging;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.Risk;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices;
using Selise.Ecap.SC.PraxisMonitor.Domain.EventHandlers.Contracts;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.EventHandlers.AssessmentEvents
{
    public class PraxisAssessmentUpdatedEventHandler : IBaseEventHandler<GqlEvent<PraxisAssessment>>
    {
        private readonly IPraxisAssessmentService praxisAssessmentService;
        private readonly IPraxisRiskService praxisRiskService;
        private readonly ILogger<PraxisAssessmentUpdatedEventHandler> _logger;
        private readonly IRepository repository;

        public PraxisAssessmentUpdatedEventHandler(IPraxisAssessmentService praxisAssessmentService,
            IPraxisRiskService praxisRiskService,
            ILogger<PraxisAssessmentUpdatedEventHandler> logger,
            IRepository repository
        )
        {
            this.praxisAssessmentService = praxisAssessmentService;
            this.praxisRiskService = praxisRiskService;
            _logger = logger;
            this.repository = repository;
        }

        public bool Handle(GqlEvent<PraxisAssessment> eventPayload)
        {
            try
            {
                UpdateRecentAssessment(eventPayload);

                return true;
            }
            catch (Exception e)
            {
                _logger.LogError("PraxisAssessmentCreatedEventHandler -> {ErrorMessage}", e.Message);
            }

            return false;
        }

        private void UpdateRecentAssessment(GqlEvent<PraxisAssessment> eventPayload)
        {
            PraxisRisk risk = praxisRiskService.GetPraxisRisk(eventPayload.EntityData.RiskId);

            if (risk != null && risk.RecentAssessment.ItemId.Equals(eventPayload.Filter))
            {
                PraxisAssessment assessment = praxisAssessmentService.GetPraxisAssessment(eventPayload.Filter);

                if (assessment != null)
                {
                    risk.RecentAssessment = assessment;
                    repository.Update<PraxisRisk>(r => r.ItemId.Equals(risk.ItemId), risk);

                    _logger.LogInformation("Recent Assessment of Risk {ItemId} updated due to assessment updated", risk.ItemId);
                }
            }
        }
    }
}
