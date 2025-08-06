using Microsoft.Extensions.Logging;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.Risk;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Queries;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;

namespace Selise.Ecap.SC.PraxisMonitor.QueryHandlers
{
    class UpdateMeasuresTakenPendingCountQueryHandler : IQueryHandler<UpdateMeasuresTakenPendingCountQuery, QueryHandlerResponse>
    {
        private readonly IRepository _repository;
        private readonly ILogger<UpdateMeasuresTakenPendingCountQueryHandler> _logger;

        public UpdateMeasuresTakenPendingCountQueryHandler(
            IRepository repository,
            ILogger<UpdateMeasuresTakenPendingCountQueryHandler> logger)
        {
            _repository = repository;
            _logger = logger;
        }
        [Invocable]
        public QueryHandlerResponse Handle(UpdateMeasuresTakenPendingCountQuery query)
        {
            _logger.LogInformation("Enter {HandlerName} with query: {Query}.",
                nameof(UpdateMeasuresTakenPendingCountQueryHandler), JsonConvert.SerializeObject(query));

            try
            {
                var existingRisk = _repository.GetItem<PraxisRisk>(r => r.ItemId == query.RiskId);
                if (existingRisk != null)
                {
                    _logger.LogInformation("Existing Risk measures taken: {MeasuresTaken} and measures pending: {MeasuresPending}.", existingRisk.MeasuresTaken, existingRisk.MeasuresPending);
                    var measuresTaken = existingRisk.MeasuresTaken + query.OfflineMeasuresTaken;
                    var measuresPending = existingRisk.MeasuresPending == 0
                        ? (int)existingRisk.MeasuresPending
                        : (int)existingRisk.MeasuresPending - query.OfflineMeasuresTaken;
                    measuresPending = Math.Abs(measuresPending);
                    existingRisk.MeasuresTaken = measuresTaken;
                    existingRisk.MeasuresPending = measuresPending;
                    existingRisk.OfflineMeasuresTaken = 0;

                    var updates = new Dictionary<string, object>
                        {
                            {"MeasuresTaken", existingRisk.MeasuresTaken},
                            {"MeasuresPending", existingRisk.MeasuresPending},
                            {"OfflineMeasuresTaken", existingRisk.OfflineMeasuresTaken},
                            {"LastUpdateDate", DateTime.UtcNow.ToLocalTime() }
                        };

                    _repository.UpdateAsync<PraxisRisk>(r => r.ItemId == existingRisk.ItemId, updates).Wait();
                    _logger.LogInformation("Data has been successfully updated to {EntityName} entity with ItemId: {ItemId}. Measures taken: {MeasuresTaken} and measures pending: {MeasuresPending} and LastUpdatedDate: {LastUpdateDate}.",
                        nameof(PraxisRisk), existingRisk.ItemId, existingRisk.MeasuresTaken, existingRisk.MeasuresPending, existingRisk.LastUpdateDate);

                    var updatedRisk = _repository.GetItem<PraxisRisk>(r => r.ItemId == existingRisk.ItemId);
                    if (updatedRisk != null)
                    {
                        _logger.LogInformation("Updated Risk Information: Measures taken: {MeasuresTaken} and measures pending: {MeasuresPending} and LastUpdatedDate: {LastUpdateDate}",
                            updatedRisk.MeasuresTaken, updatedRisk.MeasuresPending, updatedRisk.LastUpdateDate);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in {HandlerName}. Error Message: {Message}. Error Details: {StackTrace}.",
                    nameof(UpdateMeasuresTakenPendingCountQueryHandler), ex.Message, ex.StackTrace);
                return new QueryHandlerResponse { StatusCode = 500, ErrorMessage = ex.Message };
            }

            _logger.LogInformation("Handled By {HandlerName} with response: {Response}.",
                nameof(UpdateMeasuresTakenPendingCountQueryHandler), JsonConvert.SerializeObject(new QueryHandlerResponse { StatusCode = 200 }));

            return new QueryHandlerResponse { StatusCode = 200 };
        }

        public Task<QueryHandlerResponse> HandleAsync(UpdateMeasuresTakenPendingCountQuery query)
        {
            throw new NotImplementedException();
        }
    }
}
