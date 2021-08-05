using System;
using System.IO;
using System.Threading.Tasks;
using CxSignHelper;
using Newtonsoft.Json.Linq;
using Serilog;

namespace cx_auto_sign
{
    public class UserDataConfig: BaseDataConfig
    {
        private const string Dir = "Configs";
        private const string KeyCourse = "Course";

        private readonly string _path;

        private readonly JObject _data;
        private readonly JObject _courses;

        public readonly string Username;
        public readonly string Password;
        public readonly string Fid;
        public readonly JToken WebApi;

        private UserDataConfig(FileSystemInfo file): this(file.FullName, null, null, null)
        {
            _path = file.FullName;
            Username = GetString(nameof(Username));
            Password = GetString(nameof(Password));
            Fid = GetString(nameof(Fid));
        }

        public UserDataConfig(string user, string pass = null, string fid = null)
            : this(GetPath(user), user, pass, fid) { }

        private UserDataConfig(string path, string user, string pass, string fid)
        {
            _path = path;
            if (File.Exists(_path))
            {
                _data = JObject.Parse(File.ReadAllText(_path));
                _courses = (JObject) (_data[KeyCourse] ?? (_data[KeyCourse] = new JObject()));
                if (user != null)
                {
                    SetAuth(nameof(Username), user);
                    SetAuth(nameof(Password), pass);
                    SetAuth(nameof(Fid), fid);
                }
                Username = GetString(nameof(Username));
                Password = GetString(nameof(Password));
                Fid = GetString(nameof(Fid));
                WebApi = Get(nameof(WebApi));
            }
            else
            {
                if (pass == null)
                {
                    throw new Exception("不存在该用户的配置");
                }
                _data = new JObject
                {
                    [nameof(Username)] = Username = user,
                    [nameof(Password)] = Password = pass,
                    [nameof(Fid)] = Fid = fid,
                    [nameof(WebApi)] = WebApi = false,
                    [KeyCourse] = _courses = new JObject()
                };
            }
        }

        public override JToken GetData()
        {
            return _data;
        }

        public CourseDataConfig GetCourse(string chatId)
        {
            return new CourseDataConfig(_courses?[chatId]);
        }

        private void Save()
        {
            if (!Directory.Exists(Dir))
            {
                Log.Debug("没有用户配置文件夹，并创建：" + Dir);
                Directory.CreateDirectory(Dir);
                Log.Debug("已创建用户配置文件夹：" + Dir);
            }
            Log.Debug("保存用户配置中...");
            File.WriteAllText(_path, _data.ToString());
            Log.Debug("已保存用户配置");
        }

        private void SetAuth(string key, string val)
        {
            if (val == null)
            {
                return;
            }

            var token = Get(key);
            if (token == null || token.Type == JTokenType.String && token.Value<string>() != key)
            {
                _data[key] = val;
            }
        }

        private static string GetPath(string user)
        {
            return Dir + "/" + user + ".json5";
        }

        public async Task UpdateAsync()
        {
            Log.Information("正在登录 {User}...", Username);
            CxSignClient client;
            if (string.IsNullOrEmpty(Fid))
                client = await CxSignClient.LoginAsync(Username, Password);
            else
                client = await CxSignClient.LoginAsync(Username, Password, Fid);
            Log.Information("成功登录账号 {User} ", Username);

            Log.Information("获取课程数据中...");
            await client.GetCoursesAsync(_courses);
            foreach (var (_, course) in _courses)
            {
                Log.Information($"发现课程：{course["CourseName"]}-{course["ClassName"]} ({course["CourseId"]}, {course["CourseId"]})");
            }
            Save();
        }

        public static async Task UpdateAsync(string user)
        {
            var path = GetPath(user);
            var file = new FileInfo(path);
            if (!file.Exists)
            {
                throw new Exception("不存在该用户的配置");
            }
            await new UserDataConfig(file).UpdateAsync();
        }

        public static async Task UpdateAllAsync()
        {
            var dir = new DirectoryInfo(Dir);
            if (!dir.Exists)
            {
                return;
            }
            var infos = dir.GetFiles();
            foreach (var file in infos)
            {
                try
                {
                    await new UserDataConfig(file).UpdateAsync();
                }
                catch (Exception e)
                {
                    Log.Error(e.ToString());
                }
            }
        }
    }
}