using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Commands
{
    public class UpdateManualFileUploadStatusCommand
    {
        [JsonPropertyName("file_id")]
        public string FileId { get; set; }

        [JsonPropertyName("is_old_cluster")]
        public bool IsOldCluster { get; set; }

        [JsonPropertyName("file_url")]
        public string FileUrl { get; set; }

        [JsonPropertyName("file_name")]
        public string FileName { get; set; }

        [JsonPropertyName("tenant_id")]
        public string TenantId { get; set; }

        [JsonPropertyName("create_date")]
        public string CreateDate { get; set; }

        [JsonPropertyName("additional_key_value")]
        public List<IAdditionalKeyValue> AdditionalKeyValue { get; set; }

        [JsonPropertyName("subscription_filter")]
        public ISubscriptionFilter SubscriptionFilter { get; set; }

        [JsonPropertyName("webhook_url")]
        public string WebhookUrl { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; }
    }
}
