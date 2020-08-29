using System;
using System.Collections.Generic;
using System.Text;

namespace cx_auto_sign.Models
{
    public class AppConfig
    {
        public string Username { get; set; }
        public string Password { get; set; }
        public string Fid { get; set; } = null;
        public string Address { get; set; } = "中国";
        public string Latitude { get; set; } = "-1";
        public string Longitude { get; set; } = "-1";
        public string ClientIp { get; set; } = "1.1.1.1";
    }
}
