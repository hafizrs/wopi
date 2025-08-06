using Microsoft.Extensions.Logging;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices;
using Selise.Ecap.SC.PraxisMonitor.Queries;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Queries;

namespace Selise.Ecap.SC.PraxisMonitor.QueryHandlers
{
    public class GetCalculatedSubscriptionInstallmentPaymentQueryHandler : IQueryHandler<GetCalculatedSubscriptionInstallmentPaymentQuery, QueryHandlerResponse>
    {
        private readonly ILogger<GetCalculatedSubscriptionInstallmentPaymentQueryHandler> _logger;
        private readonly ISubscriptionInstallmentPaymentCalculationService _subscriptionInstallmentPaymentCalculationService;
        public GetCalculatedSubscriptionInstallmentPaymentQueryHandler(
            ILogger<GetCalculatedSubscriptionInstallmentPaymentQueryHandler> logger,
            ISubscriptionInstallmentPaymentCalculationService subscriptionInstallmentPaymentCalculationService)
        {
            _logger = logger;
            _subscriptionInstallmentPaymentCalculationService = subscriptionInstallmentPaymentCalculationService;

        }

        public QueryHandlerResponse Handle(GetCalculatedSubscriptionInstallmentPaymentQuery query)
        {
            throw new NotImplementedException();
        }

        public async Task<QueryHandlerResponse> HandleAsync(GetCalculatedSubscriptionInstallmentPaymentQuery query)
        {
            QueryHandlerResponse response = new QueryHandlerResponse();

            _logger.LogInformation("Enter {HandlerName} with query: {Query}.",
                nameof(GetCalculatedSubscriptionInstallmentPaymentQueryHandler), JsonConvert.SerializeObject(query));

            try
            {
                var results = await _subscriptionInstallmentPaymentCalculationService.GetCalculatedSubscriptionInstallmentPayment(query.DurationOfSubscription, query.TotalAmount);

                response.StatusCode = 0;
                response.Results = results;
                response.TotalCount = results.Count;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in {HandlerName}. Error Message: {Message}. Error Details: {StackTrace}.",
                    nameof(GetCalculatedSubscriptionInstallmentPaymentQueryHandler), ex.Message, ex.StackTrace);
                response.StatusCode = 1;
                response.ErrorMessage = ex.Message;
            }

            _logger.LogInformation("Handled By {HandlerName} with response: {Response}.",
                nameof(GetCalculatedSubscriptionInstallmentPaymentQueryHandler), JsonConvert.SerializeObject(response));

            return response;
        }
    }
}
