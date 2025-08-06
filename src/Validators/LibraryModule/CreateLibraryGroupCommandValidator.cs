using FluentValidation;
using FluentValidation.Results;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands;
using System;
using System.Collections.Generic;
using System.Text;

namespace Selise.Ecap.SC.PraxisMonitor.Validators
{
    public class CreateLibraryGroupCommandValidator : AbstractValidator<CreateLibraryGroupCommand>
    {
        public CreateLibraryGroupCommandValidator()
        {
            RuleFor(command => command.OrganizationId)
                .Cascade(CascadeMode.StopOnFirstFailure)
                .Must(BeAValidGuid)
                .WithMessage("{PropertyName} must be a valid GUID.");

            RuleFor(command => command.GroupName)
                .Cascade(CascadeMode.StopOnFirstFailure)
                .NotEmpty();

            RuleFor(command => command)
            .Cascade(CascadeMode.StopOnFirstFailure)
            .Must(BeValidGroupNameAndSubGroupNames)
            .WithMessage($"The SubSubGroupName is not valid with empty SubGroup name");
        }

        public ValidationResult IsSatisfiedBy(CreateLibraryGroupCommand command)
        {
            var commandValidity = Validate(command);

            return !commandValidity.IsValid ? commandValidity : new ValidationResult();
        }

        public static bool BeAValidGuid(string guid)
        {
            return Guid.TryParse(guid, out _);
        }

        private bool BeValidGroupNameAndSubGroupNames(CreateLibraryGroupCommand command)
        {
            if (string.IsNullOrWhiteSpace(command.SubGroupName) && !string.IsNullOrWhiteSpace(command.SubSubGroupName))
            {
                return false;
            }

            return true;
        }
    }
}
