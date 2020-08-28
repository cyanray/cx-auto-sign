using CxSignHelper.Models;
using CxSignHelper.Utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CxSignHelper
{
    public partial class CxSignClient
    {
        private CookieContainer _Cookie = new CookieContainer();

        public string PUid { get; set; } = null;

        private CxSignClient(CookieContainer cookieContainer)
        {
            _Cookie = cookieContainer;
            ParseCookies();
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

        public async Task SignAsync(SignTask task)
        {
            var SignClien = new RestClient("https://mobilelearn.chaoxing.com/pptSign/stuSignajax");
            SignClien.CookieContainer = _Cookie;
            var request = new RestRequest(Method.GET);
            // ?activeId=292002019&appType=15&ifTiJiao=1&latitude=-1&longitude=-1&clientip=1.1.1.1&address=中国&objectId=3194679e88dbc9c60a4c6e31da7fa905
            request.AddParameter("activeId", task.Id);
            request.AddParameter("appType", "15");
            request.AddParameter("ifTiJiao", "1");
            request.AddParameter("latitude", "-1");
            request.AddParameter("longitude", "-1");
            request.AddParameter("clientip", "1.1.1.1");
            request.AddParameter("address", "中国");
            request.AddParameter("objectId", "3194679e88dbc9c60a4c6e31da7fa905");
            var response = await SignClien.ExecuteGetAsync(request);
            if (response.Content == "success" || response.Content == "您已签到过了") return;
            throw new Exception($"签到出错: {response.Content}");
        }

        public async Task<(string ImToken, string TUid)> GetImTokenAsync()
        {
            RestClient TokenClient = new RestClient("https://im.chaoxing.com/webim/me");
            TokenClient.CookieContainer = _Cookie;
            var request = new RestRequest(Method.GET);
            var response = await TokenClient.ExecuteGetAsync(request);
            if (response.StatusCode != HttpStatusCode.OK)
                throw new Exception("非200状态响应");
            var regex = new Regex(@"loginByToken\('(\d+?)', '([^']+?)'\);");
            var match = regex.Match(response.Content);
            if (match.Success)
            {
                return (match.Groups[2].Value, match.Groups[1].Value);
            }
            else throw new Exception("获取ImToken失败");
        }

        private void ParseCookies()
        {
            var cookies = _Cookie.GetCookies(new Uri("http://chaoxing.com"));
            PUid = cookies["_uid"].Value;
        }


    }
}
