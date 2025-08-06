using System;
using System.Threading.Tasks;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.CurrentStatus;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Queries;
using Selise.Ecap.SC.PraxisMonitor.Queries;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices;
using ZXing;

namespace Selise.Ecap.SC.PraxisMonitor.QueryHandlers
{
    public class GetSupplierGroupNameQueryHandler : IQueryHandler<GetSupplierGroupNameQuery, QueryHandlerResponse>
    {
        private readonly ILogger<GetSupplierGroupNameQueryHandler> _logger;
        private readonly IRepository _repository;

        public GetSupplierGroupNameQueryHandler(
            ILogger<GetSupplierGroupNameQueryHandler> logger,
            IRepository repository)
        {
            _logger = logger;
            _repository = repository;
        }
        [Invocable]
        public QueryHandlerResponse Handle(GetSupplierGroupNameQuery query)
        {
            throw new NotImplementedException();
        }

        public async Task<QueryHandlerResponse> HandleAsync(GetSupplierGroupNameQuery query)
        {
            var response = new QueryHandlerResponse();
            try
            {
                response.Data = query.SupplierGroupName;

                PraxisClientSupplierGroupName groupName = await _repository.GetItemAsync<PraxisClientSupplierGroupName>(x => x.PraxisClientId == query.PraxisClientId);

                if (groupName != null)
                {
                    response.Data = groupName.SupplierGroupName;
                }

            }
            catch (Exception ex)
            {
                _logger.LogError("Exception occured during incident creation");
                _logger.LogError("Exception Message: {Message} Exception Details: {StackTrace}", ex.Message, ex.StackTrace);
                response.ErrorMessage = "Exception occured during incident creation";
            }
            return response;

        }

    }
}
