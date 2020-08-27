using McMaster.Extensions.CommandLineUtils;
using System;
using System.ComponentModel.DataAnnotations;

namespace cx_auto_sign
{
    [Command(Description = "登录超星学习通以初始化课程配置")]
    class InitCommand : CommandBase
    {
        [Option("-n", Description="用户名(学号)")]
        [Required]
        public string Username { get; set; }

        [Option("-p", Description="密码")]
        [Required]
        public string Password { get; set; }

        protected override int OnExecute(CommandLineApplication app)
        {
            Console.WriteLine($"username: {Username}, password: {Password}");
            return base.OnExecute(app);
        }
    }

}
