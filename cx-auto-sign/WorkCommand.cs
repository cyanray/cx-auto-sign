using CxSignHelper;
using McMaster.Extensions.CommandLineUtils;
using Newtonsoft.Json.Linq;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
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
        /// <summary>
        /// 班级的群聊ID - 上一次查询的签到任务数量
        /// </summary>
        private Dictionary<string, int> CidCountPair = new Dictionary<string, int>();

        protected override async Task<int> OnExecuteAsync(CommandLineApplication app)
        {
            try
            {
                Log.Information("登录账号 {Username} 中...", AppConfig.Username);
                CxSignClient client = null;
                if (string.IsNullOrEmpty(AppConfig.Fid))
                    client = await CxSignClient.LoginAsync(AppConfig.Username, AppConfig.Password);
                else
                    client = await CxSignClient.LoginAsync(AppConfig.Username, AppConfig.Password, AppConfig.Fid);
                Log.Information("登录账号 {Username} 成功", AppConfig.Username);
                var imParams = await client.GetImTokenAsync();

                // 上传文件夹下所有图片
                if (!Directory.Exists("images"))
                {
                    Directory.CreateDirectory("images");
                }
                DirectoryInfo di = new DirectoryInfo("images");
                FileSystemInfo[] fis = di.GetFileSystemInfos();
                foreach (FileSystemInfo fi in fis)
                {
                    if ((fi.Attributes & FileAttributes.Directory) != FileAttributes.Directory)
                    {
                        Log.Information("正在上传: {FileName} ...", fi.Name);
                        client.Imageids.Add(await client.UploadImageAsync("images\\" + fi.Name));
                        Log.Information("上传成功, Objectid = {Objectid}", client.Imageids[client.Imageids.Count - 1]);
                    }
                }

                // 创建 Websocket 对象，监听消息
                var exitEvent = new ManualResetEvent(false);
                var url = new Uri("wss://im-api-vip6-v2.easemob.com/ws/032/xvrhfd2j/websocket");
                #region 消息处理
                using (var wsClient = new WebsocketClient(url))
                {
                    wsClient.ReconnectionHappened.Subscribe(info =>
                       Log.Warning("Reconnection happened, type: {Type}", info.Type));

                    wsClient.MessageReceived.Subscribe(async msg =>
                    {
                        Log.Information($"Message received: {msg}");
                        if (msg.Text.StartsWith("o"))
                        {
                            var loginPackage = cxim.BuildLoginPackage(imParams.TUid, imParams.ImToken);
                            Log.Information($"Message send: {loginPackage}");
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
                                Log.Information("收到来自 {cidStr} 的消息", cidStr);
                                // 签到流程
                                var course = Courses.Where(x => x.ChatId == cidStr).FirstOrDefault();
                                if (course is null) return;
                                Log.Information("获取课程 {courseName} 的签到任务...", course.CourseName);
                                var signTasks = await client.GetSignTasksAsync(course.CourseId, course.ClassId);
                                if (!CidCountPair.ContainsKey(cidStr)) CidCountPair.Add(cidStr, -1);
                                if (CidCountPair[cidStr] != signTasks.Count)
                                {
                                    Log.Information("正在签到课程 {courseName} 的所有签到任务...", course.CourseName);
                                    foreach (var task in signTasks)
                                    {
                                        await client.SignAsync(task);
                                    }
                                    CidCountPair[cidStr] = signTasks.Count;
                                    Log.Information("已完成该课程所有签到");
                                }
                                else
                                {
                                    Log.Information("课程 {courseName} 没有新的签到任务", course.CourseName);
                                }
                            }
                        }

                    });

                    await wsClient.Start();

                    exitEvent.WaitOne();
                }
                #endregion
                Console.ReadKey();
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message);
                Log.Error(ex.StackTrace);
            }

            return await base.OnExecuteAsync(app);
        }
    }
}
