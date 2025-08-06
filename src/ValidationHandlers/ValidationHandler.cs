using Microsoft.Extensions.DependencyInjection;
using Selise.Ecap.SC.WopiMonitor.Contracts.Infrastructure;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using System;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.WopiMonitor.ValidationHandlers
{
    public class ValidationHandler : IValidationHandler
    {
        private readonly IServiceProvider _serviceProvider;

        public ValidationHandler(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public async Task<CommandResponse> SubmitAsync<TCommand, TResponse>(TCommand command) where TCommand : class
        {
            var validatorType = typeof(IValidationHandler<,>).MakeGenericType(typeof(TCommand), typeof(TResponse));
            var validator = _serviceProvider.GetService(validatorType);

            if (validator == null)
            {
                return new CommandResponse();
            }

            var method = validatorType.GetMethod("Validate");
            var result = method.Invoke(validator, new object[] { command });

            return await Task.FromResult((CommandResponse)result);
        }
    }
}
