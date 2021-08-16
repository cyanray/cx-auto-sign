using System;
using System.Text;
using CxSignHelper;
using MailKit.Net.Smtp;
using MimeKit;
using Newtonsoft.Json.Linq;
using RestSharp;
using Serilog;
using Serilog.Core;
using Serilog.Events;

namespace cx_auto_sign
{
    public class Notification : ILogEventSink, IDisposable
    {
        private const string Title = "cx-auto-sign 自动签到通知";
        private readonly StringBuilder _stringBuilder = new();
        private readonly UserConfig _userConfig;
        private readonly Logger _log;

        private Notification(Logger log, UserConfig userConfig)
        {
            _userConfig = userConfig;
            _log = log;
        }

        public void Emit(LogEvent logEvent)
        {
            _stringBuilder
                .Append(logEvent.Timestamp.ToString("HH:mm:ss"))
                .Append(' ')
                .Append(logEvent.Level.ToString()[0])
                .Append(' ')
                .Append(logEvent.RenderMessage())
                .Append('\n');
            if (logEvent.Exception != null)
            {
                _stringBuilder.Append(logEvent.Exception).Append('\n');
            }
        }

        public void Dispose()
        {
            if (_stringBuilder.Length != 0)
            {
                var content = _stringBuilder.ToString();
                NotifyByEmail(content);
                NotifyByServerChan(content);
                NotifyByPushPlus(content);
            }
            GC.SuppressFinalize(this);
        }

        private static void NotifyByEmail(string subject, string text, string email, string host, int port,
            string user, string pass, bool secure = false)
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("cx-auto-sign", user));
            message.To.Add(new MailboxAddress("cx-auto-sign", email));
            message.Subject = subject;
            message.Body = new TextPart("plain")
            {
                Text = text
            };
            using var client = new SmtpClient();
            client.Connect(host, port, secure);
            client.Authenticate(user, pass);
            client.Send(message);
            client.Disconnect(true);
        }

        private void NotifyByEmail(string content)
        {
            if (string.IsNullOrEmpty(_userConfig.Email))
            {
                _log.Warning($"由于 {nameof(UserConfig.Email)} 为空，没有发送邮件通知");
                return;
            }
            if (string.IsNullOrEmpty(_userConfig.SmtpHost) ||
                string.IsNullOrEmpty(_userConfig.SmtpUsername) ||
                string.IsNullOrEmpty(_userConfig.SmtpPassword))
            {
                _log.Error("邮件配置不正确");
                return;
            }
            try
            {
                _log.Information("正在发送邮件通知");
                NotifyByEmail(Title, content,
                    _userConfig.Email, _userConfig.SmtpHost, _userConfig.SmtpPort,
                    _userConfig.SmtpUsername, _userConfig.SmtpPassword, _userConfig.SmtpSecure);
                _log.Information("已发送邮件通知");
            }
            catch (Exception e)
            {
                _log.Error(e, "发送邮件通知失败!");
            }
        }

        private static void NotifyByServerChan(string key, string title, string text = null)
        {
            var client = new RestClient($"https://sctapi.ftqq.com/{key}.send?title={title}");
            var request = new RestRequest(Method.POST);
            request.AddHeader("Content-Type", "application/x-www-form-urlencoded");
            if (!string.IsNullOrEmpty(text))
            {
                request.AddParameter("desp", "```text\n" + text + "\n```");
            }
            var response = client.Execute(request);
            CxSignClient.TestResponseCode(response);
            var json = JObject.Parse(response.Content);
            if(json["data"]!["errno"]!.Value<int>() != 0)
            {
                throw new Exception(json["data"]!["error"]!.ToString());
            }
        }

        private void NotifyByServerChan(string content)
        {
            if (string.IsNullOrEmpty(_userConfig.ServerChanKey))
            {
                _log.Warning($"由于 {nameof(UserConfig.ServerChanKey)} 为空，没有发送 ServerChan 通知");
                return;
            }
            try
            {
                _log.Information("正在发送 ServerChan 通知");
                NotifyByServerChan(_userConfig.ServerChanKey, Title, content);
                _log.Information("已发送 ServerChan 通知");
            }
            catch (Exception e)
            {
                _log.Error(e, "发送 ServerChan 通知失败!");
            }
        }

        private static void NotifyByPushPlus(string token, string title, string text)
        {
            var client = new RestClient("https://www.pushplus.plus/send");
            var request = new RestRequest(Method.POST);
            request.AddJsonBody(new JObject
            {
                ["token"] = token,
                ["title"] = title,
                ["content"] = text,
                ["template"] = "txt"
            }.ToString());
            var response = client.Execute(request);
            CxSignClient.TestResponseCode(response);
            var json = JObject.Parse(response.Content);
            if(json["code"]?.Value<int>() != 200)
            {
                throw new Exception(json["msg"]?.ToString());
            }
        }

        private void NotifyByPushPlus(string content)
        {
            if (string.IsNullOrEmpty(_userConfig.PushPlusToken))
            {
                _log.Warning($"由于 {nameof(UserConfig.PushPlusToken)} 为空，没有发送 PushPlus 通知");
                return;
            }
            try
            {
                _log.Information("正在发送 PushPlus 通知");
                NotifyByPushPlus(_userConfig.PushPlusToken, Title, content);
                _log.Information("已发送 PushPlus 通知");
            }
            catch (Exception e)
            {
                _log.Error(e, "发送 PushPlus 通知失败!");
            }
        }

        public static Logger CreateLogger(UserConfig userConfig, double startTime)
        {
            var console = new LoggerConfiguration()
                .Enrich.WithProperty("StartTime", startTime)
                .WriteTo.Console(outputTemplate:
                    "[{Timestamp:HH:mm:ss} {Level:u3}] [{StartTime}] {Message:lj}{NewLine}{Exception}")
                .CreateLogger();
            return new LoggerConfiguration()
                .WriteTo.Sink(new Notification(console, userConfig))
                .WriteTo.Logger(console)
                .CreateLogger();
        }
    }
}