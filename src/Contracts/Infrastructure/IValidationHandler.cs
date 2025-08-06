using SeliseBlocks.Genesis.Framework.Infrastructure;

namespace Selise.Ecap.SC.WopiMonitor.Contracts.Infrastructure
{
    public interface IValidationHandler
    {
        Task<CommandResponse> SubmitAsync<TCommand, TResponse>(TCommand command) where TCommand : class;
    }
} 