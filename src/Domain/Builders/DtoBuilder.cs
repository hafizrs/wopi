using Selise.Ecap.Entities.PrimaryEntities.TaskManagement;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.Builders
{
    public class DtoBuilder
    {
        protected DtoBuilder() { }
        public static TaskSummaryDto BuildTaskSummaryDto(TaskSummary taskSummary)
        {
            var taskScheduleDto = new TaskSummaryDto
            {
                ItemId = taskSummary.ItemId,
                FromDate = taskSummary.FromDate,
                FromTime = taskSummary.FromTime,
                ToDate = taskSummary.ToDate,
                ToTime = taskSummary.ToTime,
                IsRepeat = taskSummary.IsRepeat,
                RepeatValue = taskSummary.RepeatValue,
                RepeatAfterOccurence = taskSummary.RepeatAfterOccurence,
                RepeatType = taskSummary.RepeatType,
                ReserveForEver = taskSummary.ReserveForEver,
                StartsOnDate = taskSummary.StartsOnDate,
                RepeatEndDate = taskSummary.RepeatEndDate,
                RepeatEndOption = taskSummary.RepeatEndOption,
                RepeatOnDayOfWeeks = taskSummary.RepeatOnDayOfWeeks,
                IsDayOfTheMonth = taskSummary.IsDayOfTheMonth,
                SubmissionDates = taskSummary.SubmissionDates,
                RepeatingDates = taskSummary.RepeatingDates,
                Status = taskSummary.Status,
                AvoidWeekEnd=taskSummary.AvoidWeekend
            };

            return taskScheduleDto;
        }
    }
}
