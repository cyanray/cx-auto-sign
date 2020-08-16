using System;
using System.Collections.Generic;
using System.Text;

namespace CxSignHelper.Utils
{
    public partial class Functions
    {
        public static DateTime TimestampToDateTime(ulong str)
        {
            DateTime result;
            // TODO: Optimaze
            if (str.ToString().Length == 13)
            {
                result = new DateTime(1970, 1, 1, 8, 0, 0).AddMilliseconds(str);
            }
            else
            {
                result = new DateTime(1970, 1, 1, 8, 0, 0).AddSeconds(str);
            }
            return result;
        }
    }
}
