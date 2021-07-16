using System;
using System.Collections.Generic;
using System.Text;

namespace CxSignHelper.Models
{
    public class SignOptions
    {
        public string Address { get; init; } = "中国";
        public string Latitude { get; init; } = "-1";
        public string Longitude { get; init; } = "-1";
        public string ClientIp { get; init; } = "1.1.1.1";
        public string ImageId { get; set; }
    }
}
