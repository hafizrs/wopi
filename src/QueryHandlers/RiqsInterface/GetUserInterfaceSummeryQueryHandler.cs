using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.RiqsInterfaces;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using Selise.Ecap.SC.PraxisMonitor.Queries;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using System;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.QueryHandlers.RiqsInterface
{

    public class GetUserInterfaceSummeryQueryHandler : IQueryHandler<GetUserInterfaceSummeryQuery, QueryHandlerResponse>
    {
        private readonly IRiqsInterfaceUserMigrationService _service;

        public GetUserInterfaceSummeryQueryHandler(
            IRiqsInterfaceUserMigrationService service)
        {
            _service = service;
        }

        public QueryHandlerResponse Handle(GetUserInterfaceSummeryQuery query)
        {
            throw new NotImplementedException();
        }

        public async Task<QueryHandlerResponse> HandleAsync(GetUserInterfaceSummeryQuery query)
        {

            try
            {
                if (query == null || string.IsNullOrEmpty(query.MigrationSummaryId))
                {
                    return new QueryHandlerResponse
                    {
                        Data = null,
                        Results = null,
                        ErrorMessage = "Invalid Query Fields"
                    };
                }

                var results = await _service.GetUserMigrationSummery(query);

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


