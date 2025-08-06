using Selise.Ecap.SC.PraxisMonitor.Contracts.Queries;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.AIConversations
{
    public interface IAIConversationService
    {
        Task<ReceiveAIResponses> GetAIConversation(GetAIConversationQuery query);
        Task SaveAIConversationRecord(GetAIConversationQuery query, ReceiveAIResponses receiveMessages);
    }
}
