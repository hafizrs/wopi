using System.Threading.Tasks;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.ConfiguratorModule;
using Selise.Ecap.SC.PraxisMonitor.Validators.ConfiguratorModule;
using SeliseBlocks.Genesis.Framework.Infrastructure;

namespace Selise.Ecap.SC.PraxisMonitor.ValidationHandlers.ConfiguratorModule;

public class UpdateGeneratedReportTemplateSectionCommandValidationHandler : IValidationHandler<UpdateGeneratedReportTemplateSectionCommand, CommandResponse>
{
    private readonly UpdateGeneratedReportTemplateSectionCommandValidator _validator;
    public UpdateGeneratedReportTemplateSectionCommandValidationHandler(UpdateGeneratedReportTemplateSectionCommandValidator validator)
    {
        _validator = validator;
    }
    public CommandResponse Validate(UpdateGeneratedReportTemplateSectionCommand command)
    {
        throw new System.NotImplementedException();
    }

    public async Task<CommandResponse> ValidateAsync(UpdateGeneratedReportTemplateSectionCommand command)
    {
        var validationResult = await _validator.IsSatisfiedBy(command);
        return !validationResult.IsValid ? new CommandResponse(validationResult) : new CommandResponse();
    }
}