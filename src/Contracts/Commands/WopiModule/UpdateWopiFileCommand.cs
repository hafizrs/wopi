using System;

namespace Selise.Ecap.SC.WopiMonitor.Contracts.Commands.WopiModule
{
    public class UpdateWopiFileCommand
    {
        public string SessionId { get; set; }
        public byte[] FileContent { get; set; }
        public string AccessToken { get; set; }
        public string WopiOverride { get; set; }
    }
} 