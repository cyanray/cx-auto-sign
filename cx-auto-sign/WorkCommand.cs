using CxSignHelper;
using CxSignHelper.Models;
using McMaster.Extensions.CommandLineUtils;
using Newtonsoft.Json.Linq;
using Serilog;
using System;
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

        private readonly DateTime _dateTime1970 = new(1970, 1, 1, 8, 0, 0);

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

            Log.Information("正在登录账号：{Username}", username);
            var client = await CxSignClient.LoginAsync(username, password, fid);
            Log.Information("成功登录账号");
            var (imToken, uid) = await client.GetImTokenAsync();

            var enableWeiApi = false;
            var webApi = userConfig.WebApi;
            if (webApi != null)
            {
                string rule = null;
                // ReSharper disable once ConvertIfStatementToSwitchStatement
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

                async void OnMessageReceived(ResponseMessage msg)
                {
                    var startTime = DateTime.Now;
                    var startTimestamp = (long) (startTime - _dateTime1970).TotalMilliseconds;
                    try
                    {
                        Log.Information("CXIM: Message received: {Message}", msg);
                        if (msg.Text.StartsWith("o"))
                        {
                            Log.Information("CXIM 登录");
                            var loginPackage = Cxim.BuildLoginPackage(uid, imToken);
                            Log.Information("CXIM: Message send: {Message}", loginPackage);
                            _ws.Send(loginPackage);
                            return;
                        }

                        if (!msg.Text.StartsWith("a"))
                        {
                            return;
                        }
                        var arrMsg = JArray.Parse(msg.Text[1..]);
                        foreach (var message in arrMsg)
                        {
                            Logger log = null;
                            try
                            {
                                var pkgBytes = Convert.FromBase64String(message.Value<string>());
                                if (pkgBytes.Length <= 5)
                                {
                                    continue;
                                }

                                var header = new byte[5];
                                Array.Copy(pkgBytes, header, 5);
                                if (!header.SequenceEqual(new byte[] { 0x08, 0x00, 0x40, 0x02, 0x4a }))
                                {
                                    continue;
                                }

                                if (pkgBytes[5] != 0x2b)
                                {
                                    Log.Warning("可能不是课程消息");
                                    continue;
                                }

                                Log.Information("接收到课程消息");

                                string chatId;
                                try
                                {
                                    chatId = Cxim.GetChatId(pkgBytes);
                                }
                                catch (Exception e)
                                {
                                    throw new Exception("解析失败，无法获取 ChatId", e);
                                }

                                log = new LoggerConfiguration()
                                    .WriteTo.Notification(auConfig)
                                    .WriteTo.Console()
                                    .CreateLogger();
                                log.Information("消息时间：{Time}", startTimestamp);
                                log.Information("ChatId: {ChatId}", chatId);

                                var course = userConfig.GetCourse(chatId);
                                log.Information("获取 {CourseName} 签到任务中", course.CourseName);
                                var courseConfig = new CourseConfig(appConfig, userConfig, course);
                                var tasks = await client.GetSignTasksAsync(course.CourseId, course.ClassId);
                                if (tasks.Count == 0)
                                {
                                    Log.Error("没有活动任务");
                                    log = null;
                                    continue;
                                }

                                var task = tasks[0];
                                var taskStartTime = task["startTime"]!.Value<long>();
                                log.Information("任务时间: {Time}", taskStartTime);
                                if (taskStartTime - startTimestamp > 5000)
                                {
                                    // 当教师发布作业的等操作也触发「接收到课程消息」
                                    // 但这些操作不会体现在「活动列表」中
                                    // 因此，这里通过活动开始的时间来判断接收到的是否是活动消息
                                    Log.Warning("不是活动消息");
                                    log = null;
                                    continue;
                                }
                                var type = task["type"];
                                if (type?.Type != JTokenType.Integer || type.Value<int>() != 2)
                                {
                                    Log.Warning("不是签到任务");
                                    log = null;
                                    continue;
                                }

                                var activeId = task["id"]?.ToString();
                                if (string.IsNullOrEmpty(activeId))
                                {
                                    Log.Error("解析失败，ActiveId 为空");
                                    log = null;
                                    continue;
                                }
                                log.Information("准备签到 ActiveId: {ActiveId}", activeId);

                                var data = await client.GetActiveDetailAsync(activeId);
                                var signType = GetSignType(data);
                                log.Information("签到类型：{Type}", GetSignTypeName(signType));
                                // ReSharper disable once ConvertIfStatementToSwitchStatement
                                if (signType == SignType.Gesture)
                                {
                                    log.Information("手势：{Code}", data["signCode"]?.Value<string>());
                                }
                                else if (signType == SignType.Qr)
                                {
                                    log.Information("暂时无法二维码签到");
                                    continue;
                                }

                                if (enableWeiApi && !WebApi.IntervalData.Status.CxAutoSignEnabled)
                                {
                                    log.Information("因 WebApi 设置，跳过签到");
                                    continue;
                                }

                                if (!courseConfig.SignEnable)
                                {
                                    log.Information("因用户配置，跳过签到");
                                    continue;
                                }

                                var signOptions = courseConfig.GetSignOptions(signType);
                                if (signOptions == null)
                                {
                                    log.Warning("因用户课程配置，跳过签到");
                                    continue;
                                }

                                if (signType == SignType.Photo)
                                {
                                    signOptions.ImageId = await courseConfig.GetImageIdAsync(client, log);
                                    log.Information("预览：{Url}",
                                        $"https://p.ananas.chaoxing.com/star3/170_220c/{signOptions.ImageId}");
                                }

                                var runTime = (DateTime.Now - startTime).TotalMilliseconds;
                                log.Information("签到准备完毕，耗时：{Time}ms", runTime);
                                var delay = courseConfig.SignDelay;
                                log.Information("用户配置延迟签到：{Time}s", delay);
                                if (delay > 0)
                                {
                                    delay = (int)(delay * 1000 - runTime);
                                    if (delay > 0)
                                    {
                                        log.Information("将等待：{Delay}ms", delay);
                                        await Task.Delay(delay);
                                    }
                                }

                                log.Information("开始签到");
                                var content = await client.SignAsync(activeId, signOptions);
                                switch (content)
                                {
                                    case  "success":
                                        content = "签到完成";
                                        break;
                                    case "您已签到过了":
                                        break;
                                    default:
                                        log.Error("签到失败");
                                        break;
                                }
                                log.Information(content);
                            }
                            catch (Exception e)
                            {
                                (log ?? Log.Logger).Error(e, "CXIM 接收到课程消息时出错");
                            }
                            finally
                            {
                                log?.Dispose();
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Log.Error(e, "CXIM 接收到消息处理时出错");
                    }
                }

                _ws.MessageReceived.Subscribe(OnMessageReceived);
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
