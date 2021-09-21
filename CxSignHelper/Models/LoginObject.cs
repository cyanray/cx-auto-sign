using Newtonsoft.Json;

namespace CxSignHelper.Models
{
    internal class LoginObject
    {
        [JsonProperty("mes")]
        public string Message { get; set; }

        [JsonProperty("status")]
        public bool Status { get; set; }
    }

}
