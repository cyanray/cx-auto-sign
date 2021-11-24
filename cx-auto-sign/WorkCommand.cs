using CxSignHelper;
using CxSignHelper.Models;
using McMaster.Extensions.CommandLineUtils;
using Newtonsoft.Json.Linq;
using Serilog;
using System;
using System.Collections.Generic;
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
        private static readonly DateTime DateTime1970 = new(1970, 1, 1, 8, 0, 0);

        // ReSharper disable UnassignedGetOnlyAutoProperty
        [Option("-u", Description = "指定用户名（学号）")]
        private string Username { get; }
        // ReSharper restore UnassignedGetOnlyAutoProperty

        private WebsocketClient _ws;

        private readonly Dictionary<string, CountCache> _taskCache = new();

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

            Log.Information("正在缓存已有任务");
            await InitTaskCache(client);
            Log.Information("已缓存已有任务");

            // if (true) return 0;
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
                    KeepAliveInterval = Timeout.InfiniteTimeSpan
                }
            }))
            {
                _ws.ReconnectionHappened.Subscribe(info =>
                    Log.Warning("CXIM: Reconnection happened, type: {Type}", info.Type));
                _ws.DisconnectionHappened.Subscribe(info => Log.Error(
                    info.Exception,
                    @"CXIM: Disconnection happened: {Type} {Status}",
                    info.Type,
                    info.CloseStatus
                ));

                async void OnMessageReceived(ResponseMessage msg)
                {
                    var startTime = GetTimestamp();
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

                                log = Notification.CreateLogger(auConfig, GetTimestamp());
                                log.Information("消息时间：{Time}", startTime);
                                log.Information("ChatId: {ChatId}", chatId);

                                var course = userConfig.GetCourse(chatId);
                                if (course == null)
                                {
                                    log.Information("没有会话对应的课程");
                                    await userConfig.UpdateAsync(client);
                                    course = userConfig.GetCourse(chatId);
                                    if (course == null)
                                    {
                                        Log.Information("此用户可能为该课程的教师");
                                        log = null;
                                        continue;
                                    }
                                }
                                log.Information("获取 {CourseName} 活动任务中",
                                    course.CourseName);
                                var courseConfig = new CourseConfig(appConfig, userConfig, course);
                                var tasks = await client.GetSignTasksAsync(course.CourseId, course.ClassId);
                                var count = tasks.Count;
                                if (count == 0)
                                {
                                    Log.Error("没有活动任务");
                                    log = null;
                                    continue;
                                }
                               
                                CountCache countCache;
                                lock (_taskCache)
                                {
                                    if (!_taskCache.TryGetValue(chatId, out countCache))
                                    {
                                        countCache = new CountCache();
                                        _taskCache[chatId] = countCache;
                                    }
                                }
                                try
                                {
                                    await countCache.Lock.WaitAsync();
                                    count -= countCache.Count;
                                    if (count <= 0)
                                    {
                                        // 当教师发布作业的等操作也触发「接收到课程消息」
                                        // 但这些操作不会体现在「活动列表」中
                                        Log.Warning("可能不是活动消息");
                                        log = null;
                                        continue;
                                    }
                                    countCache.Count++;
                                    count = countCache.Count;
                                    var task = tasks[count - 1];
                                    var taskTime = task["startTime"]!.Value<long>();
                                    log.Information("任务时间: {Time}", taskTime);
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
                                    log.Information("签到类型：{Type}",
                                        GetSignTypeName(signType));
                                    // ReSharper disable once ConvertIfStatementToSwitchStatement
                                    if (signType == SignType.Gesture)
                                    {
                                        log.Information("手势：{Code}",
                                            data["signCode"]?.Value<string>());
                                    }
                                    else if (signType == SignType.Qr)
                                    {
                                        log.Warning("暂时无法二维码签到");
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
                                    log.Information("签到准备完毕，耗时：{Time}ms",
                                        GetTimestamp() - startTime);
                                    var takenTime = GetTimestamp() - taskTime;
                                    log.Information("签到已发布：{Time}ms", takenTime);
                                    var delay = courseConfig.SignDelay;
                                    log.Information("用户配置延迟签到：{Time}s", delay);
                                    if (delay > 0)
                                    {
                                        delay = (int) (delay * 1000 - takenTime);
                                        if (delay > 0)
                                        {
                                            log.Information("将等待：{Delay}ms", delay);
                                            await Task.Delay(delay);
                                        }
                                    }

                                    log.Information("开始签到");
                                    var ok = false;
                                    var content = await client.SignAsync(activeId, signOptions);
                                    switch (content)
                                    {
                                        case  "success":
                                            content = "签到完成";
                                            ok = true;
                                            break;
                                        case "您已签到过了":
                                            ok = true;
                                            break;
                                        default:
                                            log.Error("签到失败");
                                            break;
                                    }
                                    log.Information(content);
                                    Notification.Status(log, ok);
                                }
                                finally
                                {
                                    countCache.Lock.Release();
                                }
                            }
                            catch (Exception e)
                            {
                                (log ?? Log.Logger).Error(e, "CXIM 接收到课程消息时出错");
                            }
                            finally
                            {
                                if (log != null)
                                {
                                    Notification.Send(log);
                                }
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

        private static double GetTimestamp()
        {
            return (DateTime.Now - DateTime1970).TotalMilliseconds;
        }

        private async Task InitTaskCache(CxSignClient client)
        {
            var courses = new JObject();
            await client.GetCoursesAsync(courses);
            foreach (var (chatId, course) in courses)
            {
                var cache = new CountCache();
                var tasks = await client.GetSignTasksAsync(chatId, course["ClassId"]!.ToString());
                cache.Count = tasks.Count;
                // Log.Information("{CourseName} - {ClassName}: {Count}", 
                //     course["CourseName"]!.ToString(), 
                //     course["ClassName"]!.ToString(),
                //     cache.Count);
                lock (_taskCache)
                {
                    _taskCache[chatId] = cache;
                }
            }
        }
    }
}
