using System;
using System.Collections.Generic;

namespace CxSignHelper.Models
{
    internal class SubjectObject
    {
        public class Data
        {
            public Int64 id { get; set; }
            public string name { get; set; }
        }

        public class Course
        {
            public List<Data> data { get; set; }
        }

        public class Content
        {
            public Int64 id { get; set; }
            public bool isstart { get; set; }
            public bool isretire { get; set; }
            public Course course { get; set; }
        }

        public class ChannelList
        {
            public Content content { get; set; }
        }

        public class RootObject
        {
            public bool result { get; set; }
            public List<ChannelList> channelList { get; set; }
        }
    }

}
