using cx_auto_sign.Models;
using CxSignHelper.Models;
using McMaster.Extensions.CommandLineUtils;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace cx_auto_sign
{
    [HelpOption("-h|--help")]
    abstract class CommandBase
    {
        protected const string AppConfigPath = "AppConfig.json";

        private static List<CourseModel> _courses = null;

        protected static List<CourseModel> Courses
        {
            get
            {
                if(_courses is null)
                {
                    _courses = new List<CourseModel>();
                    var files = Directory.GetFiles("Courses", "*.json");
                    foreach (var file in files)
                    {
                        var text = File.ReadAllText(file);
                        _courses.Add(JsonConvert.DeserializeObject<CourseModel>(text));
                    }
                }
                return _courses;
            }
        }

        private static AppConfig _appConfig = null;

        protected static AppConfig AppConfig 
        {
            get
            {
                if (_appConfig is null) LoadAppConfig();
                return _appConfig;
            }
            set
            {
                _appConfig = value;
                SaveAppConfig();
            }
        }

        protected static void LoadAppConfig()
        {
            if (!File.Exists(AppConfigPath))
            {
                _appConfig = new AppConfig();
                return;
            }
            var text = File.ReadAllText(AppConfigPath);
            _appConfig = JsonConvert.DeserializeObject<AppConfig>(text);
        }

        protected static void SaveAppConfig()
        {
            File.WriteAllText(AppConfigPath, JsonConvert.SerializeObject(_appConfig));
        }

        protected virtual Task<int> OnExecuteAsync(CommandLineApplication app)
        {
            return Task.FromResult(0);
        }
    }

}
