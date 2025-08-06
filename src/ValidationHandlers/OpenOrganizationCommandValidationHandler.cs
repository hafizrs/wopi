using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Commands;
using Selise.Ecap.SC.PraxisMonitor.Validators;
using System;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.ValidationHandlers
{
    public class OpenOrganizationCommandValidationHandler : IValidationHandler<UpdateOpenOrganizationCommand, CommandResponse>
    {
        private readonly OpenOrganizationCommandValidator _openOrganizationCommandValidator;

        public OpenOrganizationCommandValidationHandler(
            OpenOrganizationCommandValidator openOrganizationCommandValidator)
        {
            _openOrganizationCommandValidator = openOrganizationCommandValidator;
        }
        public CommandResponse Validate(UpdateOpenOrganizationCommand command)
        {
            throw new NotImplementedException();
        }

        public Task<CommandResponse> ValidateAsync(UpdateOpenOrganizationCommand command)
        {
            var validationResult = _openOrganizationCommandValidator.IsSatisfiedby(command);

            if (!validationResult.IsValid)
                return Task.FromResult(new CommandResponse(validationResult));
            else
            {
                return Task.FromResult(new CommandResponse());
            }
        }
    }
}
