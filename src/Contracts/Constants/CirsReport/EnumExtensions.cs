using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Constants.CirsReport
{
    public static class EnumExtensions
    {
        public static TEnum EnumValue<TEnum>(this string value) where TEnum : struct
        {
            if (Enum.TryParse(value, true, out TEnum result))
                return result;

            throw new ArgumentNullException(nameof(TEnum), "Enum value cannot be null");
        }

        public static List<string> StatusEnumValues(this CirsDashboardName dashboardName, bool isShow = true)
        {
            var statusToBeRemoved = dashboardName.StatusToBeRemoved(isShow);
            return dashboardName.GetCirsReportStatusEnumValues()
                .Where(item => item != statusToBeRemoved)
                .ToList();
        }

        public static List<string> GetCirsReportStatusEnumValues(this CirsDashboardName dashboardName)
        {
            var values = Enum.GetValues(
                dashboardName switch
                {
                    CirsDashboardName.Complain => typeof(CirsComplainStatusEnum),
                    CirsDashboardName.Incident => typeof(CirsIncidentStatusEnum),
                    CirsDashboardName.Hint => typeof(CirsHintStatusEnum),
                    CirsDashboardName.Another => typeof(CirsAnotherStatusEnum),
                    CirsDashboardName.Idea => typeof(CirsIdeaStatusEnum),
                    CirsDashboardName.Fault => typeof(CirsFaultStatusEnum),
                    _ => throw new InvalidEnumArgumentException()
                })
            .Cast<object>()
            .Select(item => item.ToString())
            .ToList();

            return values;
        }

        private static string StatusToBeRemoved(this CirsDashboardName dashboardName, bool isShow)
        {
            string statusToBeRemoved = null;
            if (!isShow)
            {
                switch (dashboardName)
                {
                    case CirsDashboardName.Incident:
                        statusToBeRemoved = CirsIncidentStatusEnum.TO_BE_APPROVED.ToString();
                        break;
                }
            }

            return statusToBeRemoved;
        }
    }
}
