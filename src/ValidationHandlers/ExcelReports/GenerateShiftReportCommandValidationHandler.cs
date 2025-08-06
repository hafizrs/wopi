

using System.Threading.Tasks;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands;
using Selise.Ecap.SC.PraxisMonitor.Validators.ExcelReports;

namespace Selise.Ecap.SC.PraxisMonitor.ValidationHandlers.ExcelReports;

public class GenerateShiftReportCommandValidationHandler : IValidationHandler<GenerateShiftReportCommand, CommandResponse>
{
    private readonly GenerateShiftReportCommandValidator _validator;

    public GenerateShiftReportCommandValidationHandler
    (
        GenerateShiftReportCommandValidator validator
    )
    {
        _validator = validator;
    }
    public CommandResponse Validate(GenerateShiftReportCommand command)
    {
        throw new System.NotImplementedException();
    }

    public Task<CommandResponse> ValidateAsync(GenerateShiftReportCommand command)
    {
        var validationResult = _validator.IsSatisfiedBy(command);

        return Task.FromResult(validationResult.IsValid ? new CommandResponse() : new CommandResponse(validationResult));
    }
}