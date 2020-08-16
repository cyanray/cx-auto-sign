using Newtonsoft.Json;

namespace CxSignHelper.Models
{
    internal class TokenObject
    {
        [JsonProperty("result")]
        public bool Result { get; set; }
        [JsonProperty("_token")]
        public string Token { get; set; }
    }
}
