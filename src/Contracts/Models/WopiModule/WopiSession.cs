using SeliseBlocks.Genesis.Framework.PDS.Entity;
using System;

namespace Selise.Ecap.SC.WopiMonitor.Contracts.Models.WopiModule
{
    public class WopiSession : EntityBase
    {
        public string SessionId { get; set; }
        public string FileUrl { get; set; }
        public string UploadUrl { get; set; }
        public string UploadHeaders { get; set; } // JSON string
        public string FileName { get; set; }
        public string AccessToken { get; set; }
        public string UserId { get; set; }
        public string UserDisplayName { get; set; }
        public bool CanEdit { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool Downloaded { get; set; }
        public string LocalFilePath { get; set; }
        public string DepartmentId { get; set; }
        public string OrganizationId { get; set; }
    }
} 