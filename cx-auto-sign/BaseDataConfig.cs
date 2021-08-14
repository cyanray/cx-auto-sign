using Newtonsoft.Json.Linq;

namespace cx_auto_sign
{
    public abstract class BaseDataConfig: BaseConfig
    {
        public abstract JToken GetData();

        protected sealed override JToken Get(string key)
        {
            return GetData()?[key];
        }

        public override string ToString()
        {
            return GetData().ToString();
        }
    }
}