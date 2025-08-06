using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices;
using System.Net;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices
{
    public class SequenceNumberService : ISequenceNumberService
    {
        private readonly ILogger<ISequenceNumberService> _logger;
        private readonly ISecurityContextProvider _securityContextProvider;
        private readonly IServiceClient _serviceClient;

        private readonly string _sequenceNumberBaseUrl;
        private readonly string _sequenceNumberVersion;
        private readonly string _sequenceNumberGeneratePath;

        public SequenceNumberService(
            ILogger<ISequenceNumberService> logger,
            IConfiguration configuration,
            ISecurityContextProvider securityContextProvider,
            IServiceClient serviceClient)
        {
            _logger = logger;
            _securityContextProvider = securityContextProvider;
            _serviceClient = serviceClient;
            _sequenceNumberBaseUrl = configuration["SequenceNumberServiceBaseUrl"];
            _sequenceNumberVersion = configuration["SequenceNumberServiceVersion"];
            _sequenceNumberGeneratePath = configuration["SequenceNumberGeneratePath"];
        }

        public async Task<NextSequenceNumberResponse> GenerateNextSequenceNumber(string context)
        {
            _logger.LogInformation("Going to generate next Sequence Number for {context} :", context);

            try
            {
                var token = _securityContextProvider.GetSecurityContext().OauthBearerToken;
                var request = new HttpRequestMessage
                {
                    Method = HttpMethod.Get,
                    RequestUri = new Uri(_sequenceNumberBaseUrl + _sequenceNumberVersion + _sequenceNumberGeneratePath + "?Context=" + context),
                };
                request.Headers.Add("Authorization", $"bearer {token}");
                HttpResponseMessage response = await _serviceClient.SendToHttpAsync(request);

                _logger.LogInformation("Sequence number generation completed. Status code: {StatusCode}", response.StatusCode);

                return response.StatusCode.Equals(HttpStatusCode.OK) ? response.Content.ReadAsAsync<NextSequenceNumberResponse>().Result : null;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    "Exception occurred during next sequence number generation. Exception Message: {ExceptionMessage}. Exception Details: {StackTrace}.",
                    ex.Message, ex.StackTrace);
                return null;
            }
        }
    }
}
