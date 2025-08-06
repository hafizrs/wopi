using FluentValidation;
using FluentValidation.Results;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.QuickTaskModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.QuickTaskModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models.QuickTaskModule;
using System;
using System.Linq;

namespace Selise.Ecap.SC.PraxisMonitor.Validators
{
    public class CreateQuickTaskPlanCommandValidator : AbstractValidator<CreateQuickTaskPlanCommand>
    {
        private readonly IRepository _repository;
        private readonly IQuickTaskPermissionService _quickTaskPermissionService;

        public CreateQuickTaskPlanCommandValidator(
            IRepository repository,
            IQuickTaskPermissionService quickTaskPermissionService
            )
        {
            _repository = repository;
            _quickTaskPermissionService = quickTaskPermissionService;

            RuleFor(command => command.QuickTaskPlans)
                .NotNull().WithMessage("QuickTaskPlans list can't be null.")
                .Must(list => list.Count > 0).WithMessage("At least one QuickTaskPlan must be provided.");

            RuleForEach(command => command.QuickTaskPlans).ChildRules(plan =>
            {
                plan.RuleFor(x => x.QuickTaskId)
                    .Cascade(CascadeMode.StopOnFirstFailure)
                    .NotNull().WithMessage("QuickTask ID can't be null.")
                    .NotEmpty().WithMessage("QuickTask ID can't be empty.")
                    .Must((plan, quickTaskId) => QuickTaskPlanNotExistsWithProvidedDates(plan, quickTaskId))
                    .WithMessage((plan, quickTaskId) => $"QuickTask with id {quickTaskId} and date {plan.QuickTaskDate.ToString("yyyy-MM-dd")} already exists.")
                    .Must((quickTaskId) => _quickTaskPermissionService.HasQuickTaskPlanDepartmentPermission(quickTaskId))
                    .WithMessage("User does not have permission to create quick task plan for selected department");

                plan.RuleFor(x => x.QuickTaskDate)
                    .NotNull().WithMessage("QuickTask Date can't be null.")
                    .NotEmpty().WithMessage("QuickTask Date can't be empty.");

                plan.RuleFor(x => x.AssignedUsers)
                    .NotNull().WithMessage("Assigned Users list can't be null.")
                    .Must(list => list.Count > 0).WithMessage("At least one user must be assigned.")
                    .ForEach(rule => rule
                        .NotNull().WithMessage("Assigned user can't be null.")
                        .NotEmpty().WithMessage("Assigned user can't be empty."));
            });
        }

        private bool QuickTaskPlanNotExistsWithProvidedDates(dynamic quickTaskPlan, string quickTaskId)
        {
            DateTime utcQuickTaskPlanDate = DateTime.SpecifyKind(quickTaskPlan.QuickTaskDate, DateTimeKind.Utc);
            var existingQuickTaskPlan = _repository.GetItems<RiqsQuickTaskPlan>(s => s.QuickTaskShift.ItemId == quickTaskId && s.QuickTaskDate == utcQuickTaskPlanDate).FirstOrDefault();
            return existingQuickTaskPlan == null;
        }

        public ValidationResult IsSatisfiedby(CreateQuickTaskPlanCommand command)
        {
            var commandValidity = Validate(command);
            if (!commandValidity.IsValid) return commandValidity;
            return new ValidationResult();
        }
    }
} 