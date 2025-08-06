namespace EventHandlers.Models
{
    public class ImageConversionSetting
    {
        public string SourceFileId { get; set; }
        public Dimension[] Dimensions { get; set; }
        public ParentEntity[] ParentEntities { get; set; }
        public bool KeepCanvasSameWithImage { get; set; }
        public bool UseJpegEncoding { get; set; }
    }
}
