using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.RiqsInterfaces;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using Selise.Ecap.SC.PraxisMonitor.Queries;
using System;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.QueryHandlers
{
    public class GetInterfaceManagerSiteItemsQueryHandler : IQueryHandler<GetInterfaceManagerSiteItemsQuery, QueryHandlerResponse>
    {
        private readonly IRiqsInterfaceManagerService _riqsInterfaceService;

        public GetInterfaceManagerSiteItemsQueryHandler(IRiqsInterfaceManagerService riqsInterfaceService)
        {
            _riqsInterfaceService = riqsInterfaceService;
        }

        public QueryHandlerResponse Handle(GetInterfaceManagerSiteItemsQuery query)
        {
            throw new NotImplementedException();
        }

        public async Task<QueryHandlerResponse> HandleAsync(GetInterfaceManagerSiteItemsQuery query)
        {

            try
            {
                if (query == null)
                {
                    return new QueryHandlerResponse
                    {
                        Results = null,
                        ErrorMessage = "Invalid Query Fields"
                    };
                }

                var results = await _riqsInterfaceService.GetItems(query.SiteId, query.ItemId);

                if (results == null)
                {
                    return new QueryHandlerResponse
                    {
                        StatusCode = 404,
                        ErrorMessage = "Site items not found or no information available.",
                        Data = null,
                        Results = null,
                        TotalCount = 0
                    };
                }

                return new QueryHandlerResponse
                {
                    StatusCode = 200,
                    ErrorMessage = null,
                    Data = null,
                    Results = results,
                    TotalCount = results.Count
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
