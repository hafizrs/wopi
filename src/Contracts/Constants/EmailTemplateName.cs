using System;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Constants
{
    public enum EmailTemplateName
    {
        TaskNotChecked,
        TaskFulFillment,
        TaskRescheduled,
        TrainingAssigned,
        MaintenanceScheduled,
        TaskAssignedRisk,
        TaskAssignedTraining,
        TaskAssignedGeneral,
        TaskOverdue,
        TaskCompleted,
        TaskOverdueResponsible,
        UserUpdateConfirmation,
        UserLimitReached,
        SubscriptionUpdateConfirmation,
        TaskUnchecked,
        MaintenanceDeleted,
        ValidationScheduled,
        ValidationDeleted,
        HintReported,
        HintReceived,
        FeedbackReceived,
        RequalifiedAsFeedback,
        CIRSReported,
        ProcessGuideOverdue,
        TaskAssigned,
        LogRecorded,
        ExternalMaintenanceReportSignature
    }

    public enum EmailField
    {
        [StringValue("Display Name")] DisplayName,
        [StringValue("Person Id")] PersonId
    }


    public class StringValueAttribute : Attribute
    {
        private readonly string _value;
        public StringValueAttribute(string value)
        {
            _value = value;
        }
        public string Value
        {
            get { return _value; }
        }
    }
}
