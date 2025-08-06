using System;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.ValidationHandlers
{
    public class ValidationHandler
    {
        private readonly IServiceProvider _serviceProvider;
        public ValidationHandler(IServiceProvider serviceProvider)
        {
            this._serviceProvider = serviceProvider;
        }

        public async Task<TResponse> SubmitAsync<TCommand, TResponse>(TCommand command)
        {
            var validationHandler = _serviceProvider.GetService(typeof(IValidationHandler<TCommand, TResponse>)) as IValidationHandler<TCommand, TResponse>;

            return await validationHandler?.ValidateAsync(command);
        }
    }
}
