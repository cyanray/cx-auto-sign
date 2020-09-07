using cx_auto_sign.Models;
using CxSignHelper;
using McMaster.Extensions.CommandLineUtils;
using Newtonsoft.Json;
using Serilog;
using System;
using System.ComponentModel.DataAnnotations;
using System.IO;
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
            Log.Information("正在登录 {User}...", Username);
            try
            {
                CxSignClient client = null;
                if (string.IsNullOrEmpty(Fid))
                    client = await CxSignClient.LoginAsync(Username, Password);
                else
                    client = await CxSignClient.LoginAsync(Username, Password, Fid);
                Log.Information("成功登录账号 {User} ", Username);

                // 保存登录信息
                AppConfig.Username = Username;
                AppConfig.Password = Password;
                AppConfig.Fid = Fid;
                SaveAppConfig();

                // 创建 Email 配置文件
                Email.LoadEmailConfig();
                Email.SaveEmailConfig();


                Log.Information("获取课程数据中...");
                var courses = await client.GetCoursesAsync();
                Directory.CreateDirectory("Courses");
                foreach (var course in courses)
                {
                    Log.Information($"发现课程:{{a}}-{course.ClassName} ({course.CourseId},{course.ClassId})", course.CourseName);
                    File.WriteAllText($"Courses/{course.CourseId}-{course.ClassId}.json", JsonConvert.SerializeObject(course));
                }
                Console.WriteLine();
                Log.Warning("\"./Courses\" 文件夹中每个文件对应一门课程, 不需要签到的课程请删除对应文件");

                Log.Warning("执行 {a} 开始自动签到", "dotnet ./cx-auto-sign.dll work");
                Directory.CreateDirectory("Images");
                Log.Information("程序执行完毕.");

            }
            catch (Exception ex)
            {
                Log.Error(ex.Message);
                Log.Error(ex.StackTrace);
            }


            return await base.OnExecuteAsync(app);
        }
    }

}
