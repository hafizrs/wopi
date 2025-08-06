using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Selise.Ecap.Entities.PrimaryEntities.MailService;
using Selise.Ecap.Entities.PrimaryEntities.PlatformDataService;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using SeliseBlocks.MailService.Driver;
using SeliseBlocks.MailService.Services.Commands;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Notifier;
using EmailTemplate = Selise.Ecap.Entities.PrimaryEntities.MailService.EmailTemplate;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.Notifier
{
    public class EmailNotifierService : IEmailNotifierService
    {
        private readonly IMailServiceClient mailServiceClient;
        private readonly ISecurityContextProvider securityContextProvider;
        private readonly ILogger<EmailNotifierService> _logger;
        private readonly IAuthUtilityService authUtilityService;
        private readonly IRepository _repository;

        public EmailNotifierService(IMailServiceClient mailServiceClient, 
            ISecurityContextProvider securityContextProvider,
            ILogger<EmailNotifierService> logger,
            IAuthUtilityService authUtilityService,
            IRepository repository
        )
        {
            this.mailServiceClient = mailServiceClient;
            this.securityContextProvider = securityContextProvider;
            _logger = logger;
            this.authUtilityService = authUtilityService;
            _repository = repository;
        }

        public async Task<bool> SendMaintenanceScheduleEmail(Person person, Dictionary<string, string> dataContext)
        {
            if (!string.IsNullOrWhiteSpace(person?.Email))
            {
                var hasScheduleType = dataContext.TryGetValue("ScheduleType", out string scheduleType);
                var purposeName = EmailTemplateName.MaintenanceScheduled.ToString();
                if (hasScheduleType && scheduleType == "VALIDATION")
                {
                    purposeName = EmailTemplateName.ValidationScheduled.ToString();
                }
                return await SendEmail(
                    person.Email,
                    purposeName, 
                    dataContext
                );
            }
            return false;
        }

        public async Task<bool> SendTaskAssignedEmail(Person person, Dictionary<string, string> dataContext, string emailTemplate = null)
        {
            if (!string.IsNullOrWhiteSpace(person?.Email))
            {
                return await SendEmail(person.Email, emailTemplate ?? EmailTemplateName.TaskAssignedGeneral.ToString(), dataContext);
            }
            return false;
        }

        public async Task<bool> SendTaskNotCheckedEmail(Person person, Dictionary<string, string> dataContext)
        {
            if (!string.IsNullOrWhiteSpace(person?.Email))
            {
                return await SendEmail(person.Email, EmailTemplateName.TaskNotChecked.ToString(), dataContext);
            }
            return false;
        }

        public async Task<bool> SendTaskNotFulfilledEmail(Person person, Dictionary<string, string> dataContext)
        {
            if (!string.IsNullOrWhiteSpace(person?.Email))
            {
                return await SendEmail(person.Email, EmailTemplateName.TaskFulFillment.ToString(), dataContext);
            }
            return false;
        }

        public async Task<bool> SendTaskOverdueEmail(Person person, Dictionary<string, string> dataContext)
        {
            if (!string.IsNullOrWhiteSpace(person?.Email))
            {
                return await SendEmail(person.Email, EmailTemplateName.TaskOverdue.ToString(), dataContext);
            }
            return false;
        }


        public async Task<bool> SendTaskOverdueResponsibleMembersEmail(Person person, Dictionary<string, string> dataContext)
        {
            if (!string.IsNullOrWhiteSpace(person?.Email))
            {
                return await SendEmail(person.Email, EmailTemplateName.TaskOverdueResponsible.ToString(), dataContext);
            }
            return false;
        }

        public async Task<bool> SendTaskRescheduledEmail(Person person, Dictionary<string, string> dataContext)
        {
            if (!string.IsNullOrWhiteSpace(person?.Email))
            {
                return await SendEmail(person.Email, EmailTemplateName.TaskRescheduled.ToString(), dataContext);
            }
            return false;
        }

        public async Task<bool> SendTrainingAssignedEmail(Person person, Dictionary<string, string> dataContext)
        {
            if (!string.IsNullOrWhiteSpace(person?.Email))
            {
                return await SendEmail(person.Email, EmailTemplateName.TrainingAssigned.ToString(), dataContext);
            }
            return false;
        }

        public async Task<bool> SendOpenItemAssignedEmail(Person person, Dictionary<string, string> dataContext)
        {
            if (!string.IsNullOrWhiteSpace(person?.Email))
            {
                return await SendEmail(person.Email, EmailTemplateName.TaskCompleted.ToString(), dataContext);
            }
            return false;
        }
        public async Task<bool> SendUserLimitReachedEmail(PraxisUser praxisUserData, Dictionary<string, string> dataContext)
        {
            if (!string.IsNullOrWhiteSpace(praxisUserData?.Email))
            {
                return await SendEmail(praxisUserData.Email, EmailTemplateName.UserLimitReached.ToString(), dataContext, true);
            }
            return false;
        }
        public async Task<bool> SendUserSubscriptionUpdateEmail(PraxisUser praxisUserData, Dictionary<string, string> dataContext)
        {
            if (!string.IsNullOrWhiteSpace(praxisUserData?.Email))
            {
                return await SendEmail(praxisUserData.Email, EmailTemplateName.SubscriptionUpdateConfirmation.ToString(), dataContext);
            }
            return false;
        }

        public async Task<bool> SendUserSubscriptionUpdateEmail(List<string> emailList, Dictionary<string, string> dataContext)
        {
            if (emailList != null && emailList.Count > 0)
            {
                return await SendEmail(emailList, EmailTemplateName.SubscriptionUpdateConfirmation.ToString(), dataContext, true);
            }
            return false;
        }

        public async Task<bool> SendUserUpdateConfirmationEmail(PraxisUser praxisUserData, Dictionary<string, string> dataContext)
        {
            if (!string.IsNullOrWhiteSpace(praxisUserData?.Email))
            {
                return await SendEmail(praxisUserData.Email, EmailTemplateName.UserUpdateConfirmation.ToString(), dataContext);
            }
            return false;
        }

        public async Task<bool> SendEmail(string emailAddress,
            string purposeName,
            Dictionary<string, string> dataContext,
            bool isAdminTokenRequired = false,
            List<string> emailList = null)
        {
            try
            {
                var securityContext = securityContextProvider.GetSecurityContext();

                if (dataContext == null || !dataContext.Any())
                {
                    _logger.LogInformation("Email data context should not be empty or null");

                    return false;
                }
                string language = securityContext.Language;
                if (dataContext.Any(o => o.Key == "Language"))
                {
                    language = dataContext["Language"];
                    _logger.LogInformation("Language from data context: {Language}", language);
                }
                else
                {
                    _logger.LogInformation("Language from security context: {Language}", language);
                }

                var emailTemplates = _repository.GetItems<EmailTemplate>(e => !e.IsMarkedToDelete && e.Name == purposeName)?.ToList() ?? new List<EmailTemplate>();

                if (emailTemplates?.Find(e => e.Language == language) == null)
                {
                    _logger.LogInformation("ERROR: No email template found for this language: {Language}", language);
                    if (emailTemplates?.Find(e => e.Language == "EN" || e.Language == "en-US") != null)
                    {
                        language = emailTemplates?.Find(e => e.Language == "EN" || e.Language == "en-US")?.Language;
                        _logger.LogInformation("ERROR: Use default template: {Language}", language);
                    }
                    else if (emailTemplates?.Find(e => e.Language == "DE" || e.Language == "de-DE") != null)
                    {
                        language = emailTemplates?.Find(e => e.Language == "DE" || e.Language == "de-DE")?.Language;
                        _logger.LogInformation("ERROR: Use default template: {Language}", language);
                    }
                    else if (emailTemplates?.Count > 0)
                    {
                        language = emailTemplates[0].Language;
                        _logger.LogInformation("ERROR: Use first created template: {Language}", language);
                    }
                }

                emailList ??= new List<string>();
                if (!string.IsNullOrEmpty(emailAddress) && !emailList.Contains(emailAddress))
                {
                    emailList.Add(emailAddress);
                }

                if (emailList?.Count > 0)
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

                    var token = string.Empty;

                    if (isAdminTokenRequired)
                    {
                        token = await authUtilityService.GetAdminToken();
                    }

                    var response = isAdminTokenRequired ? 
                        await mailServiceClient.EnqueueMail(sendMailCommand, token) :
                        await mailServiceClient.EnqueueMail(sendMailCommand);

                    if (response.StatusCode == 0)
                    {
                        _logger.LogInformation("Email Sent to {EmailAddress}, Purpose: {PurposeName}, Language: {Language}", emailAddress, purposeName, language);
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

                            _logger.LogInformation("Email Notifier Service SendEmail :: Error Message :: {ErrorMessage}", errorMessageString);
                        }
                    }
                }
                else
                {
                    _logger.LogInformation("Email address should not be empty or null");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Email Notifier Service SendEmail :: Exception Message: {Message}. Exception Details: {StackTrace}  ",
                ex.Message, ex.StackTrace);
            }

            return false;
        }

        private async Task<bool> SendEmail(List<string> emailList,
            string purposeName,
            Dictionary<string, string> dataContext,
            bool isAdminTokenRequired = false,
            string[] attachments = null
            )
        {
            try
            {
                var securityContext = securityContextProvider.GetSecurityContext();

                if (dataContext == null || !dataContext.Any())
                {
                    _logger.LogInformation("Email data context should not be empty or null");

                    return false;
                }
                string language = securityContext.Language;
                if (dataContext.Any(o => o.Key == "Language"))
                {
                    language = dataContext["Language"];
                    _logger.LogInformation("Language from data context: {Language}", language);
                }
                else
                {
                    _logger.LogInformation("Language from security context: {Language}", language);
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
                        To = emailList.ToArray(),
                        Attachments = attachments ?? Array.Empty<string>()
                    };

                    var token = string.Empty;

                    if (isAdminTokenRequired)
                    {
                        token = await authUtilityService.GetAdminToken();
                    }

                    var response = isAdminTokenRequired ? 
                        await mailServiceClient.EnqueueMail(sendMailCommand, token) : 
                        await mailServiceClient.EnqueueMail(sendMailCommand);

                    if (response.StatusCode == 0)
                    {
                        _logger.LogInformation("Email Sent to -> count -> {EmailCount} {FirstEmail}, Purpose: {PurposeName}, Language: {Language}",
                            emailList.Count, emailList[0], purposeName, securityContext.Language);
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

                            _logger.LogInformation("Email Sent to -> count -> {EmailCount} {FirstEmail}, Purpose: {PurposeName}, Language: {Language}",
                               emailList.Count, emailList[0], purposeName, securityContext.Language);
                        }
                    }
                }
                else
                {
                    _logger.LogInformation("Email address should not be empty or null");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Email Notifier Service SendEmail :: Exception Message: {Message}. Exception Details: {StackTrace}  ",
                ex.Message, ex.StackTrace);
            }

            return false;
        }

        public async Task<bool> SendMaintenanceDeleteEmail(Person person, Dictionary<string, string> dataContext)
        {
            if (!string.IsNullOrWhiteSpace(person?.Email))
            {
                var hasScheduleType = dataContext.TryGetValue("ScheduleType", out string scheduleType);
                var purposeName = EmailTemplateName.MaintenanceDeleted.ToString();
                if (hasScheduleType && scheduleType == "VALIDATION")
                {
                    purposeName = EmailTemplateName.ValidationDeleted.ToString();
                }
                return await SendEmail(person.Email, purposeName, dataContext);
            }
            return false;
        }

         public async Task<bool> SendAnonymousUser2faEmail(string email, Dictionary<string, string> dataContext)
        {
            if (!string.IsNullOrWhiteSpace(email))
            {
                return await SendEmail(email, "2FAViaEmail", dataContext,true);
            }
             return false;
        }

        public async Task<bool> SendEmail(string emailAddress, string purposeName, Dictionary<string, string> dataContext, string[] attachments,bool isAdminTokenRequired = false)
        {
            var emaillist = new List<string>() { emailAddress };
            return await SendEmail(emaillist, purposeName, dataContext, true, attachments);
        }

        public async Task<bool> SendProcessGuideOverDueEmail(Person person, Dictionary<string, string> dataContext)
        {
            if (!string.IsNullOrWhiteSpace(person?.Email))
            {
                return await SendEmail(person.Email, EmailTemplateName.ProcessGuideOverdue.ToString(), dataContext);
            }
            return false;
        }

        public async Task<bool> SendExternalSignatureEmail(Person person, Dictionary<string, string> dataContext)
        {
            if (!string.IsNullOrEmpty(person?.Email))
            {
                return await SendEmail(person.Email, EmailTemplateName.ExternalMaintenanceReportSignature.ToString(), dataContext, true);
            }
            return false;
        }
    }
}
