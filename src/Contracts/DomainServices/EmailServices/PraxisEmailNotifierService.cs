using Microsoft.Extensions.Logging;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using SeliseBlocks.MailService.Driver;
using SeliseBlocks.MailService.Services.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.EmailServices
{
    public class PraxisEmailNotifierService : IPraxisEmailNotifierService
    {
        private readonly IMailServiceClient mailServiceClient;
        private readonly ISecurityContextProvider securityContextProvider;
        private readonly ILogger<PraxisEmailNotifierService> logger;

        public PraxisEmailNotifierService
        (
            IMailServiceClient mailServiceClient,
            ISecurityContextProvider securityContextProvider,
            ILogger<PraxisEmailNotifierService> logger
        )
        {
            this.mailServiceClient = mailServiceClient;
            this.securityContextProvider = securityContextProvider;
            this.logger = logger;
        }

        public async Task<bool> SendCreateOrganizationEmail(List<string> emailList, Dictionary<string, string> dataContext, string emailTemplate = null)
        {
            if (emailList != null && emailList.Count > 0)
            {
                return await SendEmail(emailList, emailTemplate ?? "CreateOrganization", dataContext);
            }
            return false;
        }
        private async Task<bool> SendEmail(List<string> emailList, string purposeName, Dictionary<string, string> dataContext)
        {
            try
            {
                var securityContext = securityContextProvider.GetSecurityContext();

                if (dataContext == null || !dataContext.Any())
                {
                    logger.LogError("Email data context should not be empty or null");

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

                            logger.LogError(
                                "Email Notifier Service SendEmail :: Error Message :: {ErrorMessage}",
                                errorMessageString.ToString()
                            );
                        }
                    }
                }
                else
                {
                    logger.LogError("Email address should not be empty or null");
                }
            }
            catch (Exception ex)
            {
                logger.LogError("Email Notifier Service SendEmail :: {ErrorMessage}", ex.Message);
            }

            return false;
        }
    }
}
