using System.Threading.Tasks;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands;
using Selise.Ecap.SC.PraxisMonitor.Validators;

namespace Selise.Ecap.SC.PraxisMonitor.ValidationHandlers
{
    public class GeneratePdfUsingTemplateEngineCommandValidationHandler: IValidationHandler<GeneratePdfUsingTemplateEngineCommand, CommandResponse>
    {
        private readonly GeneratePdfUsingTemplateEngineCommandValidator _validator;

        public GeneratePdfUsingTemplateEngineCommandValidationHandler(GeneratePdfUsingTemplateEngineCommandValidator validator)
        {
            _validator = validator;
        }
        public CommandResponse Validate(GeneratePdfUsingTemplateEngineCommand command)
        {
            throw new System.NotImplementedException();
        }

        public Task<CommandResponse> ValidateAsync(GeneratePdfUsingTemplateEngineCommand command)
        {
            return Task.FromResult(new CommandResponse(_validator.IsSatisfiedBy(command)));
        }
    }
}