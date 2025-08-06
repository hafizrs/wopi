using System;
using System.Collections.Generic;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.Shared;
using SeliseBlocks.Genesis.Framework.Infrastructure;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Constants
{
    public class PraxisConstants
    {
        protected PraxisConstants() { }

        public const string PraxisTenant = "82D07BF9-CC75-477D-A286-F1A19A9FA0EA";
        public const string ServiceName = "PraxisMonitorHostService";
        private const string PraxisMonitorQueueName = "Selise.Ecap.SC.PraxisMonitor";
        private const string PraxisMonitorQueueName_Development = "Selise.Ecap.SC.PraxisMonitor_Development";
        private const string PraxisMonitorReportQueueName = "Selise.Ecap.SC.PraxisMonitor.Report";
        private const string PraxisMonitorReportQueueName_Development = "Selise.Ecap.SC.PraxisMonitor.Report_Development";
        private const string PraxisMonitorDmsQueueName = "Selise.Ecap.SC.PraxisMonitor.Dms";
        private const string PraxisMonitorDmsQueueName_Development = "Selise.Ecap.SC.PraxisMonitor.Dms_Development";
        private const string PraxisMonitorDmsLibraryFormSignQueueName = "Selise.Ecap.SC.PraxisMonitor.DMS_Library_Form_Sign";
        private const string PraxisMonitorDmsLibraryFormSignQueueName_Development = "Selise.Ecap.SC.PraxisMonitor.DMS_Library_Form_Sign_Development";
        public const string RQMonitorClientId = "d1ca7172-2120-4eb2-a7af-a00fd99fdbe2";
        public const string PraxisMonitorDmsConversionQueueName = "Selise.Ecap.SC.PraxisMonitor.DmsConversion";
        public const string PraxisMonitorDmsConversionQueueName_Development = "Selise.Ecap.SC.PraxisMonitor.DmsConversion_Development";
        public const string MaintenanceValidationFooterHtmlId = "21a087f1-9278-4569-ab99-64efbb43e9be";

        public static readonly PraxisKeyValue OpenItemDoneStatus = new PraxisKeyValue { Key = "done", Value = "DONE" };
        public static readonly PraxisKeyValue OpenItemPendingStatus = new PraxisKeyValue { Key = "pending", Value = "PENDING" };
        public static readonly PraxisKeyValue OpenItemAlternativelyDoneStatus = new PraxisKeyValue
            { Key = "alternatively-done", Value = "ALTERNATIVELY_DONE" };

        public static string GetPraxisQueueName()
        {
            var isDevelopment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development";
            return isDevelopment ? PraxisMonitorQueueName_Development : PraxisMonitorQueueName;
        }
        
        public static string GetReportQueueName()
        {
            var isDevelopment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development";
            return isDevelopment ? PraxisMonitorReportQueueName_Development : PraxisMonitorReportQueueName;
        }

        public static string GetPraxisDmsQueueName()
        {
            var isDevelopment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development";
            return isDevelopment ? PraxisMonitorDmsQueueName_Development : PraxisMonitorDmsQueueName;
        }

        public static string GetPraxisDmsLibraryFormSignQueueName()
        {
            var isDevelopment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development";
            return isDevelopment ? PraxisMonitorDmsLibraryFormSignQueueName_Development : PraxisMonitorDmsLibraryFormSignQueueName;
        }
        public static string GetPraxisDmsConversionQueueName()
        {
            var isDevelopment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development";
            return isDevelopment ? PraxisMonitorDmsConversionQueueName_Development : PraxisMonitorDmsConversionQueueName;
        }

        public static readonly int OrganizationMinimumUserLimit = 10;
        public static readonly int OrganizationMaximumUserLimit = 500;
        public static readonly int DepartmentMaximumUserLimit = 500;
        public static readonly string SubscriptionInvoiceTemplateId = "68fcd0a2-70ec-4a7c-b274-21d473086a3d";

        public static SecurityContext CreateSecurityContext()
        {
            return new SecurityContext
                (
                    organizationId: string.Empty,
                    email: string.Empty,
                    language: string.Empty,
                    requestOrigin: "",
                    phoneNumber: "no-phone",
                    roles: new List<string> {
                        RoleNames.Anonymous,
                        RoleNames.SystemAdmin,
                        RoleNames.Admin,
                        RoleNames.AppUser},
                    sessionId: $"ecap-{Guid.NewGuid().ToString()}",
                    siteId: string.Empty,
                    siteName: "",
                    tenantId: PraxisTenant,
                    displayName: string.Empty,
                    userId: Guid.NewGuid().ToString(),
                    isUserAuthenticated: true,
                    userName: string.Empty,
                    hasDynamicRoles: false,
                    userAutoExpire: false,
                    userExpireOn: DateTime.MinValue,
                    userPrefferedLanguage: string.Empty,
                    isAuthenticated: true,
                    oauthBearerToken: string.Empty,
                    requestUri: new Uri("about:blank"),
                    serviceVersion: string.Empty,
                    tokenHijackingProtectionHash: string.Empty,
                    postLogOutHandlerDataKey: string.Empty
                );
        }

    }
}
