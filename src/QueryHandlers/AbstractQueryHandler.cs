using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.Wopi.Contracts.Models;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.Wopi.QueryHandlers
{
    public abstract class AbstractQueryHandler<TQuery> : IQueryHandler<TQuery, QueryHandlerResponse>
    {
        public QueryHandlerResponse Handle(TQuery Query)
        {
            return HandleAsync(Query).Result;
        }

        public abstract Task<QueryHandlerResponse> HandleAsync(TQuery Query);
    }
}
