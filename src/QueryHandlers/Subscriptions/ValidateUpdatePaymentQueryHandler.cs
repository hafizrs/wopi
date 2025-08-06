using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using Newtonsoft.Json;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.PricingModule;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.PaymentModule;
using Selise.Ecap.SC.PraxisMonitor.Queries;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants;

namespace Selise.Ecap.SC.PraxisMonitor.QueryHandlers
{
    public class ValidateUpdatePaymentQueryHandler : IQueryHandler<ValidateUpdatePaymentQuery, bool>
    {
        private readonly ILogger<ValidateUpdatePaymentQueryHandler> _logger;
        private readonly AccessTokenProvider _accessTokenProvider;
        private readonly IBlocksMongoDbDataContextProvider _mongoDbDataContextProvider;
        private readonly IServiceClient _serviceClient;

        private readonly string _origin;
        private readonly string _paymentServiceBaseUrl;
        private readonly string _paymentValidationUrl;

        private readonly string _praxisMonitorWebServiceBaseUrl;
        private readonly string _praxisMonitorWebServiceVersion;
        private readonly string _praxisMonitorWebServiceCommandUrl;

        public ValidateUpdatePaymentQueryHandler(ILogger<ValidateUpdatePaymentQueryHandler> logger,
            AccessTokenProvider accessTokenProvider,
            IBlocksMongoDbDataContextProvider mongoDbDataContextProvider,
            IConfiguration configuration,
            IServiceClient serviceClient)
        {
            _logger = logger;
            _accessTokenProvider = accessTokenProvider;
            _mongoDbDataContextProvider = mongoDbDataContextProvider;
            _serviceClient = serviceClient;
            _origin = configuration["PraxisWebUrl"];
            _paymentServiceBaseUrl = configuration["PaymentServiceBaseUrl"];
            _paymentValidationUrl = configuration["PaymentServiceValidationUrl"];
            _praxisMonitorWebServiceBaseUrl = configuration["PraxisMonitorWebServiceBaseUrl"];
            _praxisMonitorWebServiceVersion = configuration["PraxisMonitorWebServiceVersion"];
            _praxisMonitorWebServiceCommandUrl = configuration["PraxisMonitorWebServiceCommandUrl"];
        }
        public bool Handle(ValidateUpdatePaymentQuery query)
        {
            throw new NotImplementedException();
        }

        public async Task<bool> HandleAsync(ValidateUpdatePaymentQuery query)
        {
            _logger.LogInformation("Enter {HandlerName} with query: {Query}.",
                nameof(ValidateUpdatePaymentQueryHandler), JsonConvert.SerializeObject(query));

            try
            {
                var collection = _mongoDbDataContextProvider
                    .GetTenantDataContext(PraxisConstants.PraxisTenant)
                    .GetCollection<PaymentDetail>("PaymentDetails");
                var filter = Builders<PaymentDetail>.Filter.Eq("_id", query.PaymentInitializeId);

                var isValid = collection.Find(filter).Any();
                if (!isValid)
                {
                    _logger.LogError("Invalid request with query: {Query} ",JsonConvert.SerializeObject(query));
                    return false;
                }

                if (string.IsNullOrEmpty(query.ProviderName))
                {
                    _logger.LogError("No provider name found with query: {Query} ", JsonConvert.SerializeObject(query));
                    return false;
                }

                var isValidPayment = await ValidatePayment(query);
                if (isValidPayment)
                {
                    await InitiateSubscriptionDataUpdate(query);
                    _logger.LogInformation("Subscription update done for id -> {PaymentInitializeId}", query.PaymentInitializeId);
                    return true;
                }

                _logger.LogInformation("Payment is not validate for id -> {PaymentInitializeId}", query.PaymentInitializeId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in {HandlerName}. Error Message: {Message}. Error Details: {StackTrace}.",
                    nameof(ValidateUpdatePaymentQueryHandler), ex.Message, ex.StackTrace);
                return false;
            }

            _logger.LogInformation("Handled By {HandlerName} with response: {Response}.",
                nameof(ValidateUpdatePaymentQueryHandler), JsonConvert.SerializeObject(true));

            return true;
        }

        private async Task<string> GetAdminToken()
        {
            var tokenInfo = new TokenInfo
            {
                UserId = Guid.NewGuid().ToString(),
                TenantId = "82D07BF9-CC75-477D-A286-F1A19A9FA0EA",
                SiteId = "151996CD-412B-4F48-8413-3F5DE1B9617B",
                SiteName = "RQ-Monitor Team",
                Origin = _origin,
                DisplayName = "lalu vulu",
                UserName = "laluvulu@yopmail.com",
                Language = "en-US",
                PhoneNumber = "+8801711111111",
                Roles = new List<string> { RoleNames.Anonymous }
            };
            var accessToken = await _accessTokenProvider.CreateForUserAsync(tokenInfo);
            return accessToken;
        }

        private async Task<bool> ValidatePayment(ValidateUpdatePaymentQuery query)
        {
            var token = await GetAdminToken();
            var payload = new ValidatePaymentPayload
            {
                ProviderName = query.ProviderName,
                PaymentDetailId = query.PaymentInitializeId
            };
            var respnose = await _serviceClient.SendToHttpAsync<PaymentValidationResponse>(HttpMethod.Post, _paymentServiceBaseUrl, "", _paymentValidationUrl, payload, token);
            if (respnose == null)
            {
                _logger.LogError("Null response return from payment service for validate payment.");
                return false;
            }
            if (respnose.StatusCode != 0)
            {
                _logger.LogError("Error in method: {MethodName}. Error: {Error}", nameof(ValidatePayment),
                    respnose.ErrorMessage);
                return false;
            }
            return true;
        }

        private async Task<bool> InitiateSubscriptionDataUpdate(ValidateUpdatePaymentQuery query)
        {
            try
            {
                var collection = _mongoDbDataContextProvider
                    .GetTenantDataContext(PraxisConstants.PraxisTenant)
                    .GetCollection<PraxisClientSubscription>($"{nameof(PraxisClientSubscription)}s");

                var filter = Builders<PraxisClientSubscription>.Filter.Eq(pcs => pcs.PaymentHistoryId, query.PaymentInitializeId);
                var paymentDetails = await collection.Find(filter).ToListAsync();

                if (paymentDetails.Count == 1)
                {
                    var command = new UpdateClientSubscriptionInformationCommand
                    {
                        ClientId = paymentDetails[0].ClientId,
                        OrganizationId = paymentDetails[0].OrganizationId,
                        PaymentDetailId = query.PaymentInitializeId,
                        ActionName = "update-renew-subscription-success",
                        Context = "update-renew-subscription-success",
                        NotificationSubscriptionId = query.PaymentInitializeId
                    };

                    await SendHttpRequestUpdateClientSubscriptionInformation(command);
                }
                else
                {
                    _logger.LogError("Subscription data not found for payment with query: {Query}", JsonConvert.SerializeObject(query));
                }

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in {MethodName}. Error Message: {Message}. Error Details: {StackTrace}.",
                    nameof(InitiateSubscriptionDataUpdate), ex.Message, ex.StackTrace);
                return false;
            }
            return true;
        }

        private async Task<bool> SendHttpRequestUpdateClientSubscriptionInformation(UpdateClientSubscriptionInformationCommand command)
        {
            var token = await GetAdminToken();
            var respnose = await _serviceClient.SendToHttpAsync<CommandResponse>(
                HttpMethod.Post,
                _praxisMonitorWebServiceBaseUrl,
                _praxisMonitorWebServiceVersion,
                $"{_praxisMonitorWebServiceCommandUrl}UpdateClientSubscriptionInformation",
                command,
                token);
            if (respnose == null)
            {
                _logger.LogError("Null response return from UpdateClientSubscriptionInformation http call.");
                return false;
            }
            if (respnose.StatusCode != 0)
            {
                _logger.LogError("Error in method: {MethodName}. Error: {Error}",
                    nameof(SendHttpRequestUpdateClientSubscriptionInformation), respnose.ErrorMessages);
                return false;
            }
            return true;
        }
    }
}
