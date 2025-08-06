using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.RiqsInterface;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.RiqsInterfaces;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using System;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.QueryHandlers.RiqsInterface
{

    public class GetInterfaceManagerLoginInfoQueryHandler : IQueryHandler<GetInterfaceManagerLoginInfoQuery, QueryHandlerResponse>
    {
        private readonly IRiqsInterfaceManagerLoginFlowService _riqsInterfaceManagerLoginFlowService;

        public GetInterfaceManagerLoginInfoQueryHandler(IRiqsInterfaceManagerLoginFlowService riqsInterfaceManagerLoginFlowService)
        {
            _riqsInterfaceManagerLoginFlowService = riqsInterfaceManagerLoginFlowService;
        }

        public QueryHandlerResponse Handle(GetInterfaceManagerLoginInfoQuery query)
        {
            throw new NotImplementedException();
        }

        public async Task<QueryHandlerResponse> HandleAsync(GetInterfaceManagerLoginInfoQuery query)
        {

            try
            {
                if (query == null || string.IsNullOrEmpty(query.Provider))
                {
                    return new QueryHandlerResponse
                    {
                        Data = null,
                        Results = null,
                        ErrorMessage = "Invalid Query Fields"
                    };
                }

                var results = await _riqsInterfaceManagerLoginFlowService.GetInterfaceManagerLoginInfo(query.Provider);

                if (results == null)
                {
                    return new QueryHandlerResponse
                    {
                        StatusCode = 404,
                        ErrorMessage = "Results not found or no information available.",
                        Data = null,
                        Results = null,
                        TotalCount = 0
                    };
                }

                return new QueryHandlerResponse
                {
                    StatusCode = 200,
                    ErrorMessage = null,
                    Data = results,
                    Results = null,
                    TotalCount = 1
                };
            }
            catch (Exception ex)
            {

                return new QueryHandlerResponse
                {
                    StatusCode = 500,
                    ErrorMessage = $"An error occurred: {ex.Message}",
                    Data = null,
                    Results = null,
                    TotalCount = 0
                };
            }
        }

    }
}


