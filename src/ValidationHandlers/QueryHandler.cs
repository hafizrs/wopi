using Microsoft.Extensions.DependencyInjection;
using Selise.Ecap.SC.WopiMonitor.Contracts.Infrastructure;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using System;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.WopiMonitor.ValidationHandlers
{
    public class QueryHandler : IQueryHandler
    {
        private readonly IServiceProvider _serviceProvider;

        public QueryHandler(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public QueryHandlerResponse Submit<TQuery, TResponse>(TQuery query) where TQuery : class
        {
            var queryHandlerType = typeof(IQueryHandler<TQuery, TResponse>);
            var queryHandler = _serviceProvider.GetService(queryHandlerType) as IQueryHandler<TQuery, TResponse>;

            return queryHandler?.Handle(query) ?? new QueryHandlerResponse();
        }

        public async Task<QueryHandlerResponse> SubmitAsync<TQuery, TResponse>(TQuery query) where TQuery : class
        {
            var queryHandlerType = typeof(IQueryHandler<TQuery, TResponse>);
            var queryHandler = _serviceProvider.GetService(queryHandlerType) as IQueryHandler<TQuery, TResponse>;

            return await queryHandler?.HandleAsync(query) ?? Task.FromResult(new QueryHandlerResponse());
        }
    }
} 