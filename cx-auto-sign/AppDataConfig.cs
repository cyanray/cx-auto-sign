using System.IO;
using Newtonsoft.Json.Linq;
using Serilog;

namespace cx_auto_sign
{
    public class AppDataConfig: BaseDataConfig
    {
        private const string Name = "AppConfig.json5";

        private readonly JObject _data;

        public AppDataConfig()
        {
            if (File.Exists(Name))
            {
                _data = JObject.Parse(File.ReadAllText(Name));
            }
            else
            {
                _data = new JObject();
                _data.Merge(UserConfig.Default);
                _data.Merge(CourseConfig.Default);
            }
        }

        public override JToken GetData()
        {
            return _data;
        }

        public void Save()
        {
            Log.Debug("保存应用配置中...");
            File.WriteAllText(Name, _data.ToString());
            Log.Debug("已保存应用配置");
        }

        public string DefaultUsername
        {
            get => GetString(nameof(DefaultUsername));
            set => _data[nameof(DefaultUsername)] = value;
        }
    }
}