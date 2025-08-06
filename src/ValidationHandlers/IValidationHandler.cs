using System.Threading.Tasks;

namespace Selise.Ecap.SC.Wopi.ValidationHandlers
{
    public interface IValidationHandler<TCommand, TResponse>
    {
        TResponse Validate(TCommand command);
        Task<TResponse> ValidateAsync(TCommand command);
    }
}
