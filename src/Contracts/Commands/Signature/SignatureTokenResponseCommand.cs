namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.Signature
{
    public class SignatureTokenResponseCommand
    {
        public string scope { get; set; }
        public string token_type { get; set; }
        public string access_token { get; set; }
        public string expires_in { get; set; }
        public string refresh_token { get; set; }
        public string ip_address { get; set; }
    }
}
