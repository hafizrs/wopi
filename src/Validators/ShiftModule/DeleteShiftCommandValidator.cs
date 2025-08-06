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
    public class DeleteShiftCommandValidator : AbstractValidator<DeleteShiftCommand>
    {
        private readonly IPraxisShiftPermissionService _praxisShiftPermissionService;
        public DeleteShiftCommandValidator(IPraxisShiftPermissionService praxisShiftPermissionService)
        {
            _praxisShiftPermissionService = praxisShiftPermissionService;
            RuleFor(command => command.ShiftId)
               .Cascade(CascadeMode.StopOnFirstFailure)
               .NotNull().WithMessage("Id for deleting record can't be null.")
               .NotEmpty().WithMessage("Id for deleting can't be empty.")
               .Must((ShiftId) => _praxisShiftPermissionService.HasShiftPlanDepartmentPermission(ShiftId))
               .WithMessage("User does not has permission to update shift plan for selected department");
        }

        public ValidationResult IsSatisfiedby(DeleteShiftCommand command)
        {
            var commandValidity = Validate(command);

            if (!commandValidity.IsValid) return commandValidity;

            return new ValidationResult();
        }
    }
}
