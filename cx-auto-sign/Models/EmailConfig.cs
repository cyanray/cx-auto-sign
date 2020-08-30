using System;
using System.Collections.Generic;
using System.Text;

namespace cx_auto_sign.Models
{
    public class EmailConfig
    {
        public string SmtpHost { get; set; }
        public int SmtpPort { get; set; }
        public string SmtpUsername { get; set; }
        public string SmtpPassword { get; set; }
        public string Email { get; set; }

        public bool IsValid()
        {
            return !string.IsNullOrEmpty(SmtpHost) &&
                   !string.IsNullOrEmpty(SmtpUsername) &&
                   !string.IsNullOrEmpty(SmtpPassword) &&
                   !string.IsNullOrEmpty(Email);
        }

    }
}
