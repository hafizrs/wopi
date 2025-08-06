using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using Selise.Ecap.SC.PraxisMonitor.Validators;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.ValidationHandlers
{
    public class LibraryFileVersionComparisonCommandValidationHandler : IValidationHandler<LibraryFileVersionComparisonCommand, RiqsCommandResponse>
    {
        private readonly LibraryFileVersionComparisonCommandValidator _validator;

        public LibraryFileVersionComparisonCommandValidationHandler(LibraryFileVersionComparisonCommandValidator validator)
        {
            _validator = validator;
        }

        public RiqsCommandResponse Validate(LibraryFileVersionComparisonCommand command)
        {
            throw new NotImplementedException();
        }

        public Task<RiqsCommandResponse> ValidateAsync(LibraryFileVersionComparisonCommand command)
        {
            var validationResult = _validator.IsSatisfiedBy(command);

            return Task.FromResult(validationResult.IsValid ? new RiqsCommandResponse() : new RiqsCommandResponse(validationResult));
        }
    }
}
