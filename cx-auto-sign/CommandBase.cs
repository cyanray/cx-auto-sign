using McMaster.Extensions.CommandLineUtils;

namespace cx_auto_sign
{
    [HelpOption("-h|--help")]
    abstract class CommandBase
    {

        protected virtual int OnExecute(CommandLineApplication app)
        {
            return 0;
        }
    }

}
