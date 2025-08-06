using SeliseBlocks.Genesis.Framework.Infrastructure;

namespace Selise.Ecap.SC.WopiMonitor.Contracts.Infrastructure
{
    public interface IQueryHandler
    {
        QueryHandlerResponse Submit<TQuery, TResponse>(TQuery query) where TQuery : class;
        Task<QueryHandlerResponse> SubmitAsync<TQuery, TResponse>(TQuery query) where TQuery : class;
    }
} 