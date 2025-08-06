using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Text;

namespace Selise.Ecap.SC.Wopi.Utils
{
    public class UrlFactoryProvider
    {
        private readonly IConfiguration _configuration;

        public UrlFactoryProvider(IConfiguration configuration)
        {
            _configuration = configuration;
        }
        public string GetUrl(bool isBlocks,string apiUrl)
        {
            if (!isBlocks) return apiUrl;

            string microserviceBaseUrl = _configuration["MicroserviceBaseUrl"];
            string blocksBaseUrl = _configuration["BlocksBaseUrl"];
            Uri uri;
            if (!Uri.TryCreate(apiUrl, UriKind.Absolute, out uri))
            {
                return apiUrl;
            }
            string _apiBaseUrl = uri.Scheme + "://" + uri.Host;

            if (isBlocks && string.Equals(_apiBaseUrl, microserviceBaseUrl, StringComparison.OrdinalIgnoreCase))
            {
                return  blocksBaseUrl + uri.PathAndQuery;
            }
            return apiUrl;

        }
    }
}
