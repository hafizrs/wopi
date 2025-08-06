namespace Selise.Ecap.SC.Wopi.Contracts.EntityResponse
{
    public class GetPreSignedUrlForUploadResponse
    {
        public string UploadUrl { get; set; }
        public string FileId { get; set; }
        public int StatusCode { get; set; }
        public string RequestUri { get; set; }
        public int HttpStatusCode { get; set; }
    }
}
