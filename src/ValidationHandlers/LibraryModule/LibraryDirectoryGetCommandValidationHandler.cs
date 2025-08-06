using System.Threading.Tasks;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using Selise.Ecap.SC.PraxisMonitor.Validators;

namespace Selise.Ecap.SC.PraxisMonitor.ValidationHandlers
{
    public class LibraryDirectoryGetCommandValidationHandler  : IValidationHandler<LibraryDirectoryGetCommand, RiqsCommandResponse>
    {
        private readonly LibraryDirectoryGetCommandValidator _validator;

        public LibraryDirectoryGetCommandValidationHandler (LibraryDirectoryGetCommandValidator validator)
        {
            _validator = validator;
        }

        public RiqsCommandResponse Validate(LibraryDirectoryGetCommand command)
        {
            throw new System.NotImplementedException();
        }

        public Task<RiqsCommandResponse> ValidateAsync(LibraryDirectoryGetCommand command)
        {
            var validationResult = _validator.IsSatisfiedBy(command);

            return Task.FromResult(validationResult.IsValid ? new RiqsCommandResponse() : new RiqsCommandResponse(validationResult));
        }
    }
}