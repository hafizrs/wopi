using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.CirsReports.Update;
using Selise.Ecap.SC.PraxisMonitor.Validators.CirsReports.Update;

namespace Selise.Ecap.SC.PraxisMonitor.ValidationHandlers.CirsReports.Update;

public class UpdateAnotherMessageCommandValidationHandler
    : AbstractUpdateCirsReportCommandValidationHandler<UpdateAnotherMessageCommand>
{
    public UpdateAnotherMessageCommandValidationHandler(
        UpdateAnotherMessageCommandValidator validator) : base(validator)
    {
    }
}

public class UpdateComplainReportCommandValidationHandler
    : AbstractUpdateCirsReportCommandValidationHandler<UpdateComplainReportCommand>
{
    public UpdateComplainReportCommandValidationHandler(
        UpdateComplainReportCommandValidator validator) : base(validator)
    {
    }
}

public class UpdateHintReportCommandValidationHandler
    : AbstractUpdateCirsReportCommandValidationHandler<UpdateHintReportCommand>
{
    public UpdateHintReportCommandValidationHandler(
        UpdateHintReportCommandValidator validator) : base(validator)
    {
    }
}

public class UpdateIdeaReportCommandValidationHandler
    : AbstractUpdateCirsReportCommandValidationHandler<UpdateIdeaReportCommand>
{
    public UpdateIdeaReportCommandValidationHandler(
        UpdateIdeaReportCommandValidator validator) : base(validator)
    {
    }
}

public class UpdateIncidentReportCommandValidationHandler
    : AbstractUpdateCirsReportCommandValidationHandler<UpdateIncidentReportCommand>
{
    public UpdateIncidentReportCommandValidationHandler(
        UpdateIncidentReportCommandValidator validator) : base(validator)
    {
    }
}

public class UpdateFaultReportCommandValidationHandler
    : AbstractUpdateCirsReportCommandValidationHandler<UpdateFaultReportCommand>
{
    public UpdateFaultReportCommandValidationHandler(
        UpdateFaultReportCommandValidator validator) : base(validator)
    {
    }
}