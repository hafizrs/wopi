using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.TwoFactorAuthentication;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Queries;

namespace Selise.Ecap.SC.PraxisMonitor.QueryHandlers
{
    public class TwoFactorCodeVerifyQueryHandler : IQueryHandler<TwoFactorCodeVerifyQuery, QueryHandlerResponse>
    {
        private readonly ITwoFactorAuthenticationServiceFactory _serviceFactory;
        private readonly ILogger<TwoFactorCodeVerifyQueryHandler> _logger;

        public TwoFactorCodeVerifyQueryHandler(
            ILogger<TwoFactorCodeVerifyQueryHandler> logger,
            ITwoFactorAuthenticationServiceFactory serviceFactory
        )
        {
            _serviceFactory = serviceFactory;
            _logger = logger;
        }

        public QueryHandlerResponse Handle(TwoFactorCodeVerifyQuery query)
        {
            return HandleAsync(query).Result;
        }

        public async Task<QueryHandlerResponse> HandleAsync(TwoFactorCodeVerifyQuery query)
        {
            _logger.LogInformation("Enter {HandlerName} with query: {Query}.",
                nameof(TwoFactorCodeVerifyQueryHandler), query);
            if (query == null)
            {
                return new QueryHandlerResponse
                {
                    Results = null,
                    ErrorMessage = "Invalid Query Fields"
                };
            }

            var response = new QueryHandlerResponse();

            try
            {
                var queryResponse = new TwoFAVerifyResponse();
                ITwoFactorAuthenticationService _authenticationService = _serviceFactory.GetService(query.TwoFactorType);
                queryResponse = await _authenticationService.VerifyCode(query);

                if (queryResponse != null)
                {
                    response.Data = new
                    {
                        queryResponse.Success,
                        queryResponse.ErrorMessage
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in {HandlerName}. Error Message: {Message}. Error Details: {StackTrace}.",
                    nameof(TwoFactorCodeVerifyQueryHandler), ex.Message, ex.StackTrace);

                response.StatusCode = 1;
                response.ErrorMessage = ex.Message;
            }

            _logger.LogInformation("Handled By {HandlerName} with response: {Response}.",
                nameof(TwoFactorCodeVerifyQueryHandler), response);

            return response;
        }
    }
}
