using McMaster.Extensions.CommandLineUtils;
using System;

namespace cx_auto_sign
{
    [Command(Description = "工作模式, 监听签到任务并自动签到")]
    class WorkCommand : CommandBase
    {

        protected override int OnExecute(CommandLineApplication app)
        {
            Console.WriteLine("working...");
            return base.OnExecute(app);
        }
    }

}
