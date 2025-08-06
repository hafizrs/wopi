using FluentValidation;
using FluentValidation.Results;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Commands;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Selise.Ecap.SC.PraxisMonitor.Validators
{
    public class EditShiftCommandValidator : AbstractValidator<EditShiftCommand>
    {
        private readonly IPraxisShiftPermissionService _praxisShiftPermissionService;
        public EditShiftCommandValidator(IPraxisShiftPermissionService praxisShiftPermissionService)
        {
            _praxisShiftPermissionService = praxisShiftPermissionService;
            RuleFor(command => command.ItemId)
               .Cascade(CascadeMode.StopOnFirstFailure)
               .NotNull().WithMessage("Id for edit record can't be null.")
               .NotEmpty().WithMessage("Id for edit can't be empty.")
               .Must((ShiftId) => _praxisShiftPermissionService.HasShiftPlanDepartmentPermission(ShiftId))
               .WithMessage("User does not has permission to update shift plan for selected department");
            RuleFor(command => command.ShiftName)
               .Cascade(CascadeMode.StopOnFirstFailure)
               .NotNull().WithMessage("Shift name  record can't be null.");
           


        }

        public ValidationResult IsSatisfiedby(EditShiftCommand command)
        {
            var commandValidity = Validate(command);

            if (!commandValidity.IsValid) return commandValidity;

            return new ValidationResult();
        }
    }
}
