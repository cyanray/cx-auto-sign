using McMaster.Extensions.CommandLineUtils;
using System;
using System.Threading.Tasks;

namespace cx_auto_sign
{
    [Command(Description = "工作模式, 监听签到任务并自动签到")]
    class WorkCommand : CommandBase
    {

        protected override Task<int> OnExecuteAsync(CommandLineApplication app)
        {
            // 读取总配置

            // 读取所有课程配置

            // 创建 Websocket 对象，监听消息

            // 收到消息：检查新签到活动

            // 收到签到活动：执行签到流程

            return Task.FromResult(0);
        }
    }

}
