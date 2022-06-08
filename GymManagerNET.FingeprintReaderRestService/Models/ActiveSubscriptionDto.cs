

using Newtonsoft.Json;

namespace GymManagerNET.Core.Models.DTOs.Subscriptions
{
    public class ActiveSubscriptionDto : SubscriptionDto
    {
        [JsonProperty("IsActive")]
        public bool IsActive { get; set; }
    }
 
}
