using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.ProcessGuide;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.CockpitModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Entities.CirsModule;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.Services
{
    public class PraxisProcessGuideDeleteService : IPraxisProcessGuideDeleteService
    {
        private readonly ILogger<PraxisProcessGuideDeleteService> _logger;
        private readonly IRepository _repository;
        private readonly ICockpitSummaryCommandService _cockpitSummaryCommandService;
        private readonly IReportingTaskCockpitSummaryCommandService _reportingTaskCockpitSummaryCommandService;

        public PraxisProcessGuideDeleteService(
            ILogger<PraxisProcessGuideDeleteService> logger,
            IRepository repository,
            ICockpitSummaryCommandService cockpitSummaryCommandService,
            IReportingTaskCockpitSummaryCommandService reportingTaskCockpitSummaryCommandService
        )
        {
            _logger = logger;
            _repository = repository;
            _cockpitSummaryCommandService = cockpitSummaryCommandService;
            _reportingTaskCockpitSummaryCommandService = reportingTaskCockpitSummaryCommandService;
        }

        public async Task DeleteClonedProcessGuide(string processGuideId)
        {
            try
            {
                var existingProcessGuide = _repository.GetItem<PraxisProcessGuide>(x => x.ItemId == processGuideId && !x.IsMarkedToDelete);
                if (existingProcessGuide != null && existingProcessGuide.IsAClonedProcessGuide)
                {
                    var havePgAnswer = await _repository.ExistsAsync<PraxisProcessGuideAnswer>(pg => pg.ProcessGuideId == processGuideId && pg.Answers != null && pg.Answers.Any());
                    if (!havePgAnswer)
                    {
                        await _repository.DeleteAsync<PraxisProcessGuide>(x => x.ItemId == existingProcessGuide.ItemId);
                        await _repository.DeleteAsync<PraxisProcessGuideConfig>(x => x.ItemId == existingProcessGuide.PraxisProcessGuideConfigId);

                        if (existingProcessGuide.RelatedEntityName == nameof(CirsGenericReport))
                        {
                            await _reportingTaskCockpitSummaryCommandService.OnProcessGuideDeletionUpdateSummary(processGuideId);
                        }
                        else
                        {
                            await _cockpitSummaryCommandService.DeleteSummaryAsync(new List<string> { existingProcessGuide.ItemId },
                                                        CockpitTypeNameEnum.PraxisProcessGuide);
                        }
                        
                        

                    }
                }
            }
            catch (Exception e)
            {
                _logger.LogError("Error occurred on DeleteClonedProcessGuide -> {Message}", e.Message);
            }
        }
    }
}