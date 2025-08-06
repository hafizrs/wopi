using FluentValidation;
using FluentValidation.Results;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.Equipment;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Selise.Ecap.SC.PraxisMonitor.Validators
{
    public class CreateShiftPlanCommandValidator : AbstractValidator<CreateShiftPlanCommand>
    {
        private readonly IRepository _repository;
        private readonly IPraxisShiftPermissionService _praxisShiftPermissionService;

        public CreateShiftPlanCommandValidator(
            IRepository repository,
            IPraxisShiftPermissionService praxisShiftPermissionService
            )
        {
            _repository = repository;
            _praxisShiftPermissionService = praxisShiftPermissionService;

            RuleLevelCascadeMode = CascadeMode.Stop;

            RuleFor(command => command.ShiftPlans)
                .NotNull().WithMessage("Shift Plans list can't be null.")
                .Must(list => list.Count > 0).WithMessage("At least one Shift Plan must be provided.");

            RuleForEach(command => command.ShiftPlans).ChildRules(plan =>
            {
                plan.RuleFor(x => x.ShiftId)
                    .Cascade(CascadeMode.StopOnFirstFailure)
                    .NotNull().WithMessage("Shift ID can't be null.")
                    .NotEmpty().WithMessage("Shift ID can't be empty.")
                    .Must((plan, ShiftId) => ShiftPlanNotExistsWithProvidedDates(plan, ShiftId))
                    .WithMessage((plan, ShiftId) => $"Shift with {getShiftName(plan, ShiftId)} and date {plan.Date.ToString("yyyy-MM-dd")} is already exists.")
                    .Must((ShiftId) => _praxisShiftPermissionService.HasShiftPlanDepartmentPermission(ShiftId))
                    .WithMessage("User does not has permission to create shift plan for selected department")
                    .When(x => x.ShiftId != null);
                plan.RuleFor(x => x.SingleShift)
                    .Cascade(CascadeMode.StopOnFirstFailure)
                    .NotNull().WithMessage("Single Shift can't be null.")
                    .Must(shift => shift != null && !string.IsNullOrEmpty(shift.ShiftName))
                    .WithMessage("Single Shift must have a valid Shift Name.")
                    .Must(shift => _praxisShiftPermissionService.HasDepartmentPermission(shift.DepartmentId))
                    .WithMessage("User does not has permission to create shift plan for selected department")
                    .When(x => x.SingleShift != null);

                plan.RuleFor(x => x.Date)
                    .NotNull().WithMessage("Shift Date can't be null.")
                    .NotEmpty().WithMessage("Shift Date can't be empty.");

                plan.RuleFor(x => x.PraxisUserIds)
                    .NotNull().WithMessage("Assigned Persons list can't be null.")
                    .Must(list => list.Count > 0).WithMessage("At least one person must be assigned.")
                    .ForEach(rule => rule
                        .NotNull().WithMessage("Assigned person can't be null.")
                        .NotEmpty().WithMessage("Assigned person can't be empty."));

                plan.RuleFor(x => x.Color)
                    .NotEmpty().WithMessage("Color can't be null or empty.");
                
                plan.RuleForEach(x => x.MaintenanceAttachments)
                    .Must(IsMaintenanceAttachmentValid)
                    .WithMessage("There are one or more issues with Maintenance Attachment")
                    .When(x => x.MaintenanceAttachments.Count > 0);
            });
        }

        private bool ShiftPlanNotExistsWithProvidedDates(ShiftPlan shiftPlan, string shiftId)
        {
            DateTime utcShiftPlanDate = DateTime.SpecifyKind(shiftPlan.Date, DateTimeKind.Utc);
            var existingShiftPlan = _repository.GetItems<RiqsShiftPlan>(s => s.Shift.ItemId == shiftId && s.ShiftDate == utcShiftPlanDate).FirstOrDefault();
            return existingShiftPlan == null;
        }

        private string getShiftName(ShiftPlan shiftPlan, string shiftId)
        {
            DateTime utcShiftPlanDate = DateTime.SpecifyKind(shiftPlan.Date, DateTimeKind.Utc);
            var existingShiftPlan = _repository.GetItems<RiqsShiftPlan>(s => s.Shift.ItemId == shiftId && s.ShiftDate == utcShiftPlanDate).FirstOrDefault();
            if (existingShiftPlan == null)
            {
                return string.Empty;
            }

            return existingShiftPlan.Shift.ShiftName;
        }

        private bool IsMaintenanceAttachmentValid(ShiftMaintenanceAttachment maintenanceAttachment)
        {
            if (string.IsNullOrEmpty(maintenanceAttachment.MaintenanceId)) return true;
            var maintenance = _repository.GetItem<PraxisEquipmentMaintenance>(m => m.ItemId == maintenanceAttachment.MaintenanceId);
            if (maintenance == null) return false;
            var originalExecitingUserIds = maintenance.ExecutivePersonIds ?? new List<string>();
            if (originalExecitingUserIds.Count() > 0)
            {
                var isValid = maintenanceAttachment.ExecutivePersonIds.Count == 0
                    || maintenanceAttachment.ExecutivePersonIds.All(userId => originalExecitingUserIds.Contains(userId));
                if (!isValid) return false;
            }
            if (maintenanceAttachment.ExecutionDate > maintenance.MaintenanceEndDate) return false;
            return true;
        }

        public ValidationResult IsSatisfiedby(CreateShiftPlanCommand command)
        {
            var commandValidity = Validate(command);

            if (!commandValidity.IsValid) return commandValidity;

            return new ValidationResult();
        }
    }
}