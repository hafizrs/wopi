using SeliseBlocks.Notifier.Dtos;
using System.Collections.Generic;
using System.Threading.Tasks;
namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Notifier
{
    public interface INotificationService
    {
        Task<bool> Notify(string result, List<SubscriptionFilter> subscriptionFilters, string denormalizePayload = null);
        Task TaskCreatedNotifyToClient(string context, string subscriptionFilterId, bool isSuccess, dynamic result,
            string errorMessage);
        Task AsyncExportReportNotifyToClient(bool isSuccess, string notifySubscriptionId, dynamic result, string errorMessage);
        Task PraxisReportStatusNotifyToClient(bool isSuccess, string notifySubscriptionId);
        Task AsyncTaskAnsSummaryStatusChangeNotifyToClient(bool isSuccess, string notifySubscriptionId, dynamic result,
            string errorMessage);
        Task DataDeleteNotifyToClient(bool isSuccess, string notifySubscriptionId, dynamic result, string errorMessage,
            string context, string actionName);
        Task UserCreateUpdateNotifyToClient(bool isSuccess, string notifySubscriptionId, dynamic result, string errorMessage,
            string context, string actionName);
        Task UserLogOutNotification(bool isSuccess, string notifySubscriptionId, dynamic result, string context,
            string actionName);

        Task PaymentNotification(bool isSuccess, string notifySubscriptionId, dynamic result, string context, string actionName, string denormalizePayload = null);
        
        Task ProcessGuideUpdateNotification(bool isSuccess, string notifySubscriptionId);
        Task QrCodeGenerateNotification(bool isSuccess, string notifySubscriptionId, string equipmentQrFileId);
        Task UpdateCustomSubscriptionNotification(bool isSuccess, string notifySubscriptionId);
        Task GetHtmlFileIdFromObjectArtifactDocumentSubscriptionNotification(bool isSuccess, string notifySubscriptionId, string denormalizePayload = null);
        Task GetCommonSubscriptionNotification(bool isSuccess, string notifySubscriptionId, string context, string actionName, string denormalizePayload = null);
        Task LibraryFromUpdateNotification(bool isSuccess, string notifySubscriptionId, string actionName, string denormalizePayload = null);
        Task EquipmentValidationReportPdfGenerationNotification(bool isSuccess, string notifySubscriptionId, string outputFileId);
    }
}
