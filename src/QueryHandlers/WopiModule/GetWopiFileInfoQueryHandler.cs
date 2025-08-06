using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.Wopi.Contracts.Queries.WopiModule;
using Selise.Ecap.SC.Wopi.Contracts.DomainServices.WopiModule;
using Selise.Ecap.SC.Wopi.Contracts.EntityResponse;
using Selise.Ecap.SC.Wopi.Contracts.Models;
using System.Threading.Tasks;
using Selise.Ecap.SC.Wopi.Contracts.Models;

namespace Selise.Ecap.SC.Wopi.QueryHandlers.WopiModule
{
    public class GetWopiFileInfoQueryHandler : IQueryHandler<GetWopiFileInfoQuery, QueryHandlerResponse>
    {
        private readonly IWopiService _service;
        public GetWopiFileInfoQueryHandler(IWopiService service)
        {
            _service = service;
        }
        public QueryHandlerResponse Handle(GetWopiFileInfoQuery query)
        {
            if (query == null)
            {
                return new QueryHandlerResponse
                {
                    Results = null,
                    ErrorMessage = "Invalid Query Fields"
                };
            }
            var data = _service.GetWopiFileInfo(query).Result;
            return new QueryHandlerResponse
            {
                Data = data
            };
        }

        public Task<QueryHandlerResponse> HandleAsync(GetWopiFileInfoQuery query)
        {
            throw new System.NotImplementedException();
        }
    }
} 