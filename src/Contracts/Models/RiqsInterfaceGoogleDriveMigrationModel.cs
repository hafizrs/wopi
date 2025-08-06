using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Models
{
    public class GoogleDriveResult
    {
        public Dictionary<string, FolderDetails> Folders { get; set; } = new Dictionary<string, FolderDetails>();
        public Dictionary<string, FileDetails> Files { get; set; } = new Dictionary<string, FileDetails>();
    }

    public class GoogleDriveFileDetails
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }
        [JsonPropertyName("name")]
        public string Name { get; set; }
        [JsonPropertyName("mimeType")]
        public string MimeType { get; set; }
        [JsonPropertyName("size")]
        public string Size { get; set; }
    }
}
