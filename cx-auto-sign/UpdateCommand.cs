using System.Threading.Tasks;
using McMaster.Extensions.CommandLineUtils;
using Serilog;

namespace cx_auto_sign
{
    [Command(Description = "更新课程")]
    public class UpdateCommand: CommandBase
    {
        // ReSharper disable UnassignedGetOnlyAutoProperty
        [Option("-u", Description = "指定用户名（学号）")]
        private string Username { get; }

        [Option("-a", Description = "更新全部用户")]
        private bool IsAll { get; }
        // ReSharper restore UnassignedGetOnlyAutoProperty

        protected override async Task<int> OnExecuteAsync(CommandLineApplication app)
        {
            if (IsAll)
            {
                await UserDataConfig.UpdateAllAsync();
            }
            else
            {
                var user = Username ?? new AppDataConfig().DefaultUsername;
                if (user == null)
                {
                    Log.Error("没有设置用户和默认用户，可以使用 -u 指定用户");
                    return 1;
                }
                await UserDataConfig.UpdateAsync(user);
            }
            Log.Information("已完成更新");
            return await base.OnExecuteAsync(app);
        }
    }
}