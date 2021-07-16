using McMaster.Extensions.CommandLineUtils;
using Serilog;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace cx_auto_sign
{
    [Command(Description = "登录超星学习通以初始化课程配置")]
    public class InitCommand : CommandBase
    {
        // ReSharper disable UnassignedGetOnlyAutoProperty
        [Option("-u", Description = "用户名（学号）")]
        [Required]
        private string Username { get; }

        [Option("-p", Description = "密码")]
        [Required]
        private string Password { get; }

        [Option("-f", Description = "学校代码")]
        private string Fid { get; }

        [Option("-d", Description = "设置为默认账号")]
        private bool IsSetDefault { get; }
        // ReSharper restore UnassignedGetOnlyAutoProperty

        protected override async Task<int> OnExecuteAsync(CommandLineApplication app)
        {

            var userConfig = new UserDataConfig(Username, Password, Fid);
            await userConfig.UpdateAsync();
            var appConfig = new AppDataConfig();
            if (IsSetDefault || appConfig.DefaultUsername == null)
            {
                Log.Information("设置为默认用户：" + Username);
                appConfig.DefaultUsername = Username;
                appConfig.Save();
            }
            Log.Warning("开始自动签到请执行：dotnet ./cx-auto-sign.dll work");
            Log.Information("程序执行完毕");
            return await Task.FromResult(0);
        }
    }

}
