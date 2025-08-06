using Microsoft.Extensions.Logging;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using System;
using System.Threading.Tasks;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using System.Collections.Generic;
using Newtonsoft.Json;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Queries;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices;

namespace Selise.Ecap.SC.PraxisMonitor.QueryHandlers
{
    public class GetEquipmentMaintenanceForExternalUserQueryHandler : IQueryHandler<GetEquipmentMaintenanceForExternalUserQuery, QueryHandlerResponse>
    {
        private readonly ILogger<GetEquipmentMaintenanceForExternalUserQueryHandler> _logger;
        private readonly IPraxisEquipmentMaintenanceService _praxisEquipmentMaintenanceService;

        public GetEquipmentMaintenanceForExternalUserQueryHandler(
            ILogger<GetEquipmentMaintenanceForExternalUserQueryHandler> logger,
            IPraxisEquipmentMaintenanceService praxisEquipmentMaintenanceService
        )
        {
            _logger = logger;
            _praxisEquipmentMaintenanceService = praxisEquipmentMaintenanceService;
        }

        public QueryHandlerResponse Handle(GetEquipmentMaintenanceForExternalUserQuery query)
        {
            throw new NotImplementedException();
        }

        public async Task<QueryHandlerResponse> HandleAsync(GetEquipmentMaintenanceForExternalUserQuery query)
        {
            QueryHandlerResponse response = new QueryHandlerResponse();

            _logger.LogInformation("Enter in {HandlerName} with query: {QueryName}.",
                nameof(GetEquipmentMaintenanceForExternalUserQueryHandler), JsonConvert.SerializeObject(query));

            try
            {
                var dictionary = await _praxisEquipmentMaintenanceService.GetEquipmentMaintenanceForExternalUser(query);
                response.Data = dictionary;
                response.StatusCode = 0;
            }
            catch (Exception ex)
            {
                _logger.LogError("Exception in {HandlerName}. Exception Message: {Message}. Exception Details: {StackTrace}",
                    nameof(GetEquipmentMaintenanceForExternalUserQueryHandler), ex.Message, ex.StackTrace);
                response.StatusCode = 1;
                response.ErrorMessage = ex.Message;
            }

            _logger.LogInformation("Handled By {HandlerName} with response: {Response}.",
                nameof(GetEquipmentMaintenanceForExternalUserQueryHandler), JsonConvert.SerializeObject(response));

            return response;
        }
    }
}
