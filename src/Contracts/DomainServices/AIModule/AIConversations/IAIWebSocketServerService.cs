using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.AIConversations
{
    public interface IAIWebSocketServerService
    {
        Task HandleClientConnectionAsync(WebSocket webSocket);
    }
}
