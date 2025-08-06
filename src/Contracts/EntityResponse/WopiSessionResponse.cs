using Selise.Ecap.SC.Wopi.Contracts.Models.WopiModule;
using System;
using SeliseBlocks.Genesis.Framework.PDS.Entity;

namespace Selise.Ecap.SC.Wopi.Contracts.EntityResponse
{
    public class WopiSessionResponse : EntityBase
    {
        public WopiSessionResponse() { }
        public WopiSessionResponse(WopiSession session)
        {
            ItemId = session.ItemId;
            SessionId = session.SessionId;
            FileUrl = session.FileUrl;
            UploadUrl = session.UploadUrl;
            FileName = session.FileName;
            UserId = session.UserId;
            UserDisplayName = session.UserDisplayName;
            CanEdit = session.CanEdit;
            Downloaded = session.Downloaded;
            CreatedAt = session.CreatedAt;
            DepartmentId = session.DepartmentId;
            OrganizationId = session.OrganizationId;
        }

        public string SessionId { get; set; }
        public string FileUrl { get; set; }
        public string UploadUrl { get; set; }
        public string FileName { get; set; }
        public string UserId { get; set; }
        public string UserDisplayName { get; set; }
        public bool CanEdit { get; set; }
        public bool Downloaded { get; set; }
        public DateTime CreatedAt { get; set; }
        public string DepartmentId { get; set; }
        public string OrganizationId { get; set; }
    }
} 