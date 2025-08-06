using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Selise.Ecap.Entities.PrimaryEntities.DWT;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Commands;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.Signature;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using Selise.Ecap.SC.PraxisMonitor.Queries;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.WebService
{
    public class ExternalUserController : ControllerBase
    {
        private readonly CommandHandler _commandHandler;
        private readonly QueryHandler _queryHandler;
        private readonly ISecurityContextProvider _securityContextProvider;
        public ExternalUserController(
            ISecurityContextProvider securityContextProvider,
            CommandHandler commandService,
            QueryHandler queryHandler)
        {
            _commandHandler = commandService;
            _queryHandler = queryHandler;
            _securityContextProvider = securityContextProvider;
        }

        [HttpGet]
        [AnonymousEndPoint]
        public async Task<IActionResult> GetToken([FromQuery] ExternalTokenQuery query)
        {
            
            if (query == null)
            {
                return BadRequest("Query cannot be null");
            }
            var response = await _queryHandler.SubmitAsync<ExternalTokenQuery, QueryHandlerResponse>(query);
            if (response != null && response.StatusCode == 0 && response.Data!=null)
            {
                ExternalUserTokenResponse tokenResponse = response.Data as ExternalUserTokenResponse;
                var hostName = _securityContextProvider.GetSecurityContext().RequestOrigin;
                var cookieDomain = hostName.Substring(hostName.IndexOf('.') + 1);
                var cookieOptions = new CookieOptions
                {
                    Domain = cookieDomain,
                    Expires = DateTime.UtcNow.AddDays(1),
                    HttpOnly = true,
                    Path = "/",
                    Secure = false,
                };
                Response.Cookies.Append(hostName, tokenResponse.access_token, cookieOptions);
                return Ok(tokenResponse);
            }
            else if(response != null)
            {
                return Unauthorized(response);
            }

            return Unauthorized();

        }

        [HttpPost]
        [Authorize]
        public async Task<RiqsCommandResponse> Generate2FaCodeForExternal([FromBody] GenerateTwofaCodeForExternalCommand command)
        {
            if (command is null ||
                string.IsNullOrWhiteSpace(command.TwoFactorId)
                )
            {
                var response = new RiqsCommandResponse();
                response.SetError("Generate2FaCodeForEquipment", "Invalid command");
                return response;
            }
            return await _commandHandler.SubmitAsync<GenerateTwofaCodeForExternalCommand, RiqsCommandResponse>(command);
        }

        [HttpGet]
        [Authorize]
        public async Task<QueryHandlerResponse> GetPraxisUserInfo([FromQuery] ExternalUserQuery query)
        {
            if (query == null || string.IsNullOrEmpty(query.ExternalUserItemId))
            {
                var response = new QueryHandlerResponse()
                {
                    ErrorMessage = " Invalid Query value"
                };
                return await Task.FromResult(response);
            }
            return  await _queryHandler.SubmitAsync<ExternalUserQuery, QueryHandlerResponse>(query);
        }

    }
}
