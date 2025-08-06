using SeliseBlocks.Genesis.Framework.PDS.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Entities.RiqsAIConversations
{
    public class RiqsAIConversation : EntityBase
    {
        public string ConversationId { get; set; }
        public string RelatedEntityId { get; set; }
        public string RelatedEntityName { get; set; }
        public QARecord QARecord { get; set; }
    }

    public class QARecord
    {
        public string RecordId { get; set; }
        public RecordDetail RecordDetail { get; set; }
    }

    public class RecordDetail
    {
        public string Question { get; set; }
        public string Answer { get; set; }
        public CalculatedUsageTokens CalculatedUsageTokens { get; set; }
    }

    public class CalculatedUsageTokens
    {
        public double TotalTokens { get; set; }
        public double InputTokens { get; set; }
        public double OutputTokens { get; set; }
    }

}
