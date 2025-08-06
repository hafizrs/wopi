using FluentValidation;
using FluentValidation.Results;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands;
using System;

namespace Selise.Ecap.SC.PraxisMonitor.Validators
{
    public class LibraryDirectoryGetCommandValidator : AbstractValidator<LibraryDirectoryGetCommand>
    {
        public LibraryDirectoryGetCommandValidator()
        {
        }

        public ValidationResult IsSatisfiedBy(LibraryDirectoryGetCommand command)
        {
            var commandValidity = Validate(command);

            return !commandValidity.IsValid ? commandValidity : new ValidationResult();
        }

        public static implicit operator LibraryDirectoryGetCommandValidator(LibraryDirectoryGetCommand v)
        {
            throw new NotImplementedException();
        }
    }
}