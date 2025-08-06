using System.Threading.Tasks;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands;
using Selise.Ecap.SC.PraxisMonitor.Validators;

namespace Selise.Ecap.SC.PraxisMonitor.ValidationHandlers
{
    public class TasksUpdateCommandValidationHandler : IValidationHandler<TasksUpdateCommand, CommandResponse>
    {
        private readonly TasksUpdateCommandValidator _validator;

        public TasksUpdateCommandValidationHandler(TasksUpdateCommandValidator validator)
        {
            _validator = validator;
        }

        public CommandResponse Validate(TasksUpdateCommand command)
        {
            throw new System.NotImplementedException();
        }

        public Task<CommandResponse> ValidateAsync(TasksUpdateCommand command)
        {
            var validationResult = _validator.IsSatisfiedBy(command);

            return Task.FromResult(validationResult.IsValid ? new CommandResponse() : new CommandResponse(validationResult));
        }
    }
}