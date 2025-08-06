using System.Collections.Generic;

namespace EventHandlers.Services.EmailService
{
   public interface IEmailDataBuilders
    {
        Dictionary<string, string> BuildPaymentCompletedEmailData();
    }
}
