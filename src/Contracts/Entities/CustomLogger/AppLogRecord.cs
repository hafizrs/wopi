using Microsoft.Extensions.Logging;
using MongoDB.Bson.Serialization.Attributes;
using System;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Entities.CustomLogger
{
    public class AppLogRecord
    {
        [BsonElement("_id")]
        public string ItemId { get; set; }
        public string CreatedBy { get; set; }
        public DateTime CreateDate { get; set; }

        public string LogLevel { get; set; } // "Error", "Info", "Warning", etc.

        public string Source { get; set; } // Category, like class name

        public string State { get; set; }

        public string Message { get; set; } // Formatted log message

        public string ExceptionMessage { get; set; }

        public string StackTrace { get; set; }

        public string StructuredData { get; set; } // Optional key-value data (e.g. serialized JSON)

        public EventId? EventId { get; set; } // Optional event ID
    }
}
