using System;
using System.Text;
using CxSignHelper;
using MailKit.Net.Smtp;
using MimeKit;
using Newtonsoft.Json.Linq;
using RestSharp;
using Serilog;
using Serilog.Configuration;
using Serilog.Core;
using Serilog.Events;

namespace cx_auto_sign
{
    public class Notification : ILogEventSink, IDisposable
    {
        private const string Title = "cx-auto-sign 自动签到通知";
        private readonly StringBuilder _stringBuilder = new();
        private readonly UserConfig _userConfig;

        public Notification(UserConfig userConfig)
        {
            _userConfig = userConfig;
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
                var str = _stringBuilder.ToString();
                NotifyByEmail(Title, str, _userConfig.Email, _userConfig.SmtpHost, _userConfig.SmtpPort,
                    _userConfig.SmtpUsername, _userConfig.SmtpPassword, _userConfig.SmtpSecure);
                NotifyByServerChan(_userConfig.ServerChanKey, Title, str);
                NotifyByPushPlus(_userConfig.PushPlusToken, Title, str);
                _stringBuilder.Clear();
            }
            GC.SuppressFinalize(this);
        }

        private static void NotifyByEmail(string subject, string text, string email, string host, int port,
            string user, string pass, bool secure = false)
        {
            
            if (string.IsNullOrEmpty(email))
            {
                Log.Warning("由于 Email 为空，没有发送邮件通知");
                return;
            }
            try
            {
                Log.Information("正在发送邮件通知");
                if (string.IsNullOrEmpty(host) ||
                    string.IsNullOrEmpty(user) ||
                    string.IsNullOrEmpty(pass))
                {
                    throw new Exception("邮件配置不正确");
                }
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
                Log.Information("已发送邮件通知");
            }
            catch (Exception e)
            {
                Log.Error(e, "发送邮件通知失败!");
            }
        }

        private static void NotifyByServerChan(string key, string title, string text = null)
        {
            if (string.IsNullOrEmpty(key))
            {
                Log.Warning("由于 Key 为空，没有发送 ServerChan 通知");
                return;
            }
            try
            {
                Log.Information("正在发送 ServerChan 通知");
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
                Log.Information("已发送 ServerChan 通知");
            }
            catch (Exception e)
            {
                Log.Error(e, "发送 ServerChan 通知失败!");
            }
        }

        private static void NotifyByPushPlus(string token, string title, string text)
        {
            if (string.IsNullOrEmpty(token))
            {
                Log.Warning("由于 Token 为空，没有发送 PushPlus 通知");
                return;
            }
            try
            {
                Log.Information("正在发送 PushPlus 通知");
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
                Log.Information("已发送 PushPlus 通知");
            }
            catch (Exception e)
            {
                Log.Error(e, "发送 PushPlus 通知失败!");
            }
        }
    }

    public static class NotificationSinkExtend
    {
        public static LoggerConfiguration Notification(
            this LoggerSinkConfiguration loggerConfiguration,
            UserConfig userConfig)
        {
            return loggerConfiguration.Sink(new Notification(userConfig));
        }
    }
}