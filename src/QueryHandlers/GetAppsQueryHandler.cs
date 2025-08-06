
using Microsoft.Extensions.Logging;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using Selise.Ecap.SC.PraxisMonitor.Queries;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.QueryHandlers
{
    public class GetAppsQueryHandler : IQueryHandler<GetAppsQuery, IEnumerable<AppResponse>>
    {
        private readonly ILogger<GetAppsQueryHandler> _logger;
        private readonly ISecurityContextProvider _securityContextProvider;
        private readonly IAppCatalogueRepositoryService _appCatalogueRepositoryService;

        

        public GetAppsQueryHandler(ILogger<GetAppsQueryHandler> logger, ISecurityContextProvider securityContextProvider, IAppCatalogueRepositoryService appCatalogueRepositoryService)
        {
            _logger = logger;
            _securityContextProvider = securityContextProvider;
            _appCatalogueRepositoryService = appCatalogueRepositoryService;
        }

        public IEnumerable<AppResponse> Handle(GetAppsQuery query)
        {

            try
            {
     
                var appResponses = _appCatalogueRepositoryService.GetFeatureRoles();
                _logger.LogInformation("Successfully retrieved app responses.");
                return appResponses;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while handling GetAppsQuery");
                throw;
            }


        }
        public Task<IEnumerable<AppResponse>> HandleAsync(GetAppsQuery query)
        {
            throw new NotImplementedException();
        }
    }



}
