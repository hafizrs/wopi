using System;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Constants
{
   public class PraxisPaymentConstants
    {
        protected PraxisPaymentConstants() { }

        public const string RQMonitor = "RQ_MONITOR";
        public const string ProcessGuide = "PROCESS_GUIDE";
        public const string CompletePackage = "COMPLETE_PACKAGE";
        public const string OrganizationTypeId = "5a0fb78e-31fb-4de2-b260-326b4abee469";
        private const string CDN = "https://az-cdn.selise.biz";
        public const string CdnBaseUrl = CDN + "/selisecdn/cdn/praxismonitor/images/logo/";
        public static string GetInvoiceLogoBase64String()
        {
            var logoUrl = CdnBaseUrl + "RiQS-Brand.png";
            byte[] image = new System.Net.WebClient().DownloadData(logoUrl);
            return Convert.ToBase64String(image);
        }
    }
}
