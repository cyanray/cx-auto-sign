using Newtonsoft.Json;

namespace CxSignHelper.Models
{
    internal class LoginObject
    {
        [JsonProperty("mes")]
        public string Messagee { get; set; }

        [JsonProperty("status")]
        public bool Status { get; set; }
    }

}
