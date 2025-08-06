using FluentValidation;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.CirsReports.Update;

namespace Selise.Ecap.SC.PraxisMonitor.Validators.CirsReports.Update;

public class UpdateComplainReportCommandValidator
    : AbstractUpdateCirsReportCommandValidator<UpdateComplainReportCommand>
{
    public UpdateComplainReportCommandValidator()
    {
        
    }
}