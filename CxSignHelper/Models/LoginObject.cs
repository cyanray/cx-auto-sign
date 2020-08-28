using Newtonsoft.Json;
using System.ComponentModel;

namespace CxSignHelper.Models
{
    internal class LoginObject
    {
        public string Message { get; set; }

        [JsonProperty("status")]
        public bool Status { get; set; }

        [JsonProperty("mes")]
        private string _message1 
        {
            get => Message;
            set => Message = value;
        }

        [JsonProperty("msg")]
        private string _message2
        {
            get => Message;
            set => Message = value;
        }

    }

}
