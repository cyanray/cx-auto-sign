using CxSignHelper.Models;
using CxSignHelper.Utils;
using Newtonsoft.Json;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CxSignHelper
{
    public partial class CxSignClient
    {
        private string _Cookie;
        private readonly RestClient _SignClient = new RestClient("https://mobilelearn.chaoxing.com");
        private readonly List<string> CourseName = new List<string>();
        private readonly List<Int64> CourseId = new List<Int64>();
        private readonly List<Int64> ClassId = new List<Int64>();
        private Int64 UID = 0;

        private readonly string Name;
        private readonly string Address;
        private readonly string Latitude;
        private readonly string Longitude;
        private readonly string PicturePath;


        public CxSignClient(string name, string address, string latitude, string longitude, string picturepath)
        {
            Name = name;
            Address = address;
            Latitude = latitude;
            Longitude = longitude;
            PicturePath = picturepath;
        }

        public void Login(string username, string password)
        {
            RestClient LoginClient = new RestClient("https://passport2-api.chaoxing.com");
            var request = new RestRequest("v11/loginregister");
            request.AddParameter("uname", username);
            request.AddParameter("code", password);
            var response = LoginClient.Get(request);
            if (response.StatusCode != System.Net.HttpStatusCode.OK)
                throw new Exception("非200状态响应");
            var loginObject = JsonConvert.DeserializeObject<LoginObject>(response.Content);
            if (loginObject.Status != true)
                throw new Exception("登录失败");
            List<RestResponseCookie> cookie = response.Cookies.ToList<RestResponseCookie>();

            string cookies = null;

            foreach (var Item in cookie)
            {
                cookies += Item.Name + "=" + Item.Value.ToString() + "; ";
                if (Item.Name == "UID")
                {
                    Int64.TryParse(Item.Value.ToString(), out UID);
                }
            }

            if (UID == 0)
            {
                throw new Exception("UID获取异常");
            }

            _Cookie = cookies;
            Console.WriteLine(UID);
        }

        public void Login(string cookie)
        {
            _Cookie = cookie;
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
            if (tokenObject.Result != true)
                throw new Exception("获取token失败");
            return tokenObject.Token;
        }

        public void GetSubject()
        {
            RestClient SubjectClient = new RestClient("http://mooc1-api.chaoxing.com");
            var request = new RestRequest("mycourse/backclazzdata");
            request.AddHeader("Cookie", _Cookie);
            var response = SubjectClient.Get(request);
            if (response.StatusCode != System.Net.HttpStatusCode.OK)
                throw new Exception("非200状态响应");
            var subjectObject = JsonConvert.DeserializeObject<SubjectObject.RootObject>(response.Content);
            if (subjectObject.result != true)
                throw new Exception("获取课表失败");

            //Console.WriteLine(response.Content);

            foreach (var item in subjectObject.channelList)
            {
                //部分content不是course
                if (item.content.course != null)
                {
                    //已开课并且未结课
                    if (item.content.isstart && (item.content.isretire))
                    {
                        ClassId.Add(item.content.id);
                        CourseId.Add(item.content.course.data[0].id);
                        CourseName.Add(item.content.course.data[0].name);
                        Console.WriteLine(item.content.id + " " + item.content.course.data[0].id + " " + item.content.course.data[0].name);
                    }
                }
                else
                {
                    //Console.WriteLine(item.content.data[0].name);
                }
            }
        }


        public void GetTaskActive()
        {
            RestClient GetTaskActiveClient = new RestClient("https://mobilelearn.chaoxing.com");

            for (int i = 0; i < CourseId.Count; i++)
            {
                var request = new RestRequest("ppt/activeAPI/taskactivelist");
                request.AddHeader("Cookie", _Cookie);
                request.AddParameter("courseId", CourseId[i].ToString());
                request.AddParameter("classId", ClassId[i].ToString());
                request.AddParameter("uid", UID);

                var response = GetTaskActiveClient.Get(request);
                if (response.StatusCode != System.Net.HttpStatusCode.OK)
                    throw new Exception("非200状态响应");

                var taskObject = JsonConvert.DeserializeObject<TaskObject.RootObject>(response.Content);
                if (taskObject.result != true)
                    throw new Exception("获取任务失败");

                Console.WriteLine(CourseName[i] + " 获取到 " + taskObject.count + " 个任务:");
                Console.WriteLine(taskObject.groupList[0].name + ", " + taskObject.groupList[1].name + "\r\n");
                //Console.WriteLine(response.Content);


                for (int t = 0; t < taskObject.activeList.Count; t++)
                {
                    if (taskObject.activeList[t].activeType.Equals("2"))
                    {
                        ulong time = 0;
                        ulong.TryParse(taskObject.activeList[t].startTime, out time);
                        Console.WriteLine(CourseName[i] + "的签到任务: " + Functions.TimestampToDateTime(time).ToString() + " " + taskObject.activeList[t].id);

                        Sign(taskObject.activeList[t].id);
                    }
                }
            }
        }

        public void Sign(Int64 id)
        {
            RestClient SignClient = new RestClient("https://mobilelearn.chaoxing.com");
            var request = new RestRequest("pptSign/stuSignajax");
            request.AddParameter("name", Name);

        }

    }
}
