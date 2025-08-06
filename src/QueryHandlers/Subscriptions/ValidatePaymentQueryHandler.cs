using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using Newtonsoft.Json;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.PaymentModule;
using Selise.Ecap.SC.PraxisMonitor.Queries;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.EmailServices;

namespace Selise.Ecap.SC.PraxisMonitor.QueryHandlers
{
    public class ValidatePaymentQueryHandler : IQueryHandler<ValidatePaymentQuery, bool>
    {
        private readonly ILogger<ValidatePaymentQueryHandler> _logger;
        private readonly AccessTokenProvider _accessTokenProvider;
        private readonly IBlocksMongoDbDataContextProvider _mongoDbDataContextProvider;
        private readonly IServiceClient _serviceClient;
        private readonly IPraxisEmailNotifierService _emailNotifierService;
        private readonly IPraxisEmailDataBuilders _emailDataBuilders;

        private readonly string _origin;
        private readonly string _paymentServiceBaseUrl;
        private readonly string _paymentValidationUrl;

        public ValidatePaymentQueryHandler(
            ILogger<ValidatePaymentQueryHandler> logger,
            AccessTokenProvider accessTokenProvider,
            IBlocksMongoDbDataContextProvider mongoDbDataContextProvider,
            IConfiguration configuration,
            ISecurityContextProvider securityContextProvider,
            IServiceClient serviceClient,
            IRepository repository,
            IPraxisEmailNotifierService emailNotifierService,
            IPraxisEmailDataBuilders emailDataBuilders
            )
        {
            _logger = logger;
            _accessTokenProvider = accessTokenProvider;
            _mongoDbDataContextProvider = mongoDbDataContextProvider;
            _serviceClient = serviceClient;
            _emailNotifierService = emailNotifierService;
            _emailDataBuilders = emailDataBuilders;
            _origin = configuration["PraxisWebUrl"];
            _paymentServiceBaseUrl = configuration["PaymentServiceBaseUrl"];
            _paymentValidationUrl = configuration["PaymentServiceValidationUrl"];
        }

        public bool Handle(ValidatePaymentQuery query)
        {
            throw new NotImplementedException();
        }

        public async Task<bool> HandleAsync(ValidatePaymentQuery query)
        {
            _logger.LogInformation("Enter {HandlerName} with query: {Query}.",
                nameof(ValidatePaymentQueryHandler), JsonConvert.SerializeObject(query));
            try
            {
                if (string.IsNullOrEmpty(query.ProviderName))
                {
                    _logger.LogError("No provider name found with query: {Query}", JsonConvert.SerializeObject(query));
                    return false;
                }

                var paymentDetailData = GetPaymentDetailData(query.PaymentInitializeId);
                if (paymentDetailData == null || paymentDetailData.Count > 1)
                {
                    _logger.LogError("Invalid request with query: {Query}", JsonConvert.SerializeObject(query));
                    return false;
                }

                await ValidatePayment(query);

                paymentDetailData = GetPaymentDetailData(query.PaymentInitializeId);
                if (paymentDetailData[0].BillingAddress == null)
                {
                    _logger.LogError("Billing address not found with query: {Query}",JsonConvert.SerializeObject(query));
                    return false;
                }

                var emailData = _emailDataBuilders.BuildCreateOrganizationEmailData(query.PaymentInitializeId, paymentDetailData[0].BillingAddress.LastName);
                List<string> emailList = new List<string> { paymentDetailData[0].BillingAddress.Email };
                await _emailNotifierService.SendCreateOrganizationEmail(emailList, emailData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in {HandlerName}. Error Message: {Message}. Error Details: {StackTrace}.",
                    nameof(ValidatePaymentQueryHandler), ex.Message, ex.StackTrace);
                return false;
            }
            
            _logger.LogInformation("Handled By {HandlerName} with response: {Response}.",
                nameof(ValidatePaymentQueryHandler), JsonConvert.SerializeObject(true));

            return true;
        }

        private List<PaymentDetail> GetPaymentDetailData(string paymentInitializeId)
        {
            var collection =
                _mongoDbDataContextProvider.GetTenantDataContext(PraxisConstants.PraxisTenant).GetCollection<PaymentDetail>("PaymentDetails");
            var filter = Builders<PaymentDetail>.Filter.Eq("_id", paymentInitializeId);
            filter &= Builders<PaymentDetail>.Filter.Eq("IsMarkedToDelete", false);
            var paymentDetailData = collection.Find(filter).ToList();
            return paymentDetailData;
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

        private async Task ValidatePayment(ValidatePaymentQuery query)
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
                return;
            }
            if (respnose.StatusCode != 0)
            {
                _logger.LogError("Error in method: {MethodName}. Error: {Error}", nameof(ValidatePayment), respnose.ErrorMessage);
            }
        }
    }
}
