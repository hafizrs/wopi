using System;
using System.Collections.Generic;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Queries.EquipmentModule;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.EquipmentModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Entities.EquipmentModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Entities.LibraryModule;
using Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.Services;
using Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.EquipmentModule;

namespace Selise.Ecap.SC.PraxisMonitor.QueryHandlers.EquipmentModule
{
    public class GetEquipementRoomQueryHandler : IQueryHandler<GetEquipementRoomQuery, QueryHandlerResponse>
    {
        private readonly ILogger<GetEquipementRoomQueryHandler> _logger;
        private readonly IPraxisEquipmentQueryService _service;

        public GetEquipementRoomQueryHandler(
            ILogger<GetEquipementRoomQueryHandler> logger,
            IPraxisEquipmentQueryService service)
        {
            _logger = logger;
            _service = service;
        }
        public QueryHandlerResponse Handle(GetEquipementRoomQuery query)
        {
            return HandleAsync(query).Result;
        }

        private QueryHandlerResponse CreateQueryHandlerResponse(string message)
        {
            return new QueryHandlerResponse()
            {
                Results = null,
                ErrorMessage = message
            };
        }

        public async Task<QueryHandlerResponse> HandleAsync(GetEquipementRoomQuery query)
        {
            if (query == null)
            {
                return CreateQueryHandlerResponse("Invalid Query: query is null");

            }
           
            var response = new QueryHandlerResponse();

            try
            {
                var queryResult = await _service.GetPraxisRoomForEquipement(query);
                response.Results = queryResult.Results;
                response.TotalCount = queryResult.TotalRecordCount;
                response.ErrorMessage = queryResult.ErrorMessage;
                response.StatusCode = queryResult.StatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception in {HandlerName}. Exception Message: {Message}. Exception Details: {StackTrace}.",
                    nameof(GetEquipementRoomQueryHandler), ex.Message, ex.StackTrace);
            }
            
            _logger.LogInformation("Handled by {HandlerName} with query: {Query}",
                nameof(GetEquipementQueryHandler), query);
            return response;
        }
    }
}