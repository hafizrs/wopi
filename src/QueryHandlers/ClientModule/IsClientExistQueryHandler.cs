using Microsoft.Extensions.Logging;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Queries;
using System;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;

namespace Selise.Ecap.SC.PraxisMonitor.QueryHandlers
{
    public class IsClientExistQueryHandler : IQueryHandler<IsClientExistQuery, QueryHandlerResponse>
    {
        private readonly ILogger<IsClientExistQueryHandler> _logger;
        private readonly IRepository _repository;
        public IsClientExistQueryHandler(ILogger<IsClientExistQueryHandler> logger,
            IRepository repository)
        {
            _logger = logger;
            _repository = repository;
        }
        public QueryHandlerResponse Handle(IsClientExistQuery query)
        {
            throw new NotImplementedException();
        }

        public Task<QueryHandlerResponse> HandleAsync(IsClientExistQuery query)
        {
            QueryHandlerResponse response = new QueryHandlerResponse();
            _logger.LogInformation("Enter {HandlerName} with query: {Query}.",
                nameof(IsClientExistQueryHandler), JsonConvert.SerializeObject(query));
            try
            {
                var clientList = _repository.GetItems<PraxisClient>(x => !x.IsMarkedToDelete).ToList();
                var filteredClientCount = clientList.Count(pxc => pxc.ClientName.Equals( query.ClientName, StringComparison.OrdinalIgnoreCase));
                if(filteredClientCount > 0)
                {
                    response.Results = new
                    {
                        ClientExist = true
                    };
                    response.StatusCode = 0;
                    response.TotalCount = clientList.Count;
                    return Task.FromResult(response);
                }
                response.Results = new
                {
                    ClientExist = false
                };
                response.StatusCode = 0;
                response.TotalCount = 0;
                return Task.FromResult(response);
            }
            catch(Exception ex)
            {
                _logger.LogError("Error in {HandlerName}. Error Message: {Message}. Error Details: {StackTrace}.",
                    nameof(IsClientExistQueryHandler), ex.Message, ex.StackTrace);
                response.StatusCode = 1;
                response.ErrorMessage = ex.Message;
                return Task.FromResult(response);
            }
        }
    }
}
