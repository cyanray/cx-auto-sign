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

        public string Fid { get; set; } = null;

        public string PUid { get; set; } = null;

        public List<string> ImageIds { get; set; } = new List<string>();

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
            RestClient client = new RestClient("https://mobilelearn.chaoxing.com");
            client.CookieContainer = _Cookie;
            var request = new RestRequest("v2/apis/active/student/activelist");
            request.AddParameter("fid", "0");
            request.AddParameter("courseId", courseId);
            request.AddParameter("classId", classId);
            var response = await client.ExecuteGetAsync(request);
            if (response.StatusCode != HttpStatusCode.OK)
                throw new Exception("非200状态响应");
            var json = JObject.Parse(response.Content);
            if (json["result"].Value<int>() != 1)
                new Exception(json["msg"].Value<string>());
            var taskJArray = JArray.FromObject(json["data"]["activeList"]);
            return taskJArray.ToObject<List<SignTask>>().Where(x => x.Type == 2).OrderByDescending(x => x.StartTime).ToList();
        }

        public async Task SignAsync(SignTask task)
        {
            var SignClien = new RestClient("https://mobilelearn.chaoxing.com/pptSign/stuSignajax");
            SignClien.CookieContainer = _Cookie;

            string imageId;
            if (ImageIds.Count != 0)
            {
                Random rd = new Random();
                imageId = ImageIds[rd.Next(0, ImageIds.Count - 1)];
            }
            else
            {
                imageId = "041ed4756ca9fdf1f9b6dde7a83f8794";
            }

            var request = new RestRequest(Method.GET);
            // ?activeId=292002019&appType=15&ifTiJiao=1&latitude=-1&longitude=-1&clientip=1.1.1.1&address=中国&objectId=3194679e88dbc9c60a4c6e31da7fa905
            request.AddParameter("activeId", task.Id);
            request.AddParameter("appType", "15");
            request.AddParameter("ifTiJiao", "1");
            request.AddParameter("latitude", "-1");
            request.AddParameter("longitude", "-1");
            request.AddParameter("clientip", "1.1.1.1");
            request.AddParameter("address", "中国");
            request.AddParameter("objectId", imageId);
            var response = await SignClien.ExecuteGetAsync(request);
            if (response.Content == "success" || response.Content == "您已签到过了") return;
            throw new Exception($"签到出错: {response.Content}");
        }

        public async Task<(string ImToken, string TUid)> GetImTokenAsync()
        {
            RestClient client = new RestClient("https://im.chaoxing.com/webim/me");
            client.CookieContainer = _Cookie;
            var request = new RestRequest(Method.GET);
            var response = await client.ExecuteGetAsync(request);
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

        public async Task<List<CourseModel>> GetCoursesAsync()
        {
            RestClient client = new RestClient("https://mooc2-ans.chaoxing.com/visit/courses/list?rss=1&start=0&size=500&catalogId=0&searchname=");
            client.CookieContainer = _Cookie;
            var request = new RestRequest(Method.GET);
            var response = await client.ExecuteGetAsync(request);
            if (response.StatusCode != HttpStatusCode.OK)
                throw new Exception("非200状态响应");
            var regex = new Regex(@"/mycourse/stu\?courseid=(\d+?)&clazzid=(\d+)");
            var matches = regex.Matches(response.Content);
            List<CourseModel> result = new List<CourseModel>();
            foreach (Match match in matches)
            {
                if (match.Groups.Count <= 2) continue;
                string courseId = match.Groups[1].Value;
                string classId = match.Groups[2].Value;
                var classDetail = await GetClassDetailAsync(courseId, classId);
                result.Add(new CourseModel()
                {
                    CourseId = courseId,
                    ClassId = classId,
                    ChatId = classDetail.ChatId,
                    ClassName = classDetail.ClassName,
                    CourseName = classDetail.CourseName
                });
            }
            return result;
        }

        private async Task<(string ChatId, string CourseName, string ClassName)> GetClassDetailAsync(string CourseId, string ClassId)
        {
            RestClient client = new RestClient($"https://mobilelearn.chaoxing.com/v2/apis/class/getClassDetail?fid={Fid}&courseId={CourseId}&classId={ClassId}");
            client.CookieContainer = _Cookie;
            var request = new RestRequest(Method.GET);
            var response = await client.ExecuteGetAsync(request);
            if (response.StatusCode != HttpStatusCode.OK)
                throw new Exception("非200状态响应");
            var json = JObject.Parse(response.Content);
            if (json["result"].Value<int>() != 1) 
                throw new Exception(json["msg"].Value<string>());
            var chatId = json["data"]["chatid"].Value<string>();
            var courseName = json["data"]["course"]["data"][0]["name"].Value<string>();
            var className = json["data"]["name"].Value<string>();
            return (chatId, courseName, className);
        }

        public async Task<string> UploadImageAsync(string path)
        {
            var client = new RestClient("https://pan-yz.chaoxing.com/upload");
            client.CookieContainer = _Cookie;
            var request = new RestRequest(Method.POST);
            request.AddParameter("puid", PUid);
            request.AddParameter("_token", await GetTokenAsync());
            request.AddFile("file", path);
            var response = await client.ExecutePostAsync(request);
            var json = JObject.Parse(response.Content);
            if (json["result"].Value<bool>() != true) 
                throw new Exception(json["msg"].Value<string>());
            return json["objectId"].Value<string>();
        }

        private void ParseCookies()
        {
            var cookies = _Cookie.GetCookies(new Uri("http://chaoxing.com"));
            Fid = cookies["fid"].Value;
            PUid = cookies["_uid"].Value;
        }


    }
}
