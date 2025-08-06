using FluentValidation;
using FluentValidation.Results;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Commands;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Selise.Ecap.SC.PraxisMonitor.Validators
{
    public class CreateShiftCommandValidator : AbstractValidator<CreateShiftCommand>
    {
        private readonly IRepository _repository;
        private readonly IPraxisShiftPermissionService _praxisShiftPermissionService;

        public CreateShiftCommandValidator(IRepository repository, IPraxisShiftPermissionService praxisShiftPermissionService)
        {
            _repository = repository;
            _praxisShiftPermissionService = praxisShiftPermissionService;

            RuleFor(command => command.ShiftName)
                .Cascade(CascadeMode.StopOnFirstFailure)
                .NotNull().WithMessage("Shift Name can't be null.")
                .NotEmpty().WithMessage("Shift Name can't be empty.")
                .Must((command, shiftName) => ContainsUniqueShiftByDepartment(command, shiftName))
                .WithMessage("Same shift already exists with the same name and department id");

            RuleFor(command => command.DepartmentId)
                .Cascade(CascadeMode.StopOnFirstFailure)
                .NotNull().WithMessage("Department Id can't be null.")
                .NotEmpty().WithMessage("Department Id can't be empty.")
                .Must((DepartmentId) => _praxisShiftPermissionService.HasDepartmentPermission(DepartmentId))
                .WithMessage("User does not has permission to create shift for selected department");
        }

        private bool ContainsUniqueShiftByDepartment(CreateShiftCommand command, string shiftName)
        {
            var exists = _repository.GetItems<RiqsShift>()
                            .Any(s => s.ShiftName == shiftName && s.DepartmentId == command.DepartmentId);
            return !exists;
        }

        public ValidationResult IsSatisfiedby(CreateShiftCommand command)
        {
            var commandValidity = Validate(command);

            if (!commandValidity.IsValid) return commandValidity;

            return new ValidationResult();
        }
    }
}
