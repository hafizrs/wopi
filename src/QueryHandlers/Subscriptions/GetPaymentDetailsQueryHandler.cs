using Microsoft.Extensions.Logging;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.PricingModule;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Queries;
using System;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;

namespace Selise.Ecap.SC.PraxisMonitor.QueryHandlers
{
    public class GetPaymentDetailsQueryHandler : IQueryHandler<GetPaymentDetailsQuery, QueryHandlerResponse>
    {
        public readonly IRepository _repositoryService;
        private readonly ILogger<GetPaymentDetailsQueryHandler> _logger;
        public GetPaymentDetailsQueryHandler(IRepository repositoryService,
            ILogger<GetPaymentDetailsQueryHandler> logger)
        {
            _repositoryService = repositoryService;
            _logger = logger;
        }
        public QueryHandlerResponse Handle(GetPaymentDetailsQuery query)
        {
            var response = new QueryHandlerResponse();

            _logger.LogInformation("Enter {HandlerName} with query: {Query}", nameof(GetPaymentDetailsQueryHandler),
                JsonConvert.SerializeObject(query));
            try
            {
                var praxisClientSubscriptionInfo = _repositoryService.GetItem<PraxisClientSubscription>(x => x.PaymentHistoryId == query.PaymentDetailId);
                if (praxisClientSubscriptionInfo != null && praxisClientSubscriptionInfo.BillingAddress != null)
                {
                    response.StatusCode = 0;
                    response.Data = new
                    { 
                        PaymentStatus = true,
                        BillingAddress = praxisClientSubscriptionInfo.BillingAddress,
                        ResponsiblePersonInformation = praxisClientSubscriptionInfo.ResponsiblePerson,
                        ModuleList = praxisClientSubscriptionInfo.ModuleList,
                        NumberOfUser = praxisClientSubscriptionInfo.NumberOfUser,
                        OrganizationId = praxisClientSubscriptionInfo.OrganizationId
                    };
                }
                else
                {
                    response.StatusCode = 1;
                    response.Data = new
                    {
                        PaymentStatus = false
                    };
                    if (praxisClientSubscriptionInfo == null)
                    {
                        response.ErrorMessage = "Data not Found";
                    }
                    else if(praxisClientSubscriptionInfo.BillingAddress == null)
                    {
                        response.ErrorMessage = "BillingAddress not Found";
                    }
                    _logger.LogInformation("Payment detail data not found");
                }
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, "Error in {HandlerName} Exception Message: {Message} Exception Details: {StackTrace}.",
                    nameof(GetPaymentDetailsQueryHandler), ex.Message, ex.StackTrace);
                return new QueryHandlerResponse()
                {
                    StatusCode = 1,
                    Data = new
                    {
                        PaymentStatus = false
                    },
                     ErrorMessage = ex.Message
                };
            }
            _logger.LogInformation("Handled By {HandlerName} with response: {Response}", nameof(GetPaymentDetailsQueryHandler),
                JsonConvert.SerializeObject(response));
            return response;
        }

        public Task<QueryHandlerResponse> HandleAsync(GetPaymentDetailsQuery query)
        {
            throw new NotImplementedException();
        }
    }
}
