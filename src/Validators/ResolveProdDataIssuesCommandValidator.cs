using FluentValidation;
using FluentValidation.Results;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands;
using System;

namespace Selise.Ecap.SC.PraxisMonitor.Validators
{
    public class ResolveProdDataIssuesCommandValidator : AbstractValidator<ResolveProdDataIssuesCommand>
    {
        public ResolveProdDataIssuesCommandValidator()
        {
        }

        public ValidationResult IsSatisfiedBy(ResolveProdDataIssuesCommand command)
        {
            var commandValidity = Validate(command);

            return !commandValidity.IsValid ? commandValidity : new ValidationResult();
        }

        public static implicit operator ResolveProdDataIssuesCommandValidator(ResolveProdDataIssuesCommand v)
        {
            throw new NotImplementedException();
        }
    }
}