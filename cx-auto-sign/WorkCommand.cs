using CxSignHelper;
using CxSignHelper.Models;
using McMaster.Extensions.CommandLineUtils;
using Newtonsoft.Json.Linq;
using Serilog;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Serilog.Core;
using Websocket.Client;

namespace cx_auto_sign
{
    [Command(Description = "工作模式, 监听签到任务并自动签到")]
    public class WorkCommand : CommandBase
    {
        // ReSharper disable UnassignedGetOnlyAutoProperty
        [Option("-u", Description = "指定用户名（学号）")]
        private string Username { get; }
        // ReSharper restore UnassignedGetOnlyAutoProperty

        private WebsocketClient _ws;

        protected override async Task<int> OnExecuteAsync(CommandLineApplication app)
        {
            var appConfig = new AppDataConfig();
            var user = Username ?? appConfig.DefaultUsername;
            if (user == null)
            {
                Log.Error("没有设置用户，可以使用 -u 指定用户");
                return 1;
            }

            var userConfig = new UserDataConfig(user);
            var auConfig = new UserConfig(appConfig, userConfig);
            var username = userConfig.Username;
            var password = userConfig.Password;
            var fid = userConfig.Fid;

            Log.Information("登录账号 {Username} 中...", username);
            CxSignClient client;
            if (string.IsNullOrEmpty(fid))
            {
                client = await CxSignClient.LoginAsync(username, password);
            }
            else
            {
                client = await CxSignClient.LoginAsync(username, password, fid);
            }
            Log.Information("登录账号 {Username} 成功", username);
            var (imToken, uid) = await client.GetImTokenAsync();

            var enableWeiApi = false;
            var webApi = userConfig.WebApi;
            if (webApi != null)
            {
                string rule = null;
                if (webApi.Type == JTokenType.Boolean)
                {
                    if (webApi.Value<bool>())
                    {
                        rule = "http://localhost:5743";
                    }
                }
                else if (webApi.Type == JTokenType.String)
                {
                    rule = webApi.Value<string>();
                }

                if (rule != null)
                {
                    // 启动 WebApi 服务
                    enableWeiApi = true;
                    Log.Information("启动 WebApi 服务");
                    WebApi.Startup.Rule = rule;
                    WebApi.IntervalData.Status = new WebApi.Status
                    {
                        Username = username,
                        CxAutoSignEnabled = true
                    };
                    _ = Task.Run(() => { WebApi.Program.Main(null); });
                }
            }

            // 创建 Websocket 对象，监听消息
            var exitEvent = new ManualResetEvent(false);
            var url = new Uri("wss://im-api-vip6-v2.easemob.com/ws/032/xvrhfd2j/websocket");
            using (_ws = new WebsocketClient(url, () => new ClientWebSocket
            {
                Options =
                {
                    KeepAliveInterval = TimeSpan.FromMilliseconds(-1)
                }
            }))
            {
                _ws.ReconnectionHappened.Subscribe(info =>
                    Log.Warning("CXIM: Reconnection happened, type: {Type}", info.Type));

                _ws.MessageReceived.Subscribe(async msg =>
                {
                    try
                    {
                        Log.Information($"CXIM: Message received: {msg}");
                        if (msg.Text.StartsWith("o"))
                        {
                            Log.Information("Websocket 登录");
                            var loginPackage = Cxim.BuildLoginPackage(uid, imToken);
                            Log.Information($"CXIM: Message send: {loginPackage}");
                            _ws.Send(loginPackage);
                            return;
                        }

                        if (!msg.Text.StartsWith("a")) return;
                        var array = JArray.Parse(msg.Text[1..]);
                        foreach (var token in array)
                        {
                            Logger log = null;
                            try
                            {
                                var str = token.Value<string>();
                                var pkgBytes = Convert.FromBase64String(str);
                                if (pkgBytes.Length <= 5)
                                {
                                    continue;
                                }
                                var bytes = new byte[5];
                                Array.Copy(pkgBytes, bytes, 5);
                                if (bytes.SequenceEqual(new byte[] {0x08, 0x00, 0x40, 0x02, 0x4a}))
                                {
                                    Log.Information("接收到课程消息并请求获取活动信息");
                                    bytes = (byte[]) pkgBytes.Clone();
                                    bytes[3] = 0x00;
                                    bytes[6] = 0x1a;
                                    bytes = bytes.Concat(new byte[] {0x58, 0x00}).ToArray();
                                    var message = Cxim.Pack(bytes);
                                    Log.Information($"CXIM: Message send: {message}");
                                    _ws.Send(message);
                                    continue;
                                }

                                if (!bytes.SequenceEqual(new byte[] {0x08, 0x00, 0x40, 0x00, 0x4a}))
                                {
                                    continue;
                                }

                                Log.Information("接收到课动活动消息");

                                log = new LoggerConfiguration()
                                    .WriteTo.Notification(auConfig)
                                    .WriteTo.Console()
                                    .CreateLogger();

                                var chatId = Cxim.GetChatId(pkgBytes);
                                if (chatId == null)
                                {
                                    Log.Error("解析失败，无法获取 ChatId");
                                    log = null;
                                    continue;
                                }
                                log.Information("ChatId: " + chatId);

                                var obj = Cxim.GetAttachment(pkgBytes)?["att_chat_course"];
                                if (obj == null)
                                {
                                    Log.Warning("解析失败，无法获取 JSON");
                                    log = null;
                                    continue;
                                }

                                var activeId = obj["aid"]?.Value<string>();
                                if (activeId is null or "0")
                                {
                                    Log.Error("解析失败，未找到 ActiveId");
                                    Log.Error(obj.ToString());
                                    log = null;
                                    continue;
                                }
                                log.Information("ActiveId: " + activeId);

                                var courseInfo = obj["courseInfo"];
                                if (courseInfo == null)
                                {
                                    Log.Error("解析失败，未找到 courseInfo");
                                    Log.Error(obj.ToString());
                                }
                                var courseName = courseInfo?["coursename"]?.Value<string>();
                                log.Information($"收到来自「{courseName}」的活动");

                                var data = await client.GetActiveDetailAsync(activeId);
                                // 是否为签到消息
                                if (data["activeType"]?.Value<int>() != 2)
                                {
                                    Log.Information("活动不是签到");
                                    continue;
                                }

                                // WebApi 设置
                                if (enableWeiApi && !WebApi.IntervalData.Status.CxAutoSignEnabled)
                                {
                                    log.Information("因 WebApi 设置，跳过签到");
                                    continue;
                                }

                                // 签到流程
                                var course = userConfig.GetCourse(chatId);
                                var courseConfig = new CourseConfig(appConfig, userConfig, course);
                                if (!courseConfig.SignEnable)
                                {
                                    log.Information("因用户配置，跳过签到");
                                    continue;
                                }

                                var signType = GetSignType(data);
                                log.Information("签到类型：" + GetSignTypeName(signType));
                                if (signType == SignType.Gesture)
                                {
                                    log.Information("手势：" + data["signCode"]?.Value<string>());
                                }
                                else if (signType == SignType.Qr)
                                {
                                    log.Information("暂时无法二维码签到");
                                    continue;
                                }

                                var signOptions = courseConfig.GetSignOptions(signType.ToString());
                                if (signOptions == null)
                                {
                                    log.Warning("因用户课程配置，跳过签到");
                                    continue;
                                }

                                if (signType == SignType.Photo)
                                {
                                    signOptions.ImageId = await courseConfig.GetImageIdAsync(client);
                                }

                                await Task.Delay(courseConfig.SignDelay * 1000);
                                log.Information("开始签到");
                                await client.SignAsync(activeId, signOptions);
                                log.Information("签到完成");
                            }
                            catch (Exception e)
                            {
                                if (log == null)
                                {
                                    Log.Error(e.ToString());
                                }
                                else
                                {
                                    log.Error(e.ToString());
                                }
                            }
                            log?.Dispose();
                        }
                    }
                    catch (Exception e)
                    {
                        Log.Error(e.ToString());
                    }
                });
                await _ws.Start();
                exitEvent.WaitOne();
            }

            Console.ReadKey();

            return 0;
        }

        private static SignType GetSignType(JToken data)
        {
            var otherId = data["otherId"].Value<int>();
            switch (otherId)
            {
                case 2:
                    return SignType.Qr;
                case 3:
                    return SignType.Gesture;
                case 4:
                    return SignType.Location;
                default:
                    var token = data["ifphoto"];
                    return token?.Type == JTokenType.Integer && token.Value<int>() != 0
                        ? SignType.Photo
                        : SignType.Normal;
            }
        }

        private static string GetSignTypeName(SignType type)
        {
            return type switch
            {
                SignType.Normal => "普通签到",
                SignType.Photo => "图片签到",
                SignType.Qr => "二维码签到",
                SignType.Gesture => "手势签到",
                SignType.Location => "位置签到",
                _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
            };
        }
    }
}
