using System;
using System.Collections.Generic;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Models
{
    public class QueryCompletionResponse
    {     
        public List<string> AssignedMembers { get; set; }
        public List<string> DoneMembers { get; set; }
        public List<string> AlternativelyDoneMembers { get; set; }
        public List<string> PendingMembers { get; set; }
        public IEnumerable<CompletionHistory> CompletionHistory { get; set; }
        public ReferenceMeasure MeasuresForReference { get; set; }
    }

    public class CompletionHistory
    {
        public string TaskTitle { get; set; }
        public string TaskId { get; set; }
        public DateTime DueDate { get; set; }
        public IEnumerable<CompletionStatus> CompletionStatus { get; set; }
        public bool IsCompleted { get; set; }
        public bool IsSingleAnswerTask { get; set; }
    }

    public class ReferenceMeasure
    {
        public int MeasuresTaken { get; set; }
        public int MeasuresPending { get; set; }
    }

    public class CompletionStatus
    {
        public string AssignMember { get; set; }
        public string Status { get; set; }
    }
}
