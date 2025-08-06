using Selise.Ecap.SC.Wopi.Contracts.Commands;

namespace Selise.Ecap.SC.Wopi.Contracts.Commands.WopiModule
{
    public class LockWopiFileCommand
    {
        public string SessionId { get; set; }
        public string AccessToken { get; set; }
        public string WopiOverride { get; set; } // LOCK, UNLOCK, REFRESH_LOCK
    }
} 