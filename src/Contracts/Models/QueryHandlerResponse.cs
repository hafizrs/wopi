namespace Selise.Ecap.SC.Wopi.Contracts.Models
{
    public class QueryHandlerResponse
    {
        public int StatusCode { get; set; }
        public string ErrorMessage { get; set; }
        public object Data { get; set; }
        public object Results { get; set; }
        public long TotalCount { get; set; }
    }
}
