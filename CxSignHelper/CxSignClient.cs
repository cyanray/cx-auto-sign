using CxSignHelper.Models;
using CxSignHelper.Utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
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
                throw new Exception(loginObject.Message);

            CxSignClient result = new CxSignClient(LoginClient.CookieContainer);
            return result;
        }

        public static async Task<CxSignClient> LoginAsync(string username, string password, string fid)
        {
            string url = $"https://passport2-api.chaoxing.com/v6/idNumberLogin?fid={fid}&idNumber={username}";
            RestClient LoginClient = new RestClient(url);
            LoginClient.CookieContainer = new CookieContainer();
            var request = new RestRequest(Method.POST);
            request.AddHeader("Content-Type", "application/x-www-form-urlencoded");
            request.AddParameter("pwd", password);
            request.AddParameter("t", "0");
            var response = await LoginClient.ExecutePostAsync(request);
            if (response.StatusCode != HttpStatusCode.OK)
                throw new Exception("非200状态响应");
            var loginObject = JsonConvert.DeserializeObject<LoginObject>(response.Content);
            if (loginObject.Status != true)
                throw new Exception(loginObject.Message);

            CxSignClient result = new CxSignClient(LoginClient.CookieContainer);
            return result;
        }


        public async Task<string> GetTokenAsync()
        {
            RestClient TokenClient = new RestClient("https://pan-yz.chaoxing.com");
            var request = new RestRequest("api/token/uservalid");
            TokenClient.CookieContainer = _Cookie;
            var response = await TokenClient.ExecuteGetAsync(request);
            if (response.StatusCode != System.Net.HttpStatusCode.OK)
                throw new Exception("非200状态响应");
            var tokenObject = JsonConvert.DeserializeObject<TokenObject>(response.Content);
            if (tokenObject.Result != true)
                throw new Exception("获取token失败");
            return tokenObject.Token;
        }

        public async Task<List<SignTask>> GetSignTasksAsync(string courseId, string classId)
        {
            RestClient TokenClient = new RestClient("https://mobilelearn.chaoxing.com");
            TokenClient.CookieContainer = _Cookie;
            var request = new RestRequest("v2/apis/active/student/activelist");
            request.AddParameter("fid", "0");
            request.AddParameter("courseId", courseId);
            request.AddParameter("classId", classId);
            var response = await TokenClient.ExecuteGetAsync(request);
            if (response.StatusCode != HttpStatusCode.OK)
                throw new Exception("非200状态响应");
            var json = JObject.Parse(response.Content);
            if (json["result"].Value<int>() != 1)
                new Exception(json["msg"].Value<string>());
            var taskJArray = JArray.FromObject(json["data"]["activeList"]);
            return taskJArray.ToObject<List<SignTask>>().Where(x => x.Type == 2).ToList();
        }

    }
}
