using System.Collections.Generic;

namespace Selise.Ecap.SC.Wopi.Contracts.Commands.WopiModule
{
    public class UploadFileToUrlCommand
    {
        public string UploadUrl { get; set; }
        public Dictionary<string, string> UploadHeaders { get; set; }
        public string SessionId { get; set; }
    }
} 