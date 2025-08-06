using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Models
{



    public class BatchResponse
    {
        [JsonPropertyName("responses")]
        public List<BatchResponseItem> Responses { get; set; }
    }

    public class BatchResponseItem
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }
        [JsonPropertyName("status")]
        public int Status { get; set; }
        [JsonPropertyName("body")]
        public JsonElement Body { get; set; }
    }

   
    public class SharePointResult
    {
        public Dictionary<string, FolderDetails> Folders { get; set; } = new Dictionary<string, FolderDetails>();
        public Dictionary<string, FileDetails> Files { get; set; } = new Dictionary<string, FileDetails>();
        public List<ItemDetails> AllItems { get; set; } = new List<ItemDetails>();
    }

    public class FolderDetails
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Path { get; set; }
        public string ParentId { get; set; }
        public long Size { get; set; }
        public int ChildCount { get; set; }
        public List<ItemChild> Children { get; set; } = new List<ItemChild>();
        public DateTime LastModified { get; set; }
    }

    public class FileDetails
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Path { get; set; }
        public string ParentId { get; set; }
        public long Size { get; set; }
        public string MimeType { get; set; }
        [JsonPropertyName("@microsoft.graph.downloadUrl")]
        public string DownloadUrl { get; set; }
        public DateTime LastModified { get; set; }
    }

    public class ItemDetails
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Path { get; set; }
        public string ParentId { get; set; }
        public long Size { get; set; }
        public string Type { get; set; }
        public DateTime LastModified { get; set; }
    }

    public class ItemChild
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Path { get; set; }
        public string Type { get; set; }
        public long Size { get; set; }
        public DateTime LastModified { get; set; }
        public string DownloadUrl { get; set; }
    }

    public class BatchRequest
    {
        public string AccessToken { get; set; }
        public List<BatchRequestItem> Requests { get; set; } = new List<BatchRequestItem>();
    }

    public class BatchRequestItem
    {
        public string Id { get; set; }
        public string Method { get; set; }
        public string Url { get; set; }
    }



}
