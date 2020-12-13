using Newtonsoft.Json.Linq;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace cx_auto_sign
{
    public static class ServerChan
    {
        public static async Task SendAsync(string ScKey, string text, string desp = null)
        {
            string url = $"https://sc.ftqq.com/{ScKey}.send?text={text}";
            RestClient LoginClient = new RestClient(url);
            var request = new RestRequest(Method.POST);
            request.AddHeader("Content-Type", "application/x-www-form-urlencoded");
            if (!string.IsNullOrEmpty(desp))
                request.AddParameter("desp", desp);

            var response = await LoginClient.ExecutePostAsync(request);
            if (response.StatusCode != HttpStatusCode.OK)
                throw new Exception("非200状态响应");
            var respJson = JObject.Parse(response.Content);
            if(respJson["errno"].ToObject<int>() != 0)
            {
                throw new Exception(respJson["errmsg"].ToString());
            }
        }
    }
}
