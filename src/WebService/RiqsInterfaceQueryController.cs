
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.RiqsInterface;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using Selise.Ecap.SC.PraxisMonitor.Queries;

using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.WebService
{
    public class RiqsInterfaceQueryController : ControllerBase
    {
        private readonly ILogger<RiqsInterfaceQueryController> _logger;
        private readonly QueryHandler queryHandler;
        private readonly ISecurityContextProvider _securityContextProvider;
        public RiqsInterfaceQueryController(
             ISecurityContextProvider securityContextProvider,
            ILogger<RiqsInterfaceQueryController> logger,
            QueryHandler queryHandler
        )
        {
            _logger = logger;
            this.queryHandler = queryHandler;
            _securityContextProvider = securityContextProvider;
        }

        [HttpPost]
        [ProtectedEndPoint]
        public Task<QueryHandlerResponse> GetSSOFileInfo([FromBody] GetFileQuery query)
        {
            return queryHandler.SubmitAsync<GetFileQuery, QueryHandlerResponse>(query);
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetAuthToken([FromQuery] GetInterfaceToken query)
        {
           
            if (query == null)
            {
                return BadRequest("Query cannot be null");
            }
            var response = await queryHandler.SubmitAsync<GetInterfaceToken, QueryHandlerResponse>(query);
            if (response != null && response.StatusCode == 0 && response.Data != null)
            {
                ExternalUserTokenResponse tokenResponse = response.Data as ExternalUserTokenResponse;
                return Ok(tokenResponse);

            }
            else if (response != null)
            {
                return Unauthorized(response);
            }

            return Unauthorized();

        }

        [HttpPost]
        [Authorize]
        public Task<QueryHandlerResponse> GetInterfaceManagerLoginFlow([FromBody] GetInterfaceManagerLoginFlowQuery query)
        {
            return queryHandler.SubmitAsync<GetInterfaceManagerLoginFlowQuery, QueryHandlerResponse>(query);
        }

        [HttpPost]
        [Authorize]
        public Task<QueryHandlerResponse> GetInterfaceManagerSites([FromBody] GetInterfaceManagerSitesQuery query)
        {
            return queryHandler.SubmitAsync<GetInterfaceManagerSitesQuery, QueryHandlerResponse>(query);
        }

        [HttpPost]
        [Authorize]
        public Task<QueryHandlerResponse> GetInterfaceManagerSiteItems([FromBody] GetInterfaceManagerSiteItemsQuery query)
        {
            return queryHandler.SubmitAsync<GetInterfaceManagerSiteItemsQuery, QueryHandlerResponse>(query);
        }

        [HttpPost]
        [Authorize]
        public Task<QueryHandlerResponse> GetEquipemtInterfaceSummery([FromBody] GetEquipmentInterfaceSummeryQuery query)
        {
            return queryHandler.SubmitAsync<GetEquipmentInterfaceSummeryQuery, QueryHandlerResponse>(query);
        }

        [HttpPost]
        [Authorize]
        public Task<QueryHandlerResponse> GetInterfaceManagerLoginInfo([FromBody] GetInterfaceManagerLoginInfoQuery query)
        {
            return queryHandler.SubmitAsync<GetInterfaceManagerLoginInfoQuery, QueryHandlerResponse>(query);
        }

        [HttpPost]
        [Authorize]
        public Task<QueryHandlerResponse> GetUserInterfaceSummery([FromBody] GetUserInterfaceSummeryQuery query)
        {
            return queryHandler.SubmitAsync<GetUserInterfaceSummeryQuery, QueryHandlerResponse>(query);
        }

        [HttpPost]
        [Authorize]
        public Task<QueryHandlerResponse> GetSupplierInterfaceSummery([FromBody] GetSupplierInterfaceSummeryQuery query)
        {
            return queryHandler.SubmitAsync<GetSupplierInterfaceSummeryQuery, QueryHandlerResponse>(query);
        }
    }
}
