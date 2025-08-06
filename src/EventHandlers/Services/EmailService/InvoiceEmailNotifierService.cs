using Microsoft.Extensions.Logging;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using SeliseBlocks.MailService.Driver;
using SeliseBlocks.MailService.Services.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventHandlers.Services.EmailService
{
   public class InvoiceEmailNotifierService: IInvoiceEmailNotifierService
    {
        private readonly IMailServiceClient mailServiceClient;
        private readonly ISecurityContextProvider securityContextProvider;
        private readonly ILogger<InvoiceEmailNotifierService> logger;
        public InvoiceEmailNotifierService(IMailServiceClient mailServiceClient,
            ISecurityContextProvider securityContextProvider,
            ILogger<InvoiceEmailNotifierService> logger)
        {
            this.mailServiceClient = mailServiceClient;
            this.securityContextProvider = securityContextProvider;
            this.logger = logger;
    }
        public async Task<bool> SendPaymentCompleteEmail(List<string> emailList, Dictionary<string, string> dataContext, IEnumerable<string> invoiceId, string emailTemplate = null)
        {
            if (emailList != null && emailList.Count > 0)
            {
                return await SendEmail(emailList, emailTemplate ?? "PaymentSuccessful", dataContext, invoiceId);
            }
            return false;
        }
        private async Task<bool> SendEmail(List<string> emailList, string purposeName, Dictionary<string, string> dataContext, IEnumerable<string> attachmentIds)
        {
            try
            {
                var securityContext = securityContextProvider.GetSecurityContext();

                if (dataContext == null || !dataContext.Any())
                {
                    logger.LogInformation("Email data context should not be empty or null");

                    return false;
                }
                string language = securityContext.Language;
                if (dataContext.Any(o => o.Key == "Language"))
                {
                    language = securityContext.Language;
                    logger.LogInformation("Language from securityContext" + language);
                }
                else
                {
                    logger.LogInformation("Language from tenant" + language);
                }

                if (emailList != null && emailList.Count > 0)
                {
                    var sendMailCommand = new SendMailToEmailCommand
                    {
                        Bcc = new string[] { },
                        Cc = new string[] { },
                        DataContext = dataContext,
                        Attachments = attachmentIds,
                        Language = language,
                        Purpose = purposeName,
                        To = emailList.ToArray()
                    };

                    var response = await mailServiceClient.EnqueueMail(sendMailCommand);

                    if (response.StatusCode == 0)
                    {
                        logger.LogInformation($"Email Sent to -> count -> {emailList.Count} {emailList[0]} , Purpose: {purposeName}" + "Language : " + securityContext.Language);
                        return true;
                    }
                    else
                    {
                        var errorMessages = response?.ErrorMessages;
                        if (errorMessages != null)
                        {
                            var errorMessageString = new StringBuilder();
                            foreach (var errorMessage in errorMessages)
                            {
                                errorMessageString.Append(errorMessage);
                            }

                            logger.LogInformation(
                                "Email Notifier Service SendEmail :: " +
                                $"Erorr Message :: {errorMessageString}"
                            );
                        }
                    }
                }
                else
                {
                    logger.LogInformation("Email address should not be empty or null");
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"Email Notifier Service SendEmail :: {ex.Message}", ex);
            }

            return false;
        }
    }
}
