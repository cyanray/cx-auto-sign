namespace CxSignHelper.Models
{
    public class SignOptions
    {
        public string Address { get; set; } = "中国";
        public string Latitude { get; set; } = "-1";
        public string Longitude { get; set; } = "-1";
        public string ClientIp { get; init; } = "1.1.1.1";
        public string ImageId { get; set; }
    }
}
