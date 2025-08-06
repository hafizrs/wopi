using System;
using System.Net.Http;
using System.Text;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices
{
    public class CreateDynamicLinkService : ICreateDynamicLink
    {
        private readonly ILogger<CreateDynamicLinkService> _logger;

        public CreateDynamicLinkService(
            ILogger<CreateDynamicLinkService> logger)
        {
            _logger = logger;
        }

        public string CreateLink(string url, DynamicLinkGeneratePayload payload)
        {
            _logger.LogInformation("Going to generate dynamic link from Firebase with url: {Url}", url);
            try
            {
                var requestUri = new UriBuilder(url).Uri;

                using var httpClient = new HttpClient();
                using var httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, requestUri);
                httpRequestMessage.Content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");
                var response = httpClient.SendAsync(httpRequestMessage).Result;

                if (!response.IsSuccessStatusCode)
                {
                    var error = response.Content.ReadAsStringAsync().Result;
                    var errorResponse = JsonConvert.DeserializeObject<FirebaseErrorResponse>(error);
                    _logger.LogError("Failed to get Dynamic Link. URL: {Url}. Reason: {ErrorMessage}.", url, errorResponse.Error.Message);
                    return payload.DynamicLinkInfo.Link;
                }
                var responseJson = response.Content.ReadAsStringAsync().Result;
                var firebaseResponse = JsonConvert.DeserializeObject<FirebaseResponse>(responseJson);
                return firebaseResponse.ShortLink;
            }
            catch (Exception ex)
            {
                _logger.LogError("Exception occurred during generate dynamic link from Firebase with url: {Url}. Exception Message: {ExceptionMessage}. Exception details: {StackTrace}.", url, ex.Message, ex.StackTrace);
                return payload.DynamicLinkInfo.Link;
            }
        }
    }

    public class FirebaseResponse
    {
        [JsonProperty("shortLink")]
        public string ShortLink { get; set; }

        [JsonProperty("previewLink")]
        public string PreviewLink { get; set; }
    }

    public class FirebaseErrorResponse
    {
        [JsonProperty("error")]
        public FirebaseError Error { get; set; }
    }
    public class FirebaseError
    {
        [JsonProperty("code")]
        public string Code { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; }

        [JsonProperty("status")]
        public string Status { get; set; }
    }
}
