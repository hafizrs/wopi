namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.Signature
{
    public class SignatureTokenRequestCommand
    {
        public string grant_type { get; set; }
        public string client_id { get; set; }
        public string client_secret { get; set; }
    }
}
