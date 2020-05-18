using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using RestSharp;

namespace cx_auto_sign
{
    public class CxSignHelper
    {
        private string _Cookie;
        private RestClient _SignClient = new RestClient("https://mobilelearn.chaoxing.com");
        public CxSignHelper()
        {

        }

        public void Login(string cookie)
        {
            _Cookie = cookie;
        }

        private class TokenObject
        {
            [JsonProperty("result")]
            public bool Result { get; set; }
            [JsonProperty("_token")]
            public string Token { get; set; }
        }

        public string GetToken()
        {
            RestClient TokenClient = new RestClient("https://pan-yz.chaoxing.com");
            var request = new RestRequest("api/token/uservalid");
            request.AddHeader("Cookie", _Cookie);
            var response = TokenClient.Get(request);
            if (response.StatusCode != System.Net.HttpStatusCode.OK)
                throw new Exception("非200状态响应");
            var tokenObject = JsonConvert.DeserializeObject<TokenObject>(response.Content);
            if(tokenObject.Result != true)
                throw new Exception("获取token失败");
            return tokenObject.Token;
        }

    }
}
