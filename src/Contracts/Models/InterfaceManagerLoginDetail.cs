namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Models
{
    public class InterfaceManagerLoginDetail
    {
        public string provider { get; set; }
        public string accessToken { get; set; }      
        public GoogleUserInfo user { get; set; }                
    }

    public class GoogleUserInfo
    {
        public string displayName { get; set; }  
        public string emailAddress { get; set; } 
    }
}
