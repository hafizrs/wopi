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
    public class DeleteAbsencePlanCommandValidator : AbstractValidator<DeleteAbsencePlanCommand>
    {

        public DeleteAbsencePlanCommandValidator(IRepository repository)
        {
            RuleFor(x => x.ItemIds)
                .NotNull()
                .WithMessage("ItemIds list cannot be null")
                .NotEmpty()
                .WithMessage("ItemIds list cannot be empty");

            RuleForEach(x => x.ItemIds)
                .NotEmpty()
                .WithMessage("Each ItemId cannot be empty");
        }

        public async Task<ValidationResult> IsSatisfiedby(DeleteAbsencePlanCommand command)
        {
            var commandValidity = await ValidateAsync(command);
            return !commandValidity.IsValid ? commandValidity : new ValidationResult();
        }
    }
}