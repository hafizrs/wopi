using Selise.Ecap.Entities.PrimaryEntities.PlatformDataService;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Notifier
{
    public interface IEmailNotifierService
    {
        Task<bool> SendEmail(string emailAddress, string purposeName, Dictionary<string, string> dataContext, bool isAdminTokenRequired = false, List<string> emailList = null);
        Task<bool> SendEmail(string emailAddress, string purposeName, Dictionary<string, string> dataContext, string[] attachments, bool isAdminTokenRequired = false);
        Task<bool> SendTaskNotCheckedEmail(Person person, Dictionary<string, string> dataContext);
        Task<bool> SendTaskNotFulfilledEmail(Person person, Dictionary<string, string> dataContext);
        Task<bool> SendTaskRescheduledEmail(Person person, Dictionary<string, string> dataContext);
        Task<bool> SendTrainingAssignedEmail(Person person, Dictionary<string, string> dataContext);
        Task<bool> SendMaintenanceScheduleEmail(Person person, Dictionary<string, string> dataContext);
        Task<bool> SendTaskAssignedEmail(Person person, Dictionary<string, string> dataContext, string emailTemplate = null);
        Task<bool> SendTaskOverdueEmail(Person person, Dictionary<string, string> dataContext);
        Task<bool> SendOpenItemAssignedEmail(Person person, Dictionary<string, string> dataContext);
        Task<bool> SendTaskOverdueResponsibleMembersEmail(Person person, Dictionary<string, string> dataContext);
        Task<bool> SendUserUpdateConfirmationEmail(PraxisUser praxisUserData, Dictionary<string, string> dataContext);
        Task<bool> SendUserLimitReachedEmail(PraxisUser praxisUserData, Dictionary<string, string> dataContext);
        Task<bool> SendUserSubscriptionUpdateEmail(PraxisUser praxisUserData, Dictionary<string, string> dataContext);
        Task<bool> SendUserSubscriptionUpdateEmail(List<string> emailList, Dictionary<string, string> dataContext);
        Task<bool> SendAnonymousUser2faEmail(string email, Dictionary<string, string> dataContext);
        Task<bool> SendMaintenanceDeleteEmail(Person person, Dictionary<string, string> dataContext);
        Task<bool> SendProcessGuideOverDueEmail(Person person, Dictionary<string, string> dataContext);
        Task<bool> SendExternalSignatureEmail(Person person, Dictionary<string, string> dataContext);
    }
}
