using Newtonsoft.Json;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Models
{
    public class DynamicLinkGeneratePayload
    {
        [JsonProperty("dynamicLinkInfo")]
        public DynamicLinkInfo DynamicLinkInfo { get; set; }

        [JsonProperty("suffix")]
        public Suffix Suffix { get; set; }
    }

    public class DynamicLinkInfo
    {
        [JsonProperty("domainUriPrefix")]
        public string DomainUriPrefix { get; set; }

        [JsonProperty("link")]
        public string Link { get; set; }

        [JsonProperty("androidInfo")]
        public AndroidInfo AndroidInfo { get; set; }

        [JsonProperty("iosInfo")]
        public IosInfo IosInfo { get; set; }

        [JsonProperty("navigationInfo")]
        public NavigationInfo NavigationInfo { get; set; }

        [JsonProperty("analyticsInfo")]
        public AnalyticsInfo AnalyticsInfo { get; set; }

        [JsonProperty("socialMetaTagInfo")]
        public SocialMetaTagInfo SocialMetaTagInfo { get; set; }
    }
    public class AndroidInfo
    {
        [JsonProperty("androidPackageName")]
        public string AndroidPackageName { get; set; }

        [JsonProperty("androidFallbackLink")]
        public string AndroidFallbackLink { get; set; }

        [JsonProperty("androidMinPackageVersionCode")]
        public string AndroidMinPackageVersionCode { get; set; }
    }
    public class IosInfo
    {
        [JsonProperty("iosBundleId")]
        public string IosBundleId { get; set; }

        [JsonProperty("iosFallbackLink")]
        public string IosFallbackLink { get; set; }

        [JsonProperty("iosCustomScheme")]
        public string IosCustomScheme { get; set; }

        [JsonProperty("iosIpadFallbackLink")]
        public string IosIpadFallbackLink { get; set; }

        [JsonProperty("iosIpadBundleId")]
        public string IosIpadBundleId { get; set; }

        [JsonProperty("iosAppStoreId")]
        public string IosAppStoreId { get; set; }
    }

    public class NavigationInfo
    {
        [JsonProperty("enableForcedRedirect")]
        public bool EnableForcedRedirect { get; set; }
    }

    public class AnalyticsInfo
    {
        [JsonProperty("googlePlayAnalytics")]
        public GooglePlayAnalytics GooglePlayAnalytics { get; set; }

        [JsonProperty("itunesConnectAnalytics")]
        public ItunesConnectAnalytics ItunesConnectAnalytics { get; set; }
    }
    public class GooglePlayAnalytics
    {
        [JsonProperty("utmSource")]
        public string UtmSource { get; set; }

        [JsonProperty("utmMedium")]
        public string UtmMedium { get; set; }

        [JsonProperty("utmCampaign")]
        public string UtmCampaign { get; set; }

        [JsonProperty("utmTerm")]
        public string UtmTerm { get; set; }

        [JsonProperty("utmContent")]
        public string UtmContent { get; set; }

        [JsonProperty("gclid")]
        public string Gclid { get; set; }
    }

    public class ItunesConnectAnalytics
    {
        [JsonProperty("at")]
        public string At { get; set; }

        [JsonProperty("ct")]
        public string Ct { get; set; }

        [JsonProperty("mt")]
        public string Mt { get; set; }

        [JsonProperty("pt")]
        public string Pt { get; set; }
    }

    public class SocialMetaTagInfo
    {
        [JsonProperty("socialTitle")]
        public string SocialTitle { get; set; }

        [JsonProperty("socialDescription")]
        public string SocialDescription { get; set; }

        [JsonProperty("socialImageLink")]
        public string SocialImageLink { get; set; }
    }

    public class Suffix
    {
        [JsonProperty("option")]
        public string Option { get; set; }
    }
}
