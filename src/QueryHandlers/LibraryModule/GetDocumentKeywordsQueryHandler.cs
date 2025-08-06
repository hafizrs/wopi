using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices;
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
    public class GetDocumentKeywordsQueryHandler : IQueryHandler<GetDocumentKeywordsQuery, QueryHandlerResponse>
    {
        private readonly IDocumentKeywordService _documentKeywordService;
        public GetDocumentKeywordsQueryHandler(IDocumentKeywordService documentKeywordService)
        {
            _documentKeywordService = documentKeywordService;
        }

        public QueryHandlerResponse Handle(GetDocumentKeywordsQuery query)
        {
            throw new NotImplementedException();
        }

        public async Task<QueryHandlerResponse> HandleAsync(GetDocumentKeywordsQuery query)
        {
            if (query == null)
            {
                return new QueryHandlerResponse
                {
                    Results = null,
                    ErrorMessage = "Invalid Query Fields"
                };
            }

            var data = await _documentKeywordService.GetKeywordValues(query.OrganisationId);

            return new QueryHandlerResponse
            {
                Data = data
            };
        }
    }
}
