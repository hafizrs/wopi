using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.LibraryModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Queries;
using Selise.Ecap.SC.PraxisMonitor.Queries;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.QueryHandlers
{
    public class GetDocumentEditHistoryQueryHandler : IQueryHandler<GetDocumentEditHistoryQuery, QueryHandlerResponse>
    {
        private readonly IDocumentEditHistoryService _documentEditHistoryService;
        public GetDocumentEditHistoryQueryHandler(IDocumentEditHistoryService documentEditHistoryService)
        {
            _documentEditHistoryService = documentEditHistoryService;
        }

        [Invocable]
        public QueryHandlerResponse Handle(GetDocumentEditHistoryQuery query)
        {
            if (query == null)
            {
                return new QueryHandlerResponse
                {
                    Results = null,
                    ErrorMessage = "Invalid Query Fields"
                };
            }

            var hostories = _documentEditHistoryService.GetDocumentEditHistory(query.ObjectArtifactId);

            return new QueryHandlerResponse
            {
                Data = hostories,
            };
        }

        public Task<QueryHandlerResponse> HandleAsync(GetDocumentEditHistoryQuery query)
        {
            throw new NotImplementedException();
        }
    }

}
