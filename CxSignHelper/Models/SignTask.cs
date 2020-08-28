using Newtonsoft.Json;

namespace CxSignHelper.Models
{
    public class SignTask
    {
        [JsonProperty("nameOne")]
        public string Name { get; set; }

        [JsonProperty("id")]
        public long Id { get; set; }

        [JsonProperty("startTime")]
        public long StartTime { get; set; }

        /// <summary>
        /// 任务类型，签到任务时该值为 2
        /// </summary>
        [JsonProperty("type")]
        internal int Type { get; set; }

    }
}
