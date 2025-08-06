using Selise.Ecap.SC.PraxisMonitor.Contracts;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Selise.Ecap.SC.PraxisMonitor.Validators.CirsReports;

#nullable enable
public static class ValidationHelper
{
    public static bool IsValidIncidentTopic(string topic)
    {
        var validTopicList = Enum.GetValues<RiqsIncidentTopicEnums>().Select(value => value.ToString());

        return validTopicList.Contains(topic);
    }

    public static bool IsValidTag(IEnumerable<string> tags)
    {
        var cirsReportTags = new[] { PraxisTag.IsValidCirsReport, PraxisTag.IsValidDuplicatedCirsReport };
        var isValid = cirsReportTags.Contains(tags.First());

        return isValid;
    }

    public static bool IsValidCirsReport(IEnumerable<string> tags)
    {
        return tags?.Count() == 1 && (
            tags.First() == PraxisTag.IsValidCirsReport ||
            tags.First() == PraxisTag.IsValidDuplicatedCirsReport);
    }
}
