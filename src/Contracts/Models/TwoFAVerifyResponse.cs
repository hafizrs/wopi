namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Models
{
    public class TwoFAVerifyResponse
    {
        public string UserId { get; set; }
        public string TwoFactorId { get; set; }
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }

        public static TwoFAVerifyResponse CreateSuccess(string twoFactorId)
        {
            return new TwoFAVerifyResponse
            {
              
                TwoFactorId = twoFactorId,
                Success = true
            };
        }

        public static TwoFAVerifyResponse CreateError(string errorMessage)
        {
            return new TwoFAVerifyResponse
            {
                ErrorMessage = errorMessage
            };
        }
    }
}
