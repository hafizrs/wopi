using System.Threading.Tasks;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands;
using Selise.Ecap.SC.PraxisMonitor.Validators;

namespace Selise.Ecap.SC.PraxisMonitor.ValidationHandlers
{
    public class GenerateDocumentUsingTemplateEngineCommandValidationHandler: IValidationHandler<GenerateDocumentUsingTemplateEngineCommand, CommandResponse>
    {
        private readonly GenerateDocumentUsingTemplateEngineCommandValidator _validator;

        public GenerateDocumentUsingTemplateEngineCommandValidationHandler(GenerateDocumentUsingTemplateEngineCommandValidator validator)
        {
            _validator = validator;
        }
        public CommandResponse Validate(GenerateDocumentUsingTemplateEngineCommand command)
        {
            throw new System.NotImplementedException();
        }

        public Task<CommandResponse> ValidateAsync(GenerateDocumentUsingTemplateEngineCommand command)
        {
            var validationResult = _validator.IsSatisfiedBy(command);

            return Task.FromResult(validationResult.IsValid ? new CommandResponse() : new CommandResponse(validationResult));
        }
    }
}