using cx_auto_sign.Models;
using MailKit.Net.Smtp;
using MimeKit;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace cx_auto_sign
{
    public static class Email
    {
        const string EmailConfigPath = "EmailConfig.json";

        private static EmailConfig _emailConfig = null;

        public static EmailConfig EmailConfig
        {
            get
            {
                if (_emailConfig is null) LoadEmailConfig();
                return _emailConfig;
            }
            set
            {
                _emailConfig = value;
                SaveEmailConfig();
            }
        }

        public static void LoadEmailConfig()
        {
            if (!File.Exists(EmailConfigPath))
            {
                _emailConfig = new EmailConfig();
                SaveEmailConfig();
                return;
            }
            var text = File.ReadAllText(EmailConfigPath);
            _emailConfig = JsonConvert.DeserializeObject<EmailConfig>(text);
        }

        public static void SaveEmailConfig()
        {
            File.WriteAllText(EmailConfigPath, JsonConvert.SerializeObject(_emailConfig));
        }

        public static void SendPlainText(string subject, string text)
        {
            if (!EmailConfig.IsValid()) 
                throw new Exception("邮件配置不正确");
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("cx-auto-sign", EmailConfig.SmtpUsername));
            message.To.Add(new MailboxAddress("cx-auto-sign", EmailConfig.Email));
            message.Subject = subject;

            message.Body = new TextPart("plain")
            {
                Text = text
            };

            using var client = new SmtpClient();
            client.Connect(EmailConfig.SmtpHost, EmailConfig.SmtpPort, true);
            client.Authenticate(EmailConfig.SmtpUsername, EmailConfig.SmtpPassword);
            client.Send(message);
            client.Disconnect(true);
        }
    }
}
