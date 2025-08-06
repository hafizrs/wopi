using Microsoft.Extensions.Configuration;
using System.Collections.Generic;

namespace EventHandlers.Services.EmailService
{
    public class EmailDataBuilders: IEmailDataBuilders
    {
        private readonly IConfiguration _configuration;
        public EmailDataBuilders(IConfiguration configuration)
        {
            _configuration = configuration;
        }
        public Dictionary<string, string> BuildPaymentCompletedEmailData()
        {
            return new Dictionary<string, string>
            {
                {"LoginUrl" , GetLoginUrl()},

            };
        }
        private string GetLoginUrl()
        {
            return _configuration["PraxisWebUrl"] + "/login";
        }
    }
}
