using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.Wopi.Contracts.Queries.WopiModule;
using Selise.Ecap.SC.Wopi.Contracts.DomainServices.WopiModule;
using System.Threading.Tasks;
using Selise.Ecap.SC.Wopi.Contracts.Models;

namespace Selise.Ecap.SC.Wopi.QueryHandlers.WopiModule
{
    public class GetWopiSessionQueryHandler : IQueryHandler<GetWopiSessionQuery, QueryHandlerResponse>
    {
        private readonly IWopiService _service;
        public GetWopiSessionQueryHandler(IWopiService service)
        {
            _service = service;
        }
        public QueryHandlerResponse Handle(GetWopiSessionQuery query)
        {
            if (query == null)
            {
                return new QueryHandlerResponse
                {
                    Results = null,
                    ErrorMessage = "Invalid Query Fields"
                };
            }
            var data = _service.GetWopiSession(query);
            return new QueryHandlerResponse
            {
                Data = data
            };
        }

        public Task<QueryHandlerResponse> HandleAsync(GetWopiSessionQuery query)
        {
            throw new System.NotImplementedException();
        }
    }
} 