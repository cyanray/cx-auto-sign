using cx_auto_sign.Models;
using CxSignHelper;
using McMaster.Extensions.CommandLineUtils;
using Newtonsoft.Json;
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
            Console.WriteLine($"username: {Username}, password: {Password}");
            try
            {
                CxSignClient client = null;
                if (string.IsNullOrEmpty(Fid))
                    client = await CxSignClient.LoginAsync(Username, Password);
                else
                    client = await CxSignClient.LoginAsync(Username, Password, Fid);

                // 保存登录信息
                AppConfig.Username = Username;
                AppConfig.Password = Password;
                AppConfig.Fid = Fid;

                var courses = await client.GetCoursesAsync();
                Directory.CreateDirectory("Courses");
                foreach (var course in courses)
                {
                    Console.WriteLine($"发现课程:{course.CourseName} - {course.ClassName} ({course.CourseId},{course.ClassId})");
                    File.WriteAllText($"Courses/{course.CourseId}.json", JsonConvert.SerializeObject(course));
                }


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
