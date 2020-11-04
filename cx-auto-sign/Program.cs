using CxSignHelper;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.VisualBasic;
using Newtonsoft.Json.Linq;
using RestSharp;
using Serilog;
using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace cx_auto_sign
{
    [Command(
        Name = "cx-auto-sign",
        Description = "超星自动签到工具",
        ExtendedHelpText = @"
提示:
  本程序采用 MIT 协议开源(https://github.com/cyanray/cx-auto-sign).
  任何人可免费使用本程序并查看其源代码.
")]
    [VersionOptionFromMember("--version", MemberName = nameof(GetVersion))]
    [Subcommand(
        typeof(InitCommand),
        typeof(WorkCommand)
        )]
    class Program : CommandBase
    {
        static async Task<int> Main(string[] args)
        {
            await CheckUpdate();
            _ = Task.Run(() => { cx_auto_sign.WebApi.Program.Main(null); });
            return await CommandLineApplication.ExecuteAsync<Program>(args);
        }

        private static async Task CheckUpdate()
        {
            if (File.Exists(".noupdate"))
            {
                Console.WriteLine("已跳过检查更新");
                return;
            }
            try
            {
                Console.WriteLine("正在检查更新...");
                var body = await GetLastestVersion();
                if (!body.Version.Contains(GetVersion()))
                {
                    Console.WriteLine($"发现新版本: {body.Version}");
                    Console.WriteLine(body.Info);
                    Console.WriteLine("请前往 https://github.com/cyanray/cx-auto-sign/releases 下载更新，或者按任意键继续...");
                    Console.ReadKey();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine("获取版本信息失败，请访问 https://github.com/cyanray/cx-auto-sign/releases 检查是否有更新");
            }
        }

        private static string GetVersion()
            => typeof(Program).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion;

        protected async override Task<int> OnExecuteAsync(CommandLineApplication app)
        {
            app.ShowHelp();
            return await base.OnExecuteAsync(app);
        }

        private static async Task<(string Version, string Info)> GetLastestVersion()
        {
            RestClient client = new RestClient($"https://api.github.com/repos/cyanray/cx-auto-sign/releases/latest");
            var request = new RestRequest(Method.GET);
            var response = await client.ExecuteGetAsync(request);
            var json = JObject.Parse(response.Content);
            if (response.StatusCode != HttpStatusCode.OK)
            {
                string message = string.Empty;
                if (json.ContainsKey("message"))
                    message = json["message"].Value<string>();
                throw new Exception($"获取最新版本失败: {message}");
            }
            var version = json["tag_name"].Value<string>();
            var info = json["body"].Value<string>();
            return (version, info);
        }

    }

}
