using Microsoft.Extensions.Logging;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.RiqsInterface;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.RiqsInterfaces;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Queries;
using Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.Services;
using Selise.Ecap.SC.PraxisMonitor.Queries;
using Selise.Ecap.SC.PraxisMonitor.QueryHandlers.ExternalUserModule;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZXing;

namespace Selise.Ecap.SC.PraxisMonitor.QueryHandlers.RiqsInterface
{

    public class GetinterfaceTokenQueryHandler : AbstractQueryHandler<GetInterfaceToken>
    {
        private readonly ILogger<GetinterfaceTokenQueryHandler> _logger;
        private readonly IRiqsInterfaceTokenService _riqsInterfaceTokenService;



        public GetinterfaceTokenQueryHandler(
            ILogger<GetinterfaceTokenQueryHandler> logger, IRiqsInterfaceTokenService riqsInterfaceTokenService)
        {
            _logger = logger;
            _riqsInterfaceTokenService = riqsInterfaceTokenService;
        }


        public QueryHandlerResponse Handle(GetInterfaceToken query)
        {
            throw new NotImplementedException();
        }

        public override async Task<QueryHandlerResponse> HandleAsync(GetInterfaceToken query)
        {
            if (query == null)
            {
                _logger.LogWarning("Received null query in ExternalTokenQueryHandler");
                return CreateErrorResponse("Invalid query: Query cannot be null");
            }

            try
            {
                ExternalUserTokenResponse response = await RetrieveAccessTokenAsync(query);

                return new QueryHandlerResponse
                {
                    Data = response,
                    StatusCode = response ==null? 1 : 0
                };
            }
            catch (Exception ex)
            {
                return HandleException(ex);
            }
        }

        private async Task<ExternalUserTokenResponse> RetrieveAccessTokenAsync(GetInterfaceToken query)
        {
            // Prioritize authorization code over refresh token
            if (!string.IsNullOrWhiteSpace(query.Code))
            {
                _logger.LogInformation("Retrieving token using authorization code");
                return await _riqsInterfaceTokenService.GetInterfaceTokenAsync(query.Code, query.State);
            }

            if (!string.IsNullOrWhiteSpace(query.RefreshtokenId))
            {
                _logger.LogInformation("Retrieving token using refresh token");
                return await _riqsInterfaceTokenService.GetInterfaceTokenAsync(query.RefreshtokenId);
            }

            _logger.LogWarning("No valid token retrieval method provided");
            return null;
        }

        private QueryHandlerResponse HandleException(Exception ex)
        {
            _logger.LogError(ex,
                "Exception in {HandlerName}. Exception Message: {Message}. Exception Details: {StackTrace}",
                nameof(ExternalTokenQueryHandler),
                ex.Message,
                ex.StackTrace);

            return new QueryHandlerResponse
            {
                StatusCode = 1,
                ErrorMessage = ex.ToString()
            };
        }

        private QueryHandlerResponse CreateErrorResponse(string errorMessage)
        {
            _logger.LogWarning(errorMessage);

            return new QueryHandlerResponse
            {
                StatusCode = 1,
                ErrorMessage = errorMessage
            };
        }
    }




}

