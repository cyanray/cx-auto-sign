using CxSignHelper;
using McMaster.Extensions.CommandLineUtils;
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
        static Task<int> Main(string[] args) => CommandLineApplication.ExecuteAsync<Program>(args);

        private static string GetVersion()
            => typeof(Program).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion;

        [Option]
        public bool Trace { get; }

        protected async override Task<int> OnExecuteAsync(CommandLineApplication app)
        {
            app.ShowHelp();
            return await base.OnExecuteAsync(app);
        }
    }

}
