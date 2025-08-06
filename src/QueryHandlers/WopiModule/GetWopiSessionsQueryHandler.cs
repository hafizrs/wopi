using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.WopiMonitor.Contracts.Queries.WopiModule;
using Selise.Ecap.SC.WopiMonitor.Contracts.DomainServices.WopiModule;
using Selise.Ecap.SC.WopiMonitor.Contracts.EntityResponse;
using Selise.Ecap.SC.WopiMonitor.Contracts.Models;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.WopiMonitor.QueryHandlers.WopiModule
{
    public class GetWopiSessionsQueryHandler : IQueryHandler<GetWopiSessionsQuery, QueryHandlerResponse>
    {
        private readonly IWopiService _service;
        public GetWopiSessionsQueryHandler(IWopiService service)
        {
            _service = service;
        }
        public QueryHandlerResponse Handle(GetWopiSessionsQuery query)
        {
            if (query == null)
            {
                return new QueryHandlerResponse
                {
                    Results = null,
                    ErrorMessage = "Invalid Query Fields"
                };
            }
            var data = _service.GetWopiSessions(query);
            return new QueryHandlerResponse
            {
                Data = data
            };
        }

        public Task<QueryHandlerResponse> HandleAsync(GetWopiSessionsQuery query)
        {
            throw new System.NotImplementedException();
        }
    }
} 