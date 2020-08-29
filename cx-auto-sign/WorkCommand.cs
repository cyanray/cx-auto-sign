using CxSignHelper;
using McMaster.Extensions.CommandLineUtils;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Websocket.Client;

namespace cx_auto_sign
{
    [Command(Description = "工作模式, 监听签到任务并自动签到")]
    class WorkCommand : CommandBase
    {

        protected override async Task<int> OnExecuteAsync(CommandLineApplication app)
        {
            // 读取总配置

            // 读取所有课程配置

            // 登录

            try
            {
                CxSignClient client = null;
                if (string.IsNullOrEmpty(AppConfig.Fid))
                    client = await CxSignClient.LoginAsync(AppConfig.Username, AppConfig.Password);
                else
                    client = await CxSignClient.LoginAsync(AppConfig.Username, AppConfig.Password, AppConfig.Fid);

                var imParams = await client.GetImTokenAsync();

                // 创建 Websocket 对象，监听消息

                var exitEvent = new ManualResetEvent(false);
                var url = new Uri("wss://im-api-vip6-v2.easemob.com/ws/032/xvrhfd2j/websocket");
                #region 消息处理
                using (var wsClient = new WebsocketClient(url))
                {
                    wsClient.ReconnectionHappened.Subscribe(info =>
                       Console.WriteLine($"Reconnection happened, type: {info.Type}"));

                    wsClient.MessageReceived.Subscribe(async msg =>
                    {
                        Console.WriteLine($"Message received: {msg}");
                        if (msg.Text.StartsWith("o"))
                        {
                            var loginPackage = cxim.BuildLoginPackage(imParams.TUid, imParams.ImToken);
                            Console.WriteLine($"Message send: {loginPackage}");
                            wsClient.Send(loginPackage);
                            return;
                        }

                        if (msg.Text.StartsWith("a"))
                        {
                            var json = JArray.Parse(msg.Text.Substring(1));
                            var pkgBytes = Convert.FromBase64String(json[0].Value<string>());
                            if (pkgBytes.Length <= 5) return;
                            var t = new byte[5];
                            Array.Copy(pkgBytes, t, 5);
                            if (t.SequenceEqual(new byte[] { 0x08, 0x00, 0x40, 0x02, 0x4a }))
                            {
                                var lenByte = new byte[1];
                                Array.Copy(pkgBytes, 9, lenByte, 0, 1);
                                var len = Convert.ToUInt32(lenByte[0]);
                                var cid = new byte[len];
                                Array.Copy(pkgBytes, 10, cid, 0, len);
                                var cidStr = Encoding.ASCII.GetString(cid);
                                Console.WriteLine($"收到来自 {cidStr} 的消息");
                                // 签到流程
                                Console.WriteLine("正在签到中...");
                                var course = Courses.Where(x => x.ChatId == cidStr).FirstOrDefault();
                                if (course is null) return;
                                var signTasks = await client.GetSignTasksAsync(course.CourseId, course.ClassId);
                                foreach (var task in signTasks)
                                {
                                    await client.SignAsync(task);
                                }
                                Console.WriteLine("已完成该课程所有签到");
                            }
                        }

                    });

                    await wsClient.Start();

                    exitEvent.WaitOne();
                }
                #endregion
                Console.ReadKey();

                // 收到消息：检查新签到活动

                // 收到签到活动：执行签到流程
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
            }

            return await base.OnExecuteAsync(app);
        }
    }
}
