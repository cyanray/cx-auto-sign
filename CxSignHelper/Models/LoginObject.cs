using Newtonsoft.Json;

namespace CxSignHelper.Models
{
    internal class LoginObject
    {
        [JsonProperty("status")]
        public bool Status { get; set; }
    }

}
