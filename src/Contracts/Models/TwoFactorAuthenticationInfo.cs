using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Models
{
    public class TwoFactorAuthenticationInfo
    {
        public string UserId { get; set; }
        public string TwoFactorId { get; set; }
        public string TwoFactorCode { get; set; }
        public string Email { get; set; }

        public string Serilize()
        {
            return JsonConvert.SerializeObject(this);
        }

        public static TwoFactorAuthenticationInfo Deserialize(string json)
        {
            return JsonConvert.DeserializeObject<TwoFactorAuthenticationInfo>(json);
        }
    }


}
