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
    public class GetEquipmentForExternalUserQueryHandler : IQueryHandler<GetEquipmentForExternalUserQuery, QueryHandlerResponse>
    {
        private readonly ILogger<GetEquipmentForExternalUserQueryHandler> _logger;
        private readonly IPraxisEquipmentMaintenanceService _praxisEquipmentMaintenanceService;

        public GetEquipmentForExternalUserQueryHandler(
            ILogger<GetEquipmentForExternalUserQueryHandler> logger,
            IPraxisEquipmentMaintenanceService praxisEquipmentMaintenanceService
        )
        {
            _logger = logger;
            _praxisEquipmentMaintenanceService = praxisEquipmentMaintenanceService;
        }

        public QueryHandlerResponse Handle(GetEquipmentForExternalUserQuery query)
        {
            throw new NotImplementedException();
        }

        public async Task<QueryHandlerResponse> HandleAsync(GetEquipmentForExternalUserQuery query)
        {
            QueryHandlerResponse response = new QueryHandlerResponse();

            _logger.LogInformation("Enter in {HandlerName} with query: {QueryName}.",
                nameof(GetEquipmentForExternalUserQueryHandler), JsonConvert.SerializeObject(query));

            try
            {
                if (string.IsNullOrEmpty(query.EquipmentId))
                {
                    response.StatusCode = 1;
                    response.ErrorMessage = "invalid EquipmentMaintenanceId";
                }
                else 
                {
                    var dictionary = await _praxisEquipmentMaintenanceService.GetEquipmentForExternalUser(query);
                    response.Data = dictionary;
                    response.StatusCode = 0;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Exception in {HandlerName}. Exception Message: {Message}. Exception Details: {StackTrace}",
                    nameof(GetEquipmentForExternalUserQueryHandler), ex.Message, ex.StackTrace);
                response.StatusCode = 1;
                response.ErrorMessage = ex.Message;
            }

            _logger.LogInformation("Handled By {HandlerName} with response: {Response}.",
                nameof(GetEquipmentForExternalUserQueryHandler), JsonConvert.SerializeObject(response));

            return response;
        }
    }
}
