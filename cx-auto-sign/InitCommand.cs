using CxSignHelper;
using McMaster.Extensions.CommandLineUtils;
using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace cx_auto_sign
{
    [Command(Description = "登录超星学习通以初始化课程配置")]
    class InitCommand : CommandBase
    {
        [Option("-u", Description = "用户名(学号)")]
        [Required]
        public string Username { get; set; }

        [Option("-p", Description = "密码")]
        [Required]
        public string Password { get; set; }

        [Option("-f", Description = "学校代码")]
        public string Fid { get; set; }

        protected async override Task<int> OnExecuteAsync(CommandLineApplication app)
        {
            Console.WriteLine($"username: {Username}, password: {Password}");
            try
            {
                CxSignClient client = null;
                if (string.IsNullOrEmpty(Fid))
                    client = await CxSignClient.LoginAsync(Username, Password);
                else
                    client = await CxSignClient.LoginAsync(Username, Password, Fid);

                var token = await client.GetTokenAsync();
                Console.WriteLine($"token: {token}");

                var taskList = await client.GetSignTasksAsync("213361494", "29452910");

                foreach (var task in taskList)
                {
                    Console.WriteLine($"任务编号: {task.Id}, 任务名称: {task.Name}");
                    //try
                    //{
                    //    await client.SignAsync(task);
                    //}
                    //catch (Exception ex)
                    //{
                    //    Console.WriteLine(ex.Message);
                    //}
                }

                var imToken = await client.GetImTokenAsync();
                Console.WriteLine($"ImToken: {imToken.ImToken}, TUid: {imToken.TUid}");

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
            }


            return await base.OnExecuteAsync(app);
        }
    }

}
