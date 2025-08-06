namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Models
{
    public class PersonInfo
    {
        public string FileName { get; set; }
        public string FileId { get; set; }
        public byte[] FileContent { get; set; }
        public bool IsGroupAdmin { get; set; } = false;
        public PersonInformation PersonalInformation { get; set; }
    }
}