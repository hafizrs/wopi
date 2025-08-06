using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands;
using Selise.Ecap.SC.PraxisMonitor.Validators;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.ValidationHandlers
{
    public class CreateLibraryGroupCommandValidationHandler : IValidationHandler<CreateLibraryGroupCommand, CommandResponse>
    {
        private readonly CreateLibraryGroupCommandValidator _validator;

        public CreateLibraryGroupCommandValidationHandler(CreateLibraryGroupCommandValidator validator)
        {
            _validator = validator;
        }

        public CommandResponse Validate(CreateLibraryGroupCommand command)
        {
            throw new NotImplementedException();
        }

        public Task<CommandResponse> ValidateAsync(CreateLibraryGroupCommand command)
        {
            var validationResult = _validator.IsSatisfiedBy(command);

            return Task.FromResult(validationResult.IsValid ? new CommandResponse() : new CommandResponse(validationResult));
        }
    }
}
