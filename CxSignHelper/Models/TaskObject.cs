using System;
using System.Collections.Generic;

namespace CxSignHelper
{
    internal class TaskObject
    {
        public class GroupList
        {
            public string classId { get; set; }
            public string content { get; set; }
            public string courseId { get; set; }
            public string createTime { get; set; }
            public string fid { get; set; }
            public int id { get; set; }
            public int isDelete { get; set; }
            public string name { get; set; }
            public int sort { get; set; }
            public int type { get; set; }
            public string uid { get; set; }
            public string updateTime { get; set; }
        }

        public class ActiveList
        {
            public string nameTwo { get; set; }
            public int groupId { get; set; }
            public int isLook { get; set; }
            public int releaseNum { get; set; }
            public string url { get; set; }
            public string picUrl { get; set; }
            public int attendNum { get; set; }
            public string activeType { get; set; }
            public string nameOne { get; set; }
            public string startTime { get; set; }
            public Int64 id { get; set; }
            public bool status { get; set; }
            public string nameFour { get; set; }
        }

        public class RootObject
        {
            public List<GroupList> groupList { get; set; }
            public List<ActiveList> activeList { get; set; }
            public int count { get; set; }
            public bool status { get; set; }
            public bool result { get; set; }
        }
    }



}
