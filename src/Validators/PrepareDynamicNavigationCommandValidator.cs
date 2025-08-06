using FluentValidation;
using Selise.Ecap.SC.PraxisMonitor.Commands;
using FluentValidation.Results;
using System;

namespace Selise.Ecap.SC.PraxisMonitor.Validators
{
    public class PrepareDynamicNavigationCommandValidator : AbstractValidator<PrepareDynamicNavigationCommand>
    {

        public PrepareDynamicNavigationCommandValidator()
        {

            RuleFor(command => command.OrganizationId)
                .Cascade(CascadeMode.StopOnFirstFailure)
                .NotNull().WithMessage("OrganizationId can't be null.")
                .NotEmpty().WithMessage("OrganizationId can't be empty.")
                .Must(IsValidGuid).WithMessage("OrganizationId is not valid Guid.");

            RuleFor(command => command.Type)
                .Cascade(CascadeMode.StopOnFirstFailure)
                .NotNull().WithMessage("Type can't be null.")
                .NotEmpty().WithMessage("Type can't be empty.");

            RuleFor(command => command.NavigationList)
                .Cascade(CascadeMode.StopOnFirstFailure)
                .NotNull().WithMessage("NavigationList can't be null.")
                .NotEmpty().WithMessage("NavigationList can't be empty.");

            RuleFor(command => command.ActionName)
                .Cascade(CascadeMode.StopOnFirstFailure)
                .NotNull().WithMessage("ActionName can't be null.")
                .NotEmpty().WithMessage("ActionName can't be empty.");

            RuleFor(command => command.Context)
                .Cascade(CascadeMode.StopOnFirstFailure)
                .NotNull().WithMessage("Context can't be null.")
                .NotEmpty().WithMessage("Context can't be empty.");
        }

        public ValidationResult IsSatisfiedby(PrepareDynamicNavigationCommand command)
        {
            var commandValidity = Validate(command);

            if (!commandValidity.IsValid) return commandValidity;

            return new ValidationResult();
        }

        private bool IsValidGuid(string itemId)
        {
            bool isValidGuid = Guid.TryParse(itemId, out _);

            if (!isValidGuid)
            {
                return false;
            }

            return true;
        }
    }
}
