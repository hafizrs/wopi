using FluentValidation;
using FluentValidation.Results;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.QuickTaskModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.QuickTaskModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models.QuickTaskModule;
using System.Linq;

namespace Selise.Ecap.SC.PraxisMonitor.Validators
{
    public class CreateQuickTaskCommandValidator : AbstractValidator<CreateQuickTaskCommand>
    {
        private readonly IRepository _repository;
        private readonly IQuickTaskPermissionService _quickTaskPermissionService;

        public CreateQuickTaskCommandValidator(IRepository repository, IQuickTaskPermissionService quickTaskPermissionService)
        {
            _repository = repository;
            _quickTaskPermissionService = quickTaskPermissionService;

            RuleFor(command => command.TaskGroupName)
                .Cascade(CascadeMode.StopOnFirstFailure)
                .NotNull().WithMessage("Task Group Name can't be null.")
                .NotEmpty().WithMessage("Task Group Name can't be empty.")
                .Must((command, taskGroupName) => ContainsUniqueQuickTaskByDepartment(command, taskGroupName))
                .WithMessage("Same quick task already exists with the same name and department id");

            RuleFor(command => command.DepartmentId)
                .Cascade(CascadeMode.StopOnFirstFailure)
                .NotNull().WithMessage("Department Id can't be null.")
                .NotEmpty().WithMessage("Department Id can't be empty.")
                .Must((departmentId) => _quickTaskPermissionService.HasDepartmentPermission(departmentId))
                .WithMessage("User does not have permission to create quick task for selected department");
            RuleFor(command => command.TaskList)
                .Cascade(CascadeMode.StopOnFirstFailure)
                .NotNull().WithMessage("TaskList can't be null.")
                .NotEmpty().WithMessage("TaskList can't be empty.");
        }

        private bool ContainsUniqueQuickTaskByDepartment(CreateQuickTaskCommand command, string taskGroupName)
        {
            var exists = _repository.GetItems<RiqsQuickTask>()
                .Any(s => s.TaskGroupName == taskGroupName && s.DepartmentId == command.DepartmentId);
            return !exists;
        }

        public ValidationResult IsSatisfiedby(CreateQuickTaskCommand command)
        {
            var commandValidity = Validate(command);
            if (!commandValidity.IsValid) return commandValidity;
            return new ValidationResult();
        }
    }
} 