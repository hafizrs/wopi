using FluentValidation;
using FluentValidation.Results;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.Validators
{
    public class LibraryFileVersionComparisonCommandValidator : AbstractValidator<LibraryFileVersionComparisonCommand>
    {
        public LibraryFileVersionComparisonCommandValidator() { }

        public ValidationResult IsSatisfiedBy(LibraryFileVersionComparisonCommand command)
        {
            var commandValidity = Validate(command);

            return !commandValidity.IsValid ? commandValidity : new ValidationResult();
        }

        public static implicit operator LibraryFileVersionComparisonCommandValidator(LibraryFileVersionComparisonCommand v)
        {
            throw new NotImplementedException();
        }
    }
}
