using FluentValidation;
using FluentValidation.Results;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Selise.Ecap.SC.PraxisMonitor.Validators
{
    public class TasksUpdateCommandValidator : AbstractValidator<TasksUpdateCommand>
    {
        public TasksUpdateCommandValidator()
        {
            RuleFor(command => command.TaskScheduleIds)
               .Cascade(CascadeMode.StopOnFirstFailure)
               .NotNull().WithMessage("TaskScheduleIds can't be null.")
               .NotEmpty().WithMessage("TaskScheduleIds can't be empty.")
               .Must(IsValidTaskScheduleIds).WithMessage("TaskScheduleIds are not valid");
        }

        private bool IsValidTaskScheduleIds(string[] taskScheduleIds)
        {
            var filteredtaskScheduleIds = taskScheduleIds.Where(id => !string.IsNullOrWhiteSpace(id)).Distinct().ToList();
            return filteredtaskScheduleIds.Count == taskScheduleIds.Length;
        }

        public ValidationResult IsSatisfiedBy(TasksUpdateCommand command)
        {
            var commandValidity = Validate(command);

            return !commandValidity.IsValid ? commandValidity : new ValidationResult();
        }

        public static implicit operator TasksUpdateCommandValidator(TasksUpdateCommand v)
        {
            throw new NotImplementedException();
        }
    }
}