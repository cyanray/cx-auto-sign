using Newtonsoft.Json.Linq;

namespace cx_auto_sign
{
    public abstract class BaseConfig
    {
        protected abstract JToken Get(string key);

        protected static JToken Get(JToken token)
        {
            return token == null || token.Type == JTokenType.Null ? null : token;
        }

        protected string GetString(string key)
        {
            return Get(key)?.Value<string>();
        }

        protected string GetMustString(string key)
        {
            var token = Get(key);
            return token?.Type == JTokenType.String ? token.Value<string>() : null;
        }

        private int GetInt(string key, int def)
        {
            var token = Get(key);
            return token?.Type == JTokenType.Integer ? token.Value<int>() : def;
        }

        protected int GetInt(string key)
        {
            return GetInt(key, 0);
        }

        protected bool GetBool(string key)
        {
            var token = Get(key);
            return token?.Type == JTokenType.Boolean && token.Value<bool>();
        }
    }
}