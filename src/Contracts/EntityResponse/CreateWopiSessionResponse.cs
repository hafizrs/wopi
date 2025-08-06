namespace Selise.Ecap.SC.Wopi.Contracts.EntityResponse
{
    public class CreateWopiSessionResponse
    {
        public string SessionId { get; set; }
        public string EditUrl { get; set; }
        public string WopiSrc { get; set; }
        public string AccessToken { get; set; }
        public string Message { get; set; }
    }
} 