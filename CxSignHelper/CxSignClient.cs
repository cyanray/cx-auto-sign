using CxSignHelper.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;
using System;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CxSignHelper
{
    public class CxSignClient
    {
        private readonly CookieContainer _cookie;

        private string Fid { get; set; }

        private string PUid { get; set; }

        private CxSignClient(CookieContainer cookieContainer)
        {
            _cookie = cookieContainer;
            ParseCookies();
        }

        public static async Task<CxSignClient> LoginAsync(string username, string password, string fid = null)
        {
            RestClient client;
            IRestResponse response;
            if (string.IsNullOrEmpty(fid))
            {
                client = new RestClient("https://passport2-api.chaoxing.com")
                {
                    CookieContainer = new CookieContainer()
                };
                var request = new RestRequest("v11/loginregister");
                request.AddParameter("uname", username);
                request.AddParameter("code", password);
                response = await client.ExecuteGetAsync(request);
            }
            else
            {
                client = new RestClient($"https://passport2-api.chaoxing.com/v6/idNumberLogin?fid={fid}&idNumber={username}")
                {
                    CookieContainer = new CookieContainer()
                };
                var request = new RestRequest(Method.POST);
                request.AddHeader("Content-Type", "application/x-www-form-urlencoded");
                request.AddParameter("pwd", password);
                request.AddParameter("t", "0");
                response = await client.ExecutePostAsync(request);
            }
            TestResponseCode(response);
            var loginObject = JsonConvert.DeserializeObject<LoginObject>(response.Content);
            if (loginObject.Status != true)
            {
                throw new Exception(loginObject.Message);
            }
            return new CxSignClient(client.CookieContainer);
        }

        private async Task<string> GetTokenAsync()
        {
            var client = new RestClient("https://pan-yz.chaoxing.com")
            {
                CookieContainer = _cookie
            };
            var request = new RestRequest("api/token/uservalid");
            var response = await client.ExecuteGetAsync(request);
            TestResponseCode(response);
            var tokenObject = JsonConvert.DeserializeObject<TokenObject>(response.Content);
            if (tokenObject.Result != true)
            {
                throw new Exception("获取 token 失败");
            }
            return tokenObject.Token;
        }

        public async Task<JArray> GetSignTasksAsync(string courseId, string classId)
        {
            var client = new RestClient("https://mobilelearn.chaoxing.com")
            {
                CookieContainer = _cookie
            };
            var request = new RestRequest("v2/apis/active/student/activelist");
            request.AddParameter("fid", "0");
            request.AddParameter("courseId", courseId);
            request.AddParameter("classId", classId);
            var response = await client.ExecuteGetAsync(request);
            TestResponseCode(response);
            var json = JObject.Parse(response.Content);
            if (json["result"]!.Value<int>() != 1)
            {
                throw new Exception(json["msg"]?.Value<string>());
            }
            return (JArray)json["data"]!["activeList"];
        }

        public async Task<JToken> GetActiveDetailAsync(string activeId)
        {
            var client = new RestClient("https://mobilelearn.chaoxing.com")
            {
                CookieContainer = _cookie
            };
            var request = new RestRequest("v2/apis/active/getPPTActiveInfo");
            request.AddParameter("activeId", activeId);
            var response = await client.ExecuteGetAsync(request);
            TestResponseCode(response);
            var json = JObject.Parse(response.Content);
            if (json["result"]?.Value<int>() != 1)
            {
                throw new Exception("Message: " + json["msg"]?.Value<string>() +
                                    "\nError Message: " + json["errorMsg"]?.Value<string>());
            }
            return json["data"];
        }

        public async Task<string> SignAsync(string activeId, SignOptions signOptions)
        {
            var client = new RestClient("https://mobilelearn.chaoxing.com/pptSign/stuSignajax")
            {
                CookieContainer = _cookie
            };
            var request = new RestRequest(Method.GET);
            // ?activeId=292002019&appType=15&ifTiJiao=1&latitude=-1&longitude=-1&clientip=1.1.1.1&address=中国&objectId=3194679e88dbc9c60a4c6e31da7fa905
            request.AddParameter("activeId", activeId);
            request.AddParameter("appType", "15");
            request.AddParameter("ifTiJiao", "1");
            request.AddParameter("latitude", signOptions.Latitude);
            request.AddParameter("longitude", signOptions.Longitude);
            request.AddParameter("clientip", signOptions.ClientIp);
            request.AddParameter("address", signOptions.Address);
            request.AddParameter("objectId", signOptions.ImageId);
            var response = await client.ExecuteGetAsync(request);
            return response.Content;
        }

        public async Task<(string ImToken, string TUid)> GetImTokenAsync()
        {
            var client = new RestClient("https://im.chaoxing.com/webim/me")
            {
                CookieContainer = _cookie
            };
            var response = await client.ExecuteGetAsync(new RestRequest());
            TestResponseCode(response);
            var regex = new Regex(@"loginByToken\('(\d+?)', '([^']+?)'\);");
            var match = regex.Match(response.Content);
            if (!match.Success)
            {
                throw new Exception("获取 ImToken 失败");
            }
            return (match.Groups[2].Value, match.Groups[1].Value);
        }

        public async Task GetCoursesAsync(JToken course)
        {
            var client = new RestClient("https://mooc2-ans.chaoxing.com/visit/courses/list?rss=1&catalogId=0&searchname=")
            {
                CookieContainer = _cookie
            };
            var response = await client.ExecuteGetAsync(new RestRequest());
            TestResponseCode(response);
            var regex = new Regex(@"\?courseid=(\d+?)&clazzid=(\d+)&cpi=\d+""");
            var matches = regex.Matches(response.Content);
            foreach (Match match in matches)
            {
                if (match.Groups.Count <= 2)
                {
                    continue;
                }
                var courseId = match.Groups[1].Value;
                var classId = match.Groups[2].Value;
                var (chatId, courseName, className) = await GetClassDetailAsync(courseId, classId);
                var obj = new JObject
                {
                    ["CourseId"] = courseId,
                    ["ClassId"] = classId,
                    ["ChatId"] = chatId,
                    ["CourseName"] = courseName,
                    ["ClassName"] = className
                };
                course[chatId] = obj;
            }
        }

        private async Task<(string ChatId, string CourseName, string ClassName)> GetClassDetailAsync(string courseId, string classId)
        {
            var client = new RestClient($"https://mobilelearn.chaoxing.com/v2/apis/class/getClassDetail?fid={Fid}&courseId={courseId}&classId={classId}")
            {
                CookieContainer = _cookie
            };
            var response = await client.ExecuteGetAsync(new RestRequest());
            TestResponseCode(response);
            var json = JObject.Parse(response.Content);
            if (json["result"]!.Value<int>() != 1)
            {
                throw new Exception(json["msg"]?.Value<string>());
            }
            var data = json["data"];
            var chatId = data!["chatid"]!.Value<string>();
            var courseName = data["course"]!["data"]![0]!["name"]!.Value<string>();
            var className = data["name"]!.Value<string>();
            return (chatId, courseName, className);
        }

        public async Task<string> UploadImageAsync(string path)
        {
            // 预览：
            // https://p.ananas.chaoxing.com/star3/170_220c/f5b88e10d3dfedf9829ca8c009029e7b.png
            // https://p.ananas.chaoxing.com/star3/origin/f5b88e10d3dfedf9829ca8c009029e7b.png
            // https://pan-yz.chaoxing.com/thumbnail/origin/f5b88e10d3dfedf9829ca8c009029e7b?type=img
            var client = new RestClient("https://pan-yz.chaoxing.com/upload")
            {
                CookieContainer = _cookie
            };
            var request = new RestRequest(Method.POST);
            request.AddParameter("puid", PUid);
            request.AddParameter("_token", await GetTokenAsync());
            request.AddFile("file", path);
            var response = await client.ExecutePostAsync(request);
            var json = JObject.Parse(response.Content);
            if (json["result"]!.Value<bool>() != true)
            {
                throw new Exception(json["msg"]?.Value<string>());
            }
            return json["objectId"]!.Value<string>();
        }

        private void ParseCookies()
        {
            var cookies = _cookie.GetCookies(new Uri("http://chaoxing.com"));
            Fid = cookies["fid"]?.Value;
            PUid = cookies["_uid"]!.Value;
        }

        public static void TestResponseCode(IRestResponse response)
        {
            var code = response.StatusCode;
            if (code != HttpStatusCode.OK)
            {
                throw new Exception($"非 200 状态响应：{code:D} {code:G}\n{response.Content}");
            }
        }
    }
}
