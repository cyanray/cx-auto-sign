using McMaster.Extensions.CommandLineUtils;
using Serilog;
using System.Threading.Tasks;

namespace cx_auto_sign
{
    [HelpOption("-h|--help")]
    public abstract class CommandBase
    {
        protected CommandBase()
        {
            Log.Logger = new LoggerConfiguration()
                            .WriteTo.Console()
                            .CreateLogger();
        }

        // ReSharper disable once UnusedMemberHierarchy.Global
        protected virtual Task<int> OnExecuteAsync(CommandLineApplication app)
        {
            return Task.FromResult(0);
        }
    }
}
