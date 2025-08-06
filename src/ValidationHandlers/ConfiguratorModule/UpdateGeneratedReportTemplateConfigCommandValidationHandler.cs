using System.Threading.Tasks;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.ConfiguratorModule;
using Selise.Ecap.SC.PraxisMonitor.Validators.ConfiguratorModule;
using SeliseBlocks.Genesis.Framework.Infrastructure;

namespace Selise.Ecap.SC.PraxisMonitor.ValidationHandlers.ConfiguratorModule;

public class UpdateGeneratedReportTemplateConfigCommandValidationHandler : IValidationHandler<UpdateGeneratedReportTemplateConfigCommand, CommandResponse>
{
    private readonly UpdateGeneratedReportTemplateConfigCommandValidator _validator;
    public UpdateGeneratedReportTemplateConfigCommandValidationHandler(UpdateGeneratedReportTemplateConfigCommandValidator validator)
    {
        _validator = validator;
    }
    public CommandResponse Validate(UpdateGeneratedReportTemplateConfigCommand command)
    {
        throw new System.NotImplementedException();
    }

    public async Task<CommandResponse> ValidateAsync(UpdateGeneratedReportTemplateConfigCommand command)
    {
        var validationResult = await _validator.IsSatisfiedby(command);
        return !validationResult.IsValid ? new CommandResponse(validationResult) : new CommandResponse();
    }
}