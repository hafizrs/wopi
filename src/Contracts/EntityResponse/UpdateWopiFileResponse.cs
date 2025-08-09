namespace Selise.Ecap.SC.Wopi.Contracts.EntityResponse
{
    public class UpdateWopiFileResponse
    {
        public string LastModifiedTime { get; set; }
        public string Name { get; set; }
        public long Size { get; set; }
        public string Version { get; set; }
        public string UploadResult { get; set; }
    }
}
