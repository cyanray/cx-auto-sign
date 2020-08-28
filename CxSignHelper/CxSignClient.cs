using CxSignHelper.Models;
using CxSignHelper.Utils;
using Newtonsoft.Json;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace CxSignHelper
{
    public partial class CxSignClient
    {
        private CookieContainer _Cookie = new CookieContainer();

        private CxSignClient(CookieContainer cookieContainer)
        {
            _Cookie = cookieContainer;
        }

        public static async Task<CxSignClient> LoginAsync(string username, string password)
        {
            RestClient LoginClient = new RestClient("https://passport2-api.chaoxing.com");
            LoginClient.CookieContainer = new CookieContainer();
            var request = new RestRequest("v11/loginregister");
            request.AddParameter("uname", username);
            request.AddParameter("code", password);
            var response = await LoginClient.ExecuteGetAsync(request);
            if (response.StatusCode != HttpStatusCode.OK)
                throw new Exception("非200状态响应");
            var loginObject = JsonConvert.DeserializeObject<LoginObject>(response.Content);
            if (loginObject.Status != true)
                throw new Exception(loginObject.Messagee);

            CxSignClient result = new CxSignClient(LoginClient.CookieContainer);
            return result;
        }


        public string GetToken()
        {
            RestClient TokenClient = new RestClient("https://pan-yz.chaoxing.com");
            var request = new RestRequest("api/token/uservalid");
            TokenClient.CookieContainer = _Cookie;
            var response = TokenClient.Get(request);
            if (response.StatusCode != System.Net.HttpStatusCode.OK)
                throw new Exception("非200状态响应");
            var tokenObject = JsonConvert.DeserializeObject<TokenObject>(response.Content);
            if (tokenObject.Result != true)
                throw new Exception("获取token失败");
            return tokenObject.Token;
        }

    }
}
