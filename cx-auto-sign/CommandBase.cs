using McMaster.Extensions.CommandLineUtils;
using System.Threading.Tasks;

namespace cx_auto_sign
{
    [HelpOption("-h|--help")]
    abstract class CommandBase
    {

        protected virtual Task<int> OnExecuteAsync(CommandLineApplication app)
        {
            return Task.FromResult(0);
        }
    }

}
