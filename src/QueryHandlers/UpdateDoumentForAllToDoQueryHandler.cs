using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Queries;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.OpenItem;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;

namespace Selise.Ecap.SC.PraxisMonitor.QueryHandlers
{
    public class UpdateDoumentForAllToDoQueryHandler : IQueryHandler<UpdateDocumentQuery, UpdateDocumentResponse>
    {
        private readonly IRepository _repository;
        private readonly ILogger<UpdateDoumentForAllToDoQueryHandler> _logger;

        public UpdateDoumentForAllToDoQueryHandler(
            IRepository repository,
            ILogger<UpdateDoumentForAllToDoQueryHandler> logger)
        {
            _repository = repository;
            _logger = logger;
        }

        [Invocable]
        public UpdateDocumentResponse Handle(UpdateDocumentQuery query)
        {
            var response = new UpdateDocumentResponse();

            _logger.LogInformation("Enter {HandlerName} with query: {Query}.",
                nameof(UpdateDoumentForAllToDoQueryHandler), JsonConvert.SerializeObject(query));

            try
            {
                var existingOpenItems = _repository
                    .GetItems<PraxisOpenItem>(i => i.OpenItemConfigId == query.OpenItemConfigId && !i.IsMarkedToDelete).ToList();
                foreach (var existingOpenItem in existingOpenItems)
                {
                    existingOpenItem.DocumentInfo = query.DocumentInfo;

                    var updates = new Dictionary<string, object>
                    {
                        {"LastUpdateDate", DateTime.UtcNow.ToLocalTime() },
                        {"DocumentInfo", existingOpenItem.DocumentInfo }
                    };

                    _repository.UpdateAsync<PraxisOpenItem>(i => i.ItemId == existingOpenItem.ItemId, updates).Wait();
                    _logger.LogInformation("Data has been successfully updated to {EntityName} entity with ItemId: {ItemId}.",
                        nameof(PraxisOpenItem), existingOpenItem.ItemId);
                }

                _logger.LogInformation("Handled By {HandlerName} with response: {Response}.",
                    nameof(UpdateDoumentForAllToDoQueryHandler), JsonConvert.SerializeObject(response));

                response.StatusCode = 200;
                response.Message = string.Empty;
                return response;

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in {HandlerName}. Error Message: {Message}. Error Details: {StackTrace}.",
                    nameof(UpdateDoumentForAllToDoQueryHandler), ex.Message, ex.StackTrace);
                
                response.StatusCode = 500;
                response.Message = ex.Message;
                return response;
            }
        }

        public Task<UpdateDocumentResponse> HandleAsync(UpdateDocumentQuery query)
        {
            throw new NotImplementedException();
        }
    }
}
