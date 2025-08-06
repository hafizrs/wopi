using System;

namespace Selise.Ecap.SC.PraxisMonitor.Queries
{
    [Obsolete]
    public class GetIncidentQuery
    {
        public string IncidentId { get; set; }
        public string OrganizationId { get; set; } = string.Empty;
        public bool IsArchived { get; set; } = false;
        public string TextSearchKey { get; set; }
        public IncidentFilter FilterObject { get; set;} = new IncidentFilter();
    }

    [Obsolete]
    public class IncidentFilter
    {
        public DateFilter CreateDate { get; set; } = new DateFilter();
    }

    [Obsolete]
    public class DateFilter
    {
        public DateTime? From { get; set; }
        public DateTime? To { get; set; }
    }
}