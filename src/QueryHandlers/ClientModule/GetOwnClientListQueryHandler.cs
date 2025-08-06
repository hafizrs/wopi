using System;
using System.Threading.Tasks;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using Selise.Ecap.SC.PraxisMonitor.Queries;

namespace Selise.Ecap.SC.PraxisMonitor.QueryHandlers
{
    public class GetOwnClientListQueryHandler : IQueryHandler<GetOwnClientListQuery, QueryHandlerResponse>
    {
        private readonly IPraxisClientService _clientService;

        public GetOwnClientListQueryHandler(IPraxisClientService clientService)
        {
            _clientService = clientService;
        }

        public QueryHandlerResponse Handle(GetOwnClientListQuery query)
        {
            throw new System.NotImplementedException();
        }

        public async Task<QueryHandlerResponse> HandleAsync(GetOwnClientListQuery query)
        {
            var response = new QueryHandlerResponse();
            try
            {
                var responseFromService = await _clientService.GetOwnPraxisClientList(
                    query.LoggedInPraxisUserId, query.ForProcessGuideForm ?? false
                );
                response.Results = responseFromService.Results;
                response.TotalCount = responseFromService.TotalRecordCount;
                response.ErrorMessage = responseFromService.ErrorMessage;
            }
            catch (Exception e)
            {
                response.ErrorMessage = e.Message;
            }

            return response;
        }
    }
}