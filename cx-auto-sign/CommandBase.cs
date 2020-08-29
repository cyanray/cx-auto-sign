using cx_auto_sign.Models;
using McMaster.Extensions.CommandLineUtils;
using Newtonsoft.Json;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace cx_auto_sign
{
    [HelpOption("-h|--help")]
    abstract class CommandBase
    {
        protected const string AppConfigPath = "AppConfig.json";

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
            if (!File.Exists(AppConfigPath)) return;
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
