using CxSignHelper;
using McMaster.Extensions.CommandLineUtils;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace cx_auto_sign
{
    [Command(Name = "cx-auto-sign", Description = "超星自动签到工具")]
    [VersionOptionFromMember("--version", MemberName = nameof(GetVersion))]
    [Subcommand(
        typeof(InitCommand),
        typeof(WorkCommand)
        )]
    class Program : CommandBase
    {
        static int Main(string[] args) => CommandLineApplication.Execute<Program>(args);

        private static string GetVersion()
            => typeof(Program).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion;

        [Option]
        public bool Trace { get; }

        protected override int OnExecute(CommandLineApplication app)
        {
            app.ShowHelp();
            return 1;
        }
    }

}
