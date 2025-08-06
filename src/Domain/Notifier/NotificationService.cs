using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SeliseBlocks.Genesis.Framework.Infrastructure;

using Selise.Ecap.SC.PraxisMonitor.Contracts.Notifier;
using SeliseBlocks.Notifier.Dtos;
using SeliseBlocks.Notifier.CodeSystem;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.Notifier
{
    public class NotificationService : INotificationService
    {
        private readonly IAppSettings appSettings;
        private readonly IServiceClient serviceClient;
        private readonly ILogger<NotificationService> _logger;
        private readonly ISecurityContextProvider securityContextProvider;
        private readonly string notifierApiVersion;

        public NotificationService(
            IAppSettings appSettings,
            IConfiguration configuration,
            IServiceClient serviceClient,
            ILogger<NotificationService> logger,
            ISecurityContextProvider securityContextProvider
        )
        {
            this.appSettings = appSettings;
            this.serviceClient = serviceClient;
            _logger = logger;
            this.securityContextProvider = securityContextProvider;
            notifierApiVersion = configuration["NotificationServiceVersion"];
        }

        public async Task<bool> Notify(
            string result,
            List<SubscriptionFilter> subscriptionFilters,
            string denormalizePayload = null)
        {
            if (subscriptionFilters == null || subscriptionFilters.Count == 0)
            {
                return false;
            }

            var notifierPayload = new NotifierPayloadWithResponse
            {
                ResponseValue = result,
                ResponseKey = subscriptionFilters[0].Value,
                SubscriptionFilters = subscriptionFilters,
                NotificationType = NotificationReceiverTypes.FilterSpecificReceiverType
            };
            if (denormalizePayload != null)
            {
                notifierPayload.DenormalizedPayload = denormalizePayload;
            }

            var pushNotificationUri = new Uri($"{appSettings.NotifierServerBaseUrl}/{notifierApiVersion}/api/Notifier/Notify");

            using var request = new HttpRequestMessage(HttpMethod.Post, pushNotificationUri);
            var requestBody = JsonConvert.SerializeObject(notifierPayload);

            using (request.Content = new StringContent(requestBody, Encoding.UTF8, "application/json"))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("bearer", securityContextProvider.GetSecurityContext().OauthBearerToken);

                try
                {
                    var response = await serviceClient.SendToHttpAsync(request);

                    if (!response.IsSuccessStatusCode)
                    {
                        var responseJson = await response.Content.ReadAsStringAsync();
                        _logger.LogError("Notification sending Failed. Payload: {Payload} Response: {Response}", requestBody, responseJson);
                        return false;
                    }
                    else
                    {
                        _logger.LogInformation("Notification Sent. Payload: {Payload}", requestBody);
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError("Exception occurred while sending Notification: {Payload} Exception Message: {Message}. Exception Details: {StackTrace}  ",
                        requestBody, ex.Message, ex.StackTrace);
                    return false;
                }
            }
        }

        public async Task TaskCreatedNotifyToClient(
            string context,
            string subscriptionFilterId,
            bool isSuccess,
            dynamic result,
            string errorMessage
        )
        {
            var subscriptionFilters = new List<SubscriptionFilter>
            {
                new SubscriptionFilter
                {
                    ActionName = "TaskCreated",
                    Context = context,
                    Value = subscriptionFilterId
                }
            };
            var response = JsonConvert.SerializeObject(new
            {
                Result = result,
                Success = isSuccess,
                ErrorMessage = isSuccess ? "" : errorMessage
            });

            await Notify(response, subscriptionFilters);

            _logger.LogInformation("TaskCreatedNotifyToClient -> Notification Sending!! {Response}", response);
        }

        public async Task AsyncExportReportNotifyToClient(
            bool isSuccess,
            string notifySubscriptionId,
            dynamic result,
            string errorMessage
        )
        {
            var subscriptionFilters = new List<SubscriptionFilter>
            {
                new SubscriptionFilter
                {
                    Context = "Export",
                    ActionName = "ExportReport",
                    Value = notifySubscriptionId
                }
            };
            var response = JsonConvert.SerializeObject(new
            {
                Result = result,
                Success = isSuccess,
                ErrorMessage = errorMessage
            });

            await Notify(response, subscriptionFilters);
        }

        public async Task PraxisReportStatusNotifyToClient(bool isSuccess, string notifySubscriptionId)
        {
            var subscriptionFilters = new List<SubscriptionFilter>
            {
                new SubscriptionFilter
                {
                    Context = "PraxisReportStatus",
                    ActionName = "Generation",
                    Value = notifySubscriptionId
                }
            };

            await Notify(JsonConvert.SerializeObject(new { Success = isSuccess }), subscriptionFilters);
        }

        public async Task AsyncTaskAnsSummaryStatusChangeNotifyToClient(
            bool isSuccess,
            string notifySubscriptionId,
            dynamic result,
            string errorMessage
        )
        {
            var subscriptionFilters = new List<SubscriptionFilter>
            {
                new SubscriptionFilter
                {
                    Context = "TaskSummaryStatusChange",
                    ActionName = "StatusChanged",
                    Value = notifySubscriptionId
                }
            };
            var response = JsonConvert.SerializeObject(new
            {
                Result = result,
                Success = isSuccess,
                ErrorMessage = errorMessage
            });

            await Notify(response, subscriptionFilters);
        }

        public async Task DataDeleteNotifyToClient(
            bool isSuccess,
            string notifySubscriptionId,
            dynamic result,
            string errorMessage,
            string context,
            string actionName
        )
        {
            var subscriptionFilters = new List<SubscriptionFilter>
            {
                new SubscriptionFilter
                {
                    Context = context,
                    ActionName = actionName,
                    Value = notifySubscriptionId
                }
            };
            var response = JsonConvert.SerializeObject(new
            {
                Result = result,
                Success = isSuccess,
                ErrorMessage = errorMessage
            });

            await Notify(response, subscriptionFilters);
        }

        public async Task UserLogOutNotification(
            bool isSuccess,
            string notifySubscriptionId,
            dynamic result,
            string context,
            string actionName
        )
        {
            var subscriptionFilters = new List<SubscriptionFilter>
            {
                new SubscriptionFilter
                {
                    Context = context,
                    ActionName = actionName,
                    Value = notifySubscriptionId
                }
            };
            var response = JsonConvert.SerializeObject(new
            {
                Result = result,
                Success = isSuccess
            });

            await Notify(response, subscriptionFilters);
        }

        public async Task UserCreateUpdateNotifyToClient(
            bool isSuccess,
            string notifySubscriptionId,
            dynamic result,
            string errorMessage,
            string context,
            string actionName
        )
        {
            var subscriptionFilterList = new List<SubscriptionFilter>
            {
                new SubscriptionFilter
                {
                    Context = context,
                    ActionName = actionName,
                    Value = notifySubscriptionId
                }
            };
            var response = JsonConvert.SerializeObject(new
            {
                Result = result,
                Success = isSuccess,
                ErrorMessage = errorMessage
            });

            await Notify(response, subscriptionFilterList);
        }

        public async Task PaymentNotification(
            bool isSuccess,
            string notifySubscriptionId,
            dynamic result,
            string context,
            string actionName,
            string denormalizePayload = null
        )
        {
            var subscriptionFilters = new List<SubscriptionFilter>
            {
                new SubscriptionFilter
                {
                    Context = context,
                    ActionName = actionName,
                    Value = notifySubscriptionId
                }
            };
            var response = JsonConvert.SerializeObject(new
            {
                Result = result,
                Success = isSuccess
            });

            await Notify(response, subscriptionFilters, denormalizePayload);
        }

        public async Task ProcessGuideUpdateNotification(bool isSuccess, string notifySubscriptionId)
        {
            var subscriptionFilters = new List<SubscriptionFilter>
            {
                new SubscriptionFilter
                {
                    Context = "ProcessGuideUpdated",
                    ActionName = "ProcessGuideUpdated",
                    Value = notifySubscriptionId
                }
            };
            var response = JsonConvert.SerializeObject(new
            {
                Success = isSuccess
            });

            await Notify(response, subscriptionFilters);
        }

        public async Task QrCodeGenerateNotification(bool isSuccess, string notifySubscriptionId, string equipmentQrFileId)
        {
            var subscriptionFilters = new List<SubscriptionFilter>
            {
                new SubscriptionFilter
                {
                    Context = "EquipmentCreation",
                    ActionName = "qrCodeGenerate",
                    Value = notifySubscriptionId
                }
            };
            var response = JsonConvert.SerializeObject(new
            {
                EquipmentQrFileId = equipmentQrFileId,
                Success = isSuccess
            });

            await Notify(response, subscriptionFilters);
        }

        public async Task UpdateCustomSubscriptionNotification(bool isSuccess, string notifySubscriptionId)
        {
            var subscriptionFilters = new List<SubscriptionFilter>
            {
                new SubscriptionFilter
                {
                    Context = "UsageUpdated",
                    ActionName = "UpdateUsage",
                    Value = notifySubscriptionId
                }
            };
            var response = JsonConvert.SerializeObject(new
            {
                Success = isSuccess
            });

            await Notify(response, subscriptionFilters);
        }

        public async Task GetHtmlFileIdFromObjectArtifactDocumentSubscriptionNotification(bool isSuccess, string notifySubscriptionId, string denormalizePayload = null)
        {
            var subscriptionFilters = new List<SubscriptionFilter>
            {
                new SubscriptionFilter
                {
                    Context = "HtmlFileIdFromDocumentConversion",
                    ActionName = "HtmlFileIdFromDocumentConversion",
                    Value = notifySubscriptionId
                }
            };
            var response = JsonConvert.SerializeObject(new
            {
                Success = isSuccess
            });

            await Notify(response, subscriptionFilters, denormalizePayload);
        }

        public async Task GetCommonSubscriptionNotification(bool isSuccess, string notifySubscriptionId, string context, string actionName, string denormalizePayload = null)
        {
            var subscriptionFilters = new List<SubscriptionFilter>
            {
                new SubscriptionFilter
                {
                    Context = context,
                    ActionName = actionName,
                    Value = notifySubscriptionId
                }
            };
            var response = JsonConvert.SerializeObject(new
            {
                Success = isSuccess
            });

            await Notify(response, subscriptionFilters, denormalizePayload);
        }

        public async Task LibraryFromUpdateNotification(bool isSuccess, string notifySubscriptionId, string actionName, string denormalizePayload = null)
        {
            var subscriptionFilters = new List<SubscriptionFilter>
            {
                new SubscriptionFilter
                {
                    Context = actionName,
                    ActionName = actionName,
                    Value = notifySubscriptionId
                }
            };
            var response = JsonConvert.SerializeObject(new
            {
                Success = isSuccess
            });

            await Notify(response, subscriptionFilters, denormalizePayload);
        }

        public async Task EquipmentValidationReportPdfGenerationNotification(bool isSuccess, string notifySubscriptionId, string outputFileId)
        {
            var subscriptionFilters = new List<SubscriptionFilter>
            {
                new SubscriptionFilter
                {
                    Context = "EquipmentValidationReportPdfGenerated",
                    ActionName = "EquipmentValidationReportPdfGenerated",
                    Value = notifySubscriptionId
                }
            };
            var response = JsonConvert.SerializeObject(new
            {
                Success = isSuccess
            });

            var denormalizePayload = JsonConvert.SerializeObject(new
            {
                PdfFileId = outputFileId
            });

            await Notify(response, subscriptionFilters, denormalizePayload);
        }
    }
}
