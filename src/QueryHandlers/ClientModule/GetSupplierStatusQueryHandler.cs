using Microsoft.Extensions.Logging;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Queries;
using Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.CurrentStatus;
using Selise.Ecap.SC.PraxisMonitor.Queries;
using Selise.Ecap.SC.PraxisMonitor.QueryHandlers.CockpitModule;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.QueryHandlers
{
    public class GetSupplierStatusQueryHandler : IQueryHandler<GetSupplierStatusQuery, QueryHandlerResponse>
    {
        private readonly ILogger<GetSupplierStatusQueryHandler> _logger;
        private readonly GetCurrentStatusForSupplierData _statusForSupplierData;
        public GetSupplierStatusQueryHandler(
             ILogger<GetSupplierStatusQueryHandler> logger,
            GetCurrentStatusForSupplierData statusForSupplierData
            )
        {
            _logger = logger;
            _statusForSupplierData = statusForSupplierData;
        }

        public QueryHandlerResponse Handle(GetSupplierStatusQuery query)
        {
            throw new NotImplementedException();
        }

        public Task<QueryHandlerResponse> HandleAsync(GetSupplierStatusQuery query)
        {
         

            _logger.LogInformation("Enter {HandlerName} with query: {Query}.",
               nameof(GetSupplierStatusQuery), query);

            var response = new QueryHandlerResponse();
            try
            {
                var data = _statusForSupplierData.GetSupplierStatus(query);
                response.Data = data;
            }
            catch (Exception e)
            {
                _logger.LogError("Error in {HandlerName}. Error Message -> {Message}. Error Details -> {StackTrace}",
                    nameof(GetSupplierStatusQuery), e.Message, e.StackTrace);
            }

            _logger.LogInformation("Handled by {HandlerName} with query: {Query}.",
                nameof(GetSupplierStatusQuery), query);
            return Task.FromResult(response);
        }
    }
}
