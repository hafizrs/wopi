using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.CirsReports.Create;
using Selise.Ecap.SC.PraxisMonitor.Validators.CirsReports.Create;

namespace Selise.Ecap.SC.PraxisMonitor.ValidationHandlers.CirsReports.Create;

public class CreateAnotherMessageCommandValidationHandler
    : AbstractCreateCirsReportCommandValidationHandler<CreateAnotherMessageCommand>
{
    public CreateAnotherMessageCommandValidationHandler(
        CreateAnotherMessageCommandValidator validator) : base(validator)
    {
    }
}

public class CreateComplainReportCommandValidationHandler
    : AbstractCreateCirsReportCommandValidationHandler<CreateComplainReportCommand>
{
    public CreateComplainReportCommandValidationHandler(
        CreateComplainReportCommandValidator validator) : base(validator)
    {
    }
}

public class CreateHintReportCommandValidationHandler
    : AbstractCreateCirsReportCommandValidationHandler<CreateHintReportCommand>
{
    public CreateHintReportCommandValidationHandler(
        CreateHintReportCommandValidator validator) : base(validator)
    {
    }
}

public class CreateIdeaReportCommandValidationHandler
    : AbstractCreateCirsReportCommandValidationHandler<CreateIdeaReportCommand>
{
    public CreateIdeaReportCommandValidationHandler(
        CreateIdeaReportCommandValidator validator) : base(validator)
    {
    }
}

public class CreateIncidentReportCommandValidationHandler
    : AbstractCreateCirsReportCommandValidationHandler<CreateIncidentReportCommand>
{
    public CreateIncidentReportCommandValidationHandler(
        CreateIncidentReportCommandValidator validator) : base(validator)
    {
    }
}

public class CreateFaultReportCommandValidationHandler
    : AbstractCreateCirsReportCommandValidationHandler<CreateFaultReportCommand>
{
    public CreateFaultReportCommandValidationHandler(
        CreateFaultReportCommandValidator validator) : base(validator)
    {
    }
}
