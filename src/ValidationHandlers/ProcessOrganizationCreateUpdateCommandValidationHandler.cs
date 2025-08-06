using System.Threading.Tasks;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands;
using Selise.Ecap.SC.PraxisMonitor.Validators;

namespace Selise.Ecap.SC.PraxisMonitor.ValidationHandlers
{
    public class ProcessOrganizationCreateUpdateCommandValidationHandler : IValidationHandler<ProcessOrganizationCreateUpdateCommand, CommandResponse>
    {
        private readonly ProcessOrganizationCreateUpdateCommandValidator _validator;

        public ProcessOrganizationCreateUpdateCommandValidationHandler (ProcessOrganizationCreateUpdateCommandValidator validator)
        {
            _validator = validator;
        }

        public CommandResponse Validate(ProcessOrganizationCreateUpdateCommand command)
        {
            throw new System.NotImplementedException();
        }

        public Task<CommandResponse> ValidateAsync(ProcessOrganizationCreateUpdateCommand command)
        {
            var validationResult = _validator.IsSatisfiedBy(command);

            return Task.FromResult(validationResult.IsValid ? new CommandResponse() : new CommandResponse(validationResult));
        }
    }
}