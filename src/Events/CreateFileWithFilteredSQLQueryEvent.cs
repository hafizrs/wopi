
using SeliseBlocks.Genesis.Framework.Bus.Contracts.Event;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Selise.Ecap.TemplateEngine.Events
{
    public class CreateFileWithFilteredSQLQueryEvent : BlocksEvent
    {
        public string FileId { get; set; }
        public string SubscriptionFilterId { get; set; }
        public string Message { get; set; }
        public int StatusCode { get; set; }
        public bool Success { get; set; }
        public Dictionary<string, string> EventReferenceData { get; set; } = null;
    }
}

