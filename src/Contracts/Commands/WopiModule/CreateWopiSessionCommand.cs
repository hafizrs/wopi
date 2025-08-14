using System.Collections.Generic;

namespace Selise.Ecap.SC.Wopi.Contracts.Commands.WopiModule
{
    public class CreateWopiSessionCommand
    {
        public string SessionId { get; set; }
        public string FileId { get; set; }
        public string FileUrl { get; set; }
        public string UploadUrl { get; set; }
        public Dictionary<string, string> UploadHeaders { get; set; }
        public string FileName { get; set; }
        public string AccessToken { get; set; }
        public string UserId { get; set; }
        public string UserDisplayName { get; set; }
        public bool CanEdit { get; set; }
        public string DepartmentId { get; set; }
        public string OrganizationId { get; set; }
    }
} 