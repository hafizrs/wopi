using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.RiqsInterfaces
{
    public interface IRiqsInterfaceCustomHttpClientService
    {
        Task<HttpResponseMessage> SendRequestWithRetryAsync(HttpRequestMessage request);
    }
}
