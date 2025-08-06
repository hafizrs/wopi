using System.Threading.Tasks;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands;
using Selise.Ecap.SC.PraxisMonitor.Validators;

namespace Selise.Ecap.SC.PraxisMonitor.ValidationHandlers
{
    public class ResolveProdDataIssuesCommandValidationHandler : IValidationHandler<ResolveProdDataIssuesCommand, CommandResponse>
    {
        private readonly ResolveProdDataIssuesCommandValidator _validator;

        public ResolveProdDataIssuesCommandValidationHandler (ResolveProdDataIssuesCommandValidator validator)
        {
            _validator = validator;
        }

        public CommandResponse Validate(ResolveProdDataIssuesCommand command)
        {
            throw new System.NotImplementedException();
        }

        public Task<CommandResponse> ValidateAsync(ResolveProdDataIssuesCommand command)
        {
            var validationResult = _validator.IsSatisfiedBy(command);

            return Task.FromResult(validationResult.IsValid ? new CommandResponse() : new CommandResponse(validationResult));
        }
    }
}