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

namespace Selise.Ecap.SC.PraxisMonitor.QueryHandlers.EquipmentModule
{
    public class GetEquipmentRightsQueryHandler : IQueryHandler<GetEquipmentRightsQuery, QueryHandlerResponse>
    {
        private readonly ILogger<GetEquipmentRightsQueryHandler> _logger;
        private readonly IAssignEquipmentAdminsService _service;

        public GetEquipmentRightsQueryHandler(
            ILogger<GetEquipmentRightsQueryHandler> logger,
            IAssignEquipmentAdminsService service)
        {
            _logger = logger;
            _service = service;
        }
        public QueryHandlerResponse Handle(GetEquipmentRightsQuery query)
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

        public async Task<QueryHandlerResponse> HandleAsync(GetEquipmentRightsQuery query)
        {
            if (query == null)
            {
                return CreateQueryHandlerResponse("Invalid Query: query is null");

            }
            if (query.IsOrganizationLevelRight && !string.IsNullOrWhiteSpace(query.EquipmentId))
            {
                return CreateQueryHandlerResponse("Invalid Query: EquipmentId must be null when IsOrganizationLevelRight is true");

            }
            if (!query.IsOrganizationLevelRight && string.IsNullOrWhiteSpace(query.EquipmentId))
            {
                return CreateQueryHandlerResponse("Invalid Query: EquipmentId cannot be null when IsOrganizationLevelRight is false");

            }

            var response = new QueryHandlerResponse();

            try
            {
                var data = await _service.GetEquipmentRights(query);
                if (data != null)
                {
                    response.Data = data.AssignedAdmins ?? new List<UserPraxisUserIdPair>();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Exception in {HandlerName}. Exception Message: {Message}. Exception Details: {StackTrace}",
                    nameof(GetEquipmentRightsQueryHandler), ex.Message, ex.StackTrace);
            }
            
            _logger.LogInformation("Handled by {HandlerName} with query: {Query}",
                nameof(GetEquipmentRightsQueryHandler), query);
            return response;
        }
    }
}