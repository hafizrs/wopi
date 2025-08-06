namespace Selise.Ecap.SC.Wopi.Contracts.Models
{
    public class PreSignedUrlForUploadQueryModel
    {
        public string ItemId { get; set; }
        public string Name { get; set; }
        public string Tags { get; set; }
        public string MetaData { get; set; }
        public string ParentDirectoryId { get; set; }
    }
}