using FluentValidation;
using FluentValidation.Results;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.AbsenceModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models.AbsenceModule;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.Validators.AbsenceModule
{
    public class DeleteAbsenceTypeCommandValidator : AbstractValidator<DeleteAbsenceTypeCommand>
    {
        private readonly IRepository _repository;

        public DeleteAbsenceTypeCommandValidator(IRepository repository)
        {
            _repository = repository;

            RuleFor(x => x.ItemIds)
                .NotNull()
                .WithMessage("ItemIds list cannot be null")
                .NotEmpty()
                .WithMessage("ItemIds list cannot be empty");

            RuleForEach(x => x.ItemIds)
                .NotEmpty()
                .WithMessage("Each ItemId cannot be empty");

            RuleFor(x => x)
                .MustAsync(AllNotUsedInPlansAsync)
                .WithMessage((command, context) => $"One or more absence types are used in absence plans and cannot be deleted");
        }

        private Task<bool> AllNotUsedInPlansAsync(DeleteAbsenceTypeCommand command, CancellationToken ct)
        {
            if (command.ItemIds == null || !command.ItemIds.Any())
                return Task.FromResult(false);

            var usedCount = _repository.GetItems<RiqsAbsencePlan>(ap => 
                command.ItemIds.Contains(ap.AbsenceTypeInfo.Id) && !ap.IsMarkedToDelete);

            return Task.FromResult(usedCount?.Any() != true);
        }

        public async Task<ValidationResult> IsSatisfiedby(DeleteAbsenceTypeCommand command)
        {
            var commandValidity = await ValidateAsync(command);
            return !commandValidity.IsValid ? commandValidity : new ValidationResult();
        }
    }
}