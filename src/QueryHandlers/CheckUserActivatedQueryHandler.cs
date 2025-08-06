using Microsoft.Extensions.Logging;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Queries;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.QueryHandlers
{
    public class CheckUserActivatedQueryHandler : IQueryHandler<CheckUserActivatedQuery, QueryHandlerResponse>
    {
        private readonly IRepository _repository;
        private readonly ILogger<CheckUserActivatedQueryHandler> _logger;
        public CheckUserActivatedQueryHandler(
            IRepository repository,
            ILogger<CheckUserActivatedQueryHandler> logger
        )
        {
            _repository = repository;
            _logger = logger;
        }

        public QueryHandlerResponse Handle(CheckUserActivatedQuery query)
        {
            throw new System.NotImplementedException();
        }

        public async Task<QueryHandlerResponse> HandleAsync(CheckUserActivatedQuery query)
        {
            var results = await GetResultsAsync(query);
            return new QueryHandlerResponse { Results = results };
        }

        private async Task<List<CheckUserActivatedQueryResponse>> GetResultsAsync(CheckUserActivatedQuery query)
        {
            var results = new List<CheckUserActivatedQueryResponse>();

            var praxisUsers = _repository.GetItems<PraxisUser>(p => query.PraxisUserIds.Contains(p.ItemId) || query.Email == p.Email)?.ToList();
            if (praxisUsers == null || !praxisUsers.Any())
                return results;

            foreach (var user in praxisUsers)
            {
                var primaryClient = user?.ClientList?.FirstOrDefault(c => c.IsPrimaryDepartment);
                if (primaryClient != null)
                {
                    results.Add(new CheckUserActivatedQueryResponse
                    {
                        ItemId = user.ItemId,
                        Email = user.Email,
                        ClientId = primaryClient.ClientId,
                        CanActivateUser = false
                    });
                }
            }

            if (!results.Any())
                return results;

            var clientIds = results.Select(r => r.ClientId).Distinct().ToList();
            var praxisClients = _repository.GetItems<PraxisClient>(c =>
                clientIds.Contains(c.ItemId) && c.UserCount < c.AuthorizedUserLimit)?.ToList();

            foreach (var res in results)
            {
                res.CanActivateUser = praxisClients?.FirstOrDefault(x => x.ItemId == res.ClientId) != null;
            }

            return results;
        }

    }
}